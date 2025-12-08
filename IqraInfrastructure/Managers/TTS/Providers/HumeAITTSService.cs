using Hume;
using Hume.Tts;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.HumeAI;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class HumeAITTSService : ITTSService, IDisposable
    {
        private readonly ILogger<HumeAITTSService> _logger;
        private readonly string _apiKey;
        private readonly HumeAiConfig _serviceConfig;

        // SDK Client
        private HumeClient? _client;
        private OctaveVersion _modelOctaveVersion;

        // State
        private AudioRequestDetails _finalUserRequest;
        private HumeAIOutputFormatDefinition _selectedApiFormat;
        private TTSProviderAvailableAudioFormat _optimalHumeFormat;
        private bool _audioConversationNeeded = false;

        // Context for Continuity
        private string? _lastGenerationId = null;

        public HumeAITTSService(ILogger<HumeAITTSService> logger, string apiKey, HumeAiConfig config)
        {
            _logger = logger;
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                if (_serviceConfig.ModelVersion != 1 && _serviceConfig.ModelVersion != 2)
                {
                    return result.SetFailureResult(
                        "Initialize:INVALID_MODEL_VERSION",
                        "Hume AI TTS does not support a model version other than 1 or 2"
                    );
                }

                _modelOctaveVersion = _serviceConfig.ModelVersion == 1 ? OctaveVersion.One : OctaveVersion.Two;

                if (_modelOctaveVersion == OctaveVersion.Two && string.IsNullOrEmpty(_serviceConfig.VoiceId))
                {
                    return result.SetFailureResult(
                        "Initialize:VOICE_ID_REQUIRED",
                        "Hume AI TTS requires a Voice ID when using Model Version 2 (Octave)"
                    );
                }

                _client = new HumeClient(_apiKey);

                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, HumeSupportedFormats);
                _optimalHumeFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalHumeFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Hume AI TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalHumeFormat.Encoding, _optimalHumeFormat.SampleRateHz, _optimalHumeFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalHumeFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalHumeFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalHumeFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Hume init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();
            try
            {
                if (_client == null) return result.SetFailureResult("CheckAccount:CLIENT_NULL", "Client not initialized");

                // Lightweight validation: We assume success if initialized. 
                // Actual key validation happens on first request due to Hume API design.
                return await Task.FromResult(result.SetSuccessResult());
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("CheckAccount:EXCEPTION", ex.Message);
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (_client == null) throw new InvalidOperationException("Service not initialized.");
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            try
            {
                var utterance = new PostedUtterance
                {
                    Text = text,
                    Description = !string.IsNullOrEmpty(_serviceConfig.VoiceDescription) ? _serviceConfig.VoiceDescription : null,
                    Speed = _serviceConfig.VoiceSpeed,
                };

                if (!string.IsNullOrEmpty(_serviceConfig.VoiceId))
                {
                    utterance.Voice = new PostedUtteranceVoiceWithId
                    {
                        Id = _serviceConfig.VoiceId,
                        Provider = _serviceConfig.VoiceProvider?.ToLower() == "custom" ? VoiceProvider.CustomVoice : VoiceProvider.HumeAi
                    };
                }

                var request = new PostedTts
                {
                    Utterances = new List<PostedUtterance> { utterance },
                    Format = new FormatPcm
                    {
                        Type = _selectedApiFormat.FormatString // "pcm" from our map
                    },
                    NumGenerations = 1,
                    StripHeaders = true,
                    SplitUtterances = false,
                    Version = _modelOctaveVersion
                };

                if (!string.IsNullOrEmpty(_lastGenerationId))
                {
                    request.Context = new PostedContextWithGenerationId { GenerationId = _lastGenerationId };
                }

                var response = await _client.Tts.SynthesizeJsonAsync(request, cancellationToken: cancellationToken);

                if (response.Generations == null || !response.Generations.Any())
                {
                    _logger.LogError("Hume AI returned no generations.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var generation = response.Generations.First();

                if (!string.IsNullOrEmpty(generation.GenerationId))
                {
                    _lastGenerationId = generation.GenerationId;
                }

                byte[] sourceAudioData = Convert.FromBase64String(generation.Audio);

                TimeSpan duration = TimeSpan.FromSeconds(generation.Duration);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalHumeFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (HumeClientApiException ex)
            {
                _logger.LogError("Hume API Error {Code}: {Message}", ex.StatusCode, ex.Message);
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hume Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            return Task.CompletedTask;
        }

        public string GetProviderFullName() => "HumeAITextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.HumeAITextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalHumeFormat;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record HumeAIOutputFormatDefinition(string FormatString, int SampleRateHz, int BitsPerSample);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> HumeSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), HumeAIOutputFormatDefinition> FormatMap;

        static HumeAITTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            HumeSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), HumeAIOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new("pcm", 48000, 16) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), HumeAIOutputFormatDefinition>(formatMap);
        }
    }
}