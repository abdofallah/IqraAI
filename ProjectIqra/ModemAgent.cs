using IqraInfrastructure.Services.Audio.SimcomModem;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.TTS;
using IqraInfrastructure.Services;
using SimcomModuleManager;
using IqraCore.Interfaces;
using ProjectIqraBackend.App.Entities;
using IqraCore.Entities.App.Agent;
using IqraCore.Entities.Business;

namespace ProjectIqra
{
    public class ModemAgent
    {
        private readonly Business _business;

        private readonly IAudioCache _audioChache;

        private readonly ModemInstance _modemInstance;
        private readonly SimcomModemManager _modemManager;

        private AgentStatus _agentStatus;

        private DebugApp _debugApp;

        /** Dynamic Variables for Runtime **/
        private Task _StartCheckingForCallTask;
        private CancellationTokenSource _StartCheckingForCallCancellationTokenSource;
        private bool _isRecivingIncomingCall;

        private Task _StartCheckingForCallBeginTask;
        private CancellationTokenSource _StartCheckingForCallBeginCancellationTokenSource;
        private bool _hasVoiceCallBegined;

        private Task _StartCheckingForCallEndTask;
        private CancellationTokenSource _StartCheckingForCallEndCancellationTokenSource;
        private bool _isCallEnded;
        /** END - Dynamic Variables **/

        public ModemAgent(Business business, ModemInstance modemInstance, IAudioCache audioCache)
        {
            _business = business;

            _modemInstance = modemInstance;
            _modemManager = new SimcomModemManager(_modemInstance.CompositeInstance, _modemInstance.ATInstance, _modemInstance.AudioInstance);

            _audioChache = audioCache;

            _debugApp = new DebugApp(_audioChache, _business);

            _agentStatus = AgentStatus.Created;
        }

        public async Task<bool> Initialize()
        {
            if (!await _modemManager.Initialize())
            {
                Console.WriteLine("Error initializing modem.");
                _agentStatus = AgentStatus.InitializedFailed;
                return false;
            }

            _modemManager.OnRingingCommandReceived += OnIncomingVoiceCallRecieved;
            _modemManager.OnCallBeginCommandReceived += OnVoiceCallBeginRecieved;
            _modemManager.OnCallEndCommandReceived += OnVoiceCallEndRecieved;

            ModemInputService audioInputService = new ModemInputService();
            audioInputService.SetModemAudioModule(_modemManager.sim7600ModemAudio);

            ModemOutputService audioOutputService = new ModemOutputService();
            audioOutputService.SetModemAudioModule(_modemManager.sim7600ModemAudio);

            BusinessAzureSettings? businessAzureSettings = _business.BusinessAzureSettings;
            if (businessAzureSettings == null)
            {
                Console.WriteLine("Error initializing business azure settings.");
                _agentStatus = AgentStatus.InitializedFailed;
                return false;
            }

            string businessDefaultLanguage = _business.LanguagesEnabled[0];

            AzureSpeechSTTService sttService = new AzureSpeechSTTService(businessAzureSettings.SubscriptionKey, businessAzureSettings.RegionCode, businessDefaultLanguage);
            AzureSpeechTTSService ttsService = new AzureSpeechTTSService(businessAzureSettings.SubscriptionKey, businessAzureSettings.RegionCode, businessDefaultLanguage, businessAzureSettings.SpeakerName[businessDefaultLanguage]);
            ClaudeStreamingLLMService aiService = new ClaudeStreamingLLMService(_business.BusinessClaudeApiKey);

            _debugApp.SetLanguage(businessDefaultLanguage);
            _debugApp.SetSpeakerName(businessAzureSettings.SpeakerName[businessDefaultLanguage]);
            _debugApp.SetServices(audioInputService, audioOutputService, sttService, ttsService, aiService);

            _debugApp.Initialize();

            _agentStatus = AgentStatus.Initialized;
            return true;
        }

        public void StartCheckingForIncomingCall()
        {
            if (
                _agentStatus != AgentStatus.Initialized
                &&
                _agentStatus != AgentStatus.Idle
            )
            {
                Console.WriteLine("Invalid agent status to start checking for call: " + _agentStatus.ToString());
                _agentStatus = AgentStatus.Error;
                return;
            }

            _isRecivingIncomingCall = false;
            _hasVoiceCallBegined = false;
            _isCallEnded = false;

            _StartCheckingForCallCancellationTokenSource = new CancellationTokenSource();
            _StartCheckingForCallTask = _modemManager.StartCheckingForCallBeginLoop(_StartCheckingForCallCancellationTokenSource.Token);

            _agentStatus = AgentStatus.CheckingForRingCommand;
        }

        private async void OnIncomingVoiceCallRecieved(object? sender, string phoneNumber)
        {
            if (_isRecivingIncomingCall) return;
            _isRecivingIncomingCall = true;

            _StartCheckingForCallCancellationTokenSource.Cancel();
            await Task.WhenAll(_StartCheckingForCallTask);

            _StartCheckingForCallBeginCancellationTokenSource = new CancellationTokenSource();
            _StartCheckingForCallBeginTask = _modemManager.StartCheckingForCallBeginLoop(_StartCheckingForCallBeginCancellationTokenSource.Token);

            await _modemManager.PickupIncomingCall();

            _agentStatus = AgentStatus.CheckingForVoiceBeginCommand;
        }

        private async void OnVoiceCallBeginRecieved(object? sender, bool recieved)
        {
            if (_hasVoiceCallBegined) return;
            _hasVoiceCallBegined = true;

            _StartCheckingForCallBeginCancellationTokenSource.Cancel();
            await Task.WhenAll(_StartCheckingForCallBeginTask);

            _StartCheckingForCallEndCancellationTokenSource = new CancellationTokenSource();
            _StartCheckingForCallEndTask = _modemManager.StartCheckingForCallEndLoop(_StartCheckingForCallEndCancellationTokenSource.Token);

            _debugApp.Start(); // make it into a task i think for voice call end to make sure it has been stopped fully

            _agentStatus = AgentStatus.OnCall;
        }

        private async void OnVoiceCallEndRecieved(object? sender, bool recieved)
        {
            if (_isCallEnded) return;
            _isCallEnded = true;

            _StartCheckingForCallBeginCancellationTokenSource.Cancel();
            _StartCheckingForCallEndCancellationTokenSource.Cancel();
            await Task.WhenAll(new List<Task>() { _StartCheckingForCallBeginTask, _StartCheckingForCallEndTask });

            _debugApp.Stop();

            await _modemManager.DropOngoingCall();
            if (await _modemManager.DisableEnableSerialPort() == false)
            {
                // create an alert here so there is fast support to fix this
                Console.WriteLine($"Error restarting module after call end for {_modemInstance.CompositeInstance.BaseContainerId} | {_modemInstance.PhoneNumber}");
                _agentStatus = AgentStatus.Error;
                return;
            }

            _agentStatus = AgentStatus.Idle;

            StartCheckingForIncomingCall();
        }
    }
}
