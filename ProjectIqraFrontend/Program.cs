using IqraCore.Entities.Frontend;
using IqraCore.Utilities;
using ProjectIqraFrontend.Middlewares;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Redis;
using System.Reflection;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Configuration
            var appConfig = builder.Configuration;
            builder.Services.AddSingleton<ViewLinkConfiguration>((sp) =>
            {
                return new ViewLinkConfiguration()
                {
                    BusinessLogoURL = appConfig["BusinessLogoRepository:PublicURL"] + "/" + appConfig["BusinessLogoRepository:BucketName"],
                    BusinessToolAudioURL = appConfig["BusinessToolAudioRepository:PublicURL"] + "/" + appConfig["BusinessToolAudioRepository:BucketName"],
                    IntegrationLogoURL = appConfig["IntegrationsLogoRepository:PublicURL"] + "/" + appConfig["IntegrationsLogoRepository:BucketName"],
                    BusinessAgentBackgroundAudioURL = appConfig["BusinessAgentAudioRepository:PublicURL"] + "/" + appConfig["BusinessAgentAudioRepository:BucketName"]
                };
            });

            // Repositories
            SetupRepositories(builder, appConfig);

            // Managers
            SetupManagers(builder, appConfig);

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

            // Assign the HttpContextAccessor to JSON Middleware
            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            customJSONMiddleware.SetHttpContextAccessor(httpContextAccessor);

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    appConfig["AppDatabase:ConnectionString"],
                    appConfig["AppDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
                    appConfig["LanguagesDatabase:ConnectionString"],
                    appConfig["LanguagesDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessRepository>((sp) =>
            {
                return new BusinessRepository(
                    sp.GetRequiredService<ILogger<BusinessRepository>>(),
                    appConfig["BusinessDatabase:ConnectionString"],
                    appConfig["BusinessDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessAppRepository>((sp) =>
            {
                return new BusinessAppRepository(
                    sp.GetRequiredService<ILogger<BusinessAppRepository>>(),
                    appConfig["BusinessAppDatabase:ConnectionString"],
                    appConfig["BusinessAppDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<RegionRepository>((sp) => {
                return new RegionRepository(
                    sp.GetRequiredService<ILogger<RegionRepository>>(),
                    appConfig["AppDatabase:ConnectionString"],
                    appConfig["AppDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
                    appConfig["IntegrationsDatabase:ConnectionString"],
                    appConfig["IntegrationsDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<IntegrationsLogoRepository>((sp) =>
            {
                return new IntegrationsLogoRepository(
                    sp.GetRequiredService<ILogger<IntegrationsLogoRepository>>(),
                    appConfig["IntegrationsLogoRepository:Endpoint"],
                    int.Parse(appConfig["IntegrationsLogoRepository:Port"]),
                    appConfig["IntegrationsLogoRepository:AccessKey"],
                    appConfig["IntegrationsLogoRepository:SecretKey"],
                    appConfig["IntegrationsLogoRepository:BucketName"],
                    bool.Parse(appConfig["IntegrationsLogoRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<UserSessionRepository>((sp) =>
            {
                return new UserSessionRepository(
                    sp.GetRequiredService<ILogger<UserSessionRepository>>(),
                    new RedisConnectionFactory(
                        appConfig["UserSessionDatabase:ConnectionString"],
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });
            builder.Services.AddSingleton<UserRepository>((sp) =>
            {
                return new UserRepository(
                    sp.GetRequiredService<ILogger<UserRepository>>(),
                    appConfig["UserDatabase:ConnectionString"],
                    appConfig["UserDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessLogoRepository>((sp) =>
            {
                return new BusinessLogoRepository(
                    sp.GetRequiredService<ILogger<BusinessLogoRepository>>(),
                    appConfig["BusinessLogoRepository:Endpoint"],
                    int.Parse(appConfig["BusinessLogoRepository:Port"]),
                    appConfig["BusinessLogoRepository:AccessKey"],
                    appConfig["BusinessLogoRepository:SecretKey"],
                    appConfig["BusinessLogoRepository:BucketName"],
                    bool.Parse(appConfig["BusinessLogoRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<BusinessWhiteLabelDomainRepository>((sp) =>
            {
                return new BusinessWhiteLabelDomainRepository(
                    sp.GetRequiredService<ILogger<BusinessWhiteLabelDomainRepository>>(),
                    appConfig["BusinessWhiteLabelDomainRepository:ConnectionString"],
                    appConfig["BusinessWhiteLabelDomainRepository:DatabaseName"]
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
            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    appConfig["BusinessToolAudioRepository:Endpoint"],
                    int.Parse(appConfig["BusinessToolAudioRepository:Port"]),
                    appConfig["BusinessToolAudioRepository:AccessKey"],
                    appConfig["BusinessToolAudioRepository:SecretKey"],
                    appConfig["BusinessToolAudioRepository:BucketName"],
                    bool.Parse(appConfig["BusinessToolAudioRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    appConfig["BusinessAgentAudioRepository:Endpoint"],
                    int.Parse(appConfig["BusinessAgentAudioRepository:Port"]),
                    appConfig["BusinessAgentAudioRepository:AccessKey"],
                    appConfig["BusinessAgentAudioRepository:SecretKey"],
                    appConfig["BusinessAgentAudioRepository:BucketName"],
                    bool.Parse(appConfig["BusinessAgentAudioRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<LLMProviderRepository>((sp) =>
            {
                return new LLMProviderRepository(
                    sp.GetRequiredService<ILogger<LLMProviderRepository>>(),
                    appConfig["LLMProviderDatabase:ConnectionString"],
                    appConfig["LLMProviderDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<STTProviderRepository>((sp) =>
            {
                return new STTProviderRepository(
                    sp.GetRequiredService<ILogger<STTProviderRepository>>(),
                    appConfig["STTProviderDatabase:ConnectionString"],
                    appConfig["STTProviderDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<TTSProviderRepository>((sp) =>
            {
                return new TTSProviderRepository(
                    sp.GetRequiredService<ILogger<TTSProviderRepository>>(),
                    appConfig["TTSProviderDatabase:ConnectionString"],
                    appConfig["TTSProviderDatabase:DatabaseName"]
                );
            });
        }
    
        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig)
        {
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
                AES256EncryptionService integrationFieldsEncryptionService = new AES256EncryptionService(appConfig["Integrations:EncryptionKey"]);
                return new IntegrationsManager(
                    sp.GetRequiredService<ILogger<IntegrationsManager>>(),
                    sp.GetRequiredService<IntegrationsRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
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
            builder.Services.AddSingleton<UserManager>((sp) =>
            {
                return new UserManager(
                    sp.GetRequiredService<ILogger<UserManager>>(),
                    sp.GetRequiredService<UserSessionRepository>(),
                    sp.GetRequiredService<UserRepository>()
                );
            });
            builder.Services.AddSingleton<BusinessManager>((sp) =>
            {
                return new BusinessManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    sp.GetRequiredService<BusinessLogoRepository>(),
                    sp.GetRequiredService<BusinessWhiteLabelDomainRepository>(),
                    sp.GetRequiredService<BusinessDomainVestaCPRepository>(),
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<LLMProviderManager>((sp) =>
            {
                return new LLMProviderManager(
                    sp.GetRequiredService<ILogger<LLMProviderManager>>(),
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
                try
                {
                    logger.LogInformation($"Initializing service: {service.ServiceType.FullName}");
                    serviceProvider.GetService(service.ServiceType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error initializing service {service.ServiceType.FullName}");
                }
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
