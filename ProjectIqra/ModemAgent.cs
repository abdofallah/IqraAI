using IqraInfrastructure.Caching;
using IqraInfrastructure.Services.Audio.Device;
using IqraInfrastructure.Services.Audio.SimcomModem;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.TTS;
using IqraInfrastructure.Services;
using SimcomModuleManager;
using IqraCore.Interfaces;

namespace ProjectIqra
{
    public class ModemAgent
    {
        public static string _azureSubscriptionKey = "a79219977e464ca9bc5c47fa00162451";
        public static string _azureRegionKey = "uaenorth";
        public static string _claudeAPIKey = "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";

        public static string _azureTTSSpeakerName = "en-US-JennyNeural";
        public static string _azureSTTAndTTSLanguage = "en-US";

        private IAudioCache _audioChache;

        private ModemManager _modemManager;

        private DebugApp _debugApp;

        public ModemAgent(IAudioCache audioCache)
        {
            _modemManager = new ModemManager();
            _audioChache = audioCache;
            _debugApp = new DebugApp(_audioChache);
        }

        public async Task<bool> Initialize()
        {
            if (!await _modemManager.Initialize())
            {
                Console.WriteLine("Error initializing modem.");
                return false;
            }

            ModemInputService audioInputService = new ModemInputService();
            audioInputService.SetModemAudioModule(_modemManager.sim7600ModemAudio);

            ModemOutputService audioOutputService = new ModemOutputService();


            AzureSpeechSTTService sttService = new AzureSpeechSTTService(_azureSubscriptionKey, _azureRegionKey, _azureSTTAndTTSLanguage);
            AzureSpeechTTSService ttsService = new AzureSpeechTTSService(_azureSubscriptionKey, _azureRegionKey, _azureSTTAndTTSLanguage, _azureTTSSpeakerName);
            ClaudeStreamingLLMService aiService = new ClaudeStreamingLLMService(_claudeAPIKey);

            _debugApp.SetLanguage(_azureSTTAndTTSLanguage);
            _debugApp.SetSpeakerName(_azureTTSSpeakerName);
            _debugApp.SetServices(audioInputService, audioOutputService, sttService, ttsService, aiService);
            _debugApp.Initialize();

            return true;
        }

        public void Start()
        {
            debugService.Start();
        }
    }
}
