using IqraCore.Utilities;
using IqraInfrastructure.Managers.Server;
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

            // Redis connection
            builder.Services.AddSingleton<IRedisConnectionFactory>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("Redis");
                var logger = sp.GetRequiredService<ILogger<RedisConnectionFactory>>();
                return new RedisConnectionFactory(connectionString, logger);
            });

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

            // Application services
            builder.Services.AddSingleton<ServerSelectionManager>();
            builder.Services.AddSingleton<InboundCallManager>();
            builder.Services.AddSingleton<OutboundCallManager>();
            // ADD BUSINESS MANAGER
            // ADD MODEM TEL MANAGER
            // ADD TWILIO MANAGER
            // ADD INTEGRATIONS MANAGER
            // ADD REGION MANAGER
            // ADD MODEM TEL AND TWILIO IHTTP CLIENTS
            // ADD DistributedLockFactory based on IRedisConnectionFactory
            // ADD ServerLiveStatusChannelRepository based on IRedisConnectionFactory

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
