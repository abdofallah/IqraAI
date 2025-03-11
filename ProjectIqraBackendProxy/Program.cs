
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Managers.Server;

namespace ProjectIqraBackendProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /**
             * 
             * START
             * 
            **/

            // Configuration
            var appConfig = builder.Configuration;
            string ServerIdentifier = appConfig["Server:Identifier"];

            // Repo
            ServerHistoricalStatusRepository serverHistoricalStatusRepository = new ServerHistoricalStatusRepository(
                ServerIdentifier,
                appConfig["ServerHistoricalStatus:ConnectionString"],
                appConfig["ServerHistoricalStatus:DatabaseName"]
            );

            // Managers
            ServerManager serverManager = new ServerManager(ServerIdentifier, serverHistoricalStatusRepository);
            serverManager.StartServerMonitor(new CancellationTokenSource());

            /**
             * 
             * END
             * 
            **/

            builder.Services.AddControllers();

            var app = builder.Build();

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
