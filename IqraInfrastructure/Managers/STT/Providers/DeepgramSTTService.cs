using Deepgram;
using Deepgram.Clients.Interfaces.v2;
using Deepgram.Models.Authenticate.v1;
using Deepgram.Models.Listen.v2.WebSocket;
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
using System.Web;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class DeepgramSTTService : ISTTService
    {
        private readonly string _apiKey;

        // Common Config
        private readonly string _model;
        private readonly string _language;
        private readonly List<string> _keywordsList;
        private readonly int _silenceTimeout;

        // V1 (Standard/Nova) Config
        private readonly bool _speakerDiarization;
        private readonly bool _punctuate;
        private readonly bool _smartFormat;
        private readonly bool _fillerWords;
        private readonly bool _profanityFilter;

        // V2 (Flux) Config
        private readonly double _fluxEotThreshold;

        // Input Audio Format
        private readonly TTSProviderAvailableAudioFormat _inputAudioDetails;

        // Internal State
        private IListenWebSocketClient _v1Client; // SDK client for Nova-2/3 (Standard)
        private ClientWebSocket _fluxClient;      // Raw socket for Flux (V2)
        private CancellationTokenSource _cancellationTokenSource;

        // Fallback / Conversion State
        private TTSProviderAvailableAudioFormat _optimalDeepgramFormat;
        private string _deepgramEncodingString;
        private bool _audioConversionNeeded = false;
        private AudioRequestDetails _targetProviderFormatDetails;

        // Events
        public event EventHandler<string> TranscriptionResultReceived;
        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        private bool IsFluxModel => _model.StartsWith("flux");

        public DeepgramSTTService(
            string apiKey,
            string language,
            string model,
            List<string> keywordsList,
            int silenceTimeout,
            bool speakerDiarization,
            bool punctuate,
            bool smartFormat,
            bool fillerWords,
            bool profanityFilter,
            double fluxEotThreshold,
            TTSProviderAvailableAudioFormat inputAudioDetails
        )
        {
            _apiKey = apiKey;
            _language = language;
            _model = model;
            _keywordsList = keywordsList;
            _silenceTimeout = silenceTimeout;

            // V1 Flags
            _speakerDiarization = speakerDiarization;
            _punctuate = punctuate;
            _smartFormat = smartFormat;
            _fillerWords = fillerWords;
            _profanityFilter = profanityFilter;

            // V2 Params
            _fluxEotThreshold = fluxEotThreshold;

            _inputAudioDetails = inputAudioDetails;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // 1. Determine Optimal Format
                // We restrict strictly to the formats defined in DeepgramSupportedFormats (Linear16/32)
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(
                    new AudioRequestDetails()
                    {
                        RequestedEncoding = _inputAudioDetails.Encoding,
                        RequestedBitsPerSample = _inputAudioDetails.BitsPerSample,
                        RequestedSampleRateHz = _inputAudioDetails.SampleRateHz
                    },
                    DeepgramSupportedFormats
                );

                _optimalDeepgramFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                    $"Deepgram STT does not support any format that can be reasonably converted from the input format: " +
                    $"{_inputAudioDetails.Encoding} @ {_inputAudioDetails.SampleRateHz}Hz");

                // 2. Map to Deepgram Encoding String (Strict Check)
                var formatKey = (_optimalDeepgramFormat.Encoding, _optimalDeepgramFormat.BitsPerSample);
                if (!EncodingMap.TryGetValue(formatKey, out _deepgramEncodingString))
                {
                    // This should theoretically not happen if FallbackSelector works correctly with the static list
                    throw new InvalidOperationException($"Selected format {_optimalDeepgramFormat.Encoding} {_optimalDeepgramFormat.BitsPerSample}bit is not supported by this Deepgram integration.");
                }

                // 3. Check Conversion Necessity
                _audioConversionNeeded = _optimalDeepgramFormat.Encoding != _inputAudioDetails.Encoding ||
                                         _optimalDeepgramFormat.SampleRateHz != _inputAudioDetails.SampleRateHz ||
                                         _optimalDeepgramFormat.BitsPerSample != _inputAudioDetails.BitsPerSample;

                if (_audioConversionNeeded)
                {
                    _targetProviderFormatDetails = new AudioRequestDetails
                    {
                        RequestedEncoding = _optimalDeepgramFormat.Encoding,
                        RequestedSampleRateHz = _optimalDeepgramFormat.SampleRateHz,
                        RequestedBitsPerSample = _optimalDeepgramFormat.BitsPerSample
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

            if (IsFluxModel)
            {
                StartFluxSession(_cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            else
            {
                StartStandardSession(_cancellationTokenSource.Token);
            }
        }

        private void StartStandardSession(CancellationToken token)
        {
            try
            {
                var options = new DeepgramWsClientOptions(apiKey: _apiKey, keepAlive: true);
                _v1Client = ClientFactory.CreateListenWebSocketClient(_apiKey, options);

                _v1Client.Subscribe(new EventHandler<ResultResponse>((sender, e) => {
                    var transcript = e.Channel?.Alternatives?.FirstOrDefault()?.Transcript;
                    if (string.IsNullOrWhiteSpace(transcript)) return;

                    if (e.IsFinal.GetValueOrDefault())
                    {
                        TranscriptionResultReceived?.Invoke(this, transcript);
                    }
                    else
                    {
                        OnRecoginizingRecieved?.Invoke(this, transcript);
                    }
                }));

                _v1Client.Subscribe(new EventHandler<ErrorResponse>((sender, e) => {
                    OnRecoginizingCancelled?.Invoke(this, new Exception(e.Message));
                }));

                // Build Schema with V1 Flags
                var liveSchema = new LiveSchema()
                {
                    Model = _model,
                    Language = _language,
                    Encoding = _deepgramEncodingString,
                    SampleRate = _optimalDeepgramFormat.SampleRateHz,
                    Channels = 1,
                    InterimResults = true,
                    SmartFormat = _smartFormat,
                    Punctuate = _punctuate,
                    Diarize = _speakerDiarization,
                    FillerWords = _fillerWords,
                    ProfanityFilter = _profanityFilter,
                    EndPointing = _silenceTimeout.ToString()
                };

                // Handle Keyterms/Keywords
                if (_model.Contains("nova-3")) liveSchema.Keyterm = _keywordsList;
                else liveSchema.Keywords = _keywordsList;

                _v1Client.Connect(liveSchema).Wait();
            }
            catch (Exception ex)
            {
                OnRecoginizingCancelled?.Invoke(this, ex);
            }
        }

        private async Task StartFluxSession(CancellationToken token)
        {
            try
            {
                _fluxClient = new ClientWebSocket();
                _fluxClient.Options.SetRequestHeader("Authorization", $"Token {_apiKey}");

                var uriBuilder = new UriBuilder("wss://api.deepgram.com/v2/listen");
                var query = HttpUtility.ParseQueryString(string.Empty);

                query["model"] = _model;
                query["encoding"] = _deepgramEncodingString;
                query["sample_rate"] = _optimalDeepgramFormat.SampleRateHz.ToString();
                query["eot_threshold"] = _fluxEotThreshold.ToString();
                query["eot_timeout_ms"] = _silenceTimeout.ToString();

                if (_keywordsList != null && _keywordsList.Any())
                {
                    foreach (var k in _keywordsList) query.Add("keyterm", k);
                }

                uriBuilder.Query = query.ToString();

                await _fluxClient.ConnectAsync(uriBuilder.Uri, token);

                _ = Task.Run(() => ListenFluxLoop(token));
            }
            catch (Exception ex)
            {
                OnRecoginizingCancelled?.Invoke(this, ex);
            }
        }

        private async Task ListenFluxLoop(CancellationToken token)
        {
            var buffer = new byte[4096];
            try
            {
                while (_fluxClient.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _fluxClient.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessFluxMessage(json);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { OnRecoginizingCancelled?.Invoke(this, ex); }
        }

        private void ProcessFluxMessage(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var typeProp)) return;

            if (typeProp.GetString() == "TurnInfo")
            {
                if (doc.RootElement.TryGetProperty("transcript", out var transProp))
                {
                    var text = transProp.GetString();
                    if (string.IsNullOrWhiteSpace(text)) return;

                    var evt = doc.RootElement.GetProperty("event").GetString();

                    if (evt == "EndOfTurn")
                    {
                        TranscriptionResultReceived?.Invoke(this, text);
                    }
                    else
                    {
                        OnRecoginizingRecieved?.Invoke(this, text);
                    }
                }
            }
            else if (typeProp.GetString() == "Error")
            {
                OnRecoginizingCancelled?.Invoke(this, new Exception($"Flux Error: {json}"));
            }
        }

        public void StopTranscription()
        {
            _cancellationTokenSource?.Cancel();

            if (IsFluxModel)
            {
                _fluxClient?.Abort();
                _fluxClient?.Dispose();
                _fluxClient = null;
            }
            else
            {
                _v1Client?.Stop();
                _v1Client = null;
            }
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            byte[] dataToSend = data;

            // Conversion Logic
            if (_audioConversionNeeded)
            {
                try
                {
                    var (converted, _) = AudioConversationHelper.Convert(
                        data,
                        _inputAudioDetails,
                        _targetProviderFormatDetails,
                        false
                    );
                    if (converted != null) dataToSend = converted;
                }
                catch (Exception)
                {
                    // If conversion fails, sending raw data might just produce static, 
                    // but we generally catch this to avoid crashing the thread.
                    return;
                }
            }

            if (IsFluxModel)
            {
                if (_fluxClient?.State == WebSocketState.Open)
                {
                    _fluxClient.SendAsync(new ArraySegment<byte>(dataToSend), WebSocketMessageType.Binary, true, default);
                }
            }
            else
            {
                if (_v1Client?.State() == WebSocketState.Open)
                {
                    _v1Client.Send(dataToSend);
                }
            }
        }

        public string GetProviderFullName() => "Deepgram";

        public InterfaceSTTProviderEnum GetProviderType() => GetProviderTypeStatic();

        public static InterfaceSTTProviderEnum GetProviderTypeStatic() => InterfaceSTTProviderEnum.Deepgram;

        // STATIC CONFIGURATION
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> DeepgramSupportedFormats;
        private static readonly Dictionary<(AudioEncodingTypeEnum, int), string> EncodingMap;

        static DeepgramSTTService()
        {
            // Restrict strictly to Linear PCM 16 and 32 bit
            var formats = new List<TTSProviderAvailableAudioFormat>();

            int[] rates = { 8000, 16000, 24000, 44100, 48000 };

            foreach (var r in rates)
            {
                formats.Add(new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, BitsPerSample = 16, SampleRateHz = r });
                formats.Add(new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, BitsPerSample = 32, SampleRateHz = r });
            }

            DeepgramSupportedFormats = formats.AsReadOnly();

            // Map ONLY allowed formats
            EncodingMap = new Dictionary<(AudioEncodingTypeEnum, int), string>
            {
                { (AudioEncodingTypeEnum.PCM, 16), "linear16" },
                { (AudioEncodingTypeEnum.PCM, 32), "linear32" }
            };
        }
    }
}