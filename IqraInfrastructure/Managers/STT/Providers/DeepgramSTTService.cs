using Deepgram;
using Deepgram.Clients.Interfaces.v2;
using Deepgram.Models.Authenticate.v1;
using Deepgram.Models.Listen.v2.WebSocket;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class DeepgramSTTService : ISTTService
    {
        private readonly string _apiKey;
        private string _language;
        private readonly bool _speakerDiarization;
        private readonly List<string> _keywordsList;
        private readonly int _silenceTimeout;
        private readonly string _model;
        private readonly bool _punctuate;
        private readonly bool _smartFormat;
        private readonly bool _fillerWords;
        private readonly bool _profanityFilter;
        private readonly bool _numerals;
        private readonly bool _dictation;
        private readonly bool _multiChannel;
        private readonly bool _noDelay;

        private readonly int _inputSampleRate;
        private readonly int _inputBitsPerSample;
        private readonly AudioEncodingTypeEnum _inputAudioEncodingType;

        private string _deepgramAudioFormat;

        private IListenWebSocketClient _liveClient;

        private event EventHandler<string> _transcriptionResultReceived;

        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }

        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        public DeepgramSTTService(
            string apiKey, string language, string model, bool speakerDiarization, List<string> keywordsList, int silenceTimeout, int inputSampleRate, int inputBitsPerSample, AudioEncodingTypeEnum inputAudioEncodingType, 
            bool punctuate = false, bool smartFormat = false, bool fillerWords = true,
            bool profanityFilter = false, bool numerals = false, bool dictation = false, bool multiChannel = false, bool noDelay = true)
        {
            _apiKey = apiKey;
            _language = language;
            _model = model;
            _speakerDiarization = speakerDiarization;
            _keywordsList = keywordsList;
            _silenceTimeout = silenceTimeout;
            _punctuate = punctuate;
            _smartFormat = smartFormat;
            _fillerWords = fillerWords;
            _profanityFilter = profanityFilter;
            _numerals = numerals;
            _dictation = dictation;
            _multiChannel = multiChannel;
            _noDelay = noDelay;

            _inputSampleRate = inputSampleRate;
            _inputBitsPerSample = inputBitsPerSample;
            _inputAudioEncodingType = inputAudioEncodingType;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                switch (_inputAudioEncodingType)
                {
                    case AudioEncodingTypeEnum.PCM:
                        if (_inputBitsPerSample == 16)
                        {
                            _deepgramAudioFormat = "linear16";
                        }
                        else if (_inputBitsPerSample == 32)
                        {
                            _deepgramAudioFormat = "linear32";
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid bits per sample: {_inputBitsPerSample}");
                        }

                        break;

                    case AudioEncodingTypeEnum.MULAW:
                        _deepgramAudioFormat = "mulaw";
                        break;

                    case AudioEncodingTypeEnum.ALAW:
                        _deepgramAudioFormat = "alaw";
                        break;

                    case AudioEncodingTypeEnum.G729:
                        _deepgramAudioFormat = "g729";
                        break;

                    case AudioEncodingTypeEnum.OPUS:
                        _deepgramAudioFormat = "opus";
                        break;

                    default:
                        throw new ArgumentException($"Invalid audio encoding type: {_inputAudioEncodingType}");
                }

                DeepgramWsClientOptions options = new DeepgramWsClientOptions(apiKey: _apiKey, keepAlive: true);
                _liveClient = ClientFactory.CreateListenWebSocketClient(_apiKey, options);

                _liveClient.Subscribe(new EventHandler<OpenResponse>(OnConnectionOpened));
                _liveClient.Subscribe(new EventHandler<ResultResponse>(OnResultReceived));
                _liveClient.Subscribe(new EventHandler<ErrorResponse>(OnErrorReceived));
                _liveClient.Subscribe(new EventHandler<CloseResponse>(OnConnectionClosed));
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initialize:EXCEPTION",
                    $"Internal error: {ex.Message}"
                );
            }

            return result.SetSuccessResult();
        }

        public void StartTranscription()
        {
            var liveSchema = new LiveSchema()
            {
                Encoding = _deepgramAudioFormat,
                SampleRate = _inputSampleRate,
                Channels = 1,
                InterimResults = false,
                Model = _model,
                Language = _language,
                Diarize = _speakerDiarization,
                Punctuate = _punctuate,
                SmartFormat = _smartFormat,
                FillerWords = _fillerWords,
                ProfanityFilter = _profanityFilter,
                Numerals = _numerals,
                Dictation = _dictation,
                MultiChannel = _multiChannel,
                NoDelay = _noDelay,
                EndPointing = _silenceTimeout.ToString(),
            };

            if (_model.Contains("nova-2"))
            {
                liveSchema.Keywords = _keywordsList;
            }
            else if (_model.Contains("nova-3"))
            {
                liveSchema.Keyterm = _keywordsList;
            }

            _liveClient.Connect(liveSchema).Wait();
        }

        public void StopTranscription()
        {
            StopTranscriptionAsync().GetAwaiter().GetResult();
        }

        public async Task StopTranscriptionAsync()
        {
            if (_liveClient != null)
            {
                await _liveClient.Stop();
            }
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_liveClient?.State() == System.Net.WebSockets.WebSocketState.Open)
            {
                _liveClient.Send(data);
            }
        }

        private void OnResultReceived(object? sender, ResultResponse e)
        {
            var transcript = e.Channel?.Alternatives?.FirstOrDefault()?.Transcript;
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            if (e.IsFinal.HasValue && e.IsFinal.Value)
            {
                _transcriptionResultReceived?.Invoke(this, transcript);
            }
            else
            {
                // TODO TEST
                _transcriptionResultReceived?.Invoke(this, transcript);
            }
        }

        private void OnErrorReceived(object? sender, ErrorResponse e)
        {
            Console.WriteLine($"Deepgram Error: {e.Message}"); // todo logger
            OnRecoginizingCancelled?.Invoke(this, e);
        }

        private void OnConnectionOpened(object? sender, OpenResponse e)
        {
            Console.WriteLine("Deepgram session started."); // todo logger
        }

        private void OnConnectionClosed(object? sender, CloseResponse e)
        {
            Console.WriteLine("Deepgram session stopped."); // todo logger
        }

        public string GetProviderFullName()
        {
            return "Deepgram";
        }

        public InterfaceSTTProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.Deepgram;
        }
    }
}