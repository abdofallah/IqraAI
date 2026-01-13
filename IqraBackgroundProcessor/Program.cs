using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Interfaces.Modules;
using IqraCore.Interfaces.Server;
using IqraInfrastructure.HostedServices.Call;
using IqraInfrastructure.HostedServices.Conversation;
using IqraInfrastructure.HostedServices.Lifecycle;
using IqraInfrastructure.HostedServices.Metrics;
using IqraInfrastructure.HostedServices.RAG;
using IqraInfrastructure.HostedServices.TTS;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.Node;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.Server.Metrics.Monitor;
using IqraInfrastructure.Managers.Server.Metrics.Monitor.Hardware;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.S3Storage;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.TTS.Cache;
using IqraInfrastructure.Utilities.App;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using MongoDB.Driver;
using System.Reflection;
using System.Runtime.InteropServices;
using WebSocketSharp;

namespace IqraBackgroundProcessor
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudBackgroundAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && WindowsServiceHelpers.IsWindowsService())
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "IqraAI.Background";
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
            var backgroundAppConfig = new BackgroundAppConfig()
            {
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
                DefaultS3StorageRegionId = appConfig["S3Storage:DefaultStorageRegionId"],
                ApiKey = appConfig["Security:ApiKey"],
            };
            builder.Services.AddSingleton<BackgroundAppConfig>(backgroundAppConfig);

            // Load Cloud Asembly
            if (backgroundAppConfig.IsCloudVersion)
            {
                LoadCloudAssembly();
            }
            // Load Cloud Configuration
            if (backgroundAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupConfiguration(builder.Services, appConfig);
            }

            // Repositories
            await SetupRepositories(builder, appConfig, backgroundAppConfig);
            if (backgroundAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupRepositories(builder.Services, appConfig);
            }

            // Managers
            SetupManagers(builder, appConfig, backgroundAppConfig);
            if (backgroundAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupManagers(builder.Services, appConfig);
            }

            // Hosted Services
            SetupHostedServices(builder, appConfig);
            if (backgroundAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupHostedServices(builder.Services);
            }

            // HttpClients
            builder.Services.AddHttpClient();

            // Add services to the container
            builder.Services.AddControllers();

            var app = builder.Build();

            // Initalize All Singleton Services
            SingletonWarmupHelper.InitializeAllSingletonServices<Program>(app.Services);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // BOOTSTRAP: Initial Check
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Iqra Background Bootstrapping...");

                // Make sure background node is configured correctly
                var appRepository = scope.ServiceProvider.GetRequiredService<AppRepository>();
                var coreNodesConfig = await appRepository.GetCoreNodesConfig();
                if (
                    coreNodesConfig == null ||
                    string.IsNullOrEmpty(coreNodesConfig.BackgroundNodeEndpoint) ||
                    string.IsNullOrEmpty(coreNodesConfig.BackgroundNodeApiKey)
                ) {
                    throw new Exception("Background Node is not configured in the admin dashboard. Please configure the endpoint and apikey in the infrastructure dashboard.");
                }
                if (coreNodesConfig.BackgroundNodeApiKey != backgroundAppConfig.ApiKey)
                {
                    throw new Exception("Mismatch between ApiKey in config and ApiKey in database.");
                }

                // Make sure no other background node is running
                var metricsManager = scope.ServiceProvider.GetRequiredService<ServerMetricsManager>();
                var frontendAlreadyRunning = await metricsManager.CheckAnyBackgroundNodeRunning();
                if (frontendAlreadyRunning)
                {
                    throw new Exception("Server Metrics Manager found that a background node is already running.\nThis could be a false positive too, but it's better to be safe than sorry.\n\nGiven the redis database takes 30seconds to clear previous running background status, if the issue presits for more than a minute, there must be another background node running.");
                }

                // Perform Initial Startup Integrity Check
                var startupIntregity = scope.ServiceProvider.GetRequiredService<StartupIntegrityCheckService>();
                await startupIntregity.CheckAsync();

                logger.LogInformation("Iqra Background Bootstrapping Completed.");
            }

            app.Run();
        }

        private static async Task SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig, BackgroundAppConfig backgroundAppConfig)
        {
            string redisConnectionString = appConfig["RedisDatabase:Endpoint"]!;
            string redisConfigPassword = appConfig["RedisDatabase:Password"]!;
            if (!string.IsNullOrEmpty(redisConfigPassword))
            {
                redisConnectionString += $",password={redisConfigPassword}";
            }

            IMongoClient mongoClient = new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            RegionRepository regionRepository = new RegionRepository(mongoClient);
            var allRegionServers = await regionRepository.GetRegions();
            S3StorageClientFactory s3StorageClientFactory = new S3StorageClientFactory(backgroundAppConfig.DefaultS3StorageRegionId);
            var s3StorageInitResult = await s3StorageClientFactory.Initalize(allRegionServers);
            if (!s3StorageInitResult.Success)
            {
                throw new Exception($"[{s3StorageInitResult.Code}] {s3StorageInitResult.Message}");
            }

            builder.Services.AddSingleton<IMongoClient>(mongoClient);
            builder.Services.AddSingleton<S3StorageClientFactory>(s3StorageClientFactory);

            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<MilvusKnowledgeBaseClient>((sp) =>
            {
                return new MilvusKnowledgeBaseClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    new MilvusOptions()
                    {
                        Endpoint = appConfig["Milvus:Endpoint"],
                        Username = appConfig["Milvus:Username"],
                        Password = appConfig["Milvus:Password"]
                    },
                    sp.GetRequiredService<ILogger<MilvusKnowledgeBaseClient>>()
                );
            });

            builder.Services.AddSingleton<CallQueueLogsRepository>((sp) =>
            {
                return new CallQueueLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<CallQueueLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<InboundCallQueueRepository>((sp) =>
            {
                return new InboundCallQueueRepository(
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<ConversationStateRepository>((sp) =>
            {
                return new ConversationStateRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });

            builder.Services.AddSingleton<TTSAudioCacheMetadataRepository>((sp) =>
            {
                return new TTSAudioCacheMetadataRepository(
                    sp.GetRequiredService<ILogger<TTSAudioCacheMetadataRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<TTSAudioCacheStorageRepository>((sp) =>
            {
                return new TTSAudioCacheStorageRepository(
                    sp.GetRequiredService<ILogger<TTSAudioCacheStorageRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
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

            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                return new ServerStatusRepository(
                    sp.GetRequiredService<ILogger<ServerStatusRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<RegionRepository>((sp) => {
                regionRepository.SetLogger(sp.GetRequiredService<ILogger<RegionRepository>>());
                return regionRepository;
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, BackgroundAppConfig backgroundAppConfig)
        {
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
                    AppNodeTypeEnum.Background
                );
            });

            builder.Services.AddSingleton<ServerMetricsManager>((sp) =>
            {
                return new ServerMetricsManager(
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<ServerStatusRepository>()
                );
            });

            builder.Services.AddSingleton<ServerMetricsMonitor>((sp) =>
            {
                return new ServerMetricsMonitor(
                    sp.GetRequiredService<ILogger<BackendMetricsMonitor>>(),
                    new ServerStatusData()
                    {
                        NodeId = "Background",
                        Type = AppNodeTypeEnum.Background,
                        LastUpdated = DateTime.UtcNow,
                        CpuUsagePercent = 0,
                        MemoryUsagePercent = 0,
                        NetworkDownloadMbps = 0,
                        NetworkUploadMbps = 0
                    },
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<ServerStatusRepository>(),
                    sp.GetRequiredService<IHardwareMonitor>()
                );
            });

            builder.Services.AddSingleton<NodeLifecycleManager>((sp) =>
            {
                return new NodeLifecycleManager(
                    AppNodeTypeEnum.Proxy,
                    sp.GetRequiredService<IHostApplicationLifetime>(),
                    sp.GetRequiredService<IqraAppManager>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<RegionRepository>(),
                    null,
                    sp.GetRequiredService<ILogger<NodeLifecycleManager>>()
                );
            });
        }

        private static void SetupHostedServices(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            string redisConnectionString = appConfig["RedisDatabase:Endpoint"]!;
            string redisConfigPassword = appConfig["RedisDatabase:Password"]!;
            if (!string.IsNullOrEmpty(redisConfigPassword))
            {
                redisConnectionString += $",password={redisConfigPassword}";
            }

            builder.Services.AddHostedService<CallQueueCleanupService>((sp) =>
            {
                return new CallQueueCleanupService(
                    sp.GetRequiredService<ILogger<CallQueueCleanupService>>(),
                    sp.GetRequiredService<InboundCallQueueRepository>()
                );
            });

            builder.Services.AddHostedService<ConversationTimeoutCleanupService>((sp) =>
            {
                return new ConversationTimeoutCleanupService(
                    sp.GetRequiredService<ILogger<ConversationTimeoutCleanupService>>(),
                    sp.GetRequiredService<ConversationStateRepository>()
                );
            });

            builder.Services.AddHostedService<OrphanedTTSAudioCacheCleanupService>((sp) =>
            {
                return new OrphanedTTSAudioCacheCleanupService(
                     sp.GetRequiredService<ILogger<OrphanedTTSAudioCacheCleanupService>>(),
                     sp.GetRequiredService<TTSAudioCacheMetadataRepository>(),
                     sp.GetRequiredService<TTSAudioCacheStorageRepository>()
                );
            });

            builder.Services.AddSingleton<KnowledgeBaseStaleCollectionsUnloadService>((sp) =>
            {
                return new KnowledgeBaseStaleCollectionsUnloadService(
                    sp.GetRequiredService<ILogger<KnowledgeBaseStaleCollectionsUnloadService>>(),
                    sp.GetRequiredService<MilvusKnowledgeBaseClient>(),
                    appConfig["Milvus:Database"],
                    new RedisConnectionFactory(
                        $"{redisConnectionString},defaultDatabase={KnowledgeBaseCollectionsLoadManager.DATABASE_INDEX}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });

            builder.Services.AddHostedService<NodeStateOrchestratorService>((sp) =>
            {
                return new NodeStateOrchestratorService(
                    AppNodeTypeEnum.Background,
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
                    AppNodeTypeEnum.Background,
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<NodeLifecycleManager>()
                );
            });
        }   

        private static void LoadCloudAssembly()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string cloudDllPath = Path.Combine(folder, "IqraBackgroundProcessor.Cloud.dll");
            if (!File.Exists(cloudDllPath)) throw new Exception("Cloud DLL missing");

            _cloudAssembly = Assembly.LoadFrom(cloudDllPath);
            var type = _cloudAssembly.GetTypes().FirstOrDefault(t => typeof(ICloudBackgroundAppInitalizer).IsAssignableFrom(t) && !t.IsInterface);
            if (type != null)
            {
                _cloudModule = (ICloudBackgroundAppInitalizer)Activator.CreateInstance(type);
            }
            if (_cloudModule == null) throw new Exception("Cloud module not found");
        }
    }
}
