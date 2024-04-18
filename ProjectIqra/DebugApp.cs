using IqraCore.Interfaces.AI;
using IqraCore.Interfaces;
using IqraInfrastructure.Services.Audio;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services;
using IqraInfrastructure.Services.TTS;
using IqraCore.Utilities;

namespace ProjectIqra
{
    public class DebugApp
    {
        private string _currentLanguage = "en-US";
        private string _currentSpeakerName = "en-US-JennyNeural";

        private readonly IAudioCache _audioCache;

        private readonly IAudioInputService _audioInputService;
        private readonly IAudioOutputService _audioOutputService;
        private readonly ISTTService _sttService;
        private readonly ITTSService _ttsService;
        private readonly IAIService _aiService;

        private Task? _aiTask;
        private CancellationTokenSource _aiCancellationTokenSource;

        private List<Task> _ttsTasks;
        private CancellationTokenSource _ttsCancellationTokenSource;

        private string _initialMessage;
        private string _systemPrompt;

        public DebugApp(string subscriptionKey, string region, string claudeApiKey, IAudioCache audioCache)
        {
            _audioCache = audioCache;

            _audioInputService = new DeviceMicrophoneInputService();
            _audioOutputService = new DeviceSpeakerOutputService();
            _sttService = new AzureSpeechSTTService(subscriptionKey, region, _currentLanguage);
            _ttsService = new AzureSpeechTTSService(subscriptionKey, region, _currentLanguage, _currentSpeakerName);
            _aiService = new ClaudeStreamingLLMService(claudeApiKey);

            _ttsTasks = new List<Task>();
            _ttsCancellationTokenSource = new CancellationTokenSource();

            _aiTask = null;
            _aiCancellationTokenSource = new CancellationTokenSource();   
        }

        public void Initialize()
        {
            // Context Related
            string CompanyName = "Harub Dental Surgery";
            _systemPrompt = File.ReadAllText("TestSystemPrompt.txt").Replace("{{COMPANY_NAME}}", CompanyName);
            _initialMessage = File.ReadAllText("TestInitialMessages.txt").Replace("{{COMPANY_NAME}}", CompanyName);

            // Services Related
            _audioInputService.Initialize();
            _audioInputService.AudioDataReceived += OnAudioDataReceived;

            _audioOutputService.Initialize();

            _sttService.Initialize();
            _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;

            _ttsService.Initialize();

            _aiService.MessageStreamed += OnMessageStreamed;

            _aiService.SetSystemPrompt(_systemPrompt);
            _aiService.SetInitialMessage(_initialMessage);
            _aiService.SetMaxTokens(128);
            _aiService.SetModel(Anthropic.SDK.Constants.AnthropicModels.Claude3Opus);
        }

        public async void Start()
        {
            Console.WriteLine("Starting audio recording and transcription...");
            _audioOutputService.StartPlayback();
            _sttService.StartTranscription();

            await Task.Delay(500);

            CancelAllTasksAndResetBeforeProcessing();
            OnMessageStreamed(null, _initialMessage);

            await Task.Delay(500);

            while (true)
            {
                await Task.Delay(100);

                if (_audioOutputService.IsBufferEmpty())
                {
                    break;
                }  
            }

            _audioInputService.StartRecording();
        }

        public void Stop()
        {
            Console.WriteLine("Stopping audio recording and transcription...");
            _sttService.StopTranscription();
            _audioOutputService.StopPlayback();
            _audioInputService.StopRecording();
        }

        // USER VOICE TO TEXT
        private void OnAudioDataReceived(object? sender, byte[] audioData)
        {
            // Send input audio to speech to text service
            _sttService.WriteTranscriptionAudioData(audioData);
        }

        // USER VOICE TEXT TO AI
        private void OnTranscriptionResultReceived(object? sender, string result)
        {
            if (string.IsNullOrWhiteSpace(result)) { return; }
            Console.WriteLine($"Transcription result: {result}");

            if (result.ToLower().Contains("services"))
            {
                _aiService.SetMaxTokens(333);
            }
            else
            {
                _aiService.SetMaxTokens(128);
            }

            CancelAllTasksAndResetBeforeProcessing();

            // process the AI generated result
            try
            {
                _aiTask = _aiService.ProcessInputAsync(result, _aiCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("AI processing cancelled.");
            }

            // start accepting user voice input again
            _audioInputService.StartRecording();
        }

        // USER VOICE TEXT AI GENERATED RESPONSE TO AUDIO AND ALSO OUTPUT
        // todo seperate output from this
        private void OnMessageStreamed(object? sender, string section)
        {
            try
            {
                Console.WriteLine($"AI Section: {section}");

                byte[]? sectionSound = null;

                ulong textHash = XXHashHelper.ComputeHashInUlong(section);

                sectionSound = _audioCache.GetAudioData(textHash, _ttsService.GetTTSProviderName(), _currentLanguage, _currentSpeakerName);

                if (sectionSound == null)
                {
                    sectionSound = _ttsService.SynthesizeTextAsync(section, _ttsCancellationTokenSource.Token).GetAwaiter().GetResult();

                    _audioCache.SetAudioData(textHash, _ttsService.GetTTSProviderName(), _currentLanguage, _currentSpeakerName, sectionSound);
                }
                else
                {
                    Console.WriteLine($"Using cached audio data for {textHash} - {section} | {_ttsService.GetTTSProviderName()} {_currentLanguage} {_currentSpeakerName}");
                }

                _audioOutputService.EnqueueAudioData(sectionSound);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("TTS synthesis cancelled.");
            }
        }

        private void CancelAllTasksAndResetBeforeProcessing()
        {
            // stop the recording from mic
            _audioInputService.StopRecording();

            // cancels text to speech
            // cancels ai generating any text
            _ttsCancellationTokenSource.Cancel();
            _aiCancellationTokenSource.Cancel();

            // make sure the tts and ai tasks are done
            List<Task> tasks = _ttsTasks;
            if (_aiTask != null)
            {
                tasks = _ttsTasks.Concat(new List<Task> { _aiTask }).ToList();
            }
            Task.WhenAll(tasks);

            // reset the tasks
            _ttsTasks.Clear();
            _aiTask = null;

            // reset the cancellation tokens
            _ttsCancellationTokenSource = new CancellationTokenSource();
            _aiCancellationTokenSource = new CancellationTokenSource();

            // clear any audio pending for speaker
            _audioOutputService.ClearAudioData();
        }
    }
}