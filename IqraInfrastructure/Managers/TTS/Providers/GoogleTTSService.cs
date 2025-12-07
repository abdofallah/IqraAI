using Google.Apis.Auth.OAuth2;
using Google.Cloud.Billing.V1;
using Google.Cloud.ResourceManager.V3;
using Google.Cloud.TextToSpeech.V1;
using Grpc.Auth;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Google;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class GoogleTTSService : ITTSService
    {
        private readonly ILogger<GoogleTTSService> _logger;
        private readonly string _projectId;
        private readonly string _serviceAccountKeyJson;
        private readonly GoogleConfig _serviceConfig;

        // Clients
        private TextToSpeechClient? _ttsClient;

        // Request Objects
        private VoiceSelectionParams _voiceSelectionParams;
        private AudioConfig _audioConfig;

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalGoogleFormat;
        private GoogleOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public GoogleTTSService(ILogger<GoogleTTSService> logger, string projectId, string serviceAccountKeyJson, GoogleConfig config)
        {
            _logger = logger;
            _projectId = projectId;
            _serviceAccountKeyJson = serviceAccountKeyJson;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, GoogleSupportedFormats);
                _optimalGoogleFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalGoogleFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Google TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                var formatKey = (_optimalGoogleFormat.Encoding, _optimalGoogleFormat.SampleRateHz, _optimalGoogleFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalGoogleFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalGoogleFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalGoogleFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                _voiceSelectionParams = new VoiceSelectionParams
                {
                    LanguageCode = _serviceConfig.LanguageCode,
                    Name = _serviceConfig.VoiceName
                };

                _audioConfig = new AudioConfig
                {
                    AudioEncoding = _selectedApiFormat.Encoding,
                    SampleRateHertz = _selectedApiFormat.SampleRate,
                    SpeakingRate = _serviceConfig.SpeakingRate,
                    Pitch = _serviceConfig.Pitch
                };

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Google TTS init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();
            try
            {
                var scopes = new List<string>();
                scopes.AddRange(ProjectsClient.DefaultScopes);
                scopes.AddRange(CloudBillingClient.DefaultScopes);
                scopes.AddRange(TextToSpeechClient.DefaultScopes);

                var credential = GoogleCredential
                    .FromJson(_serviceAccountKeyJson)
                    .CreateScoped(scopes)
                    .ToChannelCredentials();

                var projectsClient = new ProjectsClientBuilder { ChannelCredentials = credential }.Build();
                var projectInfo = await projectsClient.GetProjectAsync($"projects/{_projectId}");

                if (projectInfo == null)
                {
                    return result.SetFailureResult("CheckAccount:PROJECT_NOT_FOUND", "Google Cloud Project not found.");
                }

                var billingClient = new CloudBillingClientBuilder { ChannelCredentials = credential }.Build();
                var billingInfo = await billingClient.GetProjectBillingInfoAsync($"projects/{_projectId}");

                if (!billingInfo.BillingEnabled)
                {
                    return result.SetFailureResult("CheckAccount:BILLING_DISABLED", "Google Cloud Project billing is disabled.");
                }

                _ttsClient = new TextToSpeechClientBuilder
                {
                    ChannelCredentials = credential,
                    QuotaProject = _projectId
                }.Build();

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Account Check Failed.");
                return result.SetFailureResult("CheckAccount:EXCEPTION", $"Failed to validate Google Credentials: {ex.Message}");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (_ttsClient == null) throw new InvalidOperationException("Service not initialized.");
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            var input = new SynthesisInput { Text = text };

            try
            {
                var response = await _ttsClient.SynthesizeSpeechAsync(input, _voiceSelectionParams, _audioConfig, cancellationToken);

                byte[] sourceAudioData = response.AudioContent.ToByteArray();


                if (_audioConversationNeeded)
                {
                    var (convertedData, convertedDuration) = AudioConversationHelper.Convert(sourceAudioData, _optimalGoogleFormat, _finalUserRequest, false);
                    return (convertedData, convertedDuration);
                }

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalGoogleFormat);
                return (sourceAudioData, duration);
            }
            catch (OperationCanceledException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google TTS Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "GoogleCloudTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.GoogleCloudTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalGoogleFormat;

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record GoogleOutputFormatDefinition(AudioEncoding Encoding, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> GoogleSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), GoogleOutputFormatDefinition> FormatMap;

        static GoogleTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            GoogleSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), GoogleOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new(AudioEncoding.Pcm, 8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new(AudioEncoding.Pcm, 16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new(AudioEncoding.Pcm, 22050) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new(AudioEncoding.Pcm, 24000) },
                { (AudioEncodingTypeEnum.PCM, 32000, 16), new(AudioEncoding.Pcm, 32000) },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), new(AudioEncoding.Pcm, 44100) },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new(AudioEncoding.Pcm, 48000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), GoogleOutputFormatDefinition>(formatMap);
        }
    }
}