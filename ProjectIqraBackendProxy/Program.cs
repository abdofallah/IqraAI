using IqraCore.Utilities;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.Telephony;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

namespace ProjectIqraBackendProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add health checks
            builder.Services.AddHealthChecks();

            // Add health checks
            builder.Services.AddHealthChecks();

            // Add HttpClient
            // ADD MODEM TEL AND TWILIO IHTTP CLIENTS
            builder.Services.AddHttpClient();

            // Redis connection
            builder.Services.AddSingleton<IRedisConnectionFactory>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("Redis");
                var logger = sp.GetRequiredService<ILogger<RedisConnectionFactory>>();
                return new RedisConnectionFactory(connectionString, logger);
            });

            // Server status tracking
            builder.Services.AddSingleton<ServerLiveStatusChannelRepository>();
            builder.Services.AddSingleton<DistributedLockFactory>();

            // MongoDB repositories
            builder.Services.AddSingleton<CallQueueRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB");
                var databaseName = builder.Configuration["MongoDB:DatabaseName"];
                var logger = sp.GetRequiredService<ILogger<CallQueueRepository>>();
                return new CallQueueRepository(connectionString, databaseName, logger);
            });
            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB");
                var databaseName = builder.Configuration["MongoDB:DatabaseName"];
                var logger = sp.GetRequiredService<ILogger<ServerStatusRepository>>();
                return new ServerStatusRepository(connectionString, databaseName, logger);
            });

            // Telephony Providers
            builder.Services.AddSingleton<ModemTelManager>();
            builder.Services.AddSingleton<TwilioManager>();

            // Manager services
            // TODO THEIR DATABASES ARE NOT INITALIZED
            builder.Services.AddSingleton<BusinessManager>();
            builder.Services.AddSingleton<RegionManager>();
            builder.Services.AddSingleton<IntegrationsManager>();

            // Application services
            builder.Services.AddSingleton<ServerSelectionManager>();
            builder.Services.AddSingleton<InboundCallManager>();
            builder.Services.AddSingleton<OutboundCallManager>(); 

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins",
                    p => p
                        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            var app = builder.Build();

            // Configure middleware pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSerilogRequestLogging();
            app.UseCors("AllowedOrigins");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteResponse
            });

            app.Run();
        }
    }
}
