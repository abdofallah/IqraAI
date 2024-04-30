using IqraInfrastructure.Services.Audio.SimcomModem;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.TTS;
using IqraInfrastructure.Services;
using SimcomModuleManager;
using IqraCore.Interfaces;
using ProjectIqraBackend.App.Entities;
using IqraCore.Entities.App.Agent;
using IqraCore.Entities.Business;
using IqraInfrastructure.Services.Audio.Device;

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
        private Task _StartCheckingForIncomingCallTask;
        private CancellationTokenSource _StartCheckingForIncomingCallCancellationTokenSource;
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
            _modemManager = modemInstance.SimcomModemManager;

            _audioChache = audioCache;

            _debugApp = new DebugApp(_audioChache, _business);

            _agentStatus = AgentStatus.Created;
        }

        public async Task<bool> Initialize()
        {
            _debugApp.OnEndCallEvent += OnCallEndRecieved;
            _debugApp.OnLowerVolumeEvent += OnLowerVolumeRecieved;
            _debugApp.OnIncreaseVolumeEvent += OnIncreaseVolumeRecieved;

            _modemManager.OnRingingCommandReceived += OnIncomingVoiceCallRecieved;
            _modemManager.OnCallBeginCommandReceived += OnVoiceCallBeginRecieved;
            _modemManager.OnCallEndCommandReceived += OnVoiceCallEndRecieved;
            _modemManager.OnDMTFKeyPressRecieved += OnDMTFKeyPressRecieved;

            ModemInputService audioInputService = new ModemInputService();
            audioInputService.SetModemAudioModule(_modemManager.sim7600ModemAudio);

            ModemOutputService audioOutputService = new ModemOutputService();
            audioOutputService.SetModemAudioModule(_modemManager.sim7600ModemAudio);

            //DeviceMicrophoneInputService audioInputService = new DeviceMicrophoneInputService();
            //DeviceSpeakerOutputService audioOutputService = new DeviceSpeakerOutputService();

            BusinessAzureSettings? businessAzureSettings = _business.BusinessAzureSettings;
            if (businessAzureSettings == null)
            {
                Console.WriteLine("Error initializing business azure settings.");
                _agentStatus = AgentStatus.InitializedFailed;
                return false;
            }

            string businessDefaultLanguage = _business.BusinessPromptData.LanguagesEnabled[0];

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

            _StartCheckingForIncomingCallCancellationTokenSource = new CancellationTokenSource();
            _StartCheckingForIncomingCallTask = _modemManager.StartCheckingRingCommandLoop(_StartCheckingForIncomingCallCancellationTokenSource.Token);

            _agentStatus = AgentStatus.CheckingForRingCommand;
        }

        private async void OnIncomingVoiceCallRecieved(object? sender, string phoneNumber)
        {
            if (_isRecivingIncomingCall) return;
            _isRecivingIncomingCall = true;

            _StartCheckingForIncomingCallCancellationTokenSource.Cancel();
            await Task.WhenAll(_StartCheckingForIncomingCallTask);

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

            await _modemManager.EnableModemAudioTransfer();
            await _modemManager.EnableDTMFTones();

            _StartCheckingForCallEndCancellationTokenSource = new CancellationTokenSource();
            _StartCheckingForCallEndTask = _modemManager.StartCheckingForCallEndLoop(_StartCheckingForCallEndCancellationTokenSource.Token);

            string randomSessionId = Guid.NewGuid().ToString();
            _debugApp.Start(randomSessionId); // make it into a task i think for voice call end to make sure it has been stopped fully

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
            await _debugApp.CancelTasksAndRenewTokens();

            await _modemManager.DropOngoingCall();
            if (await _modemManager.DisableEnableSerialPort() == false)
            {
                // create an alert here so there is fast support to fix this
                Console.WriteLine($"Error restarting module after call end for {_modemInstance.CompositeInstance.BaseContainerId} | {_modemInstance.PhoneNumber}");
                _agentStatus = AgentStatus.Error;
                return;
            }

            _agentStatus = AgentStatus.Idle;
        }

        private void OnDMTFKeyPressRecieved(object? sender, string key)
        {
            _debugApp.OnCallKeyPressEvent?.Invoke(this, key);
        }

        /** Debug App Events **/

        private void OnCallEndRecieved(object? sender, object? result)
        {
            OnVoiceCallEndRecieved(this, true);
        }

        private async void OnIncreaseVolumeRecieved(object? sender, EventArgs e)
        {
            
        }

        private async void OnLowerVolumeRecieved(object? sender, EventArgs e)
        {
            
        }
    }
}
