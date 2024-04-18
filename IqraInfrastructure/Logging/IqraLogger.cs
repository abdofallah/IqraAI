using Serilog;

namespace IqraInfrastructure.Logging
{
    public static class IqraLogger
    {
        public static void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/iqra.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}