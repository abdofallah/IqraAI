using ElevenLabs;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.ElevenLabs;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ElevenLabsTTSService : ITTSService
    {
        private readonly ILogger<ElevenLabsTTSService> _logger;

        private readonly string _apiKey;
        private readonly ElevenLabsConfig _serviceConfig;

        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalElevenLabsFormat;
        private bool _audioConversationNeeded = false;

        private ElevenLabsClient _client;

        private VoiceResponseModel? _voiceData;
        private ModelResponseModel? _modelData;

        private VoiceSettingsResponseModel _voiceSettings;

        private TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat _outputFormat;
        private BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization _applyTextNormalization;
        private List<PronunciationDictionaryVersionLocatorRequestModel> _pronunciationDictionaryId;

        private List<string> _previousRequestIds = new List<string>();   

        public ElevenLabsTTSService(ILogger<ElevenLabsTTSService> logger, string apiKey, ElevenLabsConfig config)
        {
            _logger = logger;

            _apiKey = apiKey;
            
            _serviceConfig = config;

            _voiceSettings = new VoiceSettingsResponseModel();
            if (_serviceConfig.Stability.HasValue) _voiceSettings.Stability = _serviceConfig.Stability.Value;
            if (_serviceConfig.SimilarityBoost.HasValue) _voiceSettings.SimilarityBoost = _serviceConfig.SimilarityBoost.Value;
            if (_serviceConfig.Style.HasValue) _voiceSettings.Style = _serviceConfig.Style.Value;
            if (_serviceConfig.UseSpeakerBoost.HasValue) _voiceSettings.UseSpeakerBoost = _serviceConfig.UseSpeakerBoost.Value;
            if (_serviceConfig.Speed.HasValue) _voiceSettings.Speed = _serviceConfig.Speed.Value;

            _pronunciationDictionaryId = new List<PronunciationDictionaryVersionLocatorRequestModel>();
            if (!string.IsNullOrEmpty(_serviceConfig.PronunciationDictionaryId))
            {
                _pronunciationDictionaryId.Add(new PronunciationDictionaryVersionLocatorRequestModel() { PronunciationDictionaryId = _serviceConfig.PronunciationDictionaryId });
            }
            if (!string.IsNullOrEmpty(_serviceConfig.ApplyTextNormalization))
            {
                if (_serviceConfig.ApplyTextNormalization == "on")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.On;
                }
                else if (_serviceConfig.ApplyTextNormalization == "off")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Off;
                }
                else if (_serviceConfig.ApplyTextNormalization == "auto")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Auto;
                }
            }
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                _client = new ElevenLabsClient(_apiKey);

                var userSubscriptionResult = await _client.User.GetUserSubscriptionAsync();
                if (userSubscriptionResult.Status != SubscriptionStatusType.Active && userSubscriptionResult.Status != SubscriptionStatusType.Free)
                {
                    return result.SetFailureResult(
                        "CheckAccount:SUBSCRIPTION_NOT_ACTIVE",
                        $"Elevenlabs user scubrption is not active. Current status: {userSubscriptionResult.Status.ToString()}"
                    );
                }
                if (userSubscriptionResult.CharacterCount >= userSubscriptionResult.CharacterLimit)
                {
                    return result.SetFailureResult(
                        "CheckAccount:CHARACTER_LIMIT_REACHED",
                        $"Elevenlabs total character has been reached. Current count: {userSubscriptionResult.CharacterCount}/{userSubscriptionResult.CharacterLimit}"
                    );
                }

                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, ElevenLabsSupportedFormats);
                _optimalElevenLabsFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                    $"ElevenLabs TTS does not support any format that can be reasonably converted to the requested format: " +
                    $"{_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz");

                var formatKey = (_optimalElevenLabsFormat.Encoding, _optimalElevenLabsFormat.SampleRateHz, _optimalElevenLabsFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _outputFormat)) // Set the class-level field
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for the selected optimal ElevenLabs format: {formatKey}");
                }

                _audioConversationNeeded = _optimalElevenLabsFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                        _optimalElevenLabsFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                        _optimalElevenLabsFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample; 

                _voiceData = _client.Voices.GetVoicesByVoiceIdAsync(_serviceConfig.VoiceId).GetAwaiter().GetResult();

                var allModels = _client.Models.GetModelsAsync().GetAwaiter().GetResult().ToList();
                _modelData = allModels.Find(d => d.ModelId == _serviceConfig.ModelId);
                if (_modelData == null) throw new Exception("Model not found");

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    $"Initialize:EXCEPTION",
                    $"Internal server error occured: {ex.Message}"
                );
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var request = new BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPost(text, _modelData.ModelId, null, _voiceSettings, _pronunciationDictionaryId, null, null, null, _previousRequestIds, null, _applyTextNormalization, null);

            try
            {
                var result = await _client.TextToSpeech.CreateTextToSpeechByVoiceIdWithTimestampsAsync(_voiceData.VoiceId, request, null, null, _outputFormat, null, cancellationToken);
                
                if (!string.IsNullOrEmpty(result.Item2))
                {
                    //_previousRequestIds.Add(result.Item2);
                }
                if (_previousRequestIds.Count >= 3)
                {
                    _previousRequestIds.RemoveAt(0);
                }

                byte[] sourceAudioData = Convert.FromBase64String(result.Item1.AudioBase64);

                var duration = result.Item1.Alignment != null && result.Item1.Alignment.CharacterEndTimesSeconds.Any()
                    ? TimeSpan.FromSeconds(result.Item1.Alignment.CharacterEndTimesSeconds.Last())
                    : AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalElevenLabsFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalElevenLabsFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (HttpRequestException ex)
            {

            }
            catch (Exception ex) {
                _logger.LogError(ex, ex.Message);
            }
            

            return (new byte[] { }, TimeSpan.Zero);
        }

        public Task StopTextSynthesisAsync()
        {
            // not needed
            // todo can make task and cancel it
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "ElevenLabsTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat()
        {
            return _optimalElevenLabsFormat;
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ElevenLabsTextToSpeech;
        }

        // STATIC
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> ElevenLabsSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat> FormatMap;

        static ElevenLabsTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            ElevenLabsSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm8000 },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm16000 },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm22050 },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm24000 },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm44100 },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm48000 },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat>(formatMap);
        }
    }
}
