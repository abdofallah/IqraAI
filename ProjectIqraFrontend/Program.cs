using IqraInfrastructure.Repositories;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /** Services START **/

            var appConfig = builder.Configuration;

            RegionRepository regionRepository = new RegionRepository(appConfig["AppDatabase:ConnectionString"], appConfig["AppDatabase:DatabaseName"]);

            RegionManager regionManager = new RegionManager(regionRepository);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            UserSessionRepository userSessionRepository = new UserSessionRepository(appConfig["UserSessionDatabase:ConnectionString"]);
            UserRepository userRepository = new UserRepository(appConfig["UserDatabase:ConnectionString"], appConfig["UserDatabase:DatabaseName"]);

            UserManager userManager = new UserManager(userSessionRepository, userRepository);
            builder.Services.AddSingleton<UserManager>(userManager);

            BusinessRepository businessRepository = new BusinessRepository(appConfig["BusinessDatabase:ConnectionString"], appConfig["BusinessDatabase:DatabaseName"]);
            BusinessAppRepository businessAppRepository = new BusinessAppRepository(appConfig["BusinessAppDatabase:ConnectionString"], appConfig["BusinessAppDatabase:DatabaseName"]);

            BusinessManager businessManager = new BusinessManager(businessRepository, businessAppRepository);
            builder.Services.AddSingleton<BusinessManager>(businessManager);

            /** Services END **/

            builder.Services
                .AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new EndpointAwareJsonConverter(
                            builder.Services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>()
                        )
                    );
                });

            builder.Services.AddHttpContextAccessor();

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
