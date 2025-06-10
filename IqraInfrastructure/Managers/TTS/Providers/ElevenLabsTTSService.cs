using ElevenLabs;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ElevenLabsTTSService : ITTSService
    {
        private ElevenLabsClient _client;

        private VoiceResponseModel? _voiceData;
        private ModelResponseModel? _modelData;

        private VoiceSettingsResponseModel _voiceSettings;

        private readonly string _apiKey;
        private readonly string _modelId;
        private readonly string _voiceId;

        private readonly int _sampleRate;
        private TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat _outputFormat;
        private BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization _applyTextNormalization;
        private List<PronunciationDictionaryVersionLocatorRequestModel> _pronunciationDictionaryId;

        private List<string>? _previousRequestIds = new List<string>();

        public ElevenLabsTTSService(string apiKey, string modelId, string voiceId, float? stability = null, float? similarityBoost = null, float? style = null, bool? speakerBoost = null, float? speed = null, string? pronunciationDictionaryId = null, string? applyTextNormalization = null, int sampleRate = 8000)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _modelId = modelId;

            _voiceSettings = new VoiceSettingsResponseModel();
            if (stability.HasValue) _voiceSettings.Stability = stability.Value;
            if (similarityBoost.HasValue) _voiceSettings.SimilarityBoost = similarityBoost.Value;
            if (style.HasValue) _voiceSettings.Style = style.Value;
            if (speakerBoost.HasValue) _voiceSettings.UseSpeakerBoost = speakerBoost.Value;
            if (speed.HasValue) _voiceSettings.Speed = speed.Value;

            _sampleRate = sampleRate;

            _pronunciationDictionaryId = new List<PronunciationDictionaryVersionLocatorRequestModel>();
            if (!string.IsNullOrEmpty(pronunciationDictionaryId))
            {
                _pronunciationDictionaryId.Add(new PronunciationDictionaryVersionLocatorRequestModel() { PronunciationDictionaryId = pronunciationDictionaryId });
            }
            if (!string.IsNullOrEmpty(applyTextNormalization))
            {
                if (applyTextNormalization == "on")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.On;
                }
                else if (applyTextNormalization == "off")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Off;
                }
                else if (applyTextNormalization == "auto")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Auto;
                }
            }
        }

        public void Initialize()
        {
            _client = new ElevenLabsClient(_apiKey);

            _voiceData = _client.Voices.GetVoicesByVoiceIdAsync(_voiceId).GetAwaiter().GetResult();

            var allModels = _client.Models.GetModelsAsync().GetAwaiter().GetResult().ToList();
            _modelData = allModels.Find(d => d.ModelId == _modelId);
            if (_modelData == null) throw new Exception("Model not found");

            if (_sampleRate == 8000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm8000;
            }
            else if (_sampleRate == 16000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm16000;
            }
            else if (_sampleRate == 24000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm24000;
            }
            else if (_sampleRate == 44100)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm44100;
            }
            else
            {
                throw new Exception("Unsupported sample rate, supported are: 8000, 16000, 24000, 44100");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var request = new BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPost(text, _modelData.ModelId, null, _voiceSettings, _pronunciationDictionaryId, null, null, null, _previousRequestIds, null, _applyTextNormalization, null);

            try
            {
                var result = await _client.TextToSpeech.CreateTextToSpeechByVoiceIdWithTimestampsAsync(_voiceData.VoiceId, request, null, null, _outputFormat, null, cancellationToken);
                
                if (_previousRequestIds.Count >= 3)
                {
                    _previousRequestIds.RemoveAt(0);
                }
                if (!string.IsNullOrEmpty(result.Item2))
                {
                    _previousRequestIds.Add(result.Item2);
                }

                var audioData = Convert.FromBase64String(result.Item1.AudioBase64);
                return (audioData, TimeSpan.FromSeconds(result.Item1.Alignment.CharacterEndTimesSeconds.Last()));
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
