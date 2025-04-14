using ElevenLabs;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ElevenLabsTTSService : ITTSService
    {
        private ElevenLabsClient _client;
        private Voice _voiceModel;
        private VoiceSettings _voiceSettings;

        private readonly string _apiKey;
        private readonly string _voiceId;

        private List<string>? _previousRequestIds = new List<string>();

        public ElevenLabsTTSService(string apiKey, string voiceId, float stability, float similarityBoost, float style, bool speakerBoost, float speed)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _voiceSettings = new VoiceSettings(stability, similarityBoost, style, speakerBoost, speed);
        }

        public void Initialize()
        {
            _client = new ElevenLabsClient(
                new ElevenLabsAuthentication(_apiKey)
            );

            _voiceModel = _client.VoicesEndpoint.GetVoiceAsync(_voiceId).GetAwaiter().GetResult();
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var request = new TextToSpeechRequest(_voiceModel, text, null, _voiceSettings, ElevenLabs.OutputFormat.PCM_16000, withTimestamps: true, previousRequestIds: _previousRequestIds.ToArray());

            try
            {
                var result = await _client.TextToSpeechEndpoint.TextToSpeechAsync(request, null, cancellationToken);

                _previousRequestIds.Add(result.Id);

                return (result.ClipData.ToArray(), TimeSpan.FromSeconds(result.TimestampedTranscriptCharacters.Last().EndTime));
            }
            catch (Exception ex) {
                // todo log it
            }
            

            return (new byte[] { }, TimeSpan.Zero);
        }

        public Task StopTextSynthesisAsync()
        {
            // not needed
            // todo can make task and cancel it
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "ElevelLabsTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ElevelLabsTextToSpeech;
        }
    }
}
