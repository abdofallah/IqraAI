using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Interfaces.Modules;
using IqraCore.Interfaces.Server;
using IqraCore.Interfaces.User;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Call.Backend;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.SIP;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Managers.WebSession;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Embedding;
using IqraInfrastructure.Repositories.Embedding.Cache;
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
using IqraInfrastructure.Repositories.TTS.Cache;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WebSession;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProjectIqraBackendApp
{
    public class Program
    {
        private static Assembly? _cloudAssembly;
        private static ICloudBackendAppInitalizer? _cloudModule;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration
            var appConfig = builder.Configuration;
            var backendAppConfig = new BackendAppConfig
            {
                Id = appConfig["Server:Id"],
                RegionId = appConfig["Server:RegionId"],
                ExpectedMaxConcurrentCalls = int.Parse(appConfig["Server:ExpectedMaxConcurrentCalls"]),
                NetworkInterfaceName = appConfig["Server:NetworkInterfaceName"],
                MaxNetworkDownloadMbps = int.Parse(appConfig["Server:MaxNetworkDownloadMbps"]),
                MaxNetworkUploadMbps = int.Parse(appConfig["Server:MaxNetworkUploadMbps"]),
                ApiKey = appConfig["Server:ApiKey"],
                WebhookTokenSecret = appConfig["Server:WebhookTokenSecret"],
                IsCloudVersion = appConfig["IsCloudVersion"]?.ToLower() == "true",
            };
            builder.Services.AddSingleton<BackendAppConfig>(backendAppConfig);

            // Load Cloud Asembly
            if (backendAppConfig.IsCloudVersion)
            {
                LoadCloudAssembly();
            }

            // Preflight
            await SetupPreflight(builder, appConfig, backendAppConfig);

            // Repositories
            await SetupRepositories(builder, appConfig, backendAppConfig);
            if (backendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupRepositories(builder.Services, appConfig);
            }

            // Managers
            SetupManagers(builder, appConfig, backendAppConfig);
            if (backendAppConfig.IsCloudVersion)
            {
                _cloudModule!.SetupManagers(builder.Services, appConfig);
            }

            // HTTP Client
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

            // Postflight: Inject dependecies where needed
            SetupPostflight(app);

            // Initalize All Singleton Services
            InitializeAllSingletonServices(app.Services);

            app.UseWebSockets(new WebSocketOptions
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(30)
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
                        if (pathSegments == null || pathSegments.Length < 6 ||
                            pathSegments[0] != "ws" || pathSegments[1] != "session" || (pathSegments[3] != "telephonyclient" && pathSegments[3] != "websocket" && pathSegments[3] != "webrtc"))
                        {
                            context.Response.StatusCode = 400; await context.Response.WriteAsync("Invalid WebSocket path."); return;
                        }

                        string sessionId = pathSegments[2];
                        string clientType = pathSegments[3];
                        string clientId = pathSegments[4];
                        string sessionToken = pathSegments[5];

                        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(sessionToken))
                        {
                            context.Response.StatusCode = 400; await context.Response.WriteAsync("Invalid WebSocket path."); return;
                        }

                        if (clientType == "telephonyclient")
                        {
                            var callProcessorManager = context.RequestServices.GetRequiredService<BackendCallProcessorManager>();
                            try
                            {
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                var assignResult = await callProcessorManager.AssignWebSocketToClientAsync(sessionId, clientId, sessionToken, webSocket);
                                if (!assignResult.Success)
                                {
                                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, $"[{assignResult.Code}] {assignResult.Message}", CancellationToken.None);
                                    webSocket.Dispose();

                                    context.Response.StatusCode = 400; await context.Response.WriteAsync($"[{assignResult.Code}] {assignResult.Message}"); return;
                                }

                                // todo this is bad design, we need to await the websocket handler task for recieve itself if possible
                                // well seems like we need to wait here else we lose the websocket (it aborts)
                                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-9.0
                                while (webSocket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
                                {
                                    await Task.Delay(50);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!context.Response.HasStarted) context.Response.StatusCode = 500;
                            }
                        }
                        else if (clientType == "websocket" || clientType == "webrtc")
                        {
                            var webSessionProcessorManager = context.RequestServices.GetRequiredService<BackendWebSessionProcessorManager>();
                            try
                            {
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                var assignResult = await webSessionProcessorManager.AssignWebSocketToClientAsync(sessionId, clientId, sessionToken, webSocket, clientType);
                                if (!assignResult.Success)
                                {
                                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, $"[{assignResult.Code}] {assignResult.Message}", CancellationToken.None);
                                    webSocket.Dispose();

                                    context.Response.StatusCode = 400; await context.Response.WriteAsync($"[{assignResult.Code}] {assignResult.Message}"); return;
                                }

                                // todo this is bad design, we need to await the websocket handler task for recieve itself if possible
                                // well seems like we need to wait here else we lose the websocket (it aborts)
                                // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-9.0
                                while (webSocket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
                                {
                                    await Task.Delay(50);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!context.Response.HasStarted) context.Response.StatusCode = 500;
                            }
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

        private static void LoadCloudAssembly()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string cloudDllPath = Path.Combine(folder, "ProjectIqraBackendApp.Cloud.dll");
            if (!File.Exists(cloudDllPath)) throw new Exception("Cloud DLL missing");

            _cloudAssembly = Assembly.LoadFrom(cloudDllPath);
            var type = _cloudAssembly.GetTypes().FirstOrDefault(t => typeof(ICloudBackendAppInitalizer).IsAssignableFrom(t) && !t.IsInterface);
            if (type != null)
            {
                _cloudModule = (ICloudBackendAppInitalizer)Activator.CreateInstance(type);
            }
            if (_cloudModule == null) throw new Exception("Cloud module not found");
        }

        private static async Task SetupPreflight(WebApplicationBuilder builder, IConfiguration appConfig, BackendAppConfig backendAppConfig)
        {
            // Basic Dependencies required for Preflight
            var mongoClient = new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            builder.Services.AddSingleton<IMongoClient>(mongoClient);

            var regionRepoistory = new RegionRepository(mongoClient);
            builder.Services.AddSingleton<RegionRepository>(regionRepoistory);

            var regionManager = new RegionManager(regionRepoistory);
            builder.Services.AddSingleton<RegionManager>(regionManager);

            // Build Remaning config from dependencies
            var regionData = await regionManager.GetRegionById(backendAppConfig.RegionId);
            if (regionData == null)
            {
                throw new Exception("Region not found");
            }
            var regionServerData = regionData.Servers.FirstOrDefault(s => s.Id == backendAppConfig.Id);
            if (regionServerData == null)
            {
                throw new Exception("Server not found");
            }
            backendAppConfig.ServerEndpoint = regionServerData.Endpoint;
            backendAppConfig.SIPPort = regionServerData.SIPPort;

            var allRegionsDataResult = await regionManager.GetRegions();
            if (!allRegionsDataResult.Success)
            {
                throw new Exception($"[{allRegionsDataResult.Code}] {allRegionsDataResult.Message}");
            }

            S3StorageClientFactory s3StorageClientFactory = new S3StorageClientFactory(backendAppConfig.RegionId);
            builder.Services.AddSingleton<S3StorageClientFactory>(s3StorageClientFactory);
            var s3StorageInitResult = await s3StorageClientFactory.Initalize(allRegionsDataResult.Data!);
            if (!s3StorageInitResult.Success)
            {
                throw new Exception($"[{s3StorageInitResult.Code}] {s3StorageInitResult.Message}");
            }
        }

        private static async Task SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig, BackendAppConfig backendAppConfig)
        {
            builder.Services.AddSingleton<MilvusKnowledgeBaseClient>((sp) =>
            {
                return new MilvusKnowledgeBaseClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    new MilvusOptions()
                    {
                        Endpoint = appConfig["Milvus:Endpoint"],
                        Username = appConfig["Milvus:Username"],
                        Password = appConfig["Milvus:Password"],
                        ExpiryCheckIntervalSeconds = int.Parse(appConfig["Milvus:ExpiryCheckIntervalSeconds"]),
                        CollectionStaleTimeoutMinutes = int.Parse(appConfig["Milvus:CollectionStaleTimeoutMinutes"])
                    },
                    sp.GetRequiredService<ILogger<MilvusKnowledgeBaseClient>>()
                );
            });

            // Repositories
            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });

            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
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

            builder.Services.AddSingleton<OutboundCallQueueRepository>(sp =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>(),
                    sp.GetRequiredService<CallQueueLogsRepository>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueGroupRepository>(sp =>
            {
                return new OutboundCallQueueGroupRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    sp.GetRequiredService<ILogger<OutboundCallQueueGroupRepository>>()
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
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:ServerLiveStatusChannelDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<ServerLiveStatusChannelRepository>>()
                );
            });

            builder.Services.AddSingleton<DistributedLockRepository>((sp) =>
            {
                return new DistributedLockRepository(
                    new RedisConnectionFactory(
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:DistributedLockDatabaseIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    ),
                    sp.GetRequiredService<ILogger<DistributedLockRepository>>()
                );
            });

            builder.Services.AddSingleton<ConversationStateRepository>(sp =>
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

            builder.Services.AddSingleton<BusinessConversationAudioRepository>(sp =>
            {
                return new BusinessConversationAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessConversationAudioRepository>>(),
                    sp.GetRequiredService<S3StorageClientFactory>()
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

            builder.Services.AddSingleton<UserUsageRepository>((sp) =>
            {
                return new UserUsageRepository(
                    sp.GetRequiredService<ILogger<UserUsageRepository>>(),
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

            builder.Services.AddSingleton<TTSAudioCacheIndexRepository>((sp) =>
            {
                return new TTSAudioCacheIndexRepository(
                    sp.GetRequiredService<ILogger<TTSAudioCacheIndexRepository>>(),
                    new RedisConnectionFactory(
                        $"{appConfig["LocalRedisDatabase:ConnectionString"]},defaultDatabase={appConfig["LocalRedisDatabase:TTSAudioCacheIndex"]}",
                        sp.GetRequiredService<ILogger<RedisConnectionFactory>>()
                    )
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

            builder.Services.AddSingleton<WebSessionRepository>((sp) =>
            {
                return new WebSessionRepository(
                    sp.GetRequiredService<ILogger<WebSessionRepository>>(),
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

            builder.Services.AddSingleton<EmbeddingCacheRepository>((sp) =>
            {
                return new EmbeddingCacheRepository(
                    sp.GetRequiredService<ILogger<EmbeddingCacheRepository>>(),
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
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, BackendAppConfig backendAppConfig)
        {
            if (!backendAppConfig.IsCloudVersion)
            {
                builder.Services.AddSingleton<IUserUsageValidationManager, UserUsageValidationManager>((sp) =>
                {
                    return new UserUsageValidationManager();
                });

                builder.Services.AddSingleton<IUserBillingUsageManager, UserBillingUsageManager>((sp) =>
                {
                    return new UserBillingUsageManager();
                });
            }

            builder.Services.AddSingleton<LanguagesManager>((sp) =>
            {
                return new LanguagesManager(
                    sp.GetRequiredService<ILogger<LanguagesManager>>(),
                    sp.GetRequiredService<LanguagesRepository>()
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
                        InitalizeAgentsManager = true,
                        InitalizeScriptsManager = true,
                        InitalizeCacheManager = true,
                        InitalizeContextManager = true,
                        InitalizeIntegrationsManager = true,
                        InitalizeNumberManager = true,
                        InitalizeRoutesManager = true,
                        InitalizeToolsManager = true,
                        InitalizeCampaignManager = true
                    },
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    null,
                    sp.GetRequiredService<BusinessToolAudioRepository>(),
                    sp.GetRequiredService<BusinessAgentAudioRepository>(),
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
                    sp.GetRequiredService<IntegrationConfigurationManager>(),
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
                    sp.GetRequiredService<S3StorageClientFactory>()
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
                    integrationFieldsEncryptionService,
                    sp.GetRequiredService<S3StorageClientFactory>()
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
                        $"{appConfig["RedisDatabase:ConnectionString"]},defaultDatabase={appConfig["RedisDatabase:RAGCollectionsLoadedDatabaseIndex"]}",
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
            builder.Services.AddSingleton<BackendCallProcessorManager>((sp) =>
            {
                return new BackendCallProcessorManager(
                    sp.GetRequiredService<ILogger<BackendCallProcessorManager>>(),
                    sp,
                    backendAppConfig,
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<InboundCallQueueRepository>(),
                    sp.GetRequiredService<OutboundCallQueueRepository>(),
                    sp.GetRequiredService<OutboundCallQueueGroupRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<ConversationStateLogsRepository>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<IUserBillingUsageManager>(),
                    sp.GetRequiredService<CampaignActionExecutorService>(),
                    sp.GetRequiredService<IUserUsageValidationManager>()
                );
            });
            builder.Services.AddSingleton<BackendWebSessionProcessorManager>((sp) =>
            {
                return new BackendWebSessionProcessorManager(
                    sp.GetRequiredService<ILogger<BackendWebSessionProcessorManager>>(),
                    sp,
                    backendAppConfig,
                    sp.GetRequiredService<ServerMetricsMonitor>(),
                    sp.GetRequiredService<WebSessionRepository>(),
                    sp.GetRequiredService<ConversationStateRepository>(),
                    sp.GetRequiredService<ConversationStateLogsRepository>(),
                    sp.GetRequiredService<BusinessManager>(),
                    sp.GetRequiredService<IntegrationsManager>(),
                    sp.GetRequiredService<RegionManager>(),
                    sp.GetRequiredService<IUserBillingUsageManager>(),
                    sp.GetRequiredService<CampaignActionExecutorService>(),
                    sp.GetRequiredService<IUserUsageValidationManager>()
                );
            });

            builder.Services.AddSingleton<TTSAudioCacheManager>((sp) =>
            {
                return new TTSAudioCacheManager(
                    sp.GetRequiredService<ILogger<TTSAudioCacheManager>>(),
                    sp.GetRequiredService<TTSAudioCacheIndexRepository>(),
                    sp.GetRequiredService<TTSAudioCacheMetadataRepository>(),
                    sp.GetRequiredService<TTSAudioCacheStorageRepository>(),
                    sp.GetRequiredService<BusinessAppRepository>(),
                    appConfig["Server:RegionId"]
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

            builder.Services.AddHostedService<SipBackendListenerService>((sp) =>
            {
                return new SipBackendListenerService(
                    sp.GetRequiredService<ILogger<SipBackendListenerService>>(),
                    backendAppConfig,
                    sp.GetRequiredService<BackendCallProcessorManager>(),
                    sp.GetRequiredService<InboundCallQueueRepository>()
                );
            });
        }

        private static void SetupPostflight(WebApplication app)
        {
            var regionRepoistory = app.Services.GetRequiredService<RegionRepository>();
            regionRepoistory.SetLogger(app.Services.GetRequiredService<ILogger<RegionRepository>>());

            var regionManager = app.Services.GetRequiredService<RegionManager>();
            regionManager.SetLogger(app.Services.GetRequiredService<ILogger<RegionManager>>());
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