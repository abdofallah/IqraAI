using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.Sarvam;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class SarvamTTSService : ITTSService, IDisposable
    {
        private readonly ILogger<SarvamTTSService> _logger;
        private readonly string _apiKey;
        private readonly SarvamConfig _serviceConfig;

        private const string WsUrl = "wss://api.sarvam.ai/text-to-speech/ws";

        // State
        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalSarvamFormat;
        private SarvamOutputFormatDefinition _selectedApiFormat;
        private bool _audioConversationNeeded = false;

        private static readonly JsonSerializerOptions _jsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        public SarvamTTSService(ILogger<SarvamTTSService> logger, string apiKey, SarvamConfig config)
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
                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, SarvamSupportedFormats);
                _optimalSarvamFormat = bestFallbackOrder.FirstOrDefault();

                if (_optimalSarvamFormat == null)
                {
                    return result.SetFailureResult(
                        "Initialize:FORMAT_NOT_SUPPORTED",
                        $"Sarvam TTS does not support a format compatible with: {_finalUserRequest.RequestedEncoding}"
                    );
                }

                var formatKey = (_optimalSarvamFormat.Encoding, _optimalSarvamFormat.SampleRateHz, _optimalSarvamFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out _selectedApiFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for selected format: {formatKey}");
                }

                _audioConversationNeeded = _optimalSarvamFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                           _optimalSarvamFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                           _optimalSarvamFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var accountCheck = await CheckAccount();
                if (!accountCheck.Success)
                {
                    return result.SetFailureResult(accountCheck.Code, accountCheck.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Sarvam init error: {ex.Message}");
            }
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            // No auth check endpoint available.
            return await Task.FromResult(new FunctionReturnResult().SetSuccessResult());
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text)) return (Array.Empty<byte>(), TimeSpan.Zero);

            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("api-subscription-key", _apiKey);

            try
            {
                var connectUri = new Uri($"{WsUrl}?model={_serviceConfig.Model}&send_completion_event=true");
                await ws.ConnectAsync(connectUri, cancellationToken);

                var configMsg = new SarvamWsMessage<SarvamConfigData>
                {
                    Type = "config",
                    Data = new SarvamConfigData
                    {
                        TargetLanguageCode = _serviceConfig.TargetLanguageCode,
                        Speaker = _serviceConfig.Speaker,
                        SpeechSampleRate = _selectedApiFormat.SampleRate,
                        OutputAudioCodec = "linear16",
                        EnablePreprocessing = _serviceConfig.EnablePreprocessing,
                        Pitch = _serviceConfig.Pitch,
                        Pace = _serviceConfig.Pace,
                        Loudness = _serviceConfig.Loudness
                    }
                };
                await SendJsonAsync(ws, configMsg, cancellationToken);

                var textMsg = new SarvamWsMessage<SarvamTextData>
                {
                    Type = "text",
                    Data = new SarvamTextData { Text = text }
                };
                await SendJsonAsync(ws, textMsg, cancellationToken);

                var flushMsg = new SarvamWsMessage<object> { Type = "flush", Data = new { } };
                await SendJsonAsync(ws, flushMsg, cancellationToken);

                var accumulatedAudioBytes = new List<byte>();
                var buffer = new byte[32 * 1024];
                bool isCompleted = false;

                while (ws.State == WebSocketState.Open && !isCompleted && !cancellationToken.IsCancellationRequested)
                {
                    var ms = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var messageString = Encoding.UTF8.GetString(ms.ToArray());

                    try
                    {
                        using var doc = JsonDocument.Parse(messageString);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeProp))
                        {
                            var type = typeProp.GetString();

                            if (type == "audio")
                            {
                                var audioBase64 = root.GetProperty("data").GetProperty("audio").GetString();
                                if (!string.IsNullOrEmpty(audioBase64))
                                {
                                    accumulatedAudioBytes.AddRange(Convert.FromBase64String(audioBase64));
                                }
                            }
                            else if (type == "event")
                            {
                                var eventType = root.GetProperty("data").GetProperty("event_type").GetString();
                                if (eventType == "final") isCompleted = true;
                            }
                            else if (type == "error")
                            {
                                var errorMsg = root.GetProperty("data").GetProperty("message").GetString();
                                _logger.LogError("Sarvam WS Error: {Msg}", errorMsg);
                                return (Array.Empty<byte>(), TimeSpan.Zero);
                            }
                        }
                    }
                    catch (JsonException) { /* Handle parse error */ }
                }

                if (accumulatedAudioBytes.Count == 0)
                {
                    _logger.LogWarning("Sarvam returned 0 bytes.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] sourceAudioData = accumulatedAudioBytes.ToArray();

                var duration = AudioConversationHelper.CalculateDuration(sourceAudioData, _optimalSarvamFormat);

                if (_audioConversationNeeded)
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(sourceAudioData, _optimalSarvamFormat, _finalUserRequest, false);
                    return (convertedData, duration);
                }

                return (sourceAudioData, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sarvam Synthesis Error");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            finally
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            }
        }

        private async Task SendJsonAsync<T>(ClientWebSocket ws, T data, CancellationToken ct)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        public Task StopTextSynthesisAsync() => Task.CompletedTask;

        public string GetProviderFullName() => "SarvamTextToSpeech";
        public InterfaceTTSProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceTTSProviderEnum GetProviderTypeStatic() => InterfaceTTSProviderEnum.SarvamTextToSpeech;
        public ITTSConfig GetCacheableConfig() => _serviceConfig;

        public TTSProviderAvailableAudioFormat GetCurrentOutputFormat() => _optimalSarvamFormat;

        public void Dispose() => GC.SuppressFinalize(this);

        // =================================================================================================
        // STATIC DATA & MAPPINGS
        // =================================================================================================

        private record SarvamOutputFormatDefinition(int SampleRate);

        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> SarvamSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SarvamOutputFormatDefinition> FormatMap;

        static SarvamTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
            };
            SarvamSupportedFormats = supportedFormats.AsReadOnly();

            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), SarvamOutputFormatDefinition>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), new(8000) },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), new(16000) },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), new(22050) },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), new(24000) },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SarvamOutputFormatDefinition>(formatMap);
        }
    }
}