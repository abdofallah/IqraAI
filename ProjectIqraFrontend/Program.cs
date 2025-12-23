using HarmonyLib;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Modules;
using IqraCore.Interfaces.User;
using IqraCore.Interfaces.Validation;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Helpers.Conventions;
using IqraInfrastructure.Helpers.Providers;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Helpers.Validation;
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
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Patches;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Embedding;
using IqraInfrastructure.Repositories.Embedding.Cache;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.RAG;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Rerank;
using IqraInfrastructure.Repositories.S3Storage;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WebSession;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using MongoDB.Driver;
using ProjectIqraFrontend.Middlewares;
using ProjectIqraFrontend.Transformer;
using Scalar.AspNetCore;
using System.Net;
using System.Reflection;

namespace ProjectIqraFrontend
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudFrontendAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            var frontendAppConfig = new FrontendAppConfig()
            {
                DefaultS3StorageRegionId = appConfig["S3Storage:DefaultStorageRegionId"],
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
            };
            builder.Services.AddSingleton<FrontendAppConfig>(frontendAppConfig);
            builder.Services.AddScoped<WhiteLabelContext>();

            // Load Cloud Asembly
            if (frontendAppConfig.IsCloudVersion)
            {
                LoadCloudAssembly();
            }
            // Load Cloud Configuration
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupConfiguration(builder.Services, appConfig);
            }

            // Repositories
            await SetupRepositories(builder, appConfig, frontendAppConfig);
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupRepositories(builder.Services, appConfig);
            }

            // Managers
            SetupManagers(builder, appConfig, frontendAppConfig);
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupManagers(builder.Services, appConfig);
            }

            // Patches
            IronPatcher.Apply();

            // HTTP Client
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("AmwalPay");
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
            builder.Services.AddHttpClient("ProxyForwarder");
            builder.Services.AddHttpClient("WebSessionForwardClient");
            // UnstructuredClient
            builder.Services.AddHttpClient("UnstructuredClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(200);
                client.BaseAddress = new Uri(appConfig["Unstructured:EndPoint"]);
                client.DefaultRequestHeaders.Add("unstructured-api-key", appConfig["Unstructured:ApiKey"]);
            });

            // Controllers with custom Middleware
            var customJSONMiddleware = new EndpointAwareJsonConverter();
            LoadCloudVsOpensourceControllers(builder, customJSONMiddleware, frontendAppConfig);

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(p => p
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            // Configure Forwarded Headers
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                
                // Known Proxies
                options.KnownProxies.Clear();
                var knownProxies = appConfig.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
                if (knownProxies != null)
                {
                    foreach (var proxy in knownProxies)
                    {
                        if (IPAddress.TryParse(proxy, out var ipAddress))
                        {
                            options.KnownProxies.Add(ipAddress);
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid known proxy: {proxy}. Unable to parse ip address.");
                        }
                    }
                }
            });

            // OpenAPI
            builder.Services.AddTransient<OpenApiDocumentTransformer>();
            builder.Services.AddTransient<OpenApiEnumSchemaTransformer>();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<OpenApiDocumentTransformer>();
                options.AddSchemaTransformer<OpenApiEnumSchemaTransformer>();
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

            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.ConfigureStaticFiles(app.Environment);
            }

            app.UseForwardedHeaders();
            app.UseRouting();

            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.UseWhiteLabelResolver(app);
            }

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapOpenApi();
            app.MapScalarApiReference("/api", options =>
            {
                options.WithTitle("Iqra AI API");
                options.WithTheme(ScalarTheme.Saturn);
                options.EnableDarkMode();
            });

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }

        private static async Task SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig, FrontendAppConfig frontendAppConfig)
        {
            // Build Base Services
            IMongoClient mongoClient = new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            RegionRepository regionRepository = new RegionRepository(mongoClient, appConfig["MongoDatabase:AppRepositoryDatabaseName"]);
            var allRegionServers = await regionRepository.GetRegions();
            S3StorageClientFactory s3StorageClientFactory = new S3StorageClientFactory(frontendAppConfig.DefaultS3StorageRegionId);
            var s3StorageInitResult = await s3StorageClientFactory.Initalize(allRegionServers);
            if (!s3StorageInitResult.Success)
            {
                throw new Exception($"[{s3StorageInitResult.Code}] {s3StorageInitResult.Message}");
            }

            builder.Services.AddSingleton<IMongoClient>(mongoClient);
            builder.Services.AddSingleton<S3StorageClientFactory>(s3StorageClientFactory);
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
                    mongoClient,
                    appConfig["MongoDatabase:AppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<RegionRepository>((sp) => {
                regionRepository.SetLogger(sp.GetRequiredService<ILogger<RegionRepository>>());
                return regionRepository;
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

            builder.Services.AddSingleton<CallQueueLogsRepository>((sp) =>
            {
                return new CallQueueLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<CallQueueLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<InboundCallQueueRepository>((sp) =>
            {
                return new InboundCallQueueRepository(
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"],
                    sp.GetRequiredService<CallQueueLogsRepository>()
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

            builder.Services.AddSingleton<ConversationStateLogsRepository>(sp =>
            {
                return new ConversationStateLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ConversationStateRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueRepository>((sp) =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueGroupRepository>((sp) =>
            {
                return new OutboundCallQueueGroupRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:OutboundCallCampaignRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueGroupRepository>>()
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
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessLogoRepository>((sp) =>
            {
                return new BusinessLogoRepository(
                    sp.GetRequiredService<ILogger<BusinessLogoRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessConversationAudioRepository>((sp) =>
            {
                return new BusinessConversationAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessConversationAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<UserUsageRepository>((sp) =>
            {
                return new UserUsageRepository(
                    sp.GetRequiredService<ILogger<UserUsageRepository>>(),
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

            builder.Services.AddSingleton<EmbeddingCacheRepository>((sp) =>
            {
                return new EmbeddingCacheRepository(
                    sp.GetRequiredService<ILogger<EmbeddingCacheRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:EmbeddingCacheRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<WebSessionRepository>((sp) =>
            {
                return new WebSessionRepository(
                    sp.GetRequiredService<ILogger<WebSessionRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:WebSessionRepoistoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<ServerLiveStatusChannelRepository>((sp) =>
            {
                return new ServerLiveStatusChannelRepository(
                    new RedisConnectionFactory(
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:ServerLiveStatusChannelDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<ServerLiveStatusChannelRepository>>()
                );
            });

            builder.Services.AddSingleton<DistributedLockRepository>((sp) =>
            {
                return new DistributedLockRepository(
                    new RedisConnectionFactory(
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:DistributedLockDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<DistributedLockRepository>>()
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, FrontendAppConfig frontendAppConfig)
        {
            builder.Services.AddSingleton<IUserBusinessPermissionHelper, UserBusinessPermissionHelper>((sp) =>
            {
                return new UserBusinessPermissionHelper();
            });
            if (!frontendAppConfig.IsCloudVersion)
            {
                builder.Services.AddScoped<IUserRegistrationManager, UserRegistrationManager>((sp) =>
                {
                    return new UserRegistrationManager(
                        sp.GetRequiredService<UserApiKeyProcessor>(),
                        sp.GetRequiredService<UserRepository>()
                    );
                });

                builder.Services.AddSingleton<ISessionValidationAndPermissionHelper, SessionValidationAndPermissionHelper>((sp) =>
                {
                    return new SessionValidationAndPermissionHelper(
                        sp.GetRequiredService<UserApiKeyManager>(),
                        sp.GetRequiredService<UserManager>(),
                        sp.GetRequiredService<BusinessManager>(),
                        sp.GetRequiredService<UserRepository>(),
                        sp.GetRequiredService<IUserBusinessPermissionHelper>()
                    );
                });

                builder.Services.AddSingleton<IUserUsageValidationManager, UserUsageValidationManager>((sp) =>
                {
                    return new UserUsageValidationManager();
                });
            }
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
                var regionManager = new RegionManager(
                    sp.GetRequiredService<RegionRepository>()
                );

                regionManager.SetLogger(sp.GetRequiredService<ILogger<RegionManager>>());

                return regionManager;
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
                    integrationFieldsEncryptionService,
                    sp.GetRequiredService<S3StorageClientFactory>()
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
                    sp.GetRequiredService<IMongoClient>(),
                    new BusinessManagerInitalizationSettings()
                    { 
                        InitalizeAgentsManager = true,
                        InitalizeScriptsManager = true,
                        InitalizeCacheManager = true,
                        InitalizeContextManager = true,
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeRoutesManager = true,
                        InitalizeSettingsManager = true,
                        InitalizeToolsManager = true,
                        InitalizeConversationsManager = true,
                        InitalizeMakeCallManager = true,
                        InitalizeKnowledgeBaseManager = true,
                        InitalizeCampaignManager = true,
                        InitalizeWebSessionManager = true,
                        InitalizePostAnalysisManager = true,
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    sp.GetRequiredService<BusinessLogoRepository>(),
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<BusinessConversationAudioRepository>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<OutboundCallQueueGroupRepository>(),
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
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<WebSessionRepository>(),
                    sp.GetRequiredService<IUserUsageValidationManager>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
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
            
            builder.Services.AddSingleton<UserUsageManager>((sp) =>
            {
                return new UserUsageManager(
                    sp.GetRequiredService<ILogger<UserUsageManager>>(),
                    sp.GetRequiredService<UserUsageRepository>()
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

            builder.Services.AddSingleton<ServerSelectionManager>((sp) =>
            {
                return new ServerSelectionManager(
                    sp.GetRequiredService<ILogger<ServerSelectionManager>>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<DistributedLockRepository>()
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

        private static void LoadCloudAssembly()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string cloudDllPath = Path.Combine(folder, "ProjectIqraFrontend.Cloud.dll");
            if (!File.Exists(cloudDllPath)) throw new Exception("Cloud DLL missing");

            _cloudAssembly = Assembly.LoadFrom(cloudDllPath);
            var type = _cloudAssembly.GetTypes().FirstOrDefault(t => typeof(ICloudFrontendAppInitalizer).IsAssignableFrom(t) && !t.IsInterface);
            if (type != null)
            {
                _cloudModule = (ICloudFrontendAppInitalizer)Activator.CreateInstance(type);
            }
            if (_cloudModule == null) throw new Exception("Cloud module not found");
        }

        private static void LoadCloudVsOpensourceControllers(WebApplicationBuilder builder, EndpointAwareJsonConverter customJSONMiddleware, FrontendAppConfig appConfig)
        {
            Action<MvcOptions> configureMvc = options =>
            {
                options.Conventions.Add(new CloudAwareActionConvention(appConfig.IsCloudVersion));
            };

            var mvcBuilder = builder.Services
                .AddControllersWithViews(configureMvc)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(customJSONMiddleware);
                })
                .ConfigureApplicationPartManager(manager =>
                {
                    var defaultProvider = manager.FeatureProviders.OfType<ControllerFeatureProvider>().FirstOrDefault();
                    if (defaultProvider != null)
                    {
                        manager.FeatureProviders.Remove(defaultProvider);
                    }

                    manager.FeatureProviders.Add(new CloudAwareControllerFeatureProvider(appConfig.IsCloudVersion));
                });

            if (!appConfig.IsCloudVersion) return;

            try
            {
                mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(_cloudAssembly!));
                Console.WriteLine("Successfully loaded Cloud Controllers.");

                mvcBuilder.PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(_cloudAssembly!));
                Console.WriteLine("Successfully loaded Cloud Views.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Cloud Controllers: {ex.Message}", ex);
            }
        }

        private static List<ServiceDescriptor> GetTypes(IServiceProvider provider)
        {
            ServiceProvider serviceProvider = provider as ServiceProvider;
            var callSiteFactory = serviceProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serviceProvider);
            var serviceDescriptors = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(callSiteFactory) as ServiceDescriptor[];
            return serviceDescriptors.ToList();
        }

        // --- Patch for the IronPdf.License.LicenseKey Property ---
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
