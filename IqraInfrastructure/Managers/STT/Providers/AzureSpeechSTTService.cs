using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class AzureSpeechSTTService : ISTTService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly string _language;

        private SpeechRecognizer _recognizer;
        private PushAudioInputStream _pushStream;

        private readonly int _inputSampleRate;
        private readonly int _inputBitsPerSample;
        private readonly AudioEncodingTypeEnum _inputAudioEncodingType;

        private List<string> _continousLanguageIdentificationIds;
        private bool _speakerDiarization;
        private List<string> _phrasesList;
        private readonly int _silenceTimeout;

        private event EventHandler<string> _transcriptionResultReceived;

        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }

        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;
        public AzureSpeechSTTService(string subscriptionKey, string region, string language, List<string> continousLanguageIdentificationIds, bool speakerDiarization, List<string> phrasesList, int silenceTimeout, int inputSampleRate, int inputBitsPerSample, AudioEncodingTypeEnum inputAudioEncodingType)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
            _language = language;

            _inputSampleRate = inputSampleRate;
            _inputBitsPerSample = inputBitsPerSample;
            _inputAudioEncodingType = inputAudioEncodingType;

            _continousLanguageIdentificationIds = continousLanguageIdentificationIds;
            _speakerDiarization = speakerDiarization;
            _phrasesList = phrasesList;
            _silenceTimeout = silenceTimeout;
        }

        public void Initialize()
        {
            var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);

            speechConfig.SpeechRecognitionLanguage = _language;
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, _speakerDiarization ? "true" : "false");
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, _silenceTimeout.ToString());

            AudioStreamWaveFormat audioEncodingFormat;
            switch (_inputAudioEncodingType)
            {
                case AudioEncodingTypeEnum.PCM:
                    audioEncodingFormat = AudioStreamWaveFormat.PCM;
                    break;

                case AudioEncodingTypeEnum.MULAW:
                    audioEncodingFormat = AudioStreamWaveFormat.MULAW;
                    break;

                case AudioEncodingTypeEnum.ALAW:
                    audioEncodingFormat = AudioStreamWaveFormat.ALAW;
                    break;

                case AudioEncodingTypeEnum.G722:
                    audioEncodingFormat = AudioStreamWaveFormat.G722;
                    break;

                default:
                    throw new ArgumentException($"Invalid audio encoding type: {_inputAudioEncodingType}");
            }

            _pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormat(Convert.ToUInt32(_inputSampleRate), (byte)_inputBitsPerSample, 1, audioEncodingFormat));
            var audioConfig = AudioConfig.FromStreamInput(_pushStream);

            if (_continousLanguageIdentificationIds.Count > 0)
            {
                speechConfig.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");
                var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(_continousLanguageIdentificationIds.ToArray());
                _recognizer = new SpeechRecognizer(speechConfig, autoDetectSourceLanguageConfig, audioConfig);
            }
            else
            {
                _recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            }


            var phraseList = PhraseListGrammar.FromRecognizer(_recognizer);
            foreach (var phrase in _phrasesList)
            {
                phraseList.AddPhrase(phrase);
            }

            _recognizer.Recognizing += OnRecognizing;
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;
            _recognizer.SessionStarted += OnSessionStarted;
            _recognizer.SessionStopped += OnSessionStopped;
            _recognizer.SpeechEndDetected += OnSpeechEndDetected;
        }

        public void StartTranscription()
        {
            _recognizer.StartContinuousRecognitionAsync().Wait(100);
        }

        public void StopTranscription()
        {
            StopTranscriptionAsync().GetAwaiter().GetResult();
        }

        public async Task StopTranscriptionAsync()
        {
            await _recognizer.StopContinuousRecognitionAsync();
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            _pushStream.Write(data);
        }

        private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            OnRecoginizingRecieved?.Invoke(this, e.Result.Text);
        }

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                //Console.WriteLine($"Recognized: {e.Result.Text}");  todo logger
                _transcriptionResultReceived?.Invoke(this, e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                // todo logger
                Console.WriteLine($"No speech could be recognized.");
            }
            else
            {
                // todo logger
                Console.WriteLine($"Error details:");
            }
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            // todo logger
            Console.WriteLine($"Recognition canceled. Reason: {e.Reason}");
            if (e.Reason == CancellationReason.Error)
            {
                // TODO here notify the conversation manager that there is an error...
                // todo logger
                Console.WriteLine($"Error details: {e.ErrorDetails}");
            }

            OnRecoginizingCancelled?.Invoke(this, e);
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            // todo logger
            Console.WriteLine($"Session started. Session ID: {e.SessionId}");
        }

        private void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            // todo logger
            Console.WriteLine($"Session stopped. Session ID: {e.SessionId}");
        }

        private void OnSpeechEndDetected(object? sender, RecognitionEventArgs e)
        {
            // todo logger
            Console.WriteLine($"Speech end detected.");
        }

        public string GetProviderFullName()
        {
            return "Azure AI Speech";
        }

        public InterfaceSTTProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.AzureSpeechServices;
        }
    }
}