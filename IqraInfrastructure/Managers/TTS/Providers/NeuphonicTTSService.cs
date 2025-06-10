using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS.Providers.Neuphonic;
using IqraCore.Interfaces.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class NeuphonicTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _apiKey;
        private readonly string _langCode;
        private readonly string? _voiceId;
        private readonly int? _targetSampleRate;
        private readonly float? _speed;

        private const string BaseApiUrl = "https://eu-west-1.api.neuphonic.com";

        public NeuphonicTTSService(string apiKey, string langCode, string? voiceId = null, float? speed = 1.0f, int? targetSampleRate = 8000)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(langCode)) throw new ArgumentNullException(nameof(langCode));
            if (targetSampleRate.HasValue && (targetSampleRate.Value != 8000 && targetSampleRate.Value != 16000 && targetSampleRate.Value != 22050))
                throw new ArgumentOutOfRangeException(nameof(targetSampleRate), "Supported sample rates are 8000, 16000, 22050.");
            if (speed.HasValue && (speed.Value < 0.7f || speed.Value > 2.0f))
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between 0.7 and 2.0.");

            _apiKey = apiKey;
            _langCode = langCode;
            _voiceId = voiceId;
            _targetSampleRate = targetSampleRate;
            _speed = speed;
        }

        public void Initialize()
        {
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var requestUrl = $"{BaseApiUrl}/sse/speak/{_langCode}";

            var apiRequestPayload = new NeuphonicTtsApiRequest
            {
                Text = text,
                VoiceId = _voiceId,
                SamplingRate = _targetSampleRate,
                Speed = _speed
            };

            string jsonPayload = JsonSerializer.Serialize(apiRequestPayload, _jsonSerializerOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("X-API-KEY", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var accumulatedAudioBytes = new List<byte>();
                int? actualSampleRateFromResponse = null;

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                StringBuilder currentJsonDataBuffer = new StringBuilder();
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("data:"))
                    {
                        currentJsonDataBuffer.Append(line.Substring(5).Trim());
                    }
                    else if (string.IsNullOrWhiteSpace(line) && currentJsonDataBuffer.Length > 0)
                    {
                        try
                        {
                            var sseEventPayload = JsonSerializer.Deserialize<NeuphonicSseEventPayload>(currentJsonDataBuffer.ToString(), _jsonSerializerOptions);
                            if (sseEventPayload?.AudioDetails?.Audio != null && sseEventPayload.StatusCode == "200")
                            {
                                byte[] audioChunk = Convert.FromBase64String(sseEventPayload.AudioDetails.Audio);
                                accumulatedAudioBytes.AddRange(audioChunk);

                                if (!actualSampleRateFromResponse.HasValue && sseEventPayload.AudioDetails.SamplingRate.HasValue)
                                {
                                    actualSampleRateFromResponse = sseEventPayload.AudioDetails.SamplingRate.Value;
                                }
                            }
                        }
                        catch (JsonException) { }
                        catch (FormatException) { }
                        currentJsonDataBuffer.Clear();
                    }
                }

                if (accumulatedAudioBytes.Count == 0)
                {
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                byte[] finalAudioData = accumulatedAudioBytes.ToArray();
                TimeSpan duration = TimeSpan.Zero;

                int rateForDurationCalc = actualSampleRateFromResponse ?? _targetSampleRate ?? 22050;
                const int channels = 1;
                const int bitsPerSample = 16;

                if (rateForDurationCalc > 0 && finalAudioData.Length > 0)
                {
                    duration = TimeSpan.FromSeconds(finalAudioData.Length / (double)(rateForDurationCalc * channels * (bitsPerSample / 8)));
                }

                return (finalAudioData, duration);
            }
            catch (HttpRequestException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        public Task StopTextSynthesisAsync()
        {
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "NeuphonicTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.NeuphonicTextToSpeech;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
