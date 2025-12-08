using Deepgram.Clients.Interfaces.v1;
using Deepgram.Models.Speak.v1.REST;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Deepgram;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class DeepgramTTSService : ITTSService, IDisposable
    {
        private readonly ILogger<DeepgramTTSService> _logger;
        private readonly string _apiKey;
        private readonly DeepgramConfig _serviceConfig;

        // Clients
        private ISpeakRESTClient? _speakClient;
        private IManageClient? _manageClient;

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalDeepgramFormat;
        private DeepgramOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        public DeepgramTTSService(ILogger<DeepgramTTSService> logger, string apiKey, DeepgramConfig config)
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
                // Initialize Clients
                _speakClient = new Deepgram.Clients.Speak.v1.REST.Client(_apiKey);
                _manageClient = new Deepgram.Clients.Manage.v1.Client(_apiKey);

                // Prepare Request Details
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                // Select Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, DeepgramSupportedFormats);
                _optimalDeepgramFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalDeepgramFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Deepgram TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz"
                    );
                }

                // Map to API Definition
                var formatKey = (_optimalDeepgramFormat.Encoding, _optimalDeepgramFormat.SampleRateHz, _optimalDeepgramFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                // Determine Conversion Needs
                _audioConversationNeeded = _optimalDeepgramFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalDeepgramFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalDeepgramFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                // Check Account
                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Deepgram init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();
            try
            {
                if (_manageClient == null) return result.SetFailureResult("CheckAccount:CLIENT_NULL", "Client not initialized");

                var projectsResponse = await _manageClient.GetProjects();
                if (projectsResponse == null || projectsResponse.Projects == null || !projectsResponse.Projects.Any())
                {
                    return result.SetFailureResult("CheckAccount:NO_PROJECTS", "Deepgram API Key is valid but has no associated projects.");
                }

                // Validate Balance for the first project
                // We check the first project associated with the key.
                // There should only be one per api key, but we could ask user to define project id as well, but ig not for now
                var projectId = projectsResponse.Projects.First().ProjectId;
                var balancesResponse = await _manageClient.GetBalances(projectId);

                if (balancesResponse != null && balancesResponse.Balances != null)
                {
                    // Check if there is at least one balance entry with funds > 0
                    bool hasFunds = balancesResponse.Balances.Any(b => b.Amount > 0);

                    if (!hasFunds)
                    {
                        _logger.LogWarning("Deepgram Project {ProjectId} has 0 balance.", projectId);
                        return result.SetFailureResult("CheckAccount:INSUFFICIENT_FUNDS", "Deepgram account balance is 0 or less.");
                    }
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deepgram Account Check Failed.");
                return result.SetFailureResult("CheckAccount:EXCEPTION", $"Failed to validate Deepgram Account: {ex.Message}");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (_speakClient == null) throw new InvalidOperationException("Service not initialized.");
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            var textSource = new TextSource(text);

            // Build Schema from our pre-calculated mappings
            var speakSchema = new SpeakSchema()
            {
                Model = _serviceConfig.ModelId,
                Encoding = _selectedApiFormat.Encoding,
                Container = _selectedApiFormat.Container, // 'none' for raw PCM
                SampleRate = _selectedApiFormat.SampleRate.ToString(),
            };

            // Link tokens
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Call Deepgram SDK
                SyncResponse response = await _speakClient.ToStream(textSource, speakSchema, cts);

                if (response?.Stream != null)
                {
                    byte[] sourceAudioData;
                    using (var ms = new MemoryStream())
                    {
                        await response.Stream.CopyToAsync(ms, cts.Token);
                        sourceAudioData = ms.ToArray();
                    }

                    var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalDeepgramFormat);

                    if (_audioConversationNeeded)
                    {
                        var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalDeepgramFormat, _finalUserRequest, false);
                        return (convertedData, duration);
                    }

                    return (sourceAudioData, duration);
                }
                else
                {
                    _logger.LogError("Deepgram TTS returned null stream.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (OperationCanceledException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deepgram TTS Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "DeepgramTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.DeepgramTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalDeepgramFormat;

        public void Dispose()
        {
            (_speakClient as IDisposable)?.Dispose();
            (_manageClient as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record DeepgramOutputFormatDefinition(string Container, string Encoding, int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> DeepgramSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), DeepgramOutputFormatDefinition> FormatMap;

        static DeepgramTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },
            };
            DeepgramSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), DeepgramOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new("raw", "linear16", 8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new("raw", "linear16", 16000) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new("raw", "linear16", 24000) },
                { (AudioEncodingTypeEnum.PCM, 32000, 16), new("raw", "linear16", 32000) },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), new("raw", "linear16", 48000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), DeepgramOutputFormatDefinition>(formatMap);
        }
    }
}