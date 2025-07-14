using ElevenLabs;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.ElevenLabs;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class ElevenLabsTTSService : ITTSService
    {
        private readonly string _apiKey;
        private readonly ElevenLabsConfig _serviceConfig;

        private ElevenLabsClient _client;

        private VoiceResponseModel? _voiceData;
        private ModelResponseModel? _modelData;

        private VoiceSettingsResponseModel _voiceSettings;

        private TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat _outputFormat;
        private BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization _applyTextNormalization;
        private List<PronunciationDictionaryVersionLocatorRequestModel> _pronunciationDictionaryId;

        private List<string> _previousRequestIds = new List<string>();

        

        public ElevenLabsTTSService(string apiKey, ElevenLabsConfig config)
        {
            _apiKey = apiKey;
            
            _serviceConfig = config;

            _voiceSettings = new VoiceSettingsResponseModel();
            if (_serviceConfig.Stability.HasValue) _voiceSettings.Stability = _serviceConfig.Stability.Value;
            if (_serviceConfig.SimilarityBoost.HasValue) _voiceSettings.SimilarityBoost = _serviceConfig.SimilarityBoost.Value;
            if (_serviceConfig.Style.HasValue) _voiceSettings.Style = _serviceConfig.Style.Value;
            if (_serviceConfig.UseSpeakerBoost.HasValue) _voiceSettings.UseSpeakerBoost = _serviceConfig.UseSpeakerBoost.Value;
            if (_serviceConfig.Speed.HasValue) _voiceSettings.Speed = _serviceConfig.Speed.Value;

            _pronunciationDictionaryId = new List<PronunciationDictionaryVersionLocatorRequestModel>();
            if (!string.IsNullOrEmpty(_serviceConfig.PronunciationDictionaryId))
            {
                _pronunciationDictionaryId.Add(new PronunciationDictionaryVersionLocatorRequestModel() { PronunciationDictionaryId = _serviceConfig.PronunciationDictionaryId });
            }
            if (!string.IsNullOrEmpty(_serviceConfig.ApplyTextNormalization))
            {
                if (_serviceConfig.ApplyTextNormalization == "on")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.On;
                }
                else if (_serviceConfig.ApplyTextNormalization == "off")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Off;
                }
                else if (_serviceConfig.ApplyTextNormalization == "auto")
                {
                    _applyTextNormalization = BodyTextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostApplyTextNormalization.Auto;
                }
            }
        }

        public void Initialize()
        {
            _client = new ElevenLabsClient(_apiKey);

            _voiceData = _client.Voices.GetVoicesByVoiceIdAsync(_serviceConfig.VoiceId).GetAwaiter().GetResult();

            var allModels = _client.Models.GetModelsAsync().GetAwaiter().GetResult().ToList();
            _modelData = allModels.Find(d => d.ModelId == _serviceConfig.ModelId);
            if (_modelData == null) throw new Exception("Model not found");

            if (_serviceConfig.TargetSampleRate == 8000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm8000;
            }
            else if (_serviceConfig.TargetSampleRate == 16000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm16000;
            }
            else if (_serviceConfig.TargetSampleRate == 24000)
            {
                _outputFormat = TextToSpeechWithTimestampsV1TextToSpeechVoiceIdWithTimestampsPostOutputFormat.Pcm24000;
            }
            else if (_serviceConfig.TargetSampleRate == 44100)
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
                  
                if (!string.IsNullOrEmpty(result.Item2))
                {
                    //_previousRequestIds.Add(result.Item2);
                }
                if (_previousRequestIds.Count >= 3)
                {
                    _previousRequestIds.RemoveAt(0);
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

        public ITtsConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.ElevenLabsTextToSpeech;
        }
    }
}
