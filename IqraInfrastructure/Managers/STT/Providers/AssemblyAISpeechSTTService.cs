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
using System.Text.Json.Serialization;
using System.Web;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class AssemblyAISpeechSTTService : ISTTService
    {
        private readonly string _apiKey;

        // The format coming FROM the platform/user
        private readonly TTSProviderAvailableAudioFormat _inputAudioDetails;

        // AssemblyAI Configuration
        private readonly bool _formatTurns;
        private readonly float _endOfTurnConfidenceThreshold;
        private readonly int _minEndOfTurnSilenceWhenConfident;
        private readonly int _maxTurnSilence;
        private readonly double _vadThreshold;
        private readonly string[] _keytermsPrompt;
        private readonly string _speechModel;

        // Internal State
        private ClientWebSocket _webSocketClient;
        private CancellationTokenSource _cancellationTokenSource;

        // Conversion / Format State
        private TTSProviderAvailableAudioFormat _optimalAssemblyFormat;
        private bool _audioConversionNeeded = false;
        private AudioRequestDetails _targetProviderFormatDetails;

        // Events
        private event EventHandler<string> _transcriptionResultReceived;
        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }

        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        public AssemblyAISpeechSTTService(
            string apiKey,
            bool formatTurns,
            float endOfTurnConfidenceThreshold,
            int minEndOfTurnSilenceWhenConfident,
            int maxTurnSilence,
            double vadThreshold,
            string[] keytermsPrompt,
            string speechModel,
            TTSProviderAvailableAudioFormat inputAudioDetails
        )
        {
            _apiKey = apiKey;

            _inputAudioDetails = inputAudioDetails;

            _formatTurns = formatTurns;
            _endOfTurnConfidenceThreshold = endOfTurnConfidenceThreshold;
            _minEndOfTurnSilenceWhenConfident = minEndOfTurnSilenceWhenConfident;
            _maxTurnSilence = maxTurnSilence;
            _vadThreshold = vadThreshold;
            _keytermsPrompt = keytermsPrompt;
            _speechModel = speechModel;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // Determine Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(
                    new AudioRequestDetails()
                    {
                        RequestedEncoding = _inputAudioDetails.Encoding,
                        RequestedBitsPerSample = _inputAudioDetails.BitsPerSample,
                        RequestedSampleRateHz = _inputAudioDetails.SampleRateHz
                    },
                    AssemblyAISupportedFormats
                );

                _optimalAssemblyFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                     $"AssemblyAI STT does not support any format that can be reasonably converted from the input format: " +
                     $"{_inputAudioDetails.Encoding} @ {_inputAudioDetails.SampleRateHz}Hz");

                // Check if conversion is needed
                _audioConversionNeeded = _optimalAssemblyFormat.Encoding != _inputAudioDetails.Encoding ||
                                         _optimalAssemblyFormat.SampleRateHz != _inputAudioDetails.SampleRateHz ||
                                         _optimalAssemblyFormat.BitsPerSample != _inputAudioDetails.BitsPerSample;

                if (_audioConversionNeeded)
                {
                    _targetProviderFormatDetails = new AudioRequestDetails
                    {
                        RequestedEncoding = _optimalAssemblyFormat.Encoding,
                        RequestedSampleRateHz = _optimalAssemblyFormat.SampleRateHz,
                        RequestedBitsPerSample = _optimalAssemblyFormat.BitsPerSample
                    };
                }

                // Prepare WebSocket Client
                _webSocketClient = new ClientWebSocket();
                _webSocketClient.Options.SetRequestHeader("Authorization", _apiKey);
                _cancellationTokenSource = new CancellationTokenSource();

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initialize:EXCEPTION",
                    $"Internal error: {ex.Message}"
                );
            }
        }

        public void StartTranscription()
        {
            StartTranscriptionAsync().GetAwaiter().GetResult();
        }

        private async Task StartTranscriptionAsync()
        {
            var uriBuilder = new UriBuilder("wss://streaming.assemblyai.com/v3/ws");
            var query = HttpUtility.ParseQueryString(string.Empty);

            // Important: We tell AssemblyAI the format of the data we are *sending* (the converted format), 
            // not necessarily the original input format.
            query["sample_rate"] = _optimalAssemblyFormat.SampleRateHz.ToString();
            query["encoding"] = "pcm_s16le";

            query["format_turns"] = _formatTurns.ToString().ToLower();
            query["end_of_turn_confidence_threshold"] = _endOfTurnConfidenceThreshold.ToString();
            query["min_end_of_turn_silence_when_confident"] = _minEndOfTurnSilenceWhenConfident.ToString();
            query["max_turn_silence"] = _maxTurnSilence.ToString();
            query["vad_threshold"] = _vadThreshold.ToString();
            query["keyterms_prompt"] = string.Join(",", _keytermsPrompt);
            query["speech_model"] = _speechModel;

            uriBuilder.Query = query.ToString();

            await _webSocketClient.ConnectAsync(uriBuilder.Uri, CancellationToken.None);

            _ = Task.Run(() => ListenForMessages(_cancellationTokenSource.Token));
        }

        private async Task ListenForMessages(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            try
            {
                while (_webSocketClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocketClient.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        ProcessMessage(messageJson);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed connection", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task cancelled
            }
            catch (Exception ex)
            {
                OnRecoginizingCancelled?.Invoke(this, ex);
            }
        }

        private void ProcessMessage(string json)
        {
            using var jsonDoc = JsonDocument.Parse(json);
            if (!jsonDoc.RootElement.TryGetProperty("type", out var typeProperty)) return;

            var messageType = typeProperty.GetString();

            switch (messageType)
            {
                case "Begin":
                    // Session started
                    break;

                case "Turn":
                    var turn = JsonSerializer.Deserialize<AssemblyAITurnMessage>(json);
                    if (turn == null || (string.IsNullOrWhiteSpace(turn.Transcript) && !turn.EndOfTurn)) return;

                    if (turn.EndOfTurn)
                    {
                        if (_formatTurns && !turn.TurnIsFormatted)
                        {
                            break;
                        }
                        _transcriptionResultReceived?.Invoke(this, turn.Transcript ?? string.Empty);
                    }
                    else
                    {
                        OnRecoginizingRecieved?.Invoke(this, turn.Transcript ?? string.Empty);
                    }
                    break;

                case "Termination":
                    // Session terminated
                    break;
            }
        }

        public void StopTranscription()
        {
            StopTranscriptionAsync().GetAwaiter().GetResult();
        }

        public async Task StopTranscriptionAsync()
        {
            try
            {
                if (_webSocketClient?.State == WebSocketState.Open)
                {
                    var terminateMessage = JsonSerializer.Serialize(new { type = "Terminate" });
                    var messageBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(terminateMessage));
                    await _webSocketClient.SendAsync(messageBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

                    await Task.Delay(200); // Give server time to ack
                    await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested termination", CancellationToken.None);
                }
            }
            catch { /* Ignore close errors */ }

            _cancellationTokenSource?.Cancel();
            _webSocketClient?.Dispose();
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_webSocketClient?.State != WebSocketState.Open) return;

            // Fire and forget send
            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] dataToSend = data;

                    // CONVERSION LOGIC
                    if (_audioConversionNeeded)
                    {
                        var (convertedData, _) = AudioConversationHelper.Convert(
                           data,
                           _inputAudioDetails,
                           _targetProviderFormatDetails,
                           false // Mono
                       );

                        if (convertedData != null)
                        {
                            dataToSend = convertedData;
                        }
                    }

                    await _webSocketClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        OnRecoginizingCancelled?.Invoke(this, ex);
                    }
                }
            });
        }

        public string GetProviderFullName() => "AssemblyAI";

        public InterfaceSTTProviderEnum GetProviderType() => GetProviderTypeStatic();

        public static InterfaceSTTProviderEnum GetProviderTypeStatic() => InterfaceSTTProviderEnum.AssemblyAI;

        private record AssemblyAITurnMessage(
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("transcript")] string Transcript,
            [property: JsonPropertyName("turn_is_formatted")] bool TurnIsFormatted,
            [property: JsonPropertyName("end_of_turn")] bool EndOfTurn
        );

        // STATIC CONFIGURATION
        // AssemblyAI supports "Any" sample rate, but we list the standard ones here so our Fallback Selector
        // can explicitly match standard audio formats (16-bit PCM) closest to the user's input.
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> AssemblyAISupportedFormats;

        static AssemblyAISpeechSTTService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 }
            };
            AssemblyAISupportedFormats = supportedFormats.AsReadOnly();
        }
    }
}