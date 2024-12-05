using IqraCore.Entities.Frontend;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.Number;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Languages;
using IqraInfrastructure.Services.LLM;
using IqraInfrastructure.Services.Number;
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

            var appConfig = builder.Configuration;

            // App Repo

            AppRepository appRepository = new AppRepository(
                appConfig["AppDatabase:ConnectionString"],
                appConfig["AppDatabase:DatabaseName"]
            );

            // Languages

            LanguagesRepository languagesRepository = new LanguagesRepository(
                appConfig["LanguagesDatabase:ConnectionString"],
                appConfig["LanguagesDatabase:DatabaseName"]
            );
            LanguagesManager languagesManager = new LanguagesManager(languagesRepository);
            builder.Services.AddSingleton<LanguagesManager>(languagesManager);

            // Region

            RegionRepository regionRepository = new RegionRepository(
                appConfig["AppDatabase:ConnectionString"],
                appConfig["AppDatabase:DatabaseName"]
            );

            RegionManager regionManager = new RegionManager(regionRepository);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            // User

            UserSessionRepository userSessionRepository = new UserSessionRepository(
                appConfig["UserSessionDatabase:ConnectionString"]
            );
            UserRepository userRepository = new UserRepository(
                appConfig["UserDatabase:ConnectionString"],
                appConfig["UserDatabase:DatabaseName"]
            );

            UserManager userManager = new UserManager(userSessionRepository, userRepository);
            builder.Services.AddSingleton<UserManager>(userManager);

            // Business

            BusinessRepository businessRepository = new BusinessRepository(
                appConfig["BusinessDatabase:ConnectionString"],
                appConfig["BusinessDatabase:DatabaseName"]
            );
            BusinessAppRepository businessAppRepository = new BusinessAppRepository(
                appConfig["BusinessAppDatabase:ConnectionString"],
                appConfig["BusinessAppDatabase:DatabaseName"]
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

            BusinessManager businessManager = new BusinessManager(businessRepository, businessAppRepository, businessLogoRepository, businessWhiteLabelDomainRepository, businessDomainVestaCPRepository, businessToolAudioRepository);
            builder.Services.AddSingleton<BusinessManager>(businessManager);

            // Number

            NumberRepository numberRepository = new NumberRepository(
                appConfig["NumberDatabase:ConnectionString"],
                appConfig["NumberDatabase:DatabaseName"]
            );

            NumberManager numberManager = new NumberManager(numberRepository);
            builder.Services.AddSingleton<NumberManager>(numberManager);

            // LLM Provider
            LLMProviderRepository lLMProviderRepository = new LLMProviderRepository(
                appConfig["LLMProviderDatabase:ConnectionString"],
                appConfig["LLMProviderDatabase:DatabaseName"]
            );
            LLMProviderManager llmProviderManager = new LLMProviderManager(lLMProviderRepository, languagesManager);
            builder.Services.AddSingleton<LLMProviderManager>(llmProviderManager);

            await llmProviderManager.InitializeProvidersAsync();

            // Views Links Config
            ViewLinkConfiguration viewLinkConfiguration = new ViewLinkConfiguration()
            {
                BusinessLogoURL = appConfig["BusinessLogoRepository:PublicURL"] + "/" + appConfig["BusinessLogoRepository:BucketName"],
                BusinessToolAudioURL = appConfig["BusinessToolAudioRepository:PublicURL"] + "/" + appConfig["BusinessToolAudioRepository:BucketName"],
            };
            builder.Services.AddSingleton<ViewLinkConfiguration>(viewLinkConfiguration);

            /** 
             * 
             * Services END 
             * 
            **/

            builder.Services.AddHttpContextAccessor();

            var builtServiceProvider = builder.Services.BuildServiceProvider();

            var httpContextAccessor = builtServiceProvider.GetRequiredService<IHttpContextAccessor>();

            builder.Services
                .AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new EndpointAwareJsonConverter(
                            httpContextAccessor
                        )
                    );
                });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
