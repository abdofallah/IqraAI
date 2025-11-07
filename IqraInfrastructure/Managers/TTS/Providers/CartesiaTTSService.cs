using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.Cartesia;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class CartesiaTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();

        private readonly string _apiKey;

        private readonly string _cartesiaVersion = "2025-04-16";
        private const string BaseUrl = "https://api.cartesia.ai";

        // Hard coded by the api, these values are not configurable
        private const int BytesPerSample = 2;
        private const int Channels = 1;

        private readonly CartesiaConfig _serviceConfig;

        public CartesiaTTSService(string apiKey, CartesiaConfig config)
        {
            _apiKey = apiKey;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            return result.SetSuccessResult();

            // HttpClient is static, but we can configure default headers once if needed,
            // though it's safer to add them per request if API key can change.
            // For this Saas model where key is per instance, adding per request is better.
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
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new CartesiaTtsBytesRequest
            {
                ModelId = _serviceConfig.ModelId,
                Transcript = text,
                Voice = new CartesiaVoiceRequest { Id = _serviceConfig.VoiceId },
                OutputFormat = new CartesiaOutputFormatRequest
                {
                    SampleRate = _serviceConfig.TargetSampleRate,
                    Encoding = "pcm_s16le",
                    BitRate = (_serviceConfig.TargetSampleRate * (BytesPerSample * 8))
                },
                Language = _serviceConfig.LanguageCode,
                PronunciationDictIds = _serviceConfig.PronunciationDictIds.ToArray()
            };

            string jsonPayload = JsonSerializer.Serialize(requestPayload);
            var requestUri = $"{BaseUrl}/tts/bytes";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/*"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("Cartesia-Version", _cartesiaVersion);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    byte[] audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    double durationSeconds = (double)audioData.Length / (_serviceConfig.TargetSampleRate * BytesPerSample * Channels);
                    TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
                    return (audioData, duration);
                }
                else
                {
                    // Log error response
                    string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    // todo logging
                    //Console.WriteLine($"Cartesia API Error ({response.StatusCode}): {errorContent}");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }
            }
            catch (HttpRequestException ex)
            {
                // todo logging
                //Console.WriteLine($"Cartesia HTTP Request Error: {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                //Console.WriteLine("Cartesia TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex) // Catch broader exceptions
            {
                // todo logging
                //Console.WriteLine($"Cartesia TTS Error: {ex.Message}");
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
            return "CartesiaTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.CartesiaTextToSpeech;
        }

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }

        public void Dispose()
        {
            // If we were managing HttpClient instance per service, dispose it here.
            // Since we're using a static one, there's nothing instance-specific to dispose.
            GC.SuppressFinalize(this);
        }
    }
}