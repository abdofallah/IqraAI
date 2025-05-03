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

        private readonly int _sampleRate;

        private event EventHandler<string> _transcriptionResultReceived;

        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }

        public event EventHandler<object> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;
        public AzureSpeechSTTService(string subscriptionKey, string region, string language, int sampleRate = 8000)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
            _language = language;
            _sampleRate = sampleRate;
        }

        public void Initialize()
        {
            var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);

            // TODO MAKE THIS DYNAMICI IN STT PROVIDER TO MANUALLY SET LANGAUGES IDS FOR EACH LANGUAGE
            if (_language == "en")
            {
                speechConfig.SpeechRecognitionLanguage = "en-US";
            }
            else if (_language == "ar")
            {
                speechConfig.SpeechRecognitionLanguage = "ar-SA";
            }

            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "300"); // make it dynamic with some kind of maths

            _pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormat(Convert.ToUInt32(_sampleRate), 16, 1, AudioStreamWaveFormat.PCM));
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
            //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
            OnRecoginizingRecieved?.Invoke(this, e);
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