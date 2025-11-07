using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using Grpc.Auth;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.Google;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class GoogleTTSService : ITTSService
    {
        private readonly string _serviceAccountKeyJson;
        private readonly GoogleConfig _serviceConfig;

        private TextToSpeechClient? _client;
        private VoiceSelectionParams _voiceSelectionParams;
        private AudioConfig _audioConfig;

        // Hardcoded values based on Google TTS defaults
        private readonly AudioEncoding _audioEncoding = AudioEncoding.Pcm;
        private readonly int _bytesPerSample = 2;
        private readonly int _channels = 1;

        public GoogleTTSService(string serviceAccountKeyJson, GoogleConfig config)
        {
            _serviceAccountKeyJson = serviceAccountKeyJson;
            _serviceConfig = config;

            _voiceSelectionParams = new VoiceSelectionParams
            {
                LanguageCode = _serviceConfig.LanguageCode,
                Name = _serviceConfig.VoiceName
            };

            _audioConfig = new AudioConfig
            {
                AudioEncoding = _audioEncoding,
                SampleRateHertz = _serviceConfig.TargetSampleRate,
                SpeakingRate = _serviceConfig.SpeakingRate,
            };
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

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

            if (!(new List<int>([8000,16000,24000,32000,44100])).Contains(_audioConfig.SampleRateHertz))
            {
                throw new Exception("Sample rate support are 8000, 16000, 24000, 32000 or 44100");
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> CheckAccount()
        {
            var result = new FunctionReturnResult();

            try
            {
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    $"CheckAccount:EXCEPTION",
                    $"Internal server error occured: {ex.Message}"
                );
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var input = new SynthesisInput { Text = text };

            try
            {
                var response = await _client.SynthesizeSpeechAsync(input, _voiceSelectionParams, _audioConfig, cancellationToken);

                byte[] audioData = response.AudioContent.ToByteArray();

                double durationSeconds = (double)audioData.Length / (_audioConfig.SampleRateHertz * _bytesPerSample * _channels);
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

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }
        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.GoogleCloudTextToSpeech;
        }
    }
}