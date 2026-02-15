using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
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

        private Voice? _voiceData;
        private Model? _modelData;

        private VoiceSettings _voiceSettings;

        private OutputFormat _outputFormat;
        private TextNormalization _applyTextNormalization;
        private List<PronunciationDictionaryLocator> _pronunciationDictionaryId;

        public ElevenLabsTTSService(ILogger<ElevenLabsTTSService> logger, string apiKey, ElevenLabsConfig config)
        {
            _logger = logger;

            _apiKey = apiKey;
            
            _serviceConfig = config;

            _voiceSettings = new VoiceSettings();
            if (_serviceConfig.Stability.HasValue) _voiceSettings.Stability = _serviceConfig.Stability.Value;
            if (_serviceConfig.SimilarityBoost.HasValue) _voiceSettings.SimilarityBoost = _serviceConfig.SimilarityBoost.Value;
            if (_serviceConfig.Style.HasValue) _voiceSettings.Style = _serviceConfig.Style.Value;
            if (_serviceConfig.UseSpeakerBoost.HasValue) _voiceSettings.SpeakerBoost = _serviceConfig.UseSpeakerBoost.Value;
            if (_serviceConfig.Speed.HasValue) _voiceSettings.Speed = _serviceConfig.Speed.Value;

            _pronunciationDictionaryId = new List<PronunciationDictionaryLocator>();
            if (!string.IsNullOrEmpty(_serviceConfig.PronunciationDictionaryId))
            {
                _pronunciationDictionaryId.Add(new PronunciationDictionaryLocator(_serviceConfig.PronunciationDictionaryId, null));
            }
            if (!string.IsNullOrEmpty(_serviceConfig.ApplyTextNormalization))
            {
                if (_serviceConfig.ApplyTextNormalization == "on")
                {
                    _applyTextNormalization = TextNormalization.On;
                }
                else if (_serviceConfig.ApplyTextNormalization == "off")
                {
                    _applyTextNormalization = TextNormalization.Off;
                }
                else if (_serviceConfig.ApplyTextNormalization == "auto")
                {
                    _applyTextNormalization = TextNormalization.Auto;
                }
            }
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                _client = new ElevenLabsClient(_apiKey);

                var userSubscriptionResult = await _client.UserEndpoint.GetSubscriptionInfoAsync();
                if (userSubscriptionResult.Status != "active" && userSubscriptionResult.Status != "free" && userSubscriptionResult.Status != "trialing")
                {
                    return result.SetFailureResult(
                        "Initialize:SUBSCRIPTION_NOT_ACTIVE",
                        $"Elevenlabs user scubrption is not active. Current status: {userSubscriptionResult.Status.ToString()}"
                    );
                }
                if (userSubscriptionResult.CharacterCount >= userSubscriptionResult.CharacterLimit)
                {
                    return result.SetFailureResult(
                        "Initialize:CHARACTER_LIMIT_REACHED",
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

                _voiceData = _client.VoicesEndpoint.GetVoiceAsync(_serviceConfig.VoiceId, true).GetAwaiter().GetResult();

                var allModels = _client.ModelsEndpoint.GetModelsAsync().GetAwaiter().GetResult().ToList();
                _modelData = allModels.Find(d => d.Id == _serviceConfig.ModelId);
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
            var request = new TextToSpeechRequest(_voiceData, text, null, _voiceSettings, _outputFormat, _modelData, null, null, null, null, null, false, null, _pronunciationDictionaryId, _applyTextNormalization, null);

            try
            {
                var result = await _client.TextToSpeechEndpoint.TextToSpeechAsync(request, null, cancellationToken);

                byte[] sourceAudioData = result.ClipData.ToArray();

                var duration = result.TimestampedTranscriptCharacters != null && result.TimestampedTranscriptCharacters.Any()
                    ? TimeSpan.FromSeconds(result.TimestampedTranscriptCharacters.Last().EndTime)
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
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), OutputFormat> FormatMap;

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

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), OutputFormat>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), OutputFormat.PCM_8000 },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), OutputFormat.PCM_16000 },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), OutputFormat.PCM_22050 },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), OutputFormat.PCM_24000 },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), OutputFormat.PCM_44100 },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), OutputFormat.PCM_48000 },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), OutputFormat>(formatMap);
        }
    }
}
