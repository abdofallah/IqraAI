using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.VAD.Silero;
using IqraInfrastructure.Managers.VoiceMailDetection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentVoicemailDetector : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        private readonly ILogger<ConversationAIAgentVoicemailDetector> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly STTProviderManager _sttProviderManager;
        private readonly LLMProviderManager _llmProviderManager;

        private readonly BusinessAppAgentVoicemail _voicemailSettings;

        private CancellationTokenSource _cancellationTokenSource;

        private DateTime _startedAt;
        private bool _hasServiceEnded = false;

        // Public Events
        public event EventHandler OnStopAgentSpeaking;
        public event EventHandler OnEndCallorLeaveMessageRecieved;

        // ML MODEL RELATED STATE
        private BlandAIOnnxVoicemailDetectModel _voicemailMLModel;
        private int _currentMLChecks = 0;
        private List<(string, float)> _currentCheckResult = new List<(string, float)>();
        private bool _hasCompletedMLChecks = false;

        private List<float> _audioBuffer; // 16khz 32bit
        private Task _audioBufferProcessingTask;

        // VAD RELATED STATE
        private IVadService _vadService;
        private DateTime? _firstSpeechDetected = null;
        private DateTime? _silenceDetected = null;

        // VERIFY STT & LLM STATE
        private ISTTService _sttService;
        private string? _sttStringBuffer = null;

        private ILLMService _llmService;
        private bool _hasStartedLLMCheck = false;
        private bool _hasRecievedEndOfResponse = false;
        private StringBuilder _llmResultString;

        // TREIGGERS RELATED STATE
        private Task _triggersTask;
        private bool _hasTriggeredStopSpeakingAgentTrigger = false;
        private bool _hasTriggeredEndCallorLeaveMessageTrigger = false;    

        public ConversationAIAgentVoicemailDetector(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            STTProviderManager sTTProviderManager,
            LLMProviderManager llmProviderManager
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgentVoicemailDetector>();

            _agentState = agentState;
            _sttProviderManager = sTTProviderManager;
            _llmProviderManager = llmProviderManager;

            _audioBuffer = new List<float>();

            //_voicemailSettings = _agentState.BusinessAppAgent.Voicemail;
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            if (!_voicemailSettings.IsEnabled) return;

            _cancellationTokenSource= CancellationTokenSource.CreateLinkedTokenSource(token);

            _vadService = new SileroVadService(_loggerFactory.CreateLogger<SileroVadService>(), token);
            _vadService.VoiceActivityChanged += OnVADResultRecieved;

            _voicemailMLModel = new BlandAIOnnxVoicemailDetectModel();

            _vadService.Initialize(new VadOptions()
            {
                AudioEncodingType = _agentState.AgentConfiguration.AudioEncodingType,
                SampleRate = _agentState.AgentConfiguration.SampleRate,
                BitsPerSample = _agentState.AgentConfiguration.BitsPerSample,

                MinSilenceDurationMs = _voicemailSettings.VoiceMailMessageVADSilenceThresholdMS,
                MinSpeechDurationMs = 300
            });

            if (_voicemailSettings.OnVoiceMailMessageDetectVerifySTTAndLLM)
            {
                await InitalizeLLMSTTServices();
            }
        }

        private async Task InitalizeLLMSTTServices()
        {
            // STT SERVICE
            var sttServiceResult = await _sttProviderManager.BuildProviderServiceByIntegration(
                _agentState.STTBusinessIntegrationData,
                _voicemailSettings.TranscribeVoiceMessageSTT,
                _agentState.AgentConfiguration.SampleRate,
                _agentState.AgentConfiguration.BitsPerSample,
                _agentState.AgentConfiguration.AudioEncodingType
            );

            if (!sttServiceResult.Success || sttServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build voicemail STT service with error: {ErrorMessage}", _agentState.AgentId, sttServiceResult.Message);
                // TODO: Raise error?
                throw new InvalidOperationException($"Failed to build voicemail STT service: {sttServiceResult.Message}");
            }

            _sttService = sttServiceResult.Data;
            _sttService.OnRecoginizingRecieved += OnRecognizingReceived;
            _sttService.Initialize();

            // LLM SERVICE
            _llmResultString = new StringBuilder();

            var llmServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(
                _agentState.LLMBusinessIntegrationData,
                _voicemailSettings.VerifyVoiceMessageLLM,
                new Dictionary<string, string> { }
            );
            if (!llmServiceResult.Success || llmServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build voicemail LLM service with error: {ErrorMessage}", _agentState.AgentId, llmServiceResult.Message);
                throw new InvalidOperationException($"Failed to build voicemail LLM service: {llmServiceResult.Message}");
            }

            _llmService = llmServiceResult.Data;
            _llmService.SetSystemPrompt(
                "You are Chatbot trying to determine if this is a voicemail system or a human.\r\n\r\nYou will be provided with the time elapsed since the start of the call and the message spoken so far.\r\n\r\nIf you hear any of these phrases (or very similar ones):\r\n- \"Please leave a message after the beep\"\r\n- \"No one is available to take your call\"\r\n- \"Record your message after the tone\"\r\n- \"You have reached voicemail for...\"\r\n- \"You have reached [phone number]\"\r\n- \"[phone number] is unavailable\"\r\n- \"The person you are trying to reach...\"\r\n- \"The number you have dialed...\"\r\n- \"Your call has been forwarded to an automated voice messaging system\"\r\nand determine that it is a voicemail, then reply with: \"voice_mail_detected\".\r\n\r\nIf it sounds like a human (saying hello, asking questions, etc.) given atleast a few seconds have elapsed, then reply with: \"human_conversation_detected\".\r\n\r\nIf you are unable to determine if this is a voicemail or human, then reply with: \"unknown\"."
            );
        }

        public async Task StartAsync()
        {
            await Task.Delay(_voicemailSettings.InitialCheckDelayMS);

            _startedAt = DateTime.UtcNow;

            _audioBufferProcessingTask = Task.Run(() => ProcessAudioBuffer(), _cancellationTokenSource.Token);

            if (_voicemailSettings.OnVoiceMailMessageDetectVerifySTTAndLLM)
            {
                _sttService.StartTranscription();
            }

            _triggersTask = Task.Run(() => ProcessTriggerActions(), _cancellationTokenSource.Token);
        }

        public void BufferAudio(byte[] audioData)
        {
            if (_hasServiceEnded) return;
            if (audioData.Length == 0) return;

            if (_voicemailSettings.OnVoiceMailMessageDetectVerifySTTAndLLM)
            {
                _sttService.WriteTranscriptionAudioData(audioData);
            }

            var sourcePcm32FloatProvider = AudioConversationHelper.CreatePcm32FloatProvider(
                audioData,
                new TTSProviderAvailableAudioFormat()
                {
                    Encoding = _agentState.AgentConfiguration.AudioEncodingType,
                    SampleRateHz = _agentState.AgentConfiguration.SampleRate,
                    BitsPerSample = _agentState.AgentConfiguration.BitsPerSample
                }
            );

            var sourcePcm32ResampleTo16khz32bit = AudioConversationHelper.CreateResampler(
                sourcePcm32FloatProvider,
                new AudioRequestDetails()
                {
                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                    RequestedSampleRateHz = 16000,
                    RequestedBitsPerSample = 32
                }
            );

            float[] floatArray = new float[sourcePcm32ResampleTo16khz32bit.WaveFormat.AverageBytesPerSecond / 4];

            int currentRead = 0;
            while ((currentRead = sourcePcm32ResampleTo16khz32bit.Read(floatArray, 0, floatArray.Length)) > 0)
            {
                var currentBuffer = floatArray.Take(currentRead).ToArray();

                _vadService.Process32bitAudio(currentBuffer);
                _audioBuffer.AddRange(currentBuffer);
            }
        }

        private async Task ProcessAudioBuffer()
        {
            double mlCheckDurationInSeconds = _voicemailSettings.MLCheckDurationMS / 1000.0;
            long samplesLength = (long)(16000 * mlCheckDurationInSeconds);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_hasServiceEnded) break;
                if (_hasCompletedMLChecks) break;
                if (_currentMLChecks >= _voicemailSettings.MaxMLCheckTries)
                {
                    _hasCompletedMLChecks = true;
                    break;
                }
                if (_voicemailSettings.WaitForVADSpeechForMLCheck && _firstSpeechDetected == null)
                {
                    await Task.Delay(50, _cancellationTokenSource.Token);
                    continue;
                }
                if (_silenceDetected == null)
                {
                    await Task.Delay(50, _cancellationTokenSource.Token);
                    continue;
                }

                if (_audioBuffer.Count > samplesLength)
                {
                    var currentChunk = _audioBuffer.Take((int)samplesLength).ToArray();
                    _audioBuffer.RemoveRange(0, (int)samplesLength);

                    var prediction = _voicemailMLModel.Predict(currentChunk);
                    _currentCheckResult.Add((prediction.Label, prediction.Confidence));

                    _currentMLChecks++;
                }

                await Task.Delay(50, _cancellationTokenSource.Token);
            }
        }
        
        public async Task ProcessTriggerActions()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_hasServiceEnded) break;
                if (_hasTriggeredStopSpeakingAgentTrigger && _hasTriggeredEndCallorLeaveMessageTrigger)
                {
                    _hasServiceEnded = true;
                    break;
                }

                // Get Results

                // ML CHECK
                bool isMLCheckEnabled = _voicemailSettings.StopSpeakingAgentAfterXMlCheckSuccess;
                bool isMLCheckSuccess = false;
                if (isMLCheckEnabled)
                {
                    if (_hasCompletedMLChecks)
                    {
                        var onlyVoicemailResults = _currentCheckResult.Where(x => x.Item1 == "voicemail").ToList();
                        var totalVoicemailConfidence = onlyVoicemailResults.Sum(x => x.Item2);

                        var onlyHumanResults = _currentCheckResult.Where(x => x.Item1 == "human").ToList();
                        var totalHumanConfidence = onlyHumanResults.Sum(x => x.Item2);

                        var totalConfidence = totalVoicemailConfidence + totalHumanConfidence;

                        var voiceMailResult = totalVoicemailConfidence / totalConfidence;
                        var humanResult = totalHumanConfidence / totalConfidence;

                        if (voiceMailResult > humanResult)
                        {
                            isMLCheckSuccess = true;
                        }
                    }
                }
                else
                {
                    isMLCheckSuccess = true;
                }

                // VAD Silence Check
                bool isVADCheckSuccess = false;
                if (_firstSpeechDetected != null)
                {
                    if (_silenceDetected != null)
                    {
                        isVADCheckSuccess = true;
                    }
                }

                // LLM Confirmation Check
                bool isLLMCheckSuccess = false;
                if (_voicemailSettings.OnVoiceMailMessageDetectVerifySTTAndLLM)
                {
                    if (_hasStartedLLMCheck && _hasRecievedEndOfResponse)
                    {
                        var llmResult = _llmResultString.ToString();
                        if (llmResult.ToLower().Contains("voice_mail_detected"))
                        {
                            isLLMCheckSuccess = true;
                        }
                    }
                }
                else
                {
                    isLLMCheckSuccess = true;
                }

                // Stop Agent Speaking Trigger
                if (!_hasTriggeredStopSpeakingAgentTrigger)
                {
                    var hasAchievedResult = true;
                    if (_voicemailSettings.StopSpeakingAgentAfterXMlCheckSuccess)
                    {
                        if (!isMLCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }
                    if (_voicemailSettings.StopSpeakingAgentAfterVadSilence)
                    {
                        if (!isVADCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }
                    if (_voicemailSettings.StopSpeakingAgentAfterLLMConfirm)
                    {
                        if (!isLLMCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }

                    if (hasAchievedResult)
                    {
                        _hasTriggeredStopSpeakingAgentTrigger = true;

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(_voicemailSettings.StopSpeakingAgentDelayAfterMatchMS, _cancellationTokenSource.Token);
                            OnStopAgentSpeaking?.Invoke(this, null);
                        });
                    }
                }

                // End Call / Leave Message Triggers
                if (!_hasTriggeredEndCallorLeaveMessageTrigger)
                {
                    var hasAchievedResult = true;
                    if (_voicemailSettings.EndOrLeaveMessageAfterXMLCheckSuccess)
                    {
                        if (!isMLCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }
                    if (_voicemailSettings.EndOrLeaveMessageAfterVadSilence)
                    {
                        if (!isVADCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }
                    if (_voicemailSettings.EndOrLeaveMessageAfterLLMConfirm)
                    {
                        if (!isLLMCheckSuccess)
                        {
                            hasAchievedResult = false;
                        }
                    }

                    if (hasAchievedResult)
                    {
                        _hasTriggeredEndCallorLeaveMessageTrigger = true;

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(_voicemailSettings.EndOrLeaveMessageDelayAfterMatchMS, _cancellationTokenSource.Token);
                            OnEndCallorLeaveMessageRecieved?.Invoke(this, null);
                        });
                    }
                }

                await Task.Delay(50, _cancellationTokenSource.Token);
            }
        }

        private void PerformLLMCheck()
        {
            if (!_voicemailSettings.OnVoiceMailMessageDetectVerifySTTAndLLM) return;
            if (_hasServiceEnded) return;
            if (_hasStartedLLMCheck) return;
            _hasStartedLLMCheck = true;

            string llmMessage = "Here is the context from the call so far that you can use to determine the result:\n\n\n";
            if (_voicemailSettings.StopSpeakingAgentAfterXMlCheckSuccess && _hasCompletedMLChecks)
            {
                var predictionSeconds = _voicemailSettings.MLCheckDurationMS / 1000;

                llmMessage += "- Result of the Voicemail Detection Machine Learning Model:\n```";
                for (int i = 0; i < _currentCheckResult.Count; i++)
                {
                    llmMessage += $"Prediciton ({(predictionSeconds) * (i)}seconds to {predictionSeconds * (i + 1)} seconds): {_currentCheckResult[i].Item1} with confidence {_currentCheckResult[i].Item2.ToString("0.00")}";

                    if (i < _currentCheckResult.Count - 1) llmMessage += "\n";
                }
                llmMessage += "```\n\n";
            }

            if (_firstSpeechDetected != null && _silenceDetected != null)
            {
                var speechDetectionSinceStart = _startedAt - _firstSpeechDetected.Value;
                var speechUntilSilence = _silenceDetected.Value - _firstSpeechDetected.Value;
                llmMessage += $"- First Speech Detected:\n```{speechDetectionSinceStart.Seconds} seconds until {speechUntilSilence.Seconds} seconds```\n\n";
            }

            llmMessage += $"Here is the text from the call so far:\n```{_sttStringBuffer}```";

            _llmService.AddUserMessage(llmMessage);
            _llmService.MessageStreamed += OnLLMMessageStreamed;
            _llmService.ProcessInputAsync(_cancellationTokenSource.Token);
        }

        // EVENTS
        private void OnVADResultRecieved(object sender, VadEventArgs args)
        {
            if (_hasServiceEnded) return;
            if (_firstSpeechDetected != null && _silenceDetected != null) return;

            if (args.IsSpeechDetected && _firstSpeechDetected == null)
            {
                if (_voicemailSettings.WaitForVADSpeechForMLCheck)
                {
                    // remove all initial silence audio from buffer
                    var timespan = args.Timestamp;
                    double audioBufferSample = (timespan.TotalSeconds - 0.25 /** speech min vad options **/) * 16000;
                    _audioBuffer.RemoveRange(0, Math.Min((int)audioBufferSample, _audioBuffer.Count));
                }

                _firstSpeechDetected = DateTime.UtcNow;                

                // make sure we are not stuck in loop of no silence ending
                Task.Run(async () =>
                    {
                        await Task.Delay(_voicemailSettings.VoiceMailMessageVADMaxSpeechDurationMS, _cancellationTokenSource.Token);
                        OnVadSilenceRecived();
                    },
                    _cancellationTokenSource.Token
                );
            }

            if (!args.IsSpeechDetected)
            {
                OnVadSilenceRecived();
            }
        }

        private void OnVadSilenceRecived()
        {
            if (_hasServiceEnded) return;
            if (_silenceDetected != null) return;
            _silenceDetected = DateTime.UtcNow;

            PerformLLMCheck();
        }

        private void OnRecognizingReceived(object sender, string text)
        {
            if (_hasServiceEnded) return;

            _sttStringBuffer = text;
        }

        private void OnLLMMessageStreamed(object sender, ConversationAgentEventLLMStreamed eventData)
        {
            string? deltaText;
            bool isEndOfResponse;

            FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(eventData.ResponseObject, _llmService!.GetProviderType());
            if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
            {
                _logger.LogError("Agent {AgentId}: Error extracting voicemail LLM chunk, {Reason}", _agentState.AgentId, chunkExtractResult.Message);
                // TODO: Raise error? Stop processing this response?
                return;
            }

            deltaText = chunkExtractResult.Data.Value.deltaText;
            isEndOfResponse = chunkExtractResult.Data.Value.isEndOfResponse;

            if (!string.IsNullOrEmpty(deltaText))
            {
                _llmResultString.Append(deltaText);
            }

            if (isEndOfResponse)
            {
                _hasRecievedEndOfResponse = true;
            }
        }

        public void Dispose()
        {
            if (_hasServiceEnded) return;
            _hasServiceEnded = true;

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch { /* Ignore */ }
            
            try
            {
                _audioBufferProcessingTask.Wait(1000);
            }
            catch { /* Ignore */ }

            try
            {
                _triggersTask.Wait(1000);
            }
            catch { /* Ignore */ }

            try
            {
                _cancellationTokenSource.Dispose();
            }
            catch { /* Ignore */ }

            try
            {
                _vadService?.Dispose();
            }
            catch { /* Ignore */ }
            
            try
            {
                _voicemailMLModel?.Dispose();
            }
            catch { /* Ignore */ }

            try
            {
                _llmService?.Dispose();
            }
            catch { /* Ignore */ }
        }
    }
}
