using IqraCore.Entities.TTS.Providers.Speechify; // Import the data models
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class SpeechifyTTSService : ITTSService, IDisposable
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _voiceId;
        private readonly string _model;

        private const string ApiUrl = "https://api.sws.speechify.com/v1/audio/speech";

        // Constructor
        public SpeechifyTTSService(string apiKey, string voiceId, string model)
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _model = model;
        }

        public void Initialize()
        {
            // Static HttpClient initialization is handled implicitly
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            var requestPayload = new SpeechifyTtsRequest
            {
                Input = text,
                VoiceId = _voiceId,
                Model = _model,
                AudioFormat = "wav", // Request WAV to get PCM data and header info
                // Language = "en-US", // Optional: Can be passed via metaData or constructor
                // Options = new SpeechifyOptionsRequest { ... } // Optional: Can be configured
            };

            // Allow overriding language from metaData
            if (metaData?.TryGetValue("language", out var lang) == true && lang is string languageCode)
            {
                requestPayload.Language = languageCode;
            }

            string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var requestUri = ApiUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // todo logging
                    Console.WriteLine($"Speechify HTTP Error ({response.StatusCode}): {responseBody}");
                    // Attempt to parse error details from body
                    try
                    {
                        var errorResp = JsonSerializer.Deserialize<SpeechifyTtsResponse>(responseBody);
                        if (!string.IsNullOrEmpty(errorResp?.ErrorCode) || !string.IsNullOrEmpty(errorResp?.ErrorMessage))
                        {
                            // todo logging
                            Console.WriteLine($"Speechify API Error: Code={errorResp.ErrorCode}, Msg='{errorResp.ErrorMessage}'");
                        }
                    }
                    catch { /* Ignore deserialize error on error path */ }
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                var ttsResponse = JsonSerializer.Deserialize<SpeechifyTtsResponse>(responseBody);

                // Check for API level error message even on 200 OK
                if (!string.IsNullOrEmpty(ttsResponse?.ErrorCode) || !string.IsNullOrEmpty(ttsResponse?.ErrorMessage))
                {
                    // todo logging
                    Console.WriteLine($"Speechify API Error: Code={ttsResponse.ErrorCode}, Msg='{ttsResponse.ErrorMessage}'");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                if (string.IsNullOrWhiteSpace(ttsResponse?.AudioData))
                {
                    // todo logging
                    Console.WriteLine("Speechify TTS Error: API success but no audio data received.");
                    return (Array.Empty<byte>(), TimeSpan.Zero);
                }

                // Decode Base64 audio
                byte[] audioData = Convert.FromBase64String(ttsResponse.AudioData);
                string format = ttsResponse.AudioFormat?.ToLowerInvariant() ?? "unknown";
                TimeSpan? duration = null;

                // Try to get duration from speech marks if available and reliable
                if (ttsResponse.SpeechMarks?.EndTime.HasValue == true && ttsResponse.SpeechMarks.EndTime > 0)
                {
                    duration = TimeSpan.FromSeconds(ttsResponse.SpeechMarks.EndTime.Value);
                }

                // Handle format
                if (format == "wav")
                {
                    // todo logging
                    Console.WriteLine($"Speechify: Received WAV format. Parsing header...");
                    // Use a WAV parser similar to previous examples
                    return ParseWavAndExtractPcm(audioData, duration); // Pass duration if we got it from speech marks
                }
                else if (format == "pcm") // Unlikely based on docs, but handle
                {
                    // todo logging
                    Console.WriteLine($"Speechify: Received raw PCM format.");
                    // If we don't have duration from speech marks, calculate it
                    if (!duration.HasValue)
                    {
                        // todo logging
                        // Need sample rate, channels, bits per sample for calculation
                        // These aren't directly in the main response, would need more info or make assumptions
                        Console.WriteLine("Speechify: PCM received but cannot calculate duration without format details.");
                    }
                    return (audioData, duration);
                }
                else // mp3, ogg, aac, unknown
                {
                    // todo logging
                    Console.WriteLine($"Speechify: Received '{format}' format. Returning raw bytes.");
                    // Cannot reliably parse or calculate duration for compressed formats here
                    return (audioData, duration); // Return bytes, duration might be null or from speech marks
                }
            }
            catch (JsonException jsonEx)
            {
                // todo logging
                Console.WriteLine($"Speechify JSON Deserialization Error: {jsonEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (FormatException formatEx) // From Base64 decoding
            {
                // todo logging
                Console.WriteLine($"Speechify Base64 Decoding Error: {formatEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (HttpRequestException httpEx)
            {
                // todo logging
                Console.WriteLine($"Speechify HTTP Request Error: {httpEx.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                // todo logging
                Console.WriteLine("Speechify TTS synthesis was cancelled.");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                // todo logging
                Console.WriteLine($"Speechify TTS Error: {ex.GetType().Name} - {ex.Message}");
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }
        }

        // WAV parser (reuse or adapt from other services)
        private (byte[]?, TimeSpan?) ParseWavAndExtractPcm(byte[] wavData, TimeSpan? knownDuration)
        {
            try
            {
                if (wavData == null || wavData.Length < 44) { return (null, null); }

                using var reader = new BinaryReader(new MemoryStream(wavData));
                // --- Basic WAV Header Parsing ---
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return (wavData, knownDuration);
                reader.ReadInt32(); // File size - 8
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return (wavData, knownDuration);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return (wavData, knownDuration);

                int fmtChunkSize = reader.ReadInt32();
                short audioFormat = reader.ReadInt16();
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                short bitsPerSample = reader.ReadInt16();

                if (audioFormat != 1)
                {
                    Console.WriteLine("Speechify Error: Received WAV is not in PCM format.");
                    return (wavData, knownDuration);
                }
                if (fmtChunkSize > 16)
                    reader.BaseStream.Seek(fmtChunkSize - 16, SeekOrigin.Current);

                string dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                while (dataChunkId.ToLowerInvariant() != "data" && reader.BaseStream.Position < wavData.Length - 8)
                {
                    int listChunkSize = reader.ReadInt32();
                    if (listChunkSize <= 0 || reader.BaseStream.Position + listChunkSize > wavData.Length)
                        return (wavData, knownDuration);
                    reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);
                    dataChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                }
                if (dataChunkId.ToLowerInvariant() != "data")
                {
                    Console.WriteLine("Speechify Error: 'data' chunk not found in WAV.");
                    return (wavData, knownDuration);
                }

                int dataChunkSize = reader.ReadInt32();
                if (reader.BaseStream.Position + dataChunkSize > wavData.Length)
                {
                    Console.WriteLine("Speechify Error: WAV data chunk size exceeds available data.");
                    dataChunkSize = (int)(wavData.Length - reader.BaseStream.Position);
                    if (dataChunkSize < 0) dataChunkSize = 0;
                }

                byte[] pcmData = reader.ReadBytes(dataChunkSize);
                // --- End Parsing ---

                // Calculate duration if not already known from speech marks
                TimeSpan? duration = knownDuration;
                if (!duration.HasValue && sampleRate > 0 && bitsPerSample > 0 && channels > 0 && dataChunkSize > 0)
                {
                    double durationSeconds = (double)dataChunkSize / (sampleRate * channels * (bitsPerSample / 8));
                    duration = TimeSpan.FromSeconds(durationSeconds);
                    Console.WriteLine($"Speechify: Calculated duration from WAV header ({duration}).");
                }

                return (pcmData, duration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Speechify WAV Parsing Error: {ex.Message}. Returning original WAV data.");
                return (wavData, knownDuration); // Return original if parsing fails
            }
        }

        public Task StopTextSynthesisAsync()
        {
            // Cancellation is handled via the CancellationToken passed to SynthesizeTextAsync
            return Task.CompletedTask;
        }

        public string GetProviderFullName()
        {
            return "SpeechifyTextToSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.SpeechifyTextToSpeech;
        }

        public void Dispose()
        {
            // Static HttpClient doesn't need instance disposal
            GC.SuppressFinalize(this);
        }
    }
}