using IqraCore.Interfaces.AI;
using IqraCore.Interfaces;
using IqraCore.Utilities;
using Anthropic.SDK.Messaging;
using System.Text;
using IqraCore.Entities.Business;
using System.IO;
using IqraCore.Entities;

namespace ProjectIqra
{
    public class DebugApp
    {
        private string _sessionId;

        private readonly Business _business;

        private string _currentLanguage;
        private string _currentSpeakerName;

        private readonly IAudioCache _audioCache;

        private IAudioInputService _audioInputService;
        private IAudioOutputService _audioOutputService;
        private ISTTService _sttService;
        private ITTSService _ttsService;
        private IAIService _aiService;

        private Task? _aiTask;
        private CancellationTokenSource _aiCancellationTokenSource;

        private List<Task> _ttsTasks;
        private CancellationTokenSource _ttsCancellationTokenSource;

        private string _initialMessage;
        private string _systemPrompt;

        /** Dynamic Variables For Runtime **/
        private string _currentUserAudioInputTranscribedText;
        private string _currentAIResponseType;
        private StringBuilder _currentAIResponseGeneratedFull;
        private StringBuilder _currentUnprocessedAIResponse;
        private int _currentSectionedCharactersCount;

        private bool _isInitialMessagePlayingEnabled;

        private bool _sstAcceptBuffer;
        /** END - Dynamic Variables **/

        public event EventHandler OnLowerVolumeEvent;
        public event EventHandler OnIncreaseVolumeEvent;

        public event EventHandler OnEndCallEvent;

        public EventHandler<string> OnCallKeyPressEvent;

        public DebugApp(IAudioCache audioCache, Business business)
        {
            _business = business;

            _audioCache = audioCache;

            _ttsTasks = new List<Task>();
            _ttsCancellationTokenSource = new CancellationTokenSource();

            _aiTask = null;
            _aiCancellationTokenSource = new CancellationTokenSource();

            _currentAIResponseGeneratedFull = new StringBuilder();
            _currentUnprocessedAIResponse = new StringBuilder();
            _currentSectionedCharactersCount = 0;

            _isInitialMessagePlayingEnabled = false; // todo
        }

        public void SetLanguage(string language)
        {
            _currentLanguage = language;
        }

        public void SetSpeakerName(string speakerName)
        {
            _currentSpeakerName = speakerName;
        }
        
        public void SetServices(IAudioInputService audioInputService, IAudioOutputService audioOutputService, ISTTService sttService, ITTSService ttsService, IAIService aiService)
        {
            _audioInputService = audioInputService;
            _audioOutputService = audioOutputService;
            _sttService = sttService;
            _ttsService = ttsService;
            _aiService = aiService;
        }

        public void Initialize()
        {
            _aiService.SetTemplateVariables(_business.BusinessPromptData.TemplateVariables[_currentLanguage]);

            var dynamicVariablesWithTemplateVariables = SetDynamicVariablesToTemplateVariables();
            _initialMessage = ApplyTemplateVariablesToString(_business.BusinessInitialMessage[_currentLanguage], dynamicVariablesWithTemplateVariables);
            _systemPrompt = ApplyTemplateVariablesToString(_business.BusinessSystemPrompt[_currentLanguage], dynamicVariablesWithTemplateVariables);

            _aiService.SetInitialMessage(_initialMessage);
            _aiService.SetSystemPrompt(_systemPrompt);

            // Services Related
            _audioInputService.Initialize();
            _audioInputService.AudioDataReceived += OnAudioDataReceived;

            _audioOutputService.Initialize();

            _sttService.Initialize();
            _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;
            _sttService.OnRecoginizingRecieved += OnRecoginizingRecieved;

            _ttsService.Initialize();

            _aiService.MessageStreamed += OnAIResponseStreaming;
            
            _aiService.SetMaxTokens(512);
            _aiService.SetModel(Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku);  
        }

        public async void Start(string sessionId)
        {
            _sessionId = sessionId;
            using (var fileStream = new FileStream($"{sessionId}.raw", FileMode.CreateNew))
            {
                fileStream.Dispose();
            }

            _sstAcceptBuffer = false;

            Console.WriteLine("Starting audio recording and transcription...");
            _audioOutputService.StartPlayback();
            _sttService.StartTranscription();
            _audioInputService.StartRecording();

            if (_isInitialMessagePlayingEnabled)
            {
                await Task.Delay(500);

                OnAITextToOuputDevice(_initialMessage);

                await WaitTillAudioIsDone(500);

                Console.WriteLine("Finished Speaking Initial Message");
            }

            _sstAcceptBuffer = true;
        }

        public void Stop()
        {
            Console.WriteLine("Stopping audio recording and transcription...");
            _sttService.StopTranscription();
            _audioOutputService.StopPlayback();
            _audioInputService.StopRecording();
        }

        // USER VOICE TO TEXT
        private void OnAudioDataReceived(object? sender, (byte[], int) result)
        {
            if (!_sstAcceptBuffer) return;

            using (var fileStream = new FileStream($"sessionAudio/{_sessionId}.raw", FileMode.Append)) // make this path dynamic
            {
                fileStream.Write(result.Item1, 0, result.Item1.Length);
            }

            _sttService.WriteTranscriptionAudioData(result.Item1);
        }

        // USER VOICE TEXT TO AI
        private async void OnTranscriptionResultReceived(object? sender, string result)
        {
            if (string.IsNullOrWhiteSpace(result)) { return; }
            Console.WriteLine($"Transcription result: {result}");

            OnIncreaseVolumeEvent.Invoke(this, EventArgs.Empty);

            // add the template for customer query
            result = $"customer_query: {result}";

            // cancel all tasks
            await CancelAllTasksAndResetBeforeProcessing();
            _sstAcceptBuffer = false;

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
                var SystemPromptVariables = SetDynamicVariablesToTemplateVariables();
                _aiService.SetTemplateVariables(SystemPromptVariables);

                // Start the AI streaming api call
                _aiTask = _aiService.ProcessInputAsync(result, _aiCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("AI processing cancelled.");
            }

            // start accepting user voice input again
            _sstAcceptBuffer = true;
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
        private async void OnAIResponseStreaming(object? sender, object responseObject)
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
                    }
                    else if (_currentAIResponseType == "response_to_system")
                    {
                        // todo - implement sending the response to an actual system
                        // block any input speaking while the speaker plays the response and then wait till task has ended

                        OnTranscriptionResultReceived(null, "respone_from_system: successful: booking appointment successful");

                        await WaitTillAudioIsDone(100);

                        _sstAcceptBuffer = true;
                    }
                    else if (_currentAIResponseType == "end_call")
                    {
                        // todo - implement ending call feature
                        // block any input speaking while the speaker plays the response and then wait till task has ended and end the session or call
                        // set any necessary ending data here
                        OnAITextToOuputDevice("Thank you for contacting us! If you have any more questions, do not hesitate to get back to us. Have a nice day!"); // make this message dynamic | based on time? nice day? good night?

                        await WaitTillAudioIsDone(500);

                        OnEndCallEvent.Invoke(this, EventArgs.Empty); // todo add more data
                    }
                    else if (_currentAIResponseType == "streaming" || string.IsNullOrWhiteSpace(_currentAIResponseType))
                    {
                        throw new Exception("Unknown response type recieved: " + _currentUnprocessedAIResponse.ToString());
                    }
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

                                _sstAcceptBuffer = false;

                                OnAITextToOuputDevice("Please give me a moment while I make a booking to the system.");
                                break;

                            case "end_call":
                                _currentAIResponseType = "end_call";
                                _currentUnprocessedAIResponse.Replace("end_call:", "");

                                _sstAcceptBuffer = false;

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

        /** Other Functions out of flow **/

        private void OnRecoginizingRecieved(object? sender, object result)
        {
            OnLowerVolumeEvent.Invoke(this, EventArgs.Empty);
        }

        /** Helpers **/

        private async Task CancelAllTasksAndResetBeforeProcessing()
        {
            // stop the recording from mic
            _audioInputService.StopRecording();

            // stop output audio playing
            _audioOutputService.StopPlayback();

            // cancel tasks and wait
            await CancelTasksAndRenewTokens();

            // clear any audio pending for speaker
            _audioOutputService.ClearAudioData();
            _audioOutputService.StartPlayback();

            _audioInputService.StartRecording();
        }

        public async Task CancelTasksAndRenewTokens()
        {
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
            await Task.WhenAll(tasks);

            // reset the tasks
            _ttsTasks.Clear();

            // reset the cancellation tokens
            _ttsCancellationTokenSource = new CancellationTokenSource();
            _aiCancellationTokenSource = new CancellationTokenSource();
        }

        private Dictionary<string, string> SetDynamicVariablesToTemplateVariables()
        {
            var SystemPromptVariables = _aiService.GetSystemPromptVariables();
            SystemPromptVariables["DATETIME_TODAY"] = DateTime.Now.ToString();
            SystemPromptVariables["DATE_TODAY"] = DateTime.Now.ToString("dd-MM-yyyy");
            SystemPromptVariables["FULL_MONTH_TODAY"] = DateTime.Now.ToString("MMMM");
            SystemPromptVariables["DATE_AND_FULL_DAY_TODAY"] = DateTime.Now.ToString("dddd, d");
            SystemPromptVariables["YEAR_TODAY"] = DateTime.Now.ToString("yyyy");
            SystemPromptVariables["TIME_RIGHT_NOW"] = DateTime.Now.ToString("HH:mm");

            return SystemPromptVariables;
        }

        private string ApplyTemplateVariablesToString(string template, Dictionary<string, string> variables)
        {
            string result = template;

            // do something about these variables maybe
            if (!string.IsNullOrWhiteSpace(_initialMessage))
            {
                result.Replace("{{INITIAL_MESSAGE}}", _initialMessage);
            }
            result.Replace("{{COMPANY_NAME}}", _business.BusinessName[_currentLanguage]);

            foreach (var variable in variables)
            {
                result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            } 

            return result;
        }

        private async Task WaitTillAudioIsDone(int initialDelay = 100)
        {
            await Task.Delay(initialDelay);

            while (true)
            {
                await Task.Delay(10);

                if (_audioOutputService.IsBufferEmpty() == true)
                {
                    break;
                }   
            }
        }
    }
}