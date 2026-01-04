using IqraCore.Entities.Server.Configuration;
using IqraCore.Interfaces.Modules;
using IqraInfrastructure.HostedServices.Call;
using IqraInfrastructure.HostedServices.Conversation;
using IqraInfrastructure.HostedServices.TTS;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.S3Storage;
using IqraInfrastructure.Repositories.TTS.Cache;
using MongoDB.Driver;
using System.Reflection;

namespace IqraBackgroundProcessor
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudBackgroundAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            var backgroundAppConfig = new BackgroundAppConfig()
            {
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
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

            var app = builder.Build();

            // Initalize All Singleton Services
            InitializeAllSingletonServices(app.Services);

            app.Run();
        }

        private static async Task SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig, BackgroundAppConfig backgroundAppConfig)
        {
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
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, BackgroundAppConfig backgroundAppConfig)
        {
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

        private static List<ServiceDescriptor> GetTypes(IServiceProvider provider)
        {
            ServiceProvider serviceProvider = provider as ServiceProvider;
            var callSiteFactory = serviceProvider.GetType().GetProperty("CallSiteFactory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serviceProvider);
            var serviceDescriptors = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(callSiteFactory) as ServiceDescriptor[];
            return serviceDescriptors.ToList();
        }
    }
}
