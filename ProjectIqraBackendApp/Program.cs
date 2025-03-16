using IqraCore.Entities.Server;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Conversation;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.Telephony;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

namespace ProjectIqraBackendApp
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

            // Health checks
            builder.Services.AddHealthChecks();

            // HTTP Client
            // ADD MODEM TEL AND TWILIO IHTTP CLIENTS
            builder.Services.AddHttpClient();

            // Server configuration
            builder.Services.AddSingleton<ServerConfig>(sp =>
            {
                return new ServerConfig
                {
                    ServerId = builder.Configuration["ServerId"] ?? Environment.MachineName,
                    RegionId = builder.Configuration["RegionId"] ?? "default",
                    MaxConcurrentCalls = int.Parse(builder.Configuration["MaxConcurrentCalls"] ?? "50")
                };
            });

            // Redis
            builder.Services.AddSingleton<IRedisConnectionFactory>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var logger = sp.GetRequiredService<ILogger<RedisConnectionFactory>>();
                return new RedisConnectionFactory(connectionString, logger);
            });

            builder.Services.AddSingleton<ServerLiveStatusChannelRepository>();
            builder.Services.AddSingleton<DistributedLockFactory>();

            // MongoDB repositories
            builder.Services.AddSingleton<CallQueueRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
                var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "IqraTelephony";
                var logger = sp.GetRequiredService<ILogger<CallQueueRepository>>();
                return new CallQueueRepository(connectionString, databaseName, logger);
            });

            builder.Services.AddSingleton<CallSessionRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
                var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "IqraTelephony";
                var logger = sp.GetRequiredService<ILogger<CallSessionRepository>>();
                return new CallSessionRepository(connectionString, databaseName, logger);
            });

            builder.Services.AddSingleton<ConversationStateRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
                var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "IqraTelephony";
                var logger = sp.GetRequiredService<ILogger<ConversationStateRepository>>();
                return new ConversationStateRepository(connectionString, databaseName, logger);
            });

            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
                var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "IqraTelephony";
                var logger = sp.GetRequiredService<ILogger<ServerStatusRepository>>();
                return new ServerStatusRepository(connectionString, databaseName, logger);
            });

            // Minio storage
            builder.Services.AddSingleton<ConversationAudioRepository>(sp =>
            {
                var config = builder.Configuration.GetSection("Storage:Minio");
                return new ConversationAudioRepository(
                    config["Endpoint"],
                    int.Parse(config["Port"]),
                    config["AccessKey"],
                    config["SecretKey"],
                    config["BucketName"],
                    bool.Parse(config["UseSSL"]),
                    sp.GetRequiredService<ILogger<ConversationAudioRepository>>()
                );
            });

            // Telephony Providers
            builder.Services.AddSingleton<ModemTelManager>();
            builder.Services.AddSingleton<TwilioManager>();

            // Manager services
            // TODO THEIR DATABASES ARE NOT INITALIZED
            builder.Services.AddSingleton<BusinessManager>();
            builder.Services.AddSingleton<RegionManager>();
            builder.Services.AddSingleton<IntegrationsManager>();

            // Core server services
            builder.Services.AddSingleton<ServerStatusManager>();
            builder.Services.AddSingleton<SystemPromptGenerator>();
            builder.Services.AddSingleton<ScriptExecutionManager>();
            builder.Services.AddSingleton<CallProcessorManager>();

            // Background services
            builder.Services.AddHostedService<ServerMetricsManager>();
            builder.Services.AddHostedService<CallQueueCleanupManager>();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", p => p
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