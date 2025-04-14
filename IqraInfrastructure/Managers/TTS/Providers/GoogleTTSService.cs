using Google.Cloud.TextToSpeech.V1;
using Google.Apis.Auth.OAuth2; // Needed for GoogleCredential
using Grpc.Auth; // Needed for ToChannelCredentials
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class GoogleTTSService : ITTSService
    {
        private TextToSpeechClient? _client;
        private VoiceSelectionParams _voiceSelectionParams;
        private AudioConfig _audioConfig;

        private readonly string _languageCode;
        private readonly string _voiceName;
        private readonly int _sampleRateHertz = 16000;
        private readonly AudioEncoding _audioEncoding = AudioEncoding.Linear16;
        private readonly int _bytesPerSample = 2; // For Linear16 (16-bit)
        private readonly int _channels = 1; // Mono

        private readonly string _serviceAccountKeyJson;

        public GoogleTTSService(string serviceAccountKeyJson, string languageCode, string voiceName)
        {
            _serviceAccountKeyJson = serviceAccountKeyJson;
            _languageCode = languageCode;
            _voiceName = voiceName;

            _voiceSelectionParams = new VoiceSelectionParams
            {
                LanguageCode = _languageCode,
                Name = _voiceName
            };

            _audioConfig = new AudioConfig
            {
                AudioEncoding = _audioEncoding,
                SampleRateHertz = _sampleRateHertz
            };
        }

        public void Initialize()
        {
            try
            {
                var credential = GoogleCredential.FromJson(_serviceAccountKeyJson).CreateScoped(TextToSpeechClient.DefaultScopes);
                var clientBuilder = new TextToSpeechClientBuilder
                {
                    ChannelCredentials = credential.ToChannelCredentials()
                };
                _client = clientBuilder.Build();
            }
            catch (Exception ex)
            {
                // Log or handle initialization failure
                //Console.WriteLine($"Failed to initialize GoogleTTSService: {ex.Message}");
                // Optionally rethrow or set a state indicating failure TODO
                throw new InvalidOperationException("Failed to initialize Google Text-to-Speech client with provided credentials.", ex);
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var input = new SynthesisInput { Text = text };

            try
            {
                var response = await _client.SynthesizeSpeechAsync(input, _voiceSelectionParams, _audioConfig, cancellationToken);

                byte[] audioData = response.AudioContent.ToByteArray();

                double durationSeconds = (double)audioData.Length / (_sampleRateHertz * _bytesPerSample * _channels);
                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                return (audioData, duration);
            }
            catch (OperationCanceledException)
            {
                // todo logging
                //Console.WriteLine("Google TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex) // Catch potential gRPC exceptions etc.
            {
                // todo logging
                //Console.WriteLine($"Error during Google TTS synthesis: {ex.Message}");
                // Consider more specific exception handling (e.g., RpcException for gRPC errors)
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "GoogleCloudTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.GoogleCloudTextToSpeech;
        }
    }
}