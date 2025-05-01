using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Interfaces.AI;
using MessagePack;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class FishAudioTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _referenceId;
        private readonly string _model;

        private readonly int _sampleRate;
        private readonly int _channels = 1; //default by fishaudio
        private readonly int _bitsPerSample = 16; //default by fishaudio

        private const string ApiUrl = "https://api.fish.audio/v1/tts";

        public FishAudioTTSService(string apiKey, string referenceId, string model, int sampleRate = 8000)
        {
            _apiKey = apiKey;
            _referenceId = referenceId;
            _model = model;
            _sampleRate = sampleRate;
        }

        public void Initialize()
        {
            // Static HttpClient, initialization done in constructor or is implicit

            if (!(new List<int>([8000, 16000, 24000, 32000, 44100])).Contains(_sampleRate))
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
                ReferenceId = _referenceId,
                Format = "pcm",
                SampleRate = (_sampleRate / 100) // it expects 8 instead of 8000
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
            request.Headers.Add("model", _model);

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

            double durationSeconds = (double)pcmData.Length / (_sampleRate * _channels * (_bitsPerSample / 8));
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

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}