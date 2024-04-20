using IqraCore.Interfaces.AI;
using IqraCore.Interfaces;
using IqraInfrastructure.Services.Audio;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services;
using IqraInfrastructure.Services.TTS;
using IqraCore.Utilities;
using Anthropic.SDK.Messaging;
using System.Text;

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

        private string _currentUserAudioInputTranscribedText;
        private string _currentAIResponseType;
        private StringBuilder _currentAIResponseGeneratedFull;
        private StringBuilder _currentUnprocessedAIResponse;
        private int _currentSectionedCharactersCount;

        private bool _isInitialMessagePlayingEnabled;

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

            _currentAIResponseGeneratedFull = new StringBuilder();
            _currentUnprocessedAIResponse = new StringBuilder();
            _currentSectionedCharactersCount = 0;

            _isInitialMessagePlayingEnabled = true; // todo
        }

        public void Initialize()
        {
            // Context Related
            Dictionary<string, string> SystemPromptVariables = new Dictionary<string, string>()
            {
                {"COMPANY_NAME", "Harub Dental Surgery"},
                {"DATETIME_TODAY", DateTime.Now.ToString() },
                {"DATE_TODAY", DateTime.Now.ToString("dd-MM-yyyy") },
                {"FULL_MONTH_TODAY", DateTime.Now.ToString("MMMM") },
                {"DATE_AND_FULL_DAY_TODAY", DateTime.Now.ToString("dddd, d") },
                {"YEAR_TODAY", DateTime.Now.ToString("yyyy") },
                {"TIME_RIGHT_NOW", DateTime.Now.ToString("HH:mm") }
            };

            _initialMessage = File.ReadAllText("TestInitialMessages.txt").Replace("{{COMPANY_NAME}}", SystemPromptVariables["COMPANY_NAME"]);
            _systemPrompt = File.ReadAllText("TestSystemPrompt.txt");

            _aiService.SetSystemPrompt(_systemPrompt);
            _aiService.SetSystemPromptVariables(SystemPromptVariables);
            _aiService.SetInitialMessage(_initialMessage);  

            // Services Related
            _audioInputService.Initialize();
            _audioInputService.AudioDataReceived += OnAudioDataReceived;

            _audioOutputService.Initialize();

            _sttService.Initialize();
            _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;

            _ttsService.Initialize();

            _aiService.MessageStreamed += OnAIResponseStreaming;

            
            _aiService.SetMaxTokens(128);
            _aiService.SetModel(Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku); // change back to haiku
        }

        public async void Start()
        {
            Console.WriteLine("Starting audio recording and transcription...");
            _audioOutputService.StartPlayback();
            _sttService.StartTranscription();

            if (_isInitialMessagePlayingEnabled)
            {
                await Task.Delay(500);

                CancelAllTasksAndResetBeforeProcessing();
                OnAITextToOuputDevice(_initialMessage);

                await Task.Delay(500);

                while (true)
                {
                    await Task.Delay(100);

                    if (_audioOutputService.IsBufferEmpty())
                    {
                        break;
                    }
                }

                Console.WriteLine("Finished Speaking Initial Message");
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

            // add the template for customer query
            result = $"customer_query: {result}";

            if (result.ToLower().Contains("services"))
            {
                _aiService.SetMaxTokens(333);
            }
            else
            {
                _aiService.SetMaxTokens(128);
            }

            // cancel all tasks
            CancelAllTasksAndResetBeforeProcessing();

            // take in the previous AI Response if it exists and add it to the memory
            if (_aiTask != null)
            {
                if (_currentAIResponseGeneratedFull.Length > 0)
                {
                    if (_aiTask.Status == TaskStatus.Canceled || _aiTask.Status == TaskStatus.WaitingForActivation)
                    {
                        // user spoke over
                        _aiService.AddUserMessage(_currentUserAudioInputTranscribedText);
                        _aiService.AddAssistantMessage(_currentAIResponseGeneratedFull.ToString() + "... It seems you have spoken over while i was speaking so I will let you speak.");
                    }
                    else if (_aiTask.Status == TaskStatus.RanToCompletion)
                    {
                        // user spoke over
                        _aiService.AddUserMessage(_currentUserAudioInputTranscribedText);
                        _aiService.AddAssistantMessage(_currentAIResponseGeneratedFull.ToString());
                    }
                    else
                    {
                        _aiService.AddUserMessage(_currentUserAudioInputTranscribedText);
                        _aiService.AddAssistantMessage("I had some issues with my audio replying to your current query. Can you repeat that or is there something else I can help you with?");
                        // todo end call or soemthing
                        throw new Exception("Should not have happened");
                    }
                }
            }

            _aiTask = null;

            // process the AI generated result
            try
            {
                _currentUserAudioInputTranscribedText = result;
                _currentAIResponseGeneratedFull = new StringBuilder();
                _currentUnprocessedAIResponse = new StringBuilder();
                _currentSectionedCharactersCount = 0;
                _currentAIResponseType = "streaming";

                // Reset the dynamic timings system prompt variables
                var SystemPromptVariables = _aiService.GetSystemPromptVariables();
                SystemPromptVariables["DATETIME_TODAY"] = DateTime.Now.ToString();
                SystemPromptVariables["DATE_TODAY"] = DateTime.Now.ToString("dd-MM-yyyy");
                SystemPromptVariables["FULL_MONTH_TODAY"] = DateTime.Now.ToString("MMMM");
                SystemPromptVariables["DATE_AND_FULL_DAY_TODAY"] = DateTime.Now.ToString("dddd, d");
                SystemPromptVariables["YEAR_TODAY"] = DateTime.Now.ToString("yyyy");
                SystemPromptVariables["TIME_RIGHT_NOW"] = DateTime.Now.ToString("HH:mm");
                _aiService.SetSystemPromptVariables(SystemPromptVariables);

                // Start the AI streaming api call
                _aiTask = _aiService.ProcessInputAsync(result, _aiCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("AI processing cancelled.");
            }

            // start accepting user voice input again
            _audioInputService.StartRecording();
        }

        // USER Audio Text AI Responded Streamed Processed Text To Output Audio
        private void OnAITextToOuputDevice(string section)
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

        // Process AI Streaming Messages
        private void OnAIResponseStreaming(object? sender, object responseObject)
        {
            MessageResponse res = (MessageResponse)responseObject;

            if (res.Delta != null)
            {
                if (res.Delta.StopReason == "end_turn")
                {
                    if (_currentAIResponseType == "response_to_customer")
                    {
                        string remainingString = _currentUnprocessedAIResponse.ToString();
                        if (remainingString.Length > 0)
                        {
                            OnAITextToOuputDevice(remainingString);
                        }

                        throw new TaskCanceledException("TaskCanceledException: End Recieved for: " + _currentAIResponseGeneratedFull.ToString());
                    }
                    
                    if (_currentAIResponseType == "response_to_system")
                    {
                        // todo - implement sending the response to an actual system
                        // block any input speaking while the speaker plays the response and then wait till task has ended

                        OnTranscriptionResultReceived(null, "respone_from_system: successful: booking appointment successful");
                        throw new NotImplementedException("response_to_system: " + _currentUnprocessedAIResponse.ToString());
                    }

                    if (_currentAIResponseType == "end_call")
                    {
                        // todo - implement ending call feature
                        // block any input speaking while the speaker plays the response and then wait till task has ended and end the session or call
                        // set any necessary ending data here
                        OnTranscriptionResultReceived(null, "response_to_user: Thank you for contacting us! If you have any more questions, do not hesitate to get back to us. Have a nice day!"); // make this message dynamic | based on time? nice day? good night?
                        throw new NotImplementedException("end_call: " + _currentUnprocessedAIResponse.ToString());
                    }

                    throw new Exception("Unknown response type recieved: " + _currentUnprocessedAIResponse.ToString());
                }

                if (string.IsNullOrEmpty(res.Delta.Text))
                {
                    return;
                }

                _currentAIResponseGeneratedFull.Append(res.Delta.Text);
                _currentUnprocessedAIResponse.Append(res.Delta.Text);

                if (_currentAIResponseType == "streaming")
                {
                    var responseTemplateSection = _currentUnprocessedAIResponse.ToString().Split(":");
                    if (responseTemplateSection.Length >= 2)
                    {
                        switch (responseTemplateSection[0])
                        {
                            case "response_to_customer":
                                _currentAIResponseType = "response_to_customer";
                                _currentUnprocessedAIResponse.Replace("response_to_customer:", "");
                                break;

                            case "response_to_system":
                                _currentAIResponseType = "response_to_system";
                                _currentUnprocessedAIResponse.Replace("response_to_system:", "");

                                // Temporary, speak for now but disable input till reply comes
                                OnAITextToOuputDevice("Please give me a moment while I make a booking to the system.");
                                break;

                            case "end_call":
                                // end the call
                                _currentAIResponseType = "end_call";
                                Console.WriteLine("End Call Recieved");
                                break;

                            default:
                                break;
                        }
                    }
                }  

                if (_currentAIResponseType == "response_to_customer")
                {
                    var currentText = _currentUnprocessedAIResponse.ToString();

                    var (sections, remaining) = AIResponseHelper.SeparateTextIntoSectionsNew(currentText, ref _currentSectionedCharactersCount);
                    _currentUnprocessedAIResponse = remaining;
                    foreach (var section in sections)
                    {
                        _currentSectionedCharactersCount += section.Length;

                        OnAITextToOuputDevice(section);
                    }
                }
            }
        }       

        private void CancelAllTasksAndResetBeforeProcessing()
        {
            // stop the recording from mic
            _audioInputService.StopRecording();

            // stop output audio playing
            _audioOutputService.StopPlayback();

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

            // reset the cancellation tokens
            _ttsCancellationTokenSource = new CancellationTokenSource();
            _aiCancellationTokenSource = new CancellationTokenSource();

            // clear any audio pending for speaker
            _audioOutputService.ClearAudioData();
            _audioOutputService.StartPlayback();
        }
    }
}