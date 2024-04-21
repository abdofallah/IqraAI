using IqraCore.Interfaces.Repositories;
using IqraInfrastructure.Repositories;

namespace ProjectIqraFrontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /** Services START **/


            var appConfig = builder.Configuration;

            IUserSessionRepository userSessionRepository = new UserSessionRepository(appConfig["Redis:UserSessionConnectionString"]);
            builder.Services.AddSingleton<IUserSessionRepository>(userSessionRepository);



            /** Services END **/

            builder.Services.AddControllersWithViews();

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
