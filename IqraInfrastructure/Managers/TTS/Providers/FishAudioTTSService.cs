using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using MessagePack;
using System.Linq;
using System.Net.Http.Headers;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class FishAudioTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;

        // Hardcoded values based on FishAudio's requirements
        private readonly int _channels = 1;
        private readonly int _bitsPerSample = 16;

        private const string ApiUrl = "https://api.fish.audio/v1/tts";

        private readonly FishAudioConfig _serviceConfig;

        public FishAudioTTSService(string apiKey, FishAudioConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public void Initialize()
        {
            // Static HttpClient, initialization done in constructor or is implicit

            if (!(new List<int>([8000, 16000, 24000, 32000, 44100])).Contains(_serviceConfig.TargetSampleRate))
            {
                throw new Exception("Sample rate must be 8000, 16000, 24000, 32000 or 44100");
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new FishAudioTTSRequest
            {
                Text = text,
                ReferenceId = _serviceConfig.ReferenceId,
                Format = "pcm",
                SampleRate = (_serviceConfig.TargetSampleRate / 1000) // it expects 8 instead of 8000
            };

            byte[] messagePackData;
            try
            {
                messagePackData = MessagePackSerializer.Serialize(requestPayload);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Fish Audio MessagePack Serialization Error: {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }


            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));
            request.Headers.Add("model", _serviceConfig.Model);

            request.Content = new ByteArrayContent(messagePackData);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/msgpack");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    byte[] pcmData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    return ParseAndExtractPcm(pcmData);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    // todo logging
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (HttpRequestException ex)
            {
                // todo logging
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        private (byte[]?, TimeSpan?) ParseAndExtractPcm(byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0)
            {
                return (null, null);
            }

            double durationSeconds = (double)pcmData.Length / (_serviceConfig.TargetSampleRate * _channels * (_bitsPerSample / 8));
            TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);

            return (pcmData, duration);
        }


        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "FishAudioTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.FishAudioTextToSpeech;
        }

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}