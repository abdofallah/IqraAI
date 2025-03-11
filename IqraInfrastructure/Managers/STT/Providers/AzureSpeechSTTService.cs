using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces;
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

        private event EventHandler<string> _transcriptionResultReceived;

        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }
        public event EventHandler<object> OnRecoginizingRecieved;

        public AzureSpeechSTTService(string subscriptionKey, string region, string language)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
            _language = language;
        }

        public void Initialize()
        {
            var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            speechConfig.SpeechRecognitionLanguage = _language;
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "600"); // make it dynamic with some kind of maths

            _pushStream = AudioInputStream.CreatePushStream();
            var audioConfig = AudioConfig.FromStreamInput(_pushStream);

            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            _recognizer.Recognizing += OnRecognizing;
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;
            _recognizer.SessionStarted += OnSessionStarted;
            _recognizer.SessionStopped += OnSessionStopped;
        }

        public void StartTranscription()
        {
            _recognizer.StartContinuousRecognitionAsync().Wait();
        }

        public void StopTranscription()
        {
            _recognizer.StopContinuousRecognitionAsync().Wait();
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            _pushStream.Write(data);
        }

        private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            OnRecoginizingRecieved?.Invoke(this, e);
        }

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"Recognized: {e.Result.Text}");
                _transcriptionResultReceived?.Invoke(this, e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine($"No speech could be recognized.");
            }
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            Console.WriteLine($"Recognition canceled. Reason: {e.Reason}");
            if (e.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"Error details: {e.ErrorDetails}");
            }
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session started. Session ID: {e.SessionId}");
        }

        private void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            Console.WriteLine($"Session stopped. Session ID: {e.SessionId}");
        }

        public string GetProviderFullName()
        {
            return "Azure AI Speech";
        }

        public static InterfaceSTTProviderEnum GetProviderType()
        {
            return InterfaceSTTProviderEnum.AzureSpeechServices;
        }
    }
}