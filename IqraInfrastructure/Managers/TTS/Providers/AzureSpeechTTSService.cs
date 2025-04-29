using Deepgram.Models.Manage.v1;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class AzureSpeechTTSService : ITTSService
    {
        private SpeechSynthesizer _synthesizer;

        private readonly string _subscriptionKey;
        private readonly string _region;

        private readonly string _langauge;
        private readonly string _speakerName;

        private PullAudioOutputStream _pullStream;

        private bool _loggingEnabled = false;

        public AzureSpeechTTSService(string subscriptionKey, string region, string langauge, string speakerName)
        {
            _subscriptionKey = subscriptionKey;
            _region = region;
            _langauge = langauge;
            _speakerName = speakerName;
        }

        public void Initialize()
        {
            var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            speechConfig.SpeechSynthesisLanguage = _langauge;
            speechConfig.SpeechSynthesisVoiceName = _speakerName;

            // ai agent is forced in call processor manager . CreateConversationSessionAsync to use 16k 16bit mono
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);

            _pullStream = AudioOutputStream.CreatePullStream();
            var audioConfig = AudioConfig.FromStreamOutput(_pullStream);

            _synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
            var connection = Connection.FromSpeechSynthesizer(_synthesizer);
            connection.Open(true);

            _synthesizer.SynthesisStarted += OnSynthesisStarted;
            _synthesizer.SynthesisCompleted += OnSynthesisCompleted;
            _synthesizer.SynthesisCanceled += OnSynthesisCanceled;
            _synthesizer.Synthesizing += OnSynthesizing;
            _synthesizer.BookmarkReached += OnBookmarkReached;
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var result = await _synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return (result.AudioData, result.AudioDuration);
            }

            return (new byte[] { }, TimeSpan.Zero);
        }

        public async Task StopTextSynthesisAsync()
        {
            await _synthesizer.StopSpeakingAsync();
        }

        private void OnSynthesisStarted(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"Synthesis Started: Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesisCompleted(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"first byte client latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisFirstByteLatencyMs)} ms");
                Console.WriteLine($"finish client latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisFinishLatencyMs)} ms");
                Console.WriteLine($"network latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisNetworkLatencyMs)} ms");
                Console.WriteLine($"first byte service latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisServiceLatencyMs)} ms");
                Console.WriteLine($"Synthesis Completed: Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesisCanceled(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"Synthesis canceled. Reason: {e.Result.Reason} Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesizing(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                //Console.WriteLine($"Synthesizing. Id: {e.Result.ResultId}");
            }
        }

        private void OnBookmarkReached(object? sender, SpeechSynthesisBookmarkEventArgs e)
        {
            if (_loggingEnabled)
            {
                //Console.WriteLine($"Bookmark Reached. Text: {e.Text} Id: {e.ResultId}");
            }
        }

        public string GetProviderFullName()
        {
            return "MicrosoftAzureSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.AzureSpeechServices;
        }
    }
}