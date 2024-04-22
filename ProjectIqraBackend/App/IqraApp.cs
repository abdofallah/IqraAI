using IqraCore.Interfaces.Repositories;
using IqraCore.Interfaces;
using MongoDB.Driver;
using IqraInfrastructure.Repositories;
using IqraInfrastructure.Caching;
using IqraInfrastructure.Services.App;

namespace ProjectIqraBackend.App
{
    public class IqraApp
    {
        private readonly WebApplicationBuilder _builder;
        public IqraApp(WebApplicationBuilder builder)
        {
            _builder = builder;
        }

        public IConfigurationRoot _appConfig;

        public MongoClient _mongoClient;
        public IMongoDatabase _mongoDatabase;

        public IAudioCache _redisAudioCache;

        public IBusinessRepository _businessRepository;
        public ISessionRepository _sessionRepository;
        public IConversationRepository _conversationRepository;
        public AppRepository _appRepository;

        public ModemsManager _modemsManager;

        public ApiManager _apiManager;

        public AgentManager _agentManager;

        public async Task Initialize()
        {
            LoadAppSettings();
            LoadMongoDatabase();
            LoadAudioCaching();
            LoadRepositories();
            await LoadModemsManager();
            LoadApiManager();
            LoadAgentManager();
        }

        public void AddServicesToSingleton()
        {
            // add audio cache
            _builder.Services.AddSingleton(_redisAudioCache);

            // add repositories
            _builder.Services.AddSingleton(_businessRepository);
            _builder.Services.AddSingleton(_sessionRepository);
            _builder.Services.AddSingleton(_conversationRepository);
            _builder.Services.AddSingleton(_appRepository);

            // add modems manager
            _builder.Services.AddSingleton(_modemsManager);

            // add api manager
            _builder.Services.AddSingleton(_apiManager);

            // add agent manager
            _builder.Services.AddSingleton(_agentManager);
        }

        private void LoadAgentManager()
        {
            _agentManager = new AgentManager(_modemsManager);
        }

        private void LoadApiManager()
        {
            _apiManager = new ApiManager(_appRepository);
        }

        private async Task LoadModemsManager()
        {
            _modemsManager = new ModemsManager();
            await _modemsManager.LoadDevices();
        }

        private void LoadMongoDatabase()
        {
            _mongoClient = new MongoClient(MongoClientSettings.FromConnectionString(_appConfig["MongoDatabase:ConnectionString"]));
            _mongoDatabase = _mongoClient.GetDatabase(_appConfig["MongoDatabase:Database"]);
        }

        private void LoadAudioCaching()
        {
            _redisAudioCache = new RedisAudioCache(_appConfig["Redis:ConnectionString"]);
        }

        private void LoadAppSettings()
        {
            _appConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        private void LoadRepositories()
        {
            _businessRepository = new BusinessRepository(_mongoDatabase);
            _sessionRepository = new SessionRepository(_mongoDatabase);
            _conversationRepository = new ConversationRepository(_mongoDatabase);
            _appRepository = new AppRepository(_mongoDatabase);
        }
    }
}
