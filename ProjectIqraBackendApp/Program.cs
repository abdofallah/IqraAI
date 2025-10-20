using IqraCore.Entities.Configuration;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Server;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Managers.Billing;
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
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Managers.WebSession;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Billing;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Embedding;
using IqraInfrastructure.Repositories.Embedding.Cache;
using IqraInfrastructure.Repositories.Integrations;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Languages;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Repositories.MinIO;
using IqraInfrastructure.Repositories.RAG;
using IqraInfrastructure.Repositories.Redis;
using IqraInfrastructure.Repositories.Region;
using IqraInfrastructure.Repositories.Rerank;
using IqraInfrastructure.Repositories.Server;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Repositories.TTS.Cache;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WebSession;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MongoDB.Driver;
using Mosaik.Core;
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
            var backendAppConfig = new BackendAppConfig
            {
                ServerId = appConfig["Server:Identity"],
                RegionId = appConfig["Server:RegionId"],
                ExpectedMaxConcurrentCalls = int.Parse(appConfig["Server:ExpectedMaxConcurrentCalls"]),
                NetworkInterfaceName = appConfig["Server:NetworkInterfaceName"],
                MaxNetworkDownloadMbps = int.Parse(appConfig["Server:MaxNetworkDownloadMbps"]),
                MaxNetworkUploadMbps = int.Parse(appConfig["Server:MaxNetworkUploadMbps"]),
                ApiKey = appConfig["Server:ApiKey"],
                WebhookTokenSecret = appConfig["Server:WebhookTokenSecret"],
            };
            builder.Services.AddSingleton<BackendAppConfig>(sp =>
            {
                return backendAppConfig;
            });

            // Dependencies
            RegisterCatalystLanguages();

            // Repositories
            SetupRepositories(builder, appConfig);

            // Managers
            SetupManagers(builder, appConfig, backendAppConfig);

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
                            pathSegments[0] != "ws" || pathSegments[1] != "session" || (pathSegments[3] != "telephonyclient" && pathSegments[3] != "webclient"))
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
                        else if (clientType == "webclient")
                        {
                            var webSessionProcessorManager = context.RequestServices.GetRequiredService<BackendWebSessionProcessorManager>();
                            try
                            {
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                var assignResult = await webSessionProcessorManager.AssignWebSocketToClientAsync(sessionId, clientId, sessionToken, webSocket);
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

        private static void SetupRepositories(WebApplicationBuilder builder, IConfiguration appConfig)
        {
            // Build Base Services
            builder.Services.AddSingleton<IMongoClient>((sp) =>
            {
                return new MongoClient(appConfig["MongoDatabase:ConnectionString"]);
            });

            builder.Services.AddSingleton<MinioPrivatePublicClient>((sp) =>
            {
                return new MinioPrivatePublicClient(
                    appConfig["MinioStorage:PrivateEndpoint"],
                    int.Parse(appConfig["MinioStorage:PrivateEndpointPort"]),
                    bool.Parse(appConfig["MinioStorage:IsPrivateEndpointSecure"]),
                    appConfig["MinioStorage:PublicEndpoint"],
                    int.Parse(appConfig["MinioStorage:PublicEndpointPort"]),
                    bool.Parse(appConfig["MinioStorage:IsPublicEndpointSecure"]),
                    appConfig["MinioStorage:AccessKey"],
                    appConfig["MinioStorage:SecretKey"]
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
                        Password = appConfig["Milvus:Password"],
                        ExpiryCheckIntervalSeconds = int.Parse(appConfig["Milvus:ExpiryCheckIntervalSeconds"]),
                        CollectionStaleTimeoutMinutes = int.Parse(appConfig["Milvus:CollectionStaleTimeoutMinutes"])
                    },
                    sp.GetRequiredService<ILogger<MilvusKnowledgeBaseClient>>()
                );
            });

            // Repositories
            builder.Services.AddSingleton<RegionRepository>((sp) => {
                return new RegionRepository(
                    sp.GetRequiredService<ILogger<RegionRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:AppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<LanguagesRepository>((sp) =>
            {
                return new LanguagesRepository(
                    sp.GetRequiredService<ILogger<LanguagesRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:LanguagesRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<InboundCallQueueRepository>(sp =>
            {
                return new InboundCallQueueRepository(
                    sp.GetRequiredService<ILogger<InboundCallQueueRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueRepository>(sp =>
            {
                return new OutboundCallQueueRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:CallQueueRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueRepository>>()
                );
            });

            builder.Services.AddSingleton<OutboundCallQueueGroupRepository>(sp =>
            {
                return new OutboundCallQueueGroupRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:OutboundCallCampaignRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<OutboundCallQueueGroupRepository>>()
                );
            });

            builder.Services.AddSingleton<ServerStatusRepository>(sp =>
            {
                return new ServerStatusRepository(
                    sp.GetRequiredService<ILogger<ServerStatusRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ServerStatusRepositoryDatabaseName"]
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
                    appConfig["MongoDatabase:ConversationStateRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateRepository>>()
                );
            });

            builder.Services.AddSingleton<ConversationStateLogsRepository>(sp =>
            {
                return new ConversationStateLogsRepository(
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ConversationStateRepositoryDatabaseName"],
                    sp.GetRequiredService<ILogger<ConversationStateLogsRepository>>()
                );
            });

            builder.Services.AddSingleton<ConversationAudioRepository>(sp =>
            {
                return new ConversationAudioRepository(
                    sp.GetRequiredService<ILogger<ConversationAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:ConversationAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<IntegrationsRepository>((sp) =>
            {
                return new IntegrationsRepository(
                    sp.GetRequiredService<ILogger<IntegrationsRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:IntegrationsRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessRepository>((sp) =>
            {
                return new BusinessRepository(
                    sp.GetRequiredService<ILogger<BusinessRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessAppRepository>((sp) =>
            {
                return new BusinessAppRepository(
                    sp.GetRequiredService<ILogger<BusinessAppRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessAppRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessToolAudioRepository>((sp) =>
            {
                return new BusinessToolAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessToolAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:BusinessToolAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<BusinessAgentAudioRepository>((sp) =>
            {
                return new BusinessAgentAudioRepository(
                    sp.GetRequiredService<ILogger<BusinessAgentAudioRepository>>(),
                    sp.GetRequiredService<MinioPrivatePublicClient>(),
                    appConfig["MinioStorage:BusinessAgentAudioRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<LLMProviderRepository>((sp) =>
            {
                return new LLMProviderRepository(
                    sp.GetRequiredService<ILogger<LLMProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:LLMProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<STTProviderRepository>((sp) =>
            {
                return new STTProviderRepository(
                    sp.GetRequiredService<ILogger<STTProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:STTProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<TTSProviderRepository>((sp) =>
            {
                return new TTSProviderRepository(
                    sp.GetRequiredService<ILogger<TTSProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:TTSProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<UserUsageRepository>((sp) =>
            {
                return new UserUsageRepository(
                    sp.GetRequiredService<ILogger<UserUsageRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:ConversationUsageRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<UserRepository>((sp) =>
            {
                return new UserRepository(
                    sp.GetRequiredService<ILogger<UserRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:UserRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BillingPlanRepository>((sp) =>
            {
                return new BillingPlanRepository(
                    sp.GetRequiredService<ILogger<BillingPlanRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:PlanRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<AppRepository>((sp) =>
            {
                return new AppRepository(
                    sp.GetRequiredService<ILogger<AppRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:AppRepositoryDatabaseName"]
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
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:TTSAudioCacheMetadataRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<IqraMinioClientFactory>((sp) =>
            {
                return new IqraMinioClientFactory(
                    new Dictionary<string, MinioPrivatePublicClient>() {
                        { appConfig["Server:RegionId"], sp.GetRequiredService<MinioPrivatePublicClient>() }
                    }
                );
            });

            builder.Services.AddSingleton<TTSAudioCacheStorageRepository>((sp) =>
            {
                return new TTSAudioCacheStorageRepository(
                    sp.GetRequiredService<ILogger<TTSAudioCacheStorageRepository>>(),
                    sp.GetRequiredService<IqraMinioClientFactory>(),
                    appConfig["Server:Identity"],
                    appConfig["MinioStorage:TTSAudioCacheStorageRepositoryBucketName"]
                );
            });

            builder.Services.AddSingleton<WebSessionRepository>((sp) =>
            {
                return new WebSessionRepository(
                    sp.GetRequiredService<ILogger<WebSessionRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:WebSessionRepoistoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<EmbeddingProviderRepository>((sp) =>
            {
                return new EmbeddingProviderRepository(
                    sp.GetRequiredService<ILogger<EmbeddingProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:EmbeddingProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<RerankProviderRepository>((sp) =>
            {
                return new RerankProviderRepository(
                    sp.GetRequiredService<ILogger<RerankProviderRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:RerankProviderRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<EmbeddingCacheRepository>((sp) =>
            {
                return new EmbeddingCacheRepository(
                    sp.GetRequiredService<ILogger<EmbeddingCacheRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:EmbeddingCacheRepositoryDatabaseName"]
                );
            });

            builder.Services.AddSingleton<BusinessKnowledgeBaseDocumentRepository>((sp) =>
            {
                return new BusinessKnowledgeBaseDocumentRepository(
                    sp.GetRequiredService<ILogger<BusinessKnowledgeBaseDocumentRepository>>(),
                    sp.GetRequiredService<IMongoClient>(),
                    appConfig["MongoDatabase:BusinessKnowledgeBaseDocumentRepositoryDatabaseName"]
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
                    appConfig["MongoDatabase:RAGKeywordStoreDatabaseName"],
                    sp.GetRequiredService<KeywordExtractor>(),
                    sp.GetRequiredService<ILogger<RAGKeywordStore>>()
                );
            });
        }

        private static void SetupManagers(WebApplicationBuilder builder, IConfiguration appConfig, BackendAppConfig backendAppConfig)
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
                        InitalizeToolsManager = true,
                        InitalizeCampaignManager = true
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
                    sp.GetRequiredService<UserBillingUsageManager>(),
                    sp.GetRequiredService<CampaignActionExecutorService>(),
                    sp.GetRequiredService<UserUsageValidationManager>()
                );
            });
            builder.Services.AddSingleton<UserUsageValidationManager>((sp) =>
            {
                return new UserUsageValidationManager(
                    sp.GetRequiredService<ILogger<UserUsageValidationManager>>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<BusinessRepository>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<PlanManager>()
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
                    sp.GetRequiredService<UserBillingUsageManager>(),
                    sp.GetRequiredService<CampaignActionExecutorService>(),
                    sp.GetRequiredService<UserUsageValidationManager>()
                );
            });
            builder.Services.AddSingleton<UserBillingUsageManager>((sp) =>
            {
                return new UserBillingUsageManager(
                    sp.GetRequiredService<ILogger<UserBillingUsageManager>>(),
                    sp.GetRequiredService<AppRepository>(),
                    sp.GetRequiredService<UserRepository>(),
                    sp.GetRequiredService<UserUsageRepository>(),
                    sp.GetRequiredService<PlanManager>(),
                    sp.GetRequiredService<IMongoClient>()
                );
            });
            builder.Services.AddSingleton<PlanManager>((sp) =>
            {
                return new PlanManager(
                    sp.GetRequiredService<ILogger<PlanManager>>(),
                    sp.GetRequiredService<BillingPlanRepository>()
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

        private static void RegisterCatalystLanguages()
        {
            Catalyst.Models.English.Register(); // Load initial assembly

            foreach (var langauge in Enum.GetNames(typeof(Language)))
            {
                var langName = langauge.ToString(); // "English", "Arabic", etc.
                var typeName = $"Catalyst.Models.{langName}";

                try
                {
                    // Step 1: Load the assembly manually if it's not already loaded
                    var assembly = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == typeName)
                        ?? Assembly.Load(typeName); // Load from name (e.g., "Catalyst.Models.English")

                    if (assembly == null)
                    {
                        Console.WriteLine($"Failed to load assembly: {typeName}. Skipping!");
                        continue;
                    }

                    // Step 2: Try to find the type
                    var langType = assembly.ExportedTypes.FirstOrDefault(t => t?.FullName != null && t.FullName.Equals(typeName));

                    if (langType == null)
                    {
                        throw new Exception($"Type {typeName} not found in assembly {typeName}");
                    }
                    // Step 3: Call Register()
                    var registerMethod = langType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);

                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(null, null);
                    }
                    else
                    {
                        throw new Exception($"Register() method not found for type {typeName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to register language {langName}. Skipping!");
                    continue;
                }
            }
        }
    }
}