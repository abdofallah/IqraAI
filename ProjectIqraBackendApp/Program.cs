using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Server;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Conversation.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProjectIqraBackendApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            builder.Services.AddSingleton<BackendAppConfig>(sp =>
            {
                return new BackendAppConfig
                {
                    ServerId = appConfig["Server:Identity"],
                    RegionId = appConfig["Server:RegionId"],
                    ExpectedMaxConcurrentCalls = int.Parse(appConfig["Server:ExpectedMaxConcurrentCalls"]),
                    NetworkInterfaceName = appConfig["Server:NetworkInterfaceName"],
                    MaxNetworkDownloadMbps = int.Parse(appConfig["Server:MaxNetworkDownloadMbps"]),
                    MaxNetworkUploadMbps = int.Parse(appConfig["Server:MaxNetworkUploadMbps"]),
                    ApiKey = appConfig["Server:ApiKey"]
                };
            });

            // Repositories
            SetupRepositories(builder, appConfig);

            // Managers
            SetupManagers(builder, appConfig);

            // HTTP Client
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });

            // Add services to the container
            builder.Services.AddControllers();

            // Health checks
            builder.Services.AddHealthChecks();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", p => p
                    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            var app = builder.Build();

            // Initalize All Singleton Services
            InitializeAllSingletonServices(app.Services);

            app.UseWebSockets(new WebSocketOptions
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(10)
                }
            );
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/ws/session"))
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WebSocketMiddleware");

                        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (pathSegments == null || pathSegments.Length < 5 ||
                            pathSegments[0] != "ws" || pathSegments[1] != "session" || pathSegments[3] != "client")
                        {
                            context.Response.StatusCode = 400; await context.Response.WriteAsync("Invalid WebSocket path."); return;
                        }

                        string sessionId = pathSegments[2];
                        string clientId = pathSegments[4];

                        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(clientId))
                        {
                            context.Response.StatusCode = 400; await context.Response.WriteAsync("Invalid WebSocket path."); return;
                        }

                        var callProcessorManager = context.RequestServices.GetRequiredService<CallProcessorManager>();
                        var sessionCts = callProcessorManager.GetSessionCancellationTokenSource(sessionId);
                        if (sessionCts == null || sessionCts.IsCancellationRequested)
                        {
                            context.Response.StatusCode = 404; await context.Response.WriteAsync("Session not active or not found."); return;
                        }

                        try
                        {
                            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await callProcessorManager.AssignWebSocketToClientAsync(sessionId, clientId, webSocket, sessionCts);
                        }
                        catch (Exception ex)
                        {
                            if (!context.Response.HasStarted) context.Response.StatusCode = 500;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400; await context.Response.WriteAsync("Expected WebSocket request.");
                    }
                }
                else
                {
                    await next(context);
                }
            });

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

        private static void SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            builder.Services.AddSingleton<RegionRepository>((sp) => {
                return new RegionRepository(
                    sp.GetRequiredService<ILogger<RegionRepository>>(),
                    appConfig["AppDatabase:ConnectionString"],
                    appConfig["AppDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
                    appConfig["LanguagesDatabase:ConnectionString"],
                    appConfig["LanguagesDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<InboundCallQueueRepository>(sp =>
            {
                return new InboundCallQueueRepository(
                    appConfig["CallQueueRepository:ConnectionString"],
                    appConfig["CallQueueRepository:DatabaseName"],
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>()
                );
            });
            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                return new ServerStatusRepository(
                    appConfig["ServerStatusRepository:ConnectionString"],
                    appConfig["ServerStatusRepository:DatabaseName"],
                    sp.GetRequiredService<ILogger<ServerStatusRepository>>()
                );
            });
            builder.Services.AddSingleton<ServerLiveStatusChannelRepository>((sp) =>
            {
                return new ServerLiveStatusChannelRepository(
                    new RedisConnectionFactory(
                        appConfig["ServerLiveStatusChannelRepository:ConnectionString"],
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<ServerLiveStatusChannelRepository>>()
                );
            });
            builder.Services.AddSingleton<DistributedLockRepository>((sp) =>
            {
                return new DistributedLockRepository(
                    new RedisConnectionFactory(
                        appConfig["DistributedLockRepository:ConnectionString"],
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<DistributedLockRepository>>()
                );
            });
            builder.Services.AddSingleton<ConversationStateRepository>(sp =>
            {
                return new ConversationStateRepository(
                    appConfig["ConversationStateRepository:ConnectionString"],
                    appConfig["ConversationStateRepository:DatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });
            builder.Services.AddSingleton<ConversationAudioRepository>(sp =>
            {
                return new ConversationAudioRepository(
                    appConfig["ConversationAudioRepository:Endpoint"],
                    int.Parse(appConfig["ConversationAudioRepository:Port"]),
                    appConfig["ConversationAudioRepository:AccessKey"],
                    appConfig["ConversationAudioRepository:SecretKey"],
                    appConfig["ConversationAudioRepository:BucketName"],
                    bool.Parse(appConfig["ConversationAudioRepository:IsSecure"]),
                    sp.GetRequiredService<ILogger<ConversationAudioRepository>>()
                );
            });
            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
                    appConfig["IntegrationsDatabase:ConnectionString"],
                    appConfig["IntegrationsDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessRepository>((sp) =>
            {
                return new BusinessRepository(
                    sp.GetRequiredService<ILogger<BusinessRepository>>(),
                    appConfig["BusinessDatabase:ConnectionString"],
                    appConfig["BusinessDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessAppRepository>((sp) =>
            {
                return new BusinessAppRepository(
                    sp.GetRequiredService<ILogger<BusinessAppRepository>>(),
                    appConfig["BusinessAppDatabase:ConnectionString"],
                    appConfig["BusinessAppDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    appConfig["BusinessToolAudioRepository:Endpoint"],
                    int.Parse(appConfig["BusinessToolAudioRepository:Port"]),
                    appConfig["BusinessToolAudioRepository:AccessKey"],
                    appConfig["BusinessToolAudioRepository:SecretKey"],
                    appConfig["BusinessToolAudioRepository:BucketName"],
                    bool.Parse(appConfig["BusinessToolAudioRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    appConfig["BusinessAgentAudioRepository:Endpoint"],
                    int.Parse(appConfig["BusinessAgentAudioRepository:Port"]),
                    appConfig["BusinessAgentAudioRepository:AccessKey"],
                    appConfig["BusinessAgentAudioRepository:SecretKey"],
                    appConfig["BusinessAgentAudioRepository:BucketName"],
                    bool.Parse(appConfig["BusinessAgentAudioRepository:IsSecure"])
                );
            });
            builder.Services.AddSingleton<LLMProviderRepository>((sp) =>
            {
                return new LLMProviderRepository(
                    sp.GetRequiredService<ILogger<LLMProviderRepository>>(),
                    appConfig["LLMProviderDatabase:ConnectionString"],
                    appConfig["LLMProviderDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<STTProviderRepository>((sp) =>
            {
                return new STTProviderRepository(
                    sp.GetRequiredService<ILogger<STTProviderRepository>>(),
                    appConfig["STTProviderDatabase:ConnectionString"],
                    appConfig["STTProviderDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<TTSProviderRepository>((sp) =>
            {
                return new TTSProviderRepository(
                    sp.GetRequiredService<ILogger<TTSProviderRepository>>(),
                    appConfig["TTSProviderDatabase:ConnectionString"],
                    appConfig["TTSProviderDatabase:DatabaseName"]
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            builder.Services.AddSingleton<LanguagesManager>((sp) =>
            {
                return new LanguagesManager(
                    sp.GetRequiredService<ILogger<LanguagesManager>>(),
                    sp.GetRequiredService<LanguagesRepository>()
                );
            });
            builder.Services.AddSingleton<RegionManager>((sp) =>
            {
                return new RegionManager(
                    sp.GetRequiredService<ILogger<RegionManager>>(),
                    sp.GetRequiredService<RegionRepository>()
                );
            });
            builder.Services.AddSingleton<ModemTelManager>((sp) =>
            {
                return new ModemTelManager(
                    sp.GetRequiredService<ILogger<ModemTelManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<TwilioManager>((sp) =>
            {
                return new TwilioManager(
                    sp.GetRequiredService<ILogger<TwilioManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<BusinessManager>((sp) =>
            {
                return new BusinessManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    new BusinessManagerInitalizationSettings()
                    {
                        InitalizeAgentsManager = true,
                        InitalizeCacheManager = true,
                        InitalizeContextManager = true,
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeRoutesManager = true,
                        InitalizeToolsManager = true
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    null,
                    null,
                    null,
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );
            });
            builder.Services.AddSingleton<IntegrationsManager>((sp) =>
            {
                AES256EncryptionService integrationFieldsEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["Integrations:EncryptionKey"]
                );
                return new IntegrationsManager(
                    sp.GetRequiredService<ILogger<IntegrationsManager>>(),
                    sp.GetRequiredService<IntegrationsRepository>(),
                    null,
                    integrationFieldsEncryptionService
                );
            });
            builder.Services.AddSingleton<LLMProviderManager>((sp) =>
            {
                return new LLMProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<LLMProviderRepository>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<STTProviderManager>((sp) =>
            {
                return new STTProviderManager(
                    sp.GetRequiredService<ILogger<STTProviderManager>>(),
                    sp.GetRequiredService<STTProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<TTSProviderManager>((sp) =>
            {
                return new TTSProviderManager(
                    sp.GetRequiredService<ILogger<TTSProviderManager>>(),
                    sp.GetRequiredService<TTSProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });

            // Core server services
            builder.Services.AddSingleton<IHardwareMonitor>((sp) =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new WindowsHardwareMonitor(
                        sp.GetRequiredService<ILogger<WindowsHardwareMonitor>>(),
                        sp.GetRequiredService<BackendAppConfig>()
                    );
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return new LinuxHardwareMonitor(
                        sp.GetRequiredService<ILogger<LinuxHardwareMonitor>>(),
                        sp.GetRequiredService<BackendAppConfig>()
                    );
                }
                else
                {
                    throw new Exception("Unsupported OS for IHARDWAREMONITOR");
                }
            });

            builder.Services.AddSingleton<ServerMetricsMonitor>((sp) =>
            {
                return new ServerMetricsMonitor(
                    sp.GetRequiredService<ILogger<ServerMetricsMonitor>>(),
                    sp.GetRequiredService<BackendAppConfig>(),
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<ServerStatusRepository>(),
                    sp.GetRequiredService<IHardwareMonitor>()
                );
            });
            builder.Services.AddSingleton<SystemPromptGenerator>((sp) =>
            {
                return new SystemPromptGenerator(
                    sp.GetRequiredService<ILogger<SystemPromptGenerator>>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<LLMProviderManager>()
                );
            });
            builder.Services.AddSingleton<CallProcessorManager>((sp) =>
            {
                return new CallProcessorManager(
                    sp.GetRequiredService<ILogger<CallProcessorManager>>(),
                    sp,
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });

            // Background services
            builder.Services.AddHostedService<ServerMetricsManager>((sp) =>
            {
                return new ServerMetricsManager(
                    sp.GetRequiredService<ILogger<ServerMetricsManager>>(),
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<BackendAppConfig>()
                );
            });
        }

        private static void InitializeAllSingletonServices(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Initializing all singleton services from IqraInfrastructure namespace...");

            // Get service descriptors from the service collection
            var services = GetTypes(serviceProvider)
                .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton &&
                       descriptor.ServiceType.Namespace != null &&
                       descriptor.ServiceType.Namespace.StartsWith("IqraInfrastructure"))
                .ToList();

            logger.LogInformation($"Found {services.Count} singleton services to initialize");

            foreach (var service in services)
            {
                logger.LogInformation($"Initializing service: {service.ServiceType.Name}");
                serviceProvider.GetService(service.ServiceType);
            }

            logger.LogInformation("All IqraInfrastructure singleton services initialized successfully");
        }

        private static List<ServiceDescriptor> GetTypes(IServiceProvider provider)
        {
            ServiceProvider serviceProvider = provider as ServiceProvider;
            var callSiteFactory = serviceProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serviceProvider);
            var serviceDescriptors = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(callSiteFactory) as ServiceDescriptor[];
            return serviceDescriptors.ToList();
        }
    }
}