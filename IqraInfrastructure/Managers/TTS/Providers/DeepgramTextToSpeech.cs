using Deepgram.Models.Speak.v1.REST;
using Deepgram.Clients.Interfaces.v1;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class DeepgramTTSService : ITTSService, IDisposable
    {
        private ISpeakRESTClient? _speakClient;
        private readonly string _apiKey;
        private readonly string _modelId;

        private readonly string _encoding = "linear16"; // For raw PCM 16-bit
        private readonly int _sampleRate; // Target sample rate
        private readonly int _channels = 1; // Mono
        private readonly int _bytesPerSample = 2; // 16 bits = 2 bytes

        public DeepgramTTSService(string apiKey, string modelId, int sampleRate)
        {
            _apiKey = apiKey;
            _modelId = modelId;
            _sampleRate = sampleRate;
        }

        public void Initialize()
        {
            _speakClient = new Deepgram.Clients.Speak.v1.REST.Client(_apiKey);
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (_speakClient == null)
            {
                throw new InvalidOperationException("Service not initialized or initialization failed. Call Initialize() first.");
            }
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var textSource = new TextSource(text);
            var speakSchema = new SpeakSchema()
            {
                Model = _modelId,
                Encoding = _encoding,
                SampleRate = _sampleRate.ToString(),
                Container = "none",
                BitRate  = (_bytesPerSample * (_bytesPerSample * 8)).ToString()
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                SyncResponse response = await _speakClient.ToStream(textSource, speakSchema, cts);

                if (response?.Stream != null)
                {
                    byte[] audioData = response.Stream.ToArray();
                    response.Stream.Dispose();

                    double durationSeconds = (double)audioData.Length / (_sampleRate * _channels * _bytesPerSample);
                    TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                    return (audioData, duration);
                }
                else
                {
                    Console.WriteLine("Deepgram TTS Error: Received null stream in response.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken || ex.CancellationToken == cts.Token)
            {
                // todo logging
                //Console.WriteLine("Deepgram TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                //Console.WriteLine($"Deepgram TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            // The underlying HTTP request initiated by ToStream should respect the token via the CTS.
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "DeepgramTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.DeepgramTextToSpeech;
        }

        public void Dispose()
        {
            (_speakClient as IDisposable)?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}