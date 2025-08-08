using Google.Api;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Frontend;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Mail;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Billing;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
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
                var baseMinioUrl = appConfig["MinioStorage:PublicURL"];
                var minioUrlIsSecure = bool.Parse(appConfig["MinioStorage:PublicUrlIsSecure"]) ? "https://" : "http://";
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

            // HTTP Client
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
            builder.Services.AddHttpClient("ProxyForwarder").SetHandlerLifetime(TimeSpan.FromMinutes(5));

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

            builder.Services.AddSingleton<IMinioClient>((sp) =>
            {
                return new MinioClient()
                    .WithEndpoint(appConfig["MinioStorage:Endpoint"], int.Parse(appConfig["MinioStorage:Port"]))
                    .WithCredentials(appConfig["MinioStorage:AccessKey"], appConfig["MinioStorage:SecretKey"])
                    .WithSSL(bool.Parse(appConfig["MinioStorage:IsSecure"]))
                    .Build();
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
                    sp.GetRequiredService<IMinioClient>(),
                    appConfig["MinioStorage:IntegrationsLogoRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessLogoRepository>((sp) =>
            {
                return new BusinessLogoRepository(
                    sp.GetRequiredService<ILogger<BusinessLogoRepository>>(),
                    sp.GetRequiredService<IMinioClient>(),
                    appConfig["MinioStorage:BusinessLogoRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    sp.GetRequiredService<IMinioClient>(),
                    appConfig["MinioStorage:BusinessToolAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    sp.GetRequiredService<IMinioClient>(),
                    appConfig["MinioStorage:BusinessAgentAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<ConversationAudioRepository>((sp) =>
            {
                return new ConversationAudioRepository(
                    sp.GetRequiredService<ILogger<ConversationAudioRepository>>(),
                    sp.GetRequiredService<IMinioClient>(),
                    appConfig["MinioStorage:ConversationAudioRepositoryBucketName"],
                    appConfig["MinioStorage:PublicURL"],
                    bool.Parse(appConfig["MinioStorage:PublicUrlIsSecure"])
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
                    sp.GetRequiredService<LLMProviderManager>()
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
                    sp.GetRequiredService<IntegrationConfigurationManager>()
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
    }
}
