using IqraCore.Entities.Frontend;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Number;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /**
             * 
             * Services START
             * 
            **/

            var appConfig = builder.Configuration;

            // Region

            RegionRepository regionRepository = new RegionRepository(appConfig["AppDatabase:ConnectionString"], appConfig["AppDatabase:DatabaseName"]);

            RegionManager regionManager = new RegionManager(regionRepository);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            // User

            UserSessionRepository userSessionRepository = new UserSessionRepository(appConfig["UserSessionDatabase:ConnectionString"]);
            UserRepository userRepository = new UserRepository(appConfig["UserDatabase:ConnectionString"], appConfig["UserDatabase:DatabaseName"]);

            UserManager userManager = new UserManager(userSessionRepository, userRepository);
            builder.Services.AddSingleton<UserManager>(userManager);

            // Business

            BusinessRepository businessRepository = new BusinessRepository(appConfig["BusinessDatabase:ConnectionString"], appConfig["BusinessDatabase:DatabaseName"]);
            BusinessAppRepository businessAppRepository = new BusinessAppRepository(appConfig["BusinessAppDatabase:ConnectionString"], appConfig["BusinessAppDatabase:DatabaseName"]);
            BusinessLogoRepository businessLogoRepository = new BusinessLogoRepository(appConfig["BusinessLogoRepository:Endpoint"], int.Parse(appConfig["BusinessLogoRepository:Port"]), appConfig["BusinessLogoRepository:AccessKey"], appConfig["BusinessLogoRepository:SecretKey"], appConfig["BusinessLogoRepository:BucketName"], bool.Parse(appConfig["BusinessLogoRepository:IsSecure"]));
            BusinessWhiteLabelDomainRepository businessWhiteLabelDomainRepository = new BusinessWhiteLabelDomainRepository(appConfig["BusinessWhiteLabelDomainRepository:ConnectionString"], appConfig["BusinessWhiteLabelDomainRepository:DatabaseName"]);

            BusinessManager businessManager = new BusinessManager(businessRepository, businessAppRepository, businessLogoRepository, businessWhiteLabelDomainRepository);
            builder.Services.AddSingleton<BusinessManager>(businessManager);

            // Number

            NumberRepository numberRepository = new NumberRepository(appConfig["NumberDatabase:ConnectionString"], appConfig["NumberDatabase:DatabaseName"]);

            NumberManager numberManager = new NumberManager(numberRepository);
            builder.Services.AddSingleton<NumberManager>(numberManager);

            // Views Links Config
            ViewLinkConfiguration viewLinkConfiguration = new ViewLinkConfiguration()
            {
                BusinessLogoURL = appConfig["BusinessLogoRepository:PublicURL"] + "/" + appConfig["BusinessLogoRepository:BucketName"]
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
