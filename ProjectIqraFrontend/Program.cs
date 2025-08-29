using HarmonyLib;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Frontend;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.KnowledgeBase.Retrieval;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Mail;
using IqraInfrastructure.Managers.RAG.Extractors;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Processors;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Patches;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Billing;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Embedding;
using IqraInfrastructure.Repositories.Embedding.Cache;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.MinIO;
using IqraInfrastructure.Repositories.RAG;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Rerank;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MongoDB.Driver;
using ProjectIqraFrontend.Middlewares;
using System.Reflection;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            builder.Services.AddSingleton<ViewLinkConfiguration>((sp) =>
            {
                var baseMinioUrl = appConfig["MinioStorage:PublicEndpoint"];
                var minioUrlIsSecure = bool.Parse(appConfig["MinioStorage:IsPublicEndpointSecure"]) ? "https://" : "http://";
                baseMinioUrl = minioUrlIsSecure + baseMinioUrl;

                return new ViewLinkConfiguration()
                {
                    BusinessLogoURL = baseMinioUrl + "/" + appConfig["MinioStorage:BusinessLogoRepositoryBucketName"],
                    BusinessToolAudioURL = baseMinioUrl + "/" + appConfig["MinioStorage:BusinessToolAudioRepositoryBucketName"],
                    IntegrationLogoURL = baseMinioUrl + "/" + appConfig["MinioStorage:IntegrationsLogoRepositoryBucketName"],
                    BusinessAgentBackgroundAudioURL = baseMinioUrl + "/" + appConfig["MinioStorage:BusinessAgentAudioRepositoryBucketName"]
                };
            });

            // Repositories
            SetupRepositories(builder, appConfig);

            // Managers
            SetupManagers(builder, appConfig);

            // Patches
            IronPatcher.Apply();

            // HTTP Client
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
            // MilvusClient
            builder.Services.AddHttpClient("MilvusClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("ProxyForwarder").SetHandlerLifetime(TimeSpan.FromMinutes(5));
            // UnstructuredClient
            builder.Services.AddHttpClient("UnstructuredClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(200);
                client.BaseAddress = new Uri(appConfig["Unstructured:EndPoint"]);
                client.DefaultRequestHeaders.Add("unstructured-api-key", appConfig["Unstructured:ApiKey"]);
            });

            // JSON Middleware
            var customJSONMiddleware = new EndpointAwareJsonConverter();
            builder.Services
                .AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        customJSONMiddleware
                    );
                });

            var app = builder.Build();

            // Initalize All Singleton Services
            InitializeAllSingletonServices(app.Services);

            // SetupDependencies
            SetupDependencies(app.Services);

            // Run background tasks > will be moved to IqraBackgroundProcessor in the future
            var knowledgeBaseCollectionsLoadManager = app.Services.GetRequiredService<KnowledgeBaseCollectionsLoadManager>();
            await knowledgeBaseCollectionsLoadManager.StartAsync(CancellationToken.None);

            // Assign the HttpContextAccessor to JSON Middleware
            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            customJSONMiddleware.SetHttpContextAccessor(httpContextAccessor);

            app.MapStaticAssets();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }

        private static void SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            // Build Base Services
            builder.Services.AddSingleton<IMongoClient>((sp) =>
            {
                return new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            });

            builder.Services.AddSingleton<MinioPrivatePublicClient>((sp) =>
            {
                return new MinioPrivatePublicClient(
                    appConfig["MinioStorage:PrivateEndpoint"],
                    int.Parse(appConfig["MinioStorage:PrivateEndpointPort"]),
                    bool.Parse(appConfig["MinioStorage:IsPrivateEndpointSecure"]),
                    appConfig["MinioStorage:PublicEndpoint"],
                    int.Parse(appConfig["MinioStorage:PublicEndpointPort"]),
                    bool.Parse(appConfig["MinioStorage:IsPublicEndpointSecure"]),
                    appConfig["MinioStorage:AccessKey"],
                    appConfig["MinioStorage:SecretKey"]
                );
            });

            builder.Services.AddSingleton<MilvusKnowledgeBaseClient>((sp) =>
            {
                return new MilvusKnowledgeBaseClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    new MilvusOptions() { 
                        Endpoint = appConfig["Milvus:Endpoint"],
                        Username = appConfig["Milvus:Username"],
                        Password = appConfig["Milvus:Password"],
                        ExpiryCheckIntervalSeconds = int.Parse(appConfig["Milvus:ExpiryCheckIntervalSeconds"]),
                        CollectionStaleTimeoutMinutes = int.Parse(appConfig["Milvus:CollectionStaleTimeoutMinutes"])
                    },
                    sp.GetRequiredService<ILogger<MilvusKnowledgeBaseClient>>()
                );
            });

            // Repositories

            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:AppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:LanguagesRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessRepository>((sp) =>
            {
                return new BusinessRepository(
                    sp.GetRequiredService<ILogger<BusinessRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessAppRepository>((sp) =>
            {
                return new BusinessAppRepository(
                    sp.GetRequiredService<ILogger<BusinessAppRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessAppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<RegionRepository>((sp) => {
                return new RegionRepository(
                    sp.GetRequiredService<ILogger<RegionRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:AppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:IntegrationsRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<UserRepository>((sp) =>
            {
                return new UserRepository(
                    sp.GetRequiredService<ILogger<UserRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:UserRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessWhiteLabelDomainRepository>((sp) =>
            {
                return new BusinessWhiteLabelDomainRepository(
                    sp.GetRequiredService<ILogger<BusinessWhiteLabelDomainRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessWhiteLabelDomainRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<LLMProviderRepository>((sp) =>
            {
                return new LLMProviderRepository(
                    sp.GetRequiredService<ILogger<LLMProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:LLMProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<STTProviderRepository>((sp) =>
            {
                return new STTProviderRepository(
                    sp.GetRequiredService<ILogger<STTProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:STTProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<TTSProviderRepository>((sp) =>
            {
                return new TTSProviderRepository(
                    sp.GetRequiredService<ILogger<TTSProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:TTSProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<EmbeddingProviderRepository>((sp) =>
            {
                return new EmbeddingProviderRepository(
                    sp.GetRequiredService<ILogger<EmbeddingProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:EmbeddingProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<RerankProviderRepository>((sp) =>
            {
                return new RerankProviderRepository(
                    sp.GetRequiredService<ILogger<RerankProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:RerankProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<InboundCallQueueRepository>((sp) =>
            {
                return new InboundCallQueueRepository(
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<ConversationStateRepository>((sp) =>
            {
                return new ConversationStateRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ConversationStateRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueRepository>((sp) =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallCampaignRepository>((sp) =>
            {
                return new OutboundCallCampaignRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:OutboundCallCampaignRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallCampaignRepository>>()
                );
            });

            builder.Services.AddSingleton<PlanRepository>((sp) =>
            {
                return new PlanRepository(
                    sp.GetRequiredService<ILogger<PlanRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:PlanRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<UserSessionRepository>((sp) =>
            {
                return new UserSessionRepository(
                    sp.GetRequiredService<ILogger<UserSessionRepository>>(),
                    new RedisConnectionFactory(
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:UserSessionDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });

            builder.Services.AddSingleton<IntegrationsLogoRepository>((sp) =>
            {
                return new IntegrationsLogoRepository(
                    sp.GetRequiredService<ILogger<IntegrationsLogoRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:IntegrationsLogoRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessLogoRepository>((sp) =>
            {
                return new BusinessLogoRepository(
                    sp.GetRequiredService<ILogger<BusinessLogoRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:BusinessLogoRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:BusinessToolAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:BusinessAgentAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<ConversationAudioRepository>((sp) =>
            {
                return new ConversationAudioRepository(
                    sp.GetRequiredService<ILogger<ConversationAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:ConversationAudioRepositoryBucketName"]
                );
            });


            builder.Services.AddSingleton<BusinessDomainVestaCPRepository>((sp) =>
            {
                return new BusinessDomainVestaCPRepository(
                    sp.GetRequiredService<ILogger<BusinessDomainVestaCPRepository>>(),
                    appConfig["BusinessDomainHostingRepository:Hostname"],
                    appConfig["BusinessDomainHostingRepository:AdminUsername"],
                    appConfig["BusinessDomainHostingRepository:BusinessesUsername"],
                    appConfig["BusinessDomainHostingRepository:AdminPassword"],
                    appConfig["BusinessDomainHostingRepository:DomainIP"],
                    appConfig["BusinessDomainHostingRepository:IqraBusinessDomain"],
                    appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Endpoint"],
                    appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Username"],
                    appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Password"],
                    sp.GetRequiredService<AppRepository>()
                );
            });

            builder.Services.AddSingleton<ConversationUsageRepository>((sp) =>
            {
                return new ConversationUsageRepository(
                    sp.GetRequiredService<ILogger<ConversationUsageRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ConversationUsageRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessKnowledgeBaseDocumentRepository>((sp) =>
            {
                return new BusinessKnowledgeBaseDocumentRepository(
                    sp.GetRequiredService<ILogger<BusinessKnowledgeBaseDocumentRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessKnowledgeBaseDocumentRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<KnowledgeBaseVectorRepository>((sp) =>
            {
                return new KnowledgeBaseVectorRepository(
                    sp.GetRequiredService<MilvusKnowledgeBaseClient>(),
                    appConfig["Milvus:Database"],
                    sp.GetRequiredService<ILogger<KnowledgeBaseVectorRepository>>()
                );
            });

            builder.Services.AddSingleton<RAGKeywordStore>((sp) =>
            {
                return new RAGKeywordStore(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:RAGKeywordStoreDatabaseName"],
                    sp.GetRequiredService<KeywordExtractor>(),
                    sp.GetRequiredService<ILogger<RAGKeywordStore>>()
                );
            });
            //EmbeddingCacheRepository
            builder.Services.AddSingleton<EmbeddingCacheRepository>((sp) =>
            {
                return new EmbeddingCacheRepository(
                    sp.GetRequiredService<ILogger<EmbeddingCacheRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:EmbeddingCacheRepositoryDatabaseName"]
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            builder.Services.AddSingleton<UserSessionValidationHelper>((sp) =>
            {
                return new UserSessionValidationHelper(
                    sp.GetRequiredService<UserManager>(),
                    sp.GetRequiredService<BusinessManager>()
                );
            });
            builder.Services.AddSingleton<EmailManager>((sp) =>
            {
                return new EmailManager(
                    sp.GetRequiredService<ILogger<EmailManager>>(),
                    new EmailSettings()
                    {
                        Host = appConfig["MailSMTP:Host"],
                        Port = int.Parse(appConfig["MailSMTP:Port"]),
                        Username = appConfig["MailSMTP:Username"],
                        Password = appConfig["MailSMTP:Password"],
                        FromEmail = appConfig["MailSMTP:FromEmail"],
                        FromName = appConfig["MailSMTP:FromName"]
                    }
                );
            });
            //TextSplitterFactory
            builder.Services.AddSingleton<TextSplitterFactory>((sp) =>
            {
                return new TextSplitterFactory();
            });
            builder.Services.AddSingleton<IndexProcessorFactory>((sp) =>
            {
                return new IndexProcessorFactory(
                    sp.GetRequiredService<TextSplitterFactory>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<KeywordExtractor>()
                );
            });
            builder.Services.AddSingleton<ExtractProcessor>((sp) =>
            {
                return new ExtractProcessor(
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<LanguagesManager>((sp) =>
            {
                return new LanguagesManager(
                    sp.GetRequiredService<ILogger<LanguagesManager>>(),
                    sp.GetRequiredService<LanguagesRepository>()
                );
            });
            builder.Services.AddSingleton<RegionManager>((sp) =>
            {
                return new RegionManager(
                    sp.GetRequiredService<ILogger<RegionManager>>(),
                    sp.GetRequiredService<RegionRepository>()
                );
            });
            builder.Services.AddSingleton<IntegrationsManager>((sp) =>
            {
                AES256EncryptionService integrationFieldsEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["Integrations:EncryptionKey"]
                );
                return new IntegrationsManager(
                    sp.GetRequiredService<ILogger<IntegrationsManager>>(),
                    sp.GetRequiredService<IntegrationsRepository>(),
                    sp.GetRequiredService<IntegrationsLogoRepository>(),
                    integrationFieldsEncryptionService
                );
            });
            builder.Services.AddSingleton<ModemTelManager>((sp) =>
            {
                return new ModemTelManager(
                    sp.GetRequiredService<ILogger<ModemTelManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<TwilioManager>((sp) =>
            {
                return new TwilioManager(
                    sp.GetRequiredService<ILogger<TwilioManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<UserManager>((sp) =>
            {
                return new UserManager(
                    sp.GetRequiredService<ILogger<UserManager>>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<UserSessionRepository>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<EmailManager>(),
                    sp.GetRequiredService<UserApiKeyProcessor>()
                );
            });
            builder.Services.AddSingleton<IntegrationConfigurationManager>((sp) =>
            {
                return new IntegrationConfigurationManager(
                    sp.GetRequiredService<STTProviderManager>(),
                    sp.GetRequiredService<TTSProviderManager>(),
                    sp.GetRequiredService<LLMProviderManager>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<RerankProviderManager>()
                );
            });
            builder.Services.AddSingleton<BusinessManager>((sp) =>
            {
                return new BusinessManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    new BusinessManagerInitalizationSettings()
                    { 
                        InitalizeAgentsManager = true,
                        InitalizeCacheManager = true,
                        InitalizeContextManager = true,
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeRoutesManager = true,
                        InitalizeSettingsManager = true,
                        InitalizeToolsManager = true,
                        InitalizeConversationsManager = true,
                        InitalizeMakeCallManager = true,
                        InitalizeKnowledgeBaseManager = true
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    sp.GetRequiredService<BusinessLogoRepository>(),
                    sp.GetRequiredService<BusinessWhiteLabelDomainRepository>(),
                    sp.GetRequiredService<BusinessDomainVestaCPRepository>(),
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<ConversationAudioRepository>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<OutboundCallCampaignRepository>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<TwilioManager>(),
                    sp.GetRequiredService<IntegrationConfigurationManager>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<IndexProcessorFactory>(),
                    sp.GetRequiredService<ExtractProcessor>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<KeywordExtractor>(),
                    sp.GetRequiredService<RAGKeywordStore>()
                );
            });
            builder.Services.AddSingleton<LLMProviderManager>((sp) =>
            {
                return new LLMProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<LLMProviderRepository>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<STTProviderManager>((sp) =>
            {
                return new STTProviderManager(
                    sp.GetRequiredService<ILogger<STTProviderManager>>(),
                    sp.GetRequiredService<STTProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<TTSProviderManager>((sp) =>
            {
                return new TTSProviderManager(
                    sp.GetRequiredService<ILogger<TTSProviderManager>>(),
                    sp.GetRequiredService<TTSProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<EmbeddingProviderManager>((sp) =>
            {
                return new EmbeddingProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<EmbeddingProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<RerankProviderManager>((sp) =>
            {
                return new RerankProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<RerankProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<PlanManager>((sp) =>
            {
                return new PlanManager(
                    sp.GetRequiredService<ILogger<PlanManager>>(),
                    sp.GetRequiredService<PlanRepository>()
                );
            });
            builder.Services.AddSingleton<UserUsageManager>((sp) =>
            {
                return new UserUsageManager(
                    sp.GetRequiredService<ILogger<UserUsageManager>>(),
                    sp.GetRequiredService<ConversationUsageRepository>()
                );
            });
            builder.Services.AddSingleton<BillingValidationManager>((sp) =>
            {
                return new BillingValidationManager(
                    sp.GetRequiredService<ILogger<BillingValidationManager>>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<UserManager>(),
                    sp.GetRequiredService<PlanManager>(),
                    sp.GetRequiredService<ConversationStateRepository>()
                );
            });
            builder.Services.AddSingleton<UserApiKeyManager>((sp) =>
            {
                return new UserApiKeyManager(
                    sp.GetRequiredService<ILogger<UserApiKeyManager>>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<UserApiKeyProcessor>()
                );
            });
            builder.Services.AddSingleton<UserApiKeyProcessor>((sp) =>
            {
                AES256EncryptionService userApiKeyEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["UserApiKeys:ApiKeyEncryptionKey"]
                );
                AES256EncryptionService userApiKeyPayloadEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["UserApiKeys:PayloadEncryptionKey"]
                );
                return new UserApiKeyProcessor(
                    appConfig["User:EmailHashPepper"],
                    userApiKeyEncryptionService,
                    userApiKeyPayloadEncryptionService
                );
            });
            builder.Services.AddSingleton<KnowledgeBaseRetrievalManagerFactory>((sp) =>
            {
                return new KnowledgeBaseRetrievalManagerFactory(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<RerankProviderManager>(),
                    sp.GetRequiredService<KnowledgeBaseCollectionsLoadManager>(),
                    sp.GetRequiredService<EmbeddingCacheManager>()
                );
            });
            builder.Services.AddSingleton<KeywordExtractor>((sp) =>
            {
                // TODO load the keywords from a .json file
                return new KeywordExtractor();
            });
            builder.Services.AddSingleton<KnowledgeBaseCollectionsLoadManager>((sp) =>
            {
                return new KnowledgeBaseCollectionsLoadManager(
                    sp.GetRequiredService<ILogger<KnowledgeBaseCollectionsLoadManager>>(),
                    sp.GetRequiredService<MilvusKnowledgeBaseClient>(),
                    appConfig["Milvus:Database"],
                    new RedisConnectionFactory(
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:RAGCollectionsLoadedDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });
            builder.Services.AddSingleton<EmbeddingCacheManager>((sp) =>
            {
                return new EmbeddingCacheManager(
                    sp.GetRequiredService<ILogger<EmbeddingCacheManager>>(),
                    sp.GetRequiredService<EmbeddingCacheRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>()
                );
            });
        }

        private static void SetupDependencies(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IntegrationConfigurationManager>().SetupDependencies(
                serviceProvider.GetRequiredService<BusinessManager>().GetIntegrationsManager()
            );
        }

        private static void InitializeAllSingletonServices(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Initializing all singleton services from IqraInfrastructure namespace...");

            // Get service descriptors from the service collection
            var services = GetTypes(serviceProvider)
                .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton &&
                       descriptor.ServiceType.Namespace != null &&
                       descriptor.ServiceType.Namespace.StartsWith("IqraInfrastructure"))
                .ToList();

            logger.LogInformation($"Found {services.Count} singleton services to initialize");

            foreach (var service in services)
            {
                logger.LogInformation($"Initializing service: {service.ServiceType.Name}");
                serviceProvider.GetService(service.ServiceType);
            }

            logger.LogInformation("All IqraInfrastructure singleton services initialized successfully");
        }

        private static List<ServiceDescriptor> GetTypes(IServiceProvider provider)
        {
            ServiceProvider serviceProvider = provider as ServiceProvider;
            var callSiteFactory = serviceProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serviceProvider);
            var serviceDescriptors = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(callSiteFactory) as ServiceDescriptor[];
            return serviceDescriptors.ToList();
        }





        [HarmonyPatch("IronPdf.License", "IsLicensed", MethodType.Getter)]
        public class IsLicensed_Patch
        {
            // A 'Prefix' patch runs *before* the original method's code.
            [HarmonyPrefix]
            public static bool ForceLicensedValue(ref bool __result)
            {
                // '__result' is a special Harmony parameter that lets us modify the return value.
                __result = true;

                // By returning 'false', we tell Harmony to SKIP the original method entirely.
                // This is crucial for overriding its logic.
                return false;
            }
        }

        // --- Patch for the IronPdf.License.LicenseKey Property ---
        // This is a separate patch for the second property you wanted to modify.
        [HarmonyPatch("IronPdf.License", "LicenseKey", MethodType.Getter)]
        public class LicenseKey_Patch
        {
            [HarmonyPrefix]
            public static bool ForceLicenseKeyValue(ref string __result)
            {
                // Set the return value to the specific key from your original function.
                __result = "IRONSUITE.TAUSHIF1TEZA.GMAIL.COM.9218-C4C9C0925C-CZRWKKOVBNHWGS-CWR7KUDVDQLI-GCXHX77TEXD5-VJKK7LKZEBJ3-UXXYNFUFTWNI-FSMG77GWWVGP-7W4CTQ-TAZT6SLDBOOLUA-DEPLOYMENT.TRIAL-47I2VM.TRIAL.EXPIRES.09.FEB.2024";

                // Again, return false to ensure the original code for getting the license key is never executed.
                return false;
            }
        }
    }
}
