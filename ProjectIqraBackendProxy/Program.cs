using Google.Api;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Billing;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.User;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ProjectIqraBackendProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            ProxyAppConfig proxyAppConfig = new ProxyAppConfig()
            {
                RegionId = appConfig["Proxy:RegionId"],
                Identity = appConfig["Proxy:Identity"],
                OutboundProcessing = new ProxyAppOutboundProcessingConfig()
                {
                    DbFetchBatchSize = int.Parse(appConfig["Proxy:OutboundProcessing:DbFetchBatchSize"]),
                    PollingIntervalSeconds = int.Parse(appConfig["Proxy:OutboundProcessing:PollingIntervalSeconds"]),
                    ProcessingBatchSize = int.Parse(appConfig["Proxy:OutboundProcessing:ProcessingBatchSize"]),
                    ScheduleWindowMinutes = int.Parse(appConfig["Proxy:OutboundProcessing:ScheduleWindowMinutes"])
                }
            };

            // Repositories
            SetupRepositories(builder, appConfig);

            // Managers
            SetupManagers(builder, appConfig, proxyAppConfig);

            // HTTP Client
            builder.Services.AddHttpClient("CallManagerServerForward").ConfigureHttpMessageHandlerBuilder(builder => builder.PrimaryHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true });
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
            builder.Services.AddHttpClient("OutboundCallForwardClient");

            // Add services to the container
            builder.Services.AddControllers();

            // Add health checks
            builder.Services.AddHealthChecks();

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

            // Initalize All Singleton Services
            InitializeAllSingletonServices(app.Services);

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
            builder.Services.AddSingleton<RegionRepository>((sp) => {
                return new RegionRepository(
                    sp.GetRequiredService<ILogger<RegionRepository>>(),
                    appConfig["AppDatabase:ConnectionString"],
                    appConfig["AppDatabase:DatabaseName"]
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
            builder.Services.AddSingleton<UserRepository>((sp) =>
            {
                return new UserRepository(
                    sp.GetRequiredService<ILogger<UserRepository>>(),
                    appConfig["UserDatabase:ConnectionString"],
                    appConfig["UserDatabase:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<PlanRepository>((sp) =>
            {
                return new PlanRepository(
                    sp.GetRequiredService<ILogger<PlanRepository>>(),
                    appConfig["PlanRepository:ConnectionString"],
                    appConfig["PlanRepository:DatabaseName"]
                );
            });
            builder.Services.AddSingleton<ConversationStateRepository>((sp) =>
            {
                return new ConversationStateRepository(
                    appConfig["ConversationStateRepository:ConnectionString"],
                    appConfig["ConversationStateRepository:DatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });
            builder.Services.AddSingleton<OutboundCallQueueRepository>(sp =>
            {
                return new OutboundCallQueueRepository(
                    appConfig["CallQueueRepository:ConnectionString"],
                    appConfig["CallQueueRepository:DatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>()
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, ProxyAppConfig proxyAppConfig)
        {
            builder.Services.AddSingleton<RegionManager>((sp) =>
            {
                return new RegionManager(
                    sp.GetRequiredService<ILogger<RegionManager>>(),
                    sp.GetRequiredService<RegionRepository>()
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
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    null,
                    null,
                    null,
                    null,
                    null,
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
            builder.Services.AddSingleton<ServerSelectionManager>((sp) =>
            {
                return new ServerSelectionManager(
                    sp.GetRequiredService<ILogger<ServerSelectionManager>>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ServerLiveStatusChannelRepository>(),
                    sp.GetRequiredService<DistributedLockRepository>()
                );
            });
            builder.Services.AddSingleton<InboundCallManager>((sp) =>
            {
                return new InboundCallManager(
                    sp.GetRequiredService<ILogger<InboundCallManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<UserManager>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<PlanManager>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<TwilioManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<BillingValidationManager>()
                );
            });
            builder.Services.AddSingleton<UserManager>((sp) =>
            {
                return new UserManager(
                    sp.GetRequiredService<ILogger<UserManager>>(),
                    null,
                    null,
                    sp.GetRequiredService<UserRepository>(),
                    null
                );
            });
            builder.Services.AddSingleton<PlanManager>((sp) =>
            {
                return new PlanManager(
                    sp.GetRequiredService<ILogger<PlanManager>>(),
                    sp.GetRequiredService<PlanRepository>()
                );
            });
            builder.Services.AddSingleton<BillingValidationManager>((sp) =>
            {
                return new BillingValidationManager(
                    sp.GetRequiredService<ILogger<BillingValidationManager>>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<UserManager>(),
                    sp.GetRequiredService<PlanManager>(),
                    sp.GetRequiredService<ConversationStateRepository>()
                );
            });
            builder.Services.AddSingleton<OutboundCallProcessingOrchestrator>((sp) =>
            {
                return new OutboundCallProcessingOrchestrator(
                    sp.GetRequiredService<ILogger<OutboundCallProcessingOrchestrator>>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<BillingValidationManager>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddHostedService<OutboundCallProcessorService>((sp) =>
            {
                return new OutboundCallProcessorService(
                    sp.GetRequiredService<ILogger<OutboundCallProcessorService>>(),
                    proxyAppConfig,
                    sp.GetRequiredService<OutboundCallProcessingOrchestrator>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>()
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
