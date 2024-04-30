using ProjectIqraBackend.App;
using System.Security.Principal;

namespace ProjectIqraBackend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (!IsAdministrator())
            {
                throw new Exception("The program must be run as Administrator.");
            }

            var builder = WebApplication.CreateBuilder(args);

            /** Services START **/

            IqraApp iqraApp = new IqraApp(builder);
            await iqraApp.Initialize();
            iqraApp.AddServicesToSingleton();

            /** Services END **/

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();
            app.Run();
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
