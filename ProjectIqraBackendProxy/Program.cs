using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Interfaces.Modules;
using IqraCore.Interfaces.Node;
using IqraCore.Interfaces.Server;
using IqraCore.Interfaces.User;
using IqraCore.Utilities;
using IqraInfrastructure.HostedServices.Call.Outbound;
using IqraInfrastructure.HostedServices.Lifecycle;
using IqraInfrastructure.HostedServices.Metrics;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Call.Inbound;
using IqraInfrastructure.Managers.Call.Outbound;
using IqraInfrastructure.Managers.Call.Proxy;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Node;
using IqraInfrastructure.Managers.Node.Monitors;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.Server.Metrics.Monitor;
using IqraInfrastructure.Managers.Server.Metrics.Monitor.Hardware;
using IqraInfrastructure.Managers.SIP;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WebSession;
using IqraInfrastructure.Utilities.App;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using MongoDB.Driver;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProjectIqraBackendProxy
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudProxyAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && WindowsServiceHelpers.IsWindowsService())
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "IqraAI.Proxy";
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemdHelpers.IsSystemdService())
            {
                builder.Services.AddSystemd();
            }
            builder.Services.Configure<HostOptions>(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromMinutes(10);
            });

            // Configuration
            var appConfig = builder.Configuration;
            ProxyAppConfig proxyAppConfig = new ProxyAppConfig()
            {
                RegionId = appConfig["Proxy:RegionId"],
                ServerId = appConfig["Proxy:Id"],
                OutboundProcessing = new ProxyAppOutboundProcessingConfig()
                {
                    DbFetchBatchSize = int.Parse(appConfig["Proxy:OutboundProcessing:DbFetchBatchSize"]),
                    PollingIntervalSeconds = int.Parse(appConfig["Proxy:OutboundProcessing:PollingIntervalSeconds"]),
                    ProcessingBatchSize = int.Parse(appConfig["Proxy:OutboundProcessing:ProcessingBatchSize"]),
                    ScheduleWindowMinutes = int.Parse(appConfig["Proxy:OutboundProcessing:ScheduleWindowMinutes"])
                },
                ApiKey = appConfig["Security:ApiKey"],
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
            };

            // Load Cloud Asembly
            if (proxyAppConfig.IsCloudVersion)
            {
                LoadCloudAssembly();
            }

            // Preflight
            await SetupPreflight(builder, appConfig, proxyAppConfig);

            // Repositories
            SetupRepositories(builder, appConfig);
            if (proxyAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupRepositories(builder.Services, appConfig);
            }

            // Managers
            SetupManagers(builder, appConfig, proxyAppConfig);
            if (proxyAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupManagers(builder.Services, appConfig);
            }

            // Hosted Services
            SetupHostedServices(builder, proxyAppConfig);

            // HTTP Client
            builder.Services.AddHttpClient("CallManagerServerForward");
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
            builder.Services.AddHttpClient("OutboundCallForwardClient");

            // Add services to the container
            builder.Services.AddControllers();;

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

            // Postflight: Inject dependecies where needed
            SetupPostflight(app);

            // Initalize All Singleton Services
            SingletonWarmupHelper.InitializeAllSingletonServices<Program>(app.Services);

            app.UseCors("AllowedOrigins");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // BOOTSTRAP: Initial Check
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Iqra Proxy Bootstrapping...");

                var regionManager = scope.ServiceProvider.GetRequiredService<RegionManager>();
                var regionData = await regionManager.GetRegionById(proxyAppConfig.RegionId);
                if (regionData == null)
                {
                    throw new Exception($"Region with id {proxyAppConfig.RegionId} not found.");
                }
                var serverData = regionData.Servers.FirstOrDefault(s => s.Id == proxyAppConfig.ServerId);
                if (serverData == null)
                {
                    throw new Exception($"Server with id {proxyAppConfig.ServerId} in region {proxyAppConfig.RegionId} not found.");
                }
                if (serverData.APIKey != proxyAppConfig.ApiKey)
                {
                    throw new Exception($"Mismatch between ApiKey in config and ApiKey in database.");
                }

                // Make sure no other proxy node with same config is running
                var metricsManager = scope.ServiceProvider.GetRequiredService<ServerMetricsManager>();
                var frontendAlreadyRunning = await metricsManager.CheckProxyNodeRunning(proxyAppConfig.RegionId, proxyAppConfig.ServerId);
                if (frontendAlreadyRunning)
                {
                    throw new Exception("Server Metrics Manager found that a proxy node (with same node id in current region) is already running.\nThis could be a false positive too, but it's better to be safe than sorry.\n\nGiven the redis database takes 30seconds to clear previous running proxy status, if the issue presits for more than a minute, there must be another proxy node (with same node id in current region) running.");
                }

                // Perform Initial Startup Integrity Check
                var startupIntregity = scope.ServiceProvider.GetRequiredService<StartupIntegrityCheckService>();
                await startupIntregity.CheckAsync();

                logger.LogInformation("Iqra Proxy Bootstrapping Completed.");
            }

            app.Run();
        }

        private static void LoadCloudAssembly()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string cloudDllPath = Path.Combine(folder, "ProjectIqraBackendProxy.Cloud.dll");
            if (!File.Exists(cloudDllPath)) throw new Exception("Cloud DLL missing");

            _cloudAssembly = Assembly.LoadFrom(cloudDllPath);
            var type = _cloudAssembly.GetTypes().FirstOrDefault(t => typeof(ICloudProxyAppInitalizer).IsAssignableFrom(t) && !t.IsInterface);
            if (type != null)
            {
                _cloudModule = (ICloudProxyAppInitalizer)Activator.CreateInstance(type);
            }
            if (_cloudModule == null) throw new Exception("Cloud module not found");
        }

        private static async Task SetupPreflight(WebApplicationBuilder builder, IConfiguration appConfig, ProxyAppConfig proxyAppConfig)
        {
            // Basic Dependencies required for Preflight
            var mongoClient = new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            builder.Services.AddSingleton<IMongoClient>(mongoClient);

            var regionRepoistory = new RegionRepository(mongoClient);
            builder.Services.AddSingleton<RegionRepository>(regionRepoistory);

            var regionManager = new RegionManager(regionRepoistory);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            // Build Remaning config from dependencies
            var regionData = await regionManager.GetRegionById(proxyAppConfig.RegionId);
            if (regionData == null)
            {
                throw new Exception("Region not found");
            }
            var regionServerData = regionData.Servers.FirstOrDefault(s => s.Id == proxyAppConfig.ServerId);
            if (regionServerData == null)
            {
                throw new Exception("Server not found");
            }
            proxyAppConfig.ServerEndpoint = regionServerData.Endpoint;
            proxyAppConfig.SIPPort = regionServerData.SIPPort;
        }

        private static void SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            string redisConnectionString = appConfig["RedisDatabase:Endpoint"]!;
            string redisConfigPassword = appConfig["RedisDatabase:Password"]!;
            if (!string.IsNullOrEmpty(redisConfigPassword))
            {
                redisConnectionString += $",password={redisConfigPassword}";
            }

            // Repositories
            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<CallQueueLogsRepository>((sp) =>
            {
                return new CallQueueLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<CallQueueLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<InboundCallQueueRepository>(sp =>
            {
                return new InboundCallQueueRepository(
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                return new ServerStatusRepository(
                    sp.GetRequiredService<ILogger<ServerStatusRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<ServerLiveStatusChannelRepository>((sp) =>
            {
                return new ServerLiveStatusChannelRepository(
                    new RedisConnectionFactory(
                        $"{redisConnectionString},defaultDatabase={ServerLiveStatusChannelRepository.DATABASE_INDEX}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<ServerLiveStatusChannelRepository>>()
                );
            });

            builder.Services.AddSingleton<DistributedLockRepository>((sp) =>
            {
                return new DistributedLockRepository(
                    new RedisConnectionFactory(
                        $"{redisConnectionString},defaultDatabase={DistributedLockRepository.DATABASE_INDEX}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<DistributedLockRepository>>()
                );
            });

            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<BusinessRepository>((sp) =>
            {
                return new BusinessRepository(
                    sp.GetRequiredService<ILogger<BusinessRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<BusinessAppRepository>((sp) =>
            {
                return new BusinessAppRepository(
                    sp.GetRequiredService<ILogger<BusinessAppRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<UserRepository>((sp) =>
            {
                return new UserRepository(
                    sp.GetRequiredService<ILogger<UserRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<ConversationStateRepository>((sp) =>
            {
                return new ConversationStateRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });

            builder.Services.AddSingleton<ConversationStateLogsRepository>(sp =>
            {
                return new ConversationStateLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<ConversationStateLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueRepository>(sp =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<WebSessionRepository>((sp) =>
            {
                return new WebSessionRepository(
                    sp.GetRequiredService<ILogger<WebSessionRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, ProxyAppConfig proxyAppConfig)
        {
            if (!proxyAppConfig.IsCloudVersion)
            {
                builder.Services.AddSingleton<IUserUsageValidationManager, UserUsageValidationManager>((sp) =>
                {
                    return new UserUsageValidationManager();
                });
            }

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
                    integrationFieldsEncryptionService,
                    null
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
                    sp.GetRequiredService<IMongoClient>(),
                    new BusinessManagerInitalizationSettings()
                    {
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeCampaignCURDManager = true,
                        InitalizeToolsCURDManager = true
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    null,
                    null,
                    null,
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    null,
                    null,
                    null,
                    null,
                    sp.GetRequiredService<RegionManager>(),
                    null,
                    null,
                    null,
                    sp.GetRequiredService<TwilioManager>(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );
            });
            builder.Services.AddSingleton<ServerSelectionManager>((sp) =>
            {
                return new ServerSelectionManager(
                    sp.GetRequiredService<ILogger<ServerSelectionManager>>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ServerMetricsManager>(),
                    sp.GetRequiredService<DistributedLockRepository>()
                );
            });
            builder.Services.AddSingleton<InboundCallService>((sp) =>
            {
                return new InboundCallService(
                    sp.GetRequiredService<ILogger<InboundCallService>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<TwilioManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<IUserUsageValidationManager>(),
                    sp.GetRequiredService<UserManager>()
                );
            });
            builder.Services.AddSingleton<UserManager>((sp) =>
            {
                return new UserManager(
                    sp.GetRequiredService<ILogger<UserManager>>(),
                    null,
                    null,
                    sp.GetRequiredService<UserRepository>(),
                    null,
                    null,
                    null
                );
            });
            builder.Services.AddSingleton<OutboundCallProcessingOrchestrator>((sp) =>
            {
                return new OutboundCallProcessingOrchestrator(
                    sp.GetRequiredService<ILogger<OutboundCallProcessingOrchestrator>>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<IUserUsageValidationManager>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<TwilioManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<CampaignActionExecutorService>(),
                    sp.GetRequiredService<UserManager>()
                );
            });
            builder.Services.AddSingleton<CallStatusService>((sp) =>
            {
                return new CallStatusService(
                    sp.GetRequiredService<ILogger<CallStatusService>>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<CampaignActionExecutorService>((sp) =>
            {
                return new CampaignActionExecutorService(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<WebSessionRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<ConversationStateLogsRepository>(),
                    sp.GetRequiredService<BusinessManager>()
                );
            });

            builder.Services.AddSingleton<IqraAppManager>((sp) =>
            {
                return new IqraAppManager(
                    sp,
                    null,
                    sp.GetRequiredService<ILogger<IqraAppManager>>()
                );
            });

            builder.Services.AddSingleton<IHardwareMonitor>((sp) =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new WindowsHardwareMonitor(
                        sp.GetRequiredService<ILogger<WindowsHardwareMonitor>>(),
                        appConfig["Hardware:NetworkInterfaceName"]
                    );
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return new LinuxHardwareMonitor(
                        sp.GetRequiredService<ILogger<LinuxHardwareMonitor>>(),
                        appConfig["Hardware:NetworkInterfaceName"]
                    );
                }
                else
                {
                    throw new Exception("Unsupported OS for IHARDWAREMONITOR");
                }
            });

            builder.Services.AddSingleton<StartupIntegrityCheckService>((sp) =>
            {
                return new StartupIntegrityCheckService(
                    sp,
                    sp.GetRequiredService<ILogger<StartupIntegrityCheckService>>(),
                    AppNodeTypeEnum.Proxy
                );
            });

            builder.Services.AddSingleton<ServerMetricsManager>((sp) =>
            {
                return new ServerMetricsManager(
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<ServerStatusRepository>()
                );
            });

            builder.Services.AddSingleton<ProxyMetricsMonitor>((sp) =>
            {
                return new ProxyMetricsMonitor(
                    sp.GetRequiredService<ILogger<ProxyMetricsMonitor>>(),
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<ServerStatusRepository>(),
                    sp.GetRequiredService<IHardwareMonitor>(),
                    proxyAppConfig
                );
            });

            builder.Services.AddSingleton<ProxyWorkloadMonitor>((sp) =>
            {
                return new ProxyWorkloadMonitor();
            });

            builder.Services.AddSingleton<NodeLifecycleManager>((sp) =>
            {
                var nodeLifecycleManager = new NodeLifecycleManager(
                    AppNodeTypeEnum.Proxy,
                    sp.GetRequiredService<IHostApplicationLifetime>(),
                    sp.GetRequiredService<IqraAppManager>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<RegionRepository>(),
                    sp.GetRequiredService<ProxyWorkloadMonitor>(),
                    sp.GetRequiredService<ILogger<NodeLifecycleManager>>()
                );

                nodeLifecycleManager.SetIdentity(proxyAppConfig.RegionId, proxyAppConfig.ServerId);

                return nodeLifecycleManager;
            });
        }

        private static void SetupHostedServices(WebApplicationBuilder builder, ProxyAppConfig proxyAppConfig)
        {
            // OutboundCallProcessorService
            builder.Services.AddSingleton<OutboundCallProcessorService>((sp) =>
            {
                return new OutboundCallProcessorService(
                    sp.GetRequiredService<ILogger<OutboundCallProcessorService>>(),
                    proxyAppConfig,
                    sp.GetRequiredService<OutboundCallProcessingOrchestrator>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<NodeLifecycleManager>()
                );
            });
            builder.Services.AddHostedService<OutboundCallProcessorService>((sp) =>
            {
                return sp.GetRequiredService<OutboundCallProcessorService>();
            });

            builder.Services.AddHostedService<SipProxyService>((sp) =>
            {
                return new SipProxyService(
                    sp.GetRequiredService<ILogger<SipProxyService>>(),
                    proxyAppConfig,
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<IUserUsageValidationManager>(),
                    sp.GetRequiredService<UserManager>()
                );
            });

            builder.Services.AddHostedService<NodeStateOrchestratorService>((sp) =>
            {
                return new NodeStateOrchestratorService(
                    AppNodeTypeEnum.Proxy,
                    sp.GetRequiredService<NodeLifecycleManager>(),
                    sp.GetRequiredService<IqraAppManager>(),
                    sp.GetRequiredService<ILogger<NodeStateOrchestratorService>>()
                );
            });

            builder.Services.AddHostedService<ServerMetricsMonitorService>((sp) =>
            {
                return new ServerMetricsMonitorService(
                    sp,
                    sp.GetRequiredService<ILogger<ServerMetricsMonitorService>>(),
                    AppNodeTypeEnum.Proxy,
                    sp.GetRequiredService<ProxyMetricsMonitor>(),
                    sp.GetRequiredService<NodeLifecycleManager>()
                );
            });
        }

        private static void SetupPostflight(WebApplication app)
        {
            var regionRepoistory = app.Services.GetRequiredService<RegionRepository>();
            regionRepoistory.SetLogger(app.Services.GetRequiredService<ILogger<RegionRepository>>());

            var regionManager = app.Services.GetRequiredService<RegionManager>();
            regionManager.SetLogger(app.Services.GetRequiredService<ILogger<RegionManager>>());

            var proxyWorkloadMonitor = app.Services.GetRequiredService<ProxyWorkloadMonitor>();
            proxyWorkloadMonitor.SetupDependencies(app.Services.GetRequiredService<OutboundCallProcessorService>());
        }
    }
}
