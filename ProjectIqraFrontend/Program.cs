using IqraCore.Entities.Frontend;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Integrations;
using IqraInfrastructure.Services.Languages;
using IqraInfrastructure.Services.LLM;
using IqraInfrastructure.Services.Numbers.Providers;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.TTS;
using IqraInfrastructure.Services.User;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /**
             * 
             * Services START
             * 
            **/

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var appConfig = builder.Configuration;

            // Repo
            AppRepository appRepository = new AppRepository(
                appConfig["AppDatabase:ConnectionString"],
                appConfig["AppDatabase:DatabaseName"]
            );
            LanguagesRepository languagesRepository = new LanguagesRepository(
                appConfig["LanguagesDatabase:ConnectionString"],
                appConfig["LanguagesDatabase:DatabaseName"]
            );
            BusinessRepository businessRepository = new BusinessRepository(
                appConfig["BusinessDatabase:ConnectionString"],
                appConfig["BusinessDatabase:DatabaseName"]
            );
            BusinessAppRepository businessAppRepository = new BusinessAppRepository(
                appConfig["BusinessAppDatabase:ConnectionString"],
                appConfig["BusinessAppDatabase:DatabaseName"]
            );
            RegionRepository regionRepository = new RegionRepository(
                appConfig["AppDatabase:ConnectionString"],
                appConfig["AppDatabase:DatabaseName"]
            );
            IntegrationsRepository integrationsRepository = new IntegrationsRepository(
                appConfig["IntegrationsDatabase:ConnectionString"],
                appConfig["IntegrationsDatabase:DatabaseName"]
            );
            IntegrationsLogoRepository integrationsLogoRepository = new IntegrationsLogoRepository(
                appConfig["IntegrationsLogoRepository:Endpoint"],
                int.Parse(appConfig["IntegrationsLogoRepository:Port"]),
                appConfig["IntegrationsLogoRepository:AccessKey"],
                appConfig["IntegrationsLogoRepository:SecretKey"],
                appConfig["IntegrationsLogoRepository:BucketName"],
                bool.Parse(appConfig["IntegrationsLogoRepository:IsSecure"])
            );
            UserSessionRepository userSessionRepository = new UserSessionRepository(
                appConfig["UserSessionDatabase:ConnectionString"]
            );
            UserRepository userRepository = new UserRepository(
                appConfig["UserDatabase:ConnectionString"],
                appConfig["UserDatabase:DatabaseName"]
            );
            BusinessLogoRepository businessLogoRepository = new BusinessLogoRepository(
                appConfig["BusinessLogoRepository:Endpoint"],
                int.Parse(appConfig["BusinessLogoRepository:Port"]),
                appConfig["BusinessLogoRepository:AccessKey"],
                appConfig["BusinessLogoRepository:SecretKey"],
                appConfig["BusinessLogoRepository:BucketName"],
                bool.Parse(appConfig["BusinessLogoRepository:IsSecure"])
            );
            BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository = new BusinessWhiteLabelDomainRepository(
                appConfig["BusinessWhiteLabelDomainRepository:ConnectionString"],
                appConfig["BusinessWhiteLabelDomainRepository:DatabaseName"]
            );
            BusinessDomainVestaCPRepository businessDomainVestaCPRepository = new BusinessDomainVestaCPRepository(
                appConfig["BusinessDomainHostingRepository:Hostname"],
                appConfig["BusinessDomainHostingRepository:AdminUsername"],
                appConfig["BusinessDomainHostingRepository:BusinessesUsername"],
                appConfig["BusinessDomainHostingRepository:AdminPassword"],
                appConfig["BusinessDomainHostingRepository:DomainIP"],
                appConfig["BusinessDomainHostingRepository:IqraBusinessDomain"],
                appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Endpoint"],
                appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Username"],
                appConfig["BusinessDomainHostingRepository:ProxyTemplatesFTP:Password"],
                appRepository
            );
            BusinessToolAudioRepository businessToolAudioRepository = new BusinessToolAudioRepository(
                appConfig["BusinessToolAudioRepository:Endpoint"],
                int.Parse(appConfig["BusinessToolAudioRepository:Port"]),
                appConfig["BusinessToolAudioRepository:AccessKey"],
                appConfig["BusinessToolAudioRepository:SecretKey"],
                appConfig["BusinessToolAudioRepository:BucketName"],
                bool.Parse(appConfig["BusinessToolAudioRepository:IsSecure"])
            );
            BusinessAgentAudioRepository businessAgentAudioRepository = new BusinessAgentAudioRepository(
                appConfig["BusinessAgentAudioRepository:Endpoint"],
                int.Parse(appConfig["BusinessAgentAudioRepository:Port"]),
                appConfig["BusinessAgentAudioRepository:AccessKey"],
                appConfig["BusinessAgentAudioRepository:SecretKey"],
                appConfig["BusinessAgentAudioRepository:BucketName"],
                bool.Parse(appConfig["BusinessAgentAudioRepository:IsSecure"])
            );
            LLMProviderRepository lLMProviderRepository = new LLMProviderRepository(
                appConfig["LLMProviderDatabase:ConnectionString"],
                appConfig["LLMProviderDatabase:DatabaseName"]
            );
            STTProviderRepository sTTProviderRepository = new STTProviderRepository(
                appConfig["STTProviderDatabase:ConnectionString"],
                appConfig["STTProviderDatabase:DatabaseName"]
            );
            TTSProviderRepository tTSProviderRepository = new TTSProviderRepository(
                appConfig["TTSProviderDatabase:ConnectionString"],
                appConfig["TTSProviderDatabase:DatabaseName"]
            );

            // Languages           
            LanguagesManager languagesManager = new LanguagesManager(languagesRepository);
            builder.Services.AddSingleton<LanguagesManager>(languagesManager);

            // Region            
            RegionManager regionManager = new RegionManager(regionRepository);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            // Integrations
            AES256EncryptionService integrationFieldsEncryptionService = new AES256EncryptionService(appConfig["Integrations:EncryptionKey"]);
            IntegrationsManager integrationsManager = new IntegrationsManager(integrationsRepository, businessAppRepository, integrationsLogoRepository, integrationFieldsEncryptionService);
            builder.Services.AddSingleton<IntegrationsManager>(integrationsManager);

            // Number Providers

            // ModemTel
            ModemTelManager modemTelManager = new ModemTelManager();
            builder.Services.AddSingleton<ModemTelManager>(modemTelManager);

            // User
            UserManager userManager = new UserManager(userSessionRepository, userRepository);
            builder.Services.AddSingleton<UserManager>(userManager);

            // Business
            BusinessManager businessManager = new BusinessManager(
                businessRepository,
                businessAppRepository,
                businessLogoRepository,
                businessWhiteLabelDomainRepository,
                businessDomainVestaCPRepository,
                businessToolAudioRepository,
                businessAgentAudioRepository,
                modemTelManager,
                integrationsManager
            );
            builder.Services.AddSingleton<BusinessManager>(businessManager);

            // LLM Provider
            LLMProviderManager llmProviderManager = new LLMProviderManager(lLMProviderRepository, languagesManager);
            builder.Services.AddSingleton<LLMProviderManager>(llmProviderManager);
            await llmProviderManager.InitializeProvidersAsync();

            // STT Provider
            STTProviderManager sttProviderManager = new STTProviderManager(sTTProviderRepository);
            builder.Services.AddSingleton<STTProviderManager>(sttProviderManager);
            await sttProviderManager.InitializeProvidersAsync();

            // TTS Provider
            TTSProviderManager ttsProviderManager = new TTSProviderManager(tTSProviderRepository);
            builder.Services.AddSingleton<TTSProviderManager>(ttsProviderManager);
            await ttsProviderManager.InitializeProvidersAsync();

            // Views Links Config
            ViewLinkConfiguration viewLinkConfiguration = new ViewLinkConfiguration()
            {
                BusinessLogoURL = appConfig["BusinessLogoRepository:PublicURL"] + "/" + appConfig["BusinessLogoRepository:BucketName"],
                BusinessToolAudioURL = appConfig["BusinessToolAudioRepository:PublicURL"] + "/" + appConfig["BusinessToolAudioRepository:BucketName"],
                IntegrationLogoURL = appConfig["IntegrationsLogoRepository:PublicURL"] + "/" + appConfig["IntegrationsLogoRepository:BucketName"],
                BusinessAgentBackgroundAudioURL = appConfig["BusinessAgentAudioRepository:PublicURL"] + "/" + appConfig["BusinessAgentAudioRepository:BucketName"]
            };
            builder.Services.AddSingleton<ViewLinkConfiguration>(viewLinkConfiguration);

            /** 
             * 
             * Services END 
             * 
            **/

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

            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            customJSONMiddleware.SetHttpContextAccessor(httpContextAccessor);

            var IHttpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
            modemTelManager.SetHttpClientFactory(IHttpClientFactory);

            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
