using ElevenLabs;
using ElevenLabs.Models;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System.Linq;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ElevenLabsTTSService : ITTSService
    {
        private ElevenLabsClient _client;

        private Voice? _voiceData;
        private Model? _modelData;

        private VoiceSettings _voiceSettings;

        private readonly string _apiKey;
        private readonly string _modelId;
        private readonly string _voiceId;

        private readonly int _sampleRate;
        private OutputFormat _outputFormat;

        private List<string>? _previousRequestIds = new List<string>();

        public ElevenLabsTTSService(string apiKey, string modelId, string voiceId, float? stability = null, float? similarityBoost = null, float? style = null, bool? speakerBoost = null, float? speed = null, int sampleRate = 8000)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _modelId = modelId;

            _voiceSettings = new VoiceSettings();
            if (stability.HasValue) _voiceSettings.Stability = stability.Value;
            if (similarityBoost.HasValue) _voiceSettings.SimilarityBoost = similarityBoost.Value;
            if (style.HasValue) _voiceSettings.Style = style.Value;
            if (speakerBoost.HasValue) _voiceSettings.SpeakerBoost = speakerBoost.Value;
            if (speed.HasValue) _voiceSettings.Speed = speed.Value;

            _sampleRate = sampleRate;
        }

        public void Initialize()
        {
            _client = new ElevenLabsClient(
                new ElevenLabsAuthentication(_apiKey)
            );

            _voiceData = _client.VoicesEndpoint.GetVoiceAsync(_voiceId).GetAwaiter().GetResult();

            var allModels = _client.ModelsEndpoint.GetModelsAsync().GetAwaiter().GetResult().ToList();
            _modelData = allModels.Find(d => d.Id == _modelId);

            if (_sampleRate == 8000)
            {
                _outputFormat = OutputFormat.Ulaw_8000;
            }
            else if (_sampleRate == 16000)
            {
                _outputFormat = OutputFormat.PCM_16000;
            }
            else if (_sampleRate == 24000)
            {
                _outputFormat = OutputFormat.PCM_24000;
            }
            else if (_sampleRate == 44100)
            {
                _outputFormat = OutputFormat.PCM_44100;
            }
            else
            {
                throw new Exception("Unsupported sample rate, supported are: 8000, 16000, 24000, 44100");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var request = new TextToSpeechRequest(_voiceData, text, null, _voiceSettings, _outputFormat, model: _modelData, withTimestamps: true, previousRequestIds: _previousRequestIds.ToArray());

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
            return "ElevenLabsTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ElevenLabsTextToSpeech;
        }
    }
}
