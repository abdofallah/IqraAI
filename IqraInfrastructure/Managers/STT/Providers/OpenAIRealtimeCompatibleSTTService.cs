using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class OpenAIRealtimeCompatibleSTTConfig
    {
        public string Model { get; set; } = "";
        public string? Language { get; set; }
        public double VadThreshold { get; set; } = 0.5;
        public int SilenceDurationMs { get; set; } = 500;
        public int PrefixPaddingMs { get; set; } = 300;
    }

    public class OpenAIRealtimeCompatibleSTTService : ISTTService
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly OpenAIRealtimeCompatibleSTTConfig _config;

        // Input Audio Format
        private readonly TTSProviderAvailableAudioFormat _inputAudioDetails;

        // Internal State
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;

        // Audio Conversion
        private bool _audioConversionNeeded = false;
        private AudioRequestDetails _targetProviderFormatDetails;

        // Events
        public event EventHandler<string> TranscriptionResultReceived;
        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        // The only format we support sending to the Realtime API
        private static readonly TTSProviderAvailableAudioFormat RealtimeRequiredFormat = new()
        {
            Encoding = AudioEncodingTypeEnum.PCM,
            SampleRateHz = 16000,
            BitsPerSample = 16
        };

        public OpenAIRealtimeCompatibleSTTService(
            string apiKey,
            string endpoint,
            OpenAIRealtimeCompatibleSTTConfig config,
            TTSProviderAvailableAudioFormat inputAudioDetails)
        {
            _apiKey = apiKey;
            _endpoint = endpoint;
            _config = config;
            _inputAudioDetails = inputAudioDetails;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // Check if audio conversion is needed (input → PCM16 16kHz 16bit)
                _audioConversionNeeded = _inputAudioDetails.Encoding != RealtimeRequiredFormat.Encoding ||
                                         _inputAudioDetails.SampleRateHz != RealtimeRequiredFormat.SampleRateHz ||
                                         _inputAudioDetails.BitsPerSample != RealtimeRequiredFormat.BitsPerSample;

                if (_audioConversionNeeded)
                {
                    _targetProviderFormatDetails = new AudioRequestDetails
                    {
                        RequestedEncoding = RealtimeRequiredFormat.Encoding,
                        RequestedSampleRateHz = RealtimeRequiredFormat.SampleRateHz,
                        RequestedBitsPerSample = RealtimeRequiredFormat.BitsPerSample
                    };
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Internal error: {ex.Message}");
            }
        }

        public void StartTranscription()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    _webSocket = new ClientWebSocket();

                    // Set authentication headers
                    if (!string.IsNullOrEmpty(_apiKey))
                    {
                        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                    }
                    _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

                    // Build WebSocket URL from base endpoint
                    // User provides: https://api.openai.com/v1 or http://localhost:8080/v1
                    // We derive:    wss://api.openai.com/v1/realtime?model=MODEL or ws://localhost:8080/v1/realtime?model=MODEL
                    var wsUrl = BuildWebSocketUrl(_endpoint, _config.Model);

                    await _webSocket.ConnectAsync(new Uri(wsUrl), _cancellationTokenSource.Token);

                    // Send session configuration
                    await SendSessionUpdate();

                    // Start the receive loop
                    await ReceiveLoop(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
                catch (Exception ex)
                {
                    OnRecoginizingCancelled?.Invoke(this, ex);
                }
            }, _cancellationTokenSource.Token);
        }

        private static string BuildWebSocketUrl(string baseEndpoint, string model)
        {
            // Remove trailing slash
            var endpoint = baseEndpoint.TrimEnd('/');

            // Convert http(s) to ws(s)
            string wsEndpoint;
            if (endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                wsEndpoint = "wss://" + endpoint.Substring("https://".Length);
            }
            else if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                wsEndpoint = "ws://" + endpoint.Substring("http://".Length);
            }
            else
            {
                // Already a ws/wss URL or unknown scheme
                wsEndpoint = endpoint;
            }

            return $"{wsEndpoint}/realtime?model={Uri.EscapeDataString(model)}";
        }

        private async Task SendSessionUpdate()
        {
            var sessionUpdate = new
            {
                type = "session.update",
                session = new
                {
                    input_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = _config.Model,
                        language = _config.Language
                    },
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = _config.VadThreshold,
                        silence_duration_ms = _config.SilenceDurationMs,
                        prefix_padding_ms = _config.PrefixPaddingMs
                    }
                }
            };

            var json = JsonSerializer.Serialize(sessionUpdate, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token
            );
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[8192];
            var messageBuilder = new StringBuilder();

            while (_webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        // Only process when we have the complete message
                        if (result.EndOfMessage)
                        {
                            ProcessServerEvent(messageBuilder.ToString());
                            messageBuilder.Clear();
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (WebSocketException) { break; }
                catch (Exception ex)
                {
                    OnRecoginizingCancelled?.Invoke(this, ex);
                    break;
                }
            }
        }

        private void ProcessServerEvent(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("type", out var typeProp)) return;

                var eventType = typeProp.GetString();

                switch (eventType)
                {
                    // Interim / partial transcription
                    case "conversation.item.input_audio_transcription.delta":
                        {
                            if (doc.RootElement.TryGetProperty("delta", out var deltaProp))
                            {
                                var text = deltaProp.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    OnRecoginizingRecieved?.Invoke(this, text);
                                }
                            }
                        }
                        break;

                    // Final completed transcription
                    case "conversation.item.input_audio_transcription.completed":
                        {
                            if (doc.RootElement.TryGetProperty("transcript", out var transcriptProp))
                            {
                                var text = transcriptProp.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    TranscriptionResultReceived?.Invoke(this, text);
                                }
                            }
                        }
                        break;

                    // Error events
                    case "error":
                        {
                            var errorMessage = "Unknown error";
                            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                            {
                                if (errorProp.TryGetProperty("message", out var msgProp))
                                {
                                    errorMessage = msgProp.GetString() ?? errorMessage;
                                }
                            }
                            OnRecoginizingCancelled?.Invoke(this, new Exception($"OpenAI Realtime Error: {errorMessage}"));
                        }
                        break;

                    // Other events we don't need to handle:
                    // session.created, session.updated, input_audio_buffer.speech_started,
                    // input_audio_buffer.speech_stopped, input_audio_buffer.committed, etc.
                    default:
                        break;
                }
            }
            catch (JsonException)
            {
                // Malformed JSON — ignore
            }
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] dataToSend = data;

                    // Convert audio to PCM16 16kHz 16bit if needed
                    if (_audioConversionNeeded)
                    {
                        var (converted, _) = AudioConversationHelper.Convert(
                            data,
                            _inputAudioDetails,
                            _targetProviderFormatDetails,
                            false
                        );
                        if (converted != null) dataToSend = converted;
                        else return;
                    }

                    // Build the input_audio_buffer.append event
                    var appendEvent = JsonSerializer.Serialize(new
                    {
                        type = "input_audio_buffer.append",
                        audio = Convert.ToBase64String(dataToSend)
                    });

                    var bytes = Encoding.UTF8.GetBytes(appendEvent);
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource?.Token ?? CancellationToken.None
                    );
                }
                catch (Exception)
                {
                    // Silently ignore send failures (connection may be closing)
                }
            });
        }

        public void StopTranscription()
        {
            _cancellationTokenSource?.Cancel();

            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }
                }
                catch { }

                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        public string GetProviderFullName() => "OpenAI Realtime Compatible";

        public InterfaceSTTProviderEnum GetProviderType() => GetProviderTypeStatic();

        public static InterfaceSTTProviderEnum GetProviderTypeStatic() => InterfaceSTTProviderEnum.OpenAIRealtimeCompatible;
    }
}
