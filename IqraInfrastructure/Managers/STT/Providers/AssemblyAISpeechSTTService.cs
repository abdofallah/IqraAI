using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public enum AssemblyAIEncoding
    {
        pcm_s16le,
        pcm_mulaw
    }

    public class AssemblyAISpeechSTTService : ISTTService
    {
        private readonly string _apiKey;

        private readonly int _inputSampleRate;
        private readonly int _inputBitsPerSample;
        private readonly AudioEncodingTypeEnum _inputAudioEncodingType;

        private AssemblyAIEncoding _audioEncoding;
        private readonly bool _formatTurns;
        private readonly float _endOfTurnConfidenceThreshold;
        private readonly int _minEndOfTurnSilenceWhenConfident;
        private readonly int _maxTurnSilence;

        private ClientWebSocket _webSocketClient;
        private CancellationTokenSource _cancellationTokenSource;

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
            int inputSampleRate,
            int inputBitsPerSample,
            AudioEncodingTypeEnum inputAudioEncodingType,
            bool formatTurns,
            float endOfTurnConfidenceThreshold,
            int minEndOfTurnSilenceWhenConfident,
            int maxTurnSilence)
        {
            _apiKey = apiKey;

            _inputSampleRate = inputSampleRate;
            _inputBitsPerSample = inputBitsPerSample;
            _inputAudioEncodingType = inputAudioEncodingType;

            _formatTurns = formatTurns;
            _endOfTurnConfidenceThreshold = endOfTurnConfidenceThreshold;
            _minEndOfTurnSilenceWhenConfident = minEndOfTurnSilenceWhenConfident;
            _maxTurnSilence = maxTurnSilence;
        }

        public void Initialize()
        {
            switch (_inputAudioEncodingType)
            {
                case AudioEncodingTypeEnum.PCM:
                    if (_inputBitsPerSample == 16)
                    {
                        _audioEncoding = AssemblyAIEncoding.pcm_s16le;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid bits per sample: {_inputBitsPerSample}");
                    }

                    break;

                case AudioEncodingTypeEnum.MULAW:
                    _audioEncoding = AssemblyAIEncoding.pcm_mulaw;
                    break;

                default:
                    throw new ArgumentException($"Invalid audio encoding type: {_inputAudioEncodingType}");
            }

            _webSocketClient = new ClientWebSocket();
            _webSocketClient.Options.SetRequestHeader("Authorization", _apiKey);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void StartTranscription()
        {
            StartTranscriptionAsync().GetAwaiter().GetResult();
        }

        private async Task StartTranscriptionAsync()
        {
            var uriBuilder = new UriBuilder("wss://streaming.assemblyai.com/v3/ws");
            var query = HttpUtility.ParseQueryString(string.Empty);

            query["sample_rate"] = _inputSampleRate.ToString();
            query["encoding"] = _audioEncoding.ToString();
            query["format_turns"] = _formatTurns.ToString().ToLower();
            query["end_of_turn_confidence_threshold"] = _endOfTurnConfidenceThreshold.ToString();
            query["min_end_of_turn_silence_when_confident"] = _minEndOfTurnSilenceWhenConfident.ToString();
            query["max_turn_silence"] = _maxTurnSilence.ToString();

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
                Console.WriteLine("AssemblyAI listening task cancelled."); // todo logger
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssemblyAI Error: {ex.Message}"); // todo logger
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
                    Console.WriteLine($"AssemblyAI session started: {json}"); // todo logger
                    break;

                case "Turn":
                    var turn = JsonSerializer.Deserialize<AssemblyAITurnMessage>(json);
                    if (turn == null || string.IsNullOrWhiteSpace(turn.Transcript) && !turn.EndOfTurn) return; // Allow empty final turns

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
                    Console.WriteLine($"AssemblyAI session terminated: {json}"); // todo logger
                    break;
            }
        }

        public void StopTranscription()
        {
            StopTranscriptionAsync().GetAwaiter().GetResult();
        }

        public async Task StopTranscriptionAsync()
        {
            if (_webSocketClient?.State == WebSocketState.Open)
            {
                var terminateMessage = JsonSerializer.Serialize(new { type = "Terminate" });
                var messageBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(terminateMessage));
                await _webSocketClient.SendAsync(messageBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(200);
                await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested termination", CancellationToken.None);
            }
            _cancellationTokenSource?.Cancel();
            _webSocketClient?.Dispose();
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_webSocketClient?.State == WebSocketState.Open)
            {
                _webSocketClient.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
        }

        public string GetProviderFullName()
        {
            return "AssemblyAI";
        }

        public InterfaceSTTProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.AssemblyAI;
        }

        private record AssemblyAITurnMessage(
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("transcript")] string Transcript,
            [property: JsonPropertyName("turn_is_formatted")] bool TurnIsFormatted,
            [property: JsonPropertyName("end_of_turn")] bool EndOfTurn
        );
    }
}
