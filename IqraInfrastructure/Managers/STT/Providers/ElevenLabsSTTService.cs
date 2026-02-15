using ElevenLabs;
using ElevenLabs.SpeechToText;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class ElevenLabsSTTService : ISTTService
    {
        private readonly string _apiKey;
        private readonly string _modelId;
        private readonly TTSProviderAvailableAudioFormat _inputAudioDetails;

        private readonly string _languageCode;
        private readonly double _vadSilenceThresholdSeconds;
        private readonly double _vadThreshold;
        private readonly int _minSpeechDurationMS;
        private readonly int _minSilenceDurationMS;

        private ElevenLabsClient _client;
        private SpeechToTextSession _session;
        private CancellationTokenSource _cancellationSource;

        private TTSProviderAvailableAudioFormat _optimalElevenLabsFormat;
        private AudioFormat _elevenLabsApiFormatEnum;
        private bool _audioConversionNeeded = false;
        private AudioRequestDetails _targetProviderFormatDetails;

        public event EventHandler<string> TranscriptionResultReceived;
        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        public ElevenLabsSTTService(
            string apiKey,
            string modelId,
            string languageCode,
            double vadSilenceThresholdSeconds,
            double vadThreshold,
            int minSpeechDurationMS,
            int minSilenceDurationMS,
            TTSProviderAvailableAudioFormat inputAudioDetails
        ) {
            _apiKey = apiKey;
            _modelId = modelId;
            _languageCode = languageCode;
            _vadSilenceThresholdSeconds = vadSilenceThresholdSeconds;
            _vadThreshold = vadThreshold;
            _minSpeechDurationMS = minSpeechDurationMS;
            _minSilenceDurationMS = minSilenceDurationMS;
            _inputAudioDetails = inputAudioDetails;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                _client = new ElevenLabsClient(_apiKey);

                // Validate Subscription (Standard checks)
                var userSubscriptionResult = await _client.UserEndpoint.GetSubscriptionInfoAsync();
                if (userSubscriptionResult.Status != "active" && userSubscriptionResult.Status != "free" && userSubscriptionResult.Status != "trialing")
                {
                    return result.SetFailureResult(
                        "Initialize:SUBSCRIPTION_NOT_ACTIVE",
                        $"Elevenlabs user subscription is not active. Current status: {userSubscriptionResult.Status}"
                    );
                }
                if (userSubscriptionResult.CharacterCount >= userSubscriptionResult.CharacterLimit)
                {
                    return result.SetFailureResult(
                        "Initialize:CHARACTER_LIMIT_REACHED",
                        $"Elevenlabs total character has been reached. Current count: {userSubscriptionResult.CharacterCount}/{userSubscriptionResult.CharacterLimit}"
                    );
                }

                // Determine Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(
                    new AudioRequestDetails()
                    {
                        RequestedEncoding = _inputAudioDetails.Encoding,
                        RequestedBitsPerSample = _inputAudioDetails.BitsPerSample,
                        RequestedSampleRateHz = _inputAudioDetails.SampleRateHz
                    },
                    ElevenLabsSupportedFormats
                );

                _optimalElevenLabsFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                    $"ElevenLabs STT does not support any format that can be reasonably converted from the input format: " +
                    $"{_inputAudioDetails.Encoding} @ {_inputAudioDetails.SampleRateHz}Hz");

                // Map to SDK Enum
                var formatKey = (_optimalElevenLabsFormat.Encoding, _optimalElevenLabsFormat.SampleRateHz, _optimalElevenLabsFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _elevenLabsApiFormatEnum))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for the selected optimal ElevenLabs format: {formatKey}");
                }

                // Check if conversion is needed (User Format vs Selected Provider Format)
                _audioConversionNeeded = _optimalElevenLabsFormat.Encoding != _inputAudioDetails.Encoding ||
                                         _optimalElevenLabsFormat.SampleRateHz != _inputAudioDetails.SampleRateHz ||
                                         _optimalElevenLabsFormat.BitsPerSample != _inputAudioDetails.BitsPerSample;

                // Create a request detail object for the conversion helper if needed
                if (_audioConversionNeeded)
                {
                    _targetProviderFormatDetails = new AudioRequestDetails
                    {
                        RequestedEncoding = _optimalElevenLabsFormat.Encoding,
                        RequestedSampleRateHz = _optimalElevenLabsFormat.SampleRateHz,
                        RequestedBitsPerSample = _optimalElevenLabsFormat.BitsPerSample
                    };
                }

                // Verify Model Exists
                var allModels = await _client.ModelsEndpoint.GetModelsAsync();
                if (allModels.All(d => d.Id != _modelId))
                {
                    throw new Exception($"Model {_modelId} not found in ElevenLabs available models.");
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize", ex.Message);
            }
        }

        public void StartTranscription()
        {
            _cancellationSource = new CancellationTokenSource();

            // We start a background task to initialize the session and then pump messages
            Task.Run(async () =>
            {
                try
                {
                    var config = new SpeechToTextSessionConfiguration
                    {
                        ModelId = _modelId,
                        AudioFormat = _elevenLabsApiFormatEnum,
                        CommitStrategy = CommitStrategy.Vad,
                        IncludeTimestamps = false,
                        IncludeLanguageDetection = false,
                        LanguageCode = _languageCode,
                        VadSilenceThresholdSecs = _vadSilenceThresholdSeconds,
                        VadThreshold = _vadThreshold,
                        MinSpeechDurationMs = _minSpeechDurationMS,
                        MinSilenceDurationMs = _minSilenceDurationMS,
                        EnableLogging = false
                    };

                    // 1. Create and Connect the Session
                    _session = await _client.SpeechToTextEndpoint.CreateSpeechToTextSessionAsync(config, _cancellationSource.Token);

                    // 2. Start the Message Pump
                    // We subscribe to ISpeechToTextServerEvent so we get ALL event types sent by the server.
                    await _session.ReceiveUpdatesAsync<IServerEvent>(receivedEvent =>
                    {
                        HandleServerEvent(receivedEvent);
                    }, _cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
                catch (Exception ex)
                {
                    OnRecoginizingCancelled?.Invoke(this, ex);
                }
            }, _cancellationSource.Token);
        }

        private void HandleServerEvent(IServerEvent obj)
        {
            switch (obj)
            {
                case PartialTranscript partial:
                    if (!string.IsNullOrEmpty(partial.Text))
                    {
                        OnRecoginizingRecieved?.Invoke(this, partial.Text);
                    }
                    break;

                case CommittedTranscript committed:
                    if (!string.IsNullOrEmpty(committed.Text))
                    {
                        TranscriptionResultReceived?.Invoke(this, committed.Text);
                    }
                    break;

                case SpeechToTextError error:
                    OnRecoginizingCancelled?.Invoke(this, new Exception($"ElevenLabs Error: {error.ErrorMessage}"));
                    break;
            }
        }

        public void StopTranscription()
        {
            // Stop the ReceiveUpdatesAsync loop
            _cancellationSource?.Cancel();

            if (_session != null)
            {
                _session.Dispose();
                _session = null;
            }
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_session == null) return;

            // Fire and forget send
            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] dataToSend = data;

                    // CONVERSION LOGIC
                    if (_audioConversionNeeded)
                    {
                        var (converted, _) = AudioConversationHelper.Convert(
                            data,
                            _inputAudioDetails,
                            _targetProviderFormatDetails,
                            false
                        );

                        if (converted != null)
                        {
                            dataToSend = converted;
                        }
                    }

                    // Create the chunk
                    var chunk = new InputAudioChunk(
                        Convert.ToBase64String(dataToSend),
                        commit: false
                    );

                    // Pass the token so it cancels if StopTranscription is called mid-send
                    await _session.SendAudioChunkAsync(chunk, _cancellationSource?.Token ?? CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // If sending fails silently (e.g. task cancelled), we might ignore it or log it
                    if (_cancellationSource != null && !_cancellationSource.IsCancellationRequested)
                    {
                        OnRecoginizingCancelled?.Invoke(this, ex);
                    }
                }
            });
        }

        public string GetProviderFullName()
        {
            return "ElevenLabs Speech to Text";
        }

        public static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.ElevenLabs;
        }

        public InterfaceSTTProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        // STATIC CONFIGURATION
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> ElevenLabsSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), AudioFormat> FormatMap;

        static ElevenLabsSTTService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 }
            };
            ElevenLabsSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), AudioFormat>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), AudioFormat.Pcm8000 },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), AudioFormat.Pcm16000 },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), AudioFormat.Pcm22050 },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), AudioFormat.Pcm24000 },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), AudioFormat.Pcm44100 },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), AudioFormat.Pcm48000 },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), AudioFormat>(formatMap);
        }
    }
}