using IqraCore.Entities.App.Enum;
using IqraCore.Entities.App.Lifecycle;
using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Modules;
using IqraCore.Interfaces.Node;
using IqraCore.Interfaces.Server;
using IqraCore.Interfaces.User;
using IqraCore.Interfaces.Validation;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Helpers.Conventions;
using IqraInfrastructure.Helpers.Providers;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Helpers.Validation;
using IqraInfrastructure.HostedServices.Lifecycle;
using IqraInfrastructure.HostedServices.Metrics;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.FlowApp;
using IqraInfrastructure.Managers.Infrastructure;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.KnowledgeBase.Retrieval;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Mail;
using IqraInfrastructure.Managers.Node;
using IqraInfrastructure.Managers.RAG.Extractors;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Processors;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.Server.Metrics.Monitor;
using IqraInfrastructure.Managers.Server.Metrics.Monitor.Hardware;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Embedding;
using IqraInfrastructure.Repositories.Embedding.Cache;
using IqraInfrastructure.Repositories.FlowApp;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.RAG;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Rerank;
using IqraInfrastructure.Repositories.S3Storage;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WebSession;
using IqraInfrastructure.Utilities.App;
using IqraInfrastructure.Utilities.Templating;
using IqraInfrastructure.Utilities.Validation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using MongoDB.Driver;
using ProjectIqraFrontend.Middlewares;
using ProjectIqraFrontend.Transformer;
using Scalar.AspNetCore;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProjectIqraFrontend
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudFrontendAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && WindowsServiceHelpers.IsWindowsService())
            {
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "IqraAI.Frontend";
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
            var frontendAppConfig = new FrontendAppConfig()
            {
                DefaultS3StorageRegionId = appConfig["S3Storage:DefaultStorageRegionId"],
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
            };
            builder.Services.AddSingleton<FrontendAppConfig>(frontendAppConfig);
            builder.Services.AddScoped<WhiteLabelContext>();

            // Load Cloud Asembly
            if (frontendAppConfig.IsCloudVersion)
            {
                LoadCloudAssembly();
            }
            // Load Cloud Configuration
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupConfiguration(builder.Services, appConfig);
            }

            // Repositories
            await SetupRepositories(builder, appConfig, frontendAppConfig);
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupRepositories(builder.Services, appConfig);
            }

            // Managers
            SetupManagers(builder, appConfig, frontendAppConfig);
            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupManagers(builder.Services, appConfig);
            }

            // Hosted Services
            SetupHostedServices(builder);

            // HTTP Client
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("ModemTelClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddHttpClient("TwilioClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
            // MilvusClient
            builder.Services.AddHttpClient("MilvusClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("ProxyForwarder");
            builder.Services.AddHttpClient("WebSessionForwardClient");
            // UnstructuredClient
            builder.Services.AddHttpClient("UnstructuredClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(200);
                client.BaseAddress = new Uri(appConfig["Unstructured:EndPoint"]);
                client.DefaultRequestHeaders.Add("unstructured-api-key", appConfig["Unstructured:ApiKey"]);
            });

            // Memory Cache
            builder.Services.AddMemoryCache();

            // Controllers with custom Middleware
            var customJSONMiddleware = new EndpointAwareJsonConverter();
            LoadCloudVsOpensourceControllers(builder, customJSONMiddleware, frontendAppConfig);

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(p => p
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });

            // Configure Forwarded Headers
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                
                // Known Proxies
                options.KnownProxies.Clear();
                var knownProxies = appConfig.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
                if (knownProxies != null)
                {
                    foreach (var proxy in knownProxies)
                    {
                        if (IPAddress.TryParse(proxy, out var ipAddress))
                        {
                            options.KnownProxies.Add(ipAddress);
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid known proxy: {proxy}. Unable to parse ip address.");
                        }
                    }
                }
            });

            // OpenAPI
            builder.Services.AddTransient<OpenApiDocumentTransformer>();
            builder.Services.AddTransient<OpenApiEnumSchemaTransformer>();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<OpenApiDocumentTransformer>();
                options.AddSchemaTransformer<OpenApiEnumSchemaTransformer>();
            });   

            var app = builder.Build();

            // Initalize All Singleton Services
            SingletonWarmupHelper.InitializeAllSingletonServices<Program>(app.Services);

            // SetupDependencies
            SetupDependencies(app.Services);
            // Assign the HttpContextAccessor to JSON Middleware
            var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            customJSONMiddleware.SetHttpContextAccessor(httpContextAccessor);

            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.ConfigureStaticFiles(app.Environment);
            }

            app.UseForwardedHeaders();
            app.UseRouting();

            if (frontendAppConfig.IsCloudVersion)
            {
                _cloudModule!.UseWhiteLabelResolver(app);
            }

            app.UseCors();

            app.UseMiddleware<InstallationMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapOpenApi();
            app.MapScalarApiReference("/api", options =>
            {
                options.WithTitle("Iqra AI API");
                options.WithTheme(ScalarTheme.Saturn);
                options.EnableDarkMode();
            });

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            // BOOTSTRAP: Initial Check & Auto-Migration
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Iqra Frontend Bootstrapping...");

                // Make sure no other frontend node is running
                var metricsManager = scope.ServiceProvider.GetRequiredService<ServerMetricsManager>();
                var frontendAlreadyRunning = await metricsManager.CheckAnyFrontendNodeRunning();
                if (frontendAlreadyRunning)
                {
                    throw new Exception("Server Metrics Manager found that a frontend node is already running.\nThis could be a false positive too, but it's better to be safe than sorry.\n\nGiven the redis database takes 30seconds to clear previous running frontend status, if the issue presits for more than a minute, there must be another frontend node running.");
                }

                // Perform Initial Startup Integrity Check
                var startupIntregity = scope.ServiceProvider.GetRequiredService<StartupIntegrityCheckService>();
                await startupIntregity.CheckAsync();

                // Perform Migration Check if needed
                var appManager = scope.ServiceProvider.GetRequiredService<IqraAppManager>();
                if (appManager.CurrentStatus == AppLifecycleStatus.VersionMismatch)
                {
                    await appManager.PerformAutoMigrationAsync();
                }

                logger.LogInformation("Iqra Frontend Bootstrapping Completed.");
            }

            app.Run();
        }

        private static async Task SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig, FrontendAppConfig frontendAppConfig)
        {
            // Build Base Services
            IMongoClient mongoClient = new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            RegionRepository regionRepository = new RegionRepository(mongoClient);
            var allRegionServers = await regionRepository.GetRegions();
            S3StorageClientFactory s3StorageClientFactory = new S3StorageClientFactory(frontendAppConfig.DefaultS3StorageRegionId);
            var s3StorageInitResult = await s3StorageClientFactory.Initalize(allRegionServers);
            if (!s3StorageInitResult.Success)
            {
                throw new Exception($"[{s3StorageInitResult.Code}] {s3StorageInitResult.Message}");
            }

            builder.Services.AddSingleton<IMongoClient>(mongoClient);
            builder.Services.AddSingleton<S3StorageClientFactory>(s3StorageClientFactory);
            builder.Services.AddSingleton<MilvusKnowledgeBaseClient>((sp) =>
            {
                return new MilvusKnowledgeBaseClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    new MilvusOptions() { 
                        Endpoint = appConfig["Milvus:Endpoint"],
                        Username = appConfig["Milvus:Username"],
                        Password = appConfig["Milvus:Password"]
                    },
                    sp.GetRequiredService<ILogger<MilvusKnowledgeBaseClient>>()
                );
            });

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

            builder.Services.AddSingleton<RegionRepository>((sp) => {
                regionRepository.SetLogger(sp.GetRequiredService<ILogger<RegionRepository>>());
                return regionRepository;
            });

            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
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

            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
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

            builder.Services.AddSingleton<LLMProviderRepository>((sp) =>
            {
                return new LLMProviderRepository(
                    sp.GetRequiredService<ILogger<LLMProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<STTProviderRepository>((sp) =>
            {
                return new STTProviderRepository(
                    sp.GetRequiredService<ILogger<STTProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<TTSProviderRepository>((sp) =>
            {
                return new TTSProviderRepository(
                    sp.GetRequiredService<ILogger<TTSProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<EmbeddingProviderRepository>((sp) =>
            {
                return new EmbeddingProviderRepository(
                    sp.GetRequiredService<ILogger<EmbeddingProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<RerankProviderRepository>((sp) =>
            {
                return new RerankProviderRepository(
                    sp.GetRequiredService<ILogger<RerankProviderRepository>>(),
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

            builder.Services.AddSingleton<ConversationStateLogsRepository>(sp =>
            {
                return new ConversationStateLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<ConversationStateLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueRepository>((sp) =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueGroupRepository>((sp) =>
            {
                return new OutboundCallQueueGroupRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<OutboundCallQueueGroupRepository>>()
                );
            });

            builder.Services.AddSingleton<UserSessionRepository>((sp) =>
            {
                return new UserSessionRepository(
                    sp.GetRequiredService<ILogger<UserSessionRepository>>(),
                    new RedisConnectionFactory(
                        $"{redisConnectionString},defaultDatabase={UserSessionRepository.DATABASE_INDEX}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });

            builder.Services.AddSingleton<IntegrationsLogoRepository>((sp) =>
            {
                return new IntegrationsLogoRepository(
                    sp.GetRequiredService<ILogger<IntegrationsLogoRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessLogoRepository>((sp) =>
            {
                return new BusinessLogoRepository(
                    sp.GetRequiredService<ILogger<BusinessLogoRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<BusinessConversationAudioRepository>((sp) =>
            {
                return new BusinessConversationAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessConversationAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
                );
            });

            builder.Services.AddSingleton<UserUsageRepository>((sp) =>
            {
                return new UserUsageRepository(
                    sp.GetRequiredService<ILogger<UserUsageRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<BusinessKnowledgeBaseDocumentRepository>((sp) =>
            {
                return new BusinessKnowledgeBaseDocumentRepository(
                    sp.GetRequiredService<ILogger<BusinessKnowledgeBaseDocumentRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<KnowledgeBaseVectorRepository>((sp) =>
            {
                return new KnowledgeBaseVectorRepository(
                    sp.GetRequiredService<MilvusKnowledgeBaseClient>(),
                    appConfig["Milvus:Database"],
                    sp.GetRequiredService<ILogger<KnowledgeBaseVectorRepository>>()
                );
            });

            builder.Services.AddSingleton<RAGKeywordStore>((sp) =>
            {
                return new RAGKeywordStore(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<KeywordExtractor>(),
                    sp.GetRequiredService<ILogger<RAGKeywordStore>>()
                );
            });

            builder.Services.AddSingleton<EmbeddingCacheRepository>((sp) =>
            {
                return new EmbeddingCacheRepository(
                    sp.GetRequiredService<ILogger<EmbeddingCacheRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<WebSessionRepository>((sp) =>
            {
                return new WebSessionRepository(
                    sp.GetRequiredService<ILogger<WebSessionRepository>>(),
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

            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                return new ServerStatusRepository(
                    sp.GetRequiredService<ILogger<ServerStatusRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
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

            builder.Services.AddSingleton<FlowAppRepository>((sp) =>
            {
                return new FlowAppRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<FlowAppRepository>>()
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, FrontendAppConfig frontendAppConfig)
        {
            string redisConnectionString = appConfig["RedisDatabase:Endpoint"]!;
            string redisConfigPassword = appConfig["RedisDatabase:Password"]!;
            if (!string.IsNullOrEmpty(redisConfigPassword))
            {
                redisConnectionString += $",password={redisConfigPassword}";
            }

            if (!frontendAppConfig.IsCloudVersion)
            {
                builder.Services.AddScoped<IUserRegistrationManager, UserRegistrationManager>((sp) =>
                {
                    return new UserRegistrationManager(
                        sp.GetRequiredService<UserApiKeyProcessor>(),
                        sp.GetRequiredService<UserRepository>()
                    );
                });

                builder.Services.AddSingleton<ISessionValidationAndPermissionHelper, SessionValidationAndPermissionHelper>((sp) =>
                {
                    return new SessionValidationAndPermissionHelper(
                        sp.GetRequiredService<UserApiKeyManager>(),
                        sp.GetRequiredService<UserManager>(),
                        sp.GetRequiredService<BusinessManager>(),
                        sp.GetRequiredService<UserRepository>(),
                        sp.GetRequiredService<IUserBusinessPermissionHelper>()
                    );
                });

                builder.Services.AddSingleton<IUserUsageValidationManager, UserUsageValidationManager>((sp) =>
                {
                    return new UserUsageValidationManager();
                });
            }

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

            builder.Services.AddSingleton<IUserBusinessPermissionHelper, UserBusinessPermissionHelper>((sp) =>
            {
                return new UserBusinessPermissionHelper();
            });
            
            builder.Services.AddSingleton<EmailManager>((sp) =>
            {
                return new EmailManager(
                    sp.GetRequiredService<ILogger<EmailManager>>(),
                    new EmailSettings()
                    {
                        Host = appConfig["MailSMTP:Host"],
                        Port = int.Parse(appConfig["MailSMTP:Port"]),
                        Username = appConfig["MailSMTP:Username"],
                        Password = appConfig["MailSMTP:Password"],
                        FromEmail = appConfig["MailSMTP:FromEmail"],
                        FromName = appConfig["MailSMTP:FromName"]
                    }
                );
            });
            builder.Services.AddSingleton<TextSplitterFactory>((sp) =>
            {
                return new TextSplitterFactory();
            });
            builder.Services.AddSingleton<IndexProcessorFactory>((sp) =>
            {
                return new IndexProcessorFactory(
                    sp.GetRequiredService<TextSplitterFactory>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<KeywordExtractor>()
                );
            });
            builder.Services.AddSingleton<ExtractProcessor>((sp) =>
            {
                return new ExtractProcessor(
                    sp.GetRequiredService<IHttpClientFactory>()
                );
            });
            builder.Services.AddSingleton<LanguagesManager>((sp) =>
            {
                return new LanguagesManager(
                    sp.GetRequiredService<ILogger<LanguagesManager>>(),
                    sp.GetRequiredService<LanguagesRepository>()
                );
            });
            builder.Services.AddSingleton<RegionManager>((sp) =>
            {
                var regionManager = new RegionManager(
                    sp.GetRequiredService<RegionRepository>()
                );

                regionManager.SetLogger(sp.GetRequiredService<ILogger<RegionManager>>());

                return regionManager;
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
                    sp.GetRequiredService<IntegrationsLogoRepository>(),
                    integrationFieldsEncryptionService,
                    sp.GetRequiredService<S3StorageClientFactory>()
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
            builder.Services.AddSingleton<UserManager>((sp) =>
            {
                return new UserManager(
                    sp.GetRequiredService<ILogger<UserManager>>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<UserSessionRepository>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<EmailManager>(),
                    sp.GetRequiredService<UserApiKeyProcessor>(),
                    appConfig["URL"]
                );
            });
            builder.Services.AddSingleton<IntegrationConfigurationManager>((sp) =>
            {
                return new IntegrationConfigurationManager(
                    sp.GetRequiredService<STTProviderManager>(),
                    sp.GetRequiredService<TTSProviderManager>(),
                    sp.GetRequiredService<LLMProviderManager>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<RerankProviderManager>()
                );
            });
            builder.Services.AddSingleton<BusinessManager>((sp) =>
            {
                return new BusinessManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<IMongoClient>(),
                    new BusinessManagerInitalizationSettings()
                    { 
                        InitalizeAgentsManager = true,
                        InitalizeScriptsManager = true,
                        InitalizeCacheManager = true,
                        InitalizeContextManager = true,
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeRoutesManager = true,
                        InitalizeSettingsManager = true,
                        InitalizeToolsManager = true,
                        InitalizeConversationsManager = true,
                        InitalizeMakeCallManager = true,
                        InitalizeKnowledgeBaseManager = true,
                        InitalizeCampaignManager = true,
                        InitalizeWebSessionManager = true,
                        InitalizePostAnalysisManager = true,
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    sp.GetRequiredService<BusinessLogoRepository>(),
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
                    sp.GetRequiredService<ModemTelManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<BusinessConversationAudioRepository>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<OutboundCallQueueGroupRepository>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<LanguagesManager>(),
                    sp.GetRequiredService<TwilioManager>(),
                    sp.GetRequiredService<IntegrationConfigurationManager>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<IndexProcessorFactory>(),
                    sp.GetRequiredService<ExtractProcessor>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<KeywordExtractor>(),
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<WebSessionRepository>(),
                    sp.GetRequiredService<IUserUsageValidationManager>(),
                    sp.GetRequiredService<ServerSelectionManager>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<S3StorageClientFactory>(),
                    sp.GetRequiredService<FlowAppManager>()
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
            builder.Services.AddSingleton<EmbeddingProviderManager>((sp) =>
            {
                return new EmbeddingProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<EmbeddingProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            builder.Services.AddSingleton<RerankProviderManager>((sp) =>
            {
                return new RerankProviderManager(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<RerankProviderRepository>(),
                    sp.GetRequiredService<IntegrationsManager>()
                );
            });
            
            builder.Services.AddSingleton<UserUsageManager>((sp) =>
            {
                return new UserUsageManager(
                    sp.GetRequiredService<ILogger<UserUsageManager>>(),
                    sp.GetRequiredService<UserUsageRepository>()
                );
            });
            
            builder.Services.AddSingleton<UserApiKeyManager>((sp) =>
            {
                return new UserApiKeyManager(
                    sp.GetRequiredService<ILogger<UserApiKeyManager>>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<UserApiKeyProcessor>()
                );
            });
            builder.Services.AddSingleton<UserApiKeyProcessor>((sp) =>
            {
                AES256EncryptionService userApiKeyEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["UserApiKeys:ApiKeyEncryptionKey"]
                );
                AES256EncryptionService userApiKeyPayloadEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["UserApiKeys:PayloadEncryptionKey"]
                );
                return new UserApiKeyProcessor(
                    appConfig["User:EmailHashPepper"],
                    userApiKeyEncryptionService,
                    userApiKeyPayloadEncryptionService
                );
            });
            builder.Services.AddSingleton<KnowledgeBaseRetrievalManagerFactory>((sp) =>
            {
                return new KnowledgeBaseRetrievalManagerFactory(
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<KnowledgeBaseVectorRepository>(),
                    sp.GetRequiredService<RAGKeywordStore>(),
                    sp.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                    sp.GetRequiredService<EmbeddingProviderManager>(),
                    sp.GetRequiredService<RerankProviderManager>(),
                    sp.GetRequiredService<KnowledgeBaseCollectionsLoadManager>(),
                    sp.GetRequiredService<EmbeddingCacheManager>()
                );
            });
            builder.Services.AddSingleton<KeywordExtractor>((sp) =>
            {
                // TODO load the keywords from a .json file
                return new KeywordExtractor();
            });
            builder.Services.AddSingleton<KnowledgeBaseCollectionsLoadManager>((sp) =>
            {
                return new KnowledgeBaseCollectionsLoadManager(
                    sp.GetRequiredService<ILogger<KnowledgeBaseCollectionsLoadManager>>(),
                    sp.GetRequiredService<MilvusKnowledgeBaseClient>(),
                    appConfig["Milvus:Database"],
                    new RedisConnectionFactory(
                        $"{redisConnectionString},defaultDatabase={KnowledgeBaseCollectionsLoadManager.DATABASE_INDEX}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
                );
            });
            builder.Services.AddSingleton<EmbeddingCacheManager>((sp) =>
            {
                return new EmbeddingCacheManager(
                    sp.GetRequiredService<ILogger<EmbeddingCacheManager>>(),
                    sp.GetRequiredService<EmbeddingCacheRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>()
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

            builder.Services.AddSingleton<ScribanRenderService>((sp) =>
            {
                return new ScribanRenderService(
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<ScribanRenderService>>()
                );
            });

            builder.Services.AddSingleton<NJsonSchemaValidator>((sp) =>
            {
                return new NJsonSchemaValidator(
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<NJsonSchemaValidator>>()
                );
            });

            builder.Services.AddSingleton<FlowAppManager>((sp) =>
            {
                AES256EncryptionService integrationFieldsEncryptionService = new AES256EncryptionService(
                    sp.GetRequiredService<ILogger<AES256EncryptionService>>(),
                    appConfig["Integrations:EncryptionKey"]
                );
                return new FlowAppManager(
                    sp,
                    sp.GetRequiredService<ScribanRenderService>(),
                    sp.GetRequiredService<NJsonSchemaValidator>(),
                    integrationFieldsEncryptionService,
                    sp.GetRequiredService<FlowAppRepository>(),
                    sp.GetRequiredService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<FlowAppManager>>()
                );
            });

            builder.Services.AddSingleton<IqraAppManager>((sp) =>
            {
                return new IqraAppManager(
                    sp,
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<ILogger<IqraAppManager>>()
                );
            });

            builder.Services.AddSingleton<StartupIntegrityCheckService>((sp) =>
            {
                return new StartupIntegrityCheckService(
                    sp,
                    sp.GetRequiredService<ILogger<StartupIntegrityCheckService>>(),
                    AppNodeTypeEnum.Frontend
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
                        NodeId = "Frontend",
                        Type = AppNodeTypeEnum.Frontend,
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
                    AppNodeTypeEnum.Frontend,
                    sp.GetRequiredService<IHostApplicationLifetime>(),
                    sp.GetRequiredService<IqraAppManager>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<RegionRepository>(),
                    null,
                    sp.GetRequiredService<ILogger<NodeLifecycleManager>>()
                );
            });

            builder.Services.AddSingleton<InfrastructureManager>((sp) =>
            {
                return new InfrastructureManager(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<ServerMetricsManager>(),
                    sp.GetRequiredService<ServerStatusRepository>(),
                    sp.GetRequiredService<ILogger<InfrastructureManager>>()
                );
            });
        }

        private static void SetupHostedServices(WebApplicationBuilder builder)
        {
            builder.Services.AddHostedService<NodeStateOrchestratorService>((sp) =>
            {
                return new NodeStateOrchestratorService(
                    AppNodeTypeEnum.Frontend,
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
                    AppNodeTypeEnum.Frontend,
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<NodeLifecycleManager>()
                );
            });
        }

        private static void SetupDependencies(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IntegrationConfigurationManager>().SetupDependencies(
                serviceProvider.GetRequiredService<BusinessManager>().GetIntegrationsManager()
            );

            serviceProvider.GetRequiredService<RegionManager>().SetDependencies(
                serviceProvider.GetRequiredService<ServerMetricsManager>()
            );
        }

        private static void LoadCloudAssembly()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string cloudDllPath = Path.Combine(folder, "ProjectIqraFrontend.Cloud.dll");
            if (!File.Exists(cloudDllPath)) throw new Exception("Cloud DLL missing");

            _cloudAssembly = Assembly.LoadFrom(cloudDllPath);
            var type = _cloudAssembly.GetTypes().FirstOrDefault(t => typeof(ICloudFrontendAppInitalizer).IsAssignableFrom(t) && !t.IsInterface);
            if (type != null)
            {
                _cloudModule = (ICloudFrontendAppInitalizer)Activator.CreateInstance(type);
            }
            if (_cloudModule == null) throw new Exception("Cloud module not found");
        }

        private static void LoadCloudVsOpensourceControllers(WebApplicationBuilder builder, EndpointAwareJsonConverter customJSONMiddleware, FrontendAppConfig appConfig)
        {
            Action<MvcOptions> configureMvc = options =>
            {
                options.Conventions.Add(new CloudAwareActionConvention(appConfig.IsCloudVersion));
            };

            var mvcBuilder = builder.Services
                .AddControllersWithViews(configureMvc)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(customJSONMiddleware);
                })
                .ConfigureApplicationPartManager(manager =>
                {
                    var defaultProvider = manager.FeatureProviders.OfType<ControllerFeatureProvider>().FirstOrDefault();
                    if (defaultProvider != null)
                    {
                        manager.FeatureProviders.Remove(defaultProvider);
                    }

                    manager.FeatureProviders.Add(new CloudAwareControllerFeatureProvider(appConfig.IsCloudVersion));
                });

            if (!appConfig.IsCloudVersion) return;

            try
            {
                mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(_cloudAssembly!));
                Console.WriteLine("Successfully loaded Cloud Controllers.");

                mvcBuilder.PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(_cloudAssembly!));
                Console.WriteLine("Successfully loaded Cloud Views.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Cloud Controllers: {ex.Message}", ex);
            }
        }
    }
}
