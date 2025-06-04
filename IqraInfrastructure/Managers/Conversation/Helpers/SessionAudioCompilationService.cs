using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace IqraInfrastructure.Services
{
    public class SessionAudioCompilationService
    {
        private readonly ILogger _logger;

        private readonly ConversationStateRepository _stateRepository;
        private readonly ConversationAudioRepository _audioRepository;

        private record AudioChunkInfo(string Reference, DateTime Timestamp, int Size);
        private record ParticipantInfo(string id, string type, bool isAgent, ConversationMemberAudioInfo AudioInfo);
        public SessionAudioCompilationService(ILogger logger, ConversationStateRepository stateRepository, ConversationAudioRepository audioRepository)
        {
            _logger = logger;

            _stateRepository = stateRepository;
            _audioRepository = audioRepository;
        }   

        public async Task CompileConversationAudioAsync(
            string sessionId
        )
        {
            ConversationState? conversationState = null;
            try
            {
                conversationState = await _stateRepository.GetByIdAsync(sessionId);
                if (conversationState == null)
                {
                    _logger.LogError("Audio compilation failed: Conversation state not found for session {SessionId}", sessionId);
                    return;
                }

                List<ParticipantInfo> participantIds = conversationState.Clients.Select(c => new ParticipantInfo(c.ClientId, c.Metadata["Type"], false, c.AudioInfo))
                                        .Concat(conversationState.Agents.Select(a => new ParticipantInfo(a.AgentId, a.Metadata["Type"], true, a.AudioInfo)))
                                        .Distinct()
                                        .ToList<ParticipantInfo>();

                if (!participantIds.Any())
                {
                    return;
                }

                var compiledFileRefs = new Dictionary<string, string>();
                bool anyCompilationFailed = false;

                foreach (var pariticipantInfo in participantIds)
                {
                    try
                    {
                        string? compiledRef = await CompileAudioForParticipantAsync(
                            sessionId,
                            pariticipantInfo,
                            pariticipantInfo.type.ToLower().Contains("modemtel")
                        );

                        if (!string.IsNullOrEmpty(compiledRef))
                        {
                            compiledFileRefs[pariticipantInfo.id] = compiledRef;
                        }
                        else
                        {
                            anyCompilationFailed = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error compiling audio for participant {ParticipantId} in session {SessionId}", pariticipantInfo.id, sessionId);
                        anyCompilationFailed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during audio compilation process for session {SessionId}", sessionId);
            }
        }

        private async Task<string?> CompileAudioForParticipantAsync(
            string sessionId,
            ParticipantInfo participant,
            bool ignoreSilence
        )
        {
            bool isSuccess = false;
            string? failedReason = null;

            await _stateRepository.SetMemberAudioStatusAsync(sessionId, participant.id, ConversationMemberAudioCompilationStatus.Compiling, participant.isAgent);

            try
            {
                string participantPrefix = $"{sessionId}/{participant.id}";
                var chunkReferences = await _audioRepository.ListAudioForConversationAsync(participantPrefix);
                chunkReferences = chunkReferences.Where(r => !r.EndsWith(".raw") && !r.Contains("/compiled/")).ToList(); // Basic filtering

                if (!chunkReferences.Any())
                {
                    isSuccess = false;
                    failedReason = "No audio chunks found";
                    return null;
                }

                var chunkInfos = new List<AudioChunkInfo>();
                foreach (var reference in chunkReferences)
                {
                    var metadata = await _audioRepository.GetAudioMetadataAsync(reference);
                    if (metadata != null && metadata.TryGetValue("timestamp", out var tsStr) && metadata.TryGetValue("size", out var szStr) &&
                        DateTime.TryParse(tsStr, null, DateTimeStyles.RoundtripKind, out var timestamp) && int.TryParse(szStr, out var size))
                    {
                        chunkInfos.Add(new AudioChunkInfo(reference, timestamp.ToUniversalTime(), size));
                    }
                }

                if (!chunkInfos.Any())
                {
                    isSuccess = false;
                    failedReason = "No processable audio chunks";
                    return null;
                }

                chunkInfos.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                using var compiledAudioStream = new MemoryStream();
                DateTime expectedNextStartTime = chunkInfos.First().Timestamp;

                int bytesPerSample = participant.AudioInfo.BitsPerSample / 8;
                int bytesPerSecond = participant.AudioInfo.SampleRate * bytesPerSample * participant.AudioInfo.Channels;

                foreach (var chunkInfo in chunkInfos)
                {
                    TimeSpan gapDuration = chunkInfo.Timestamp - expectedNextStartTime;

                    if (!ignoreSilence)
                    {
                        var silenceBytes = GenerateSilence(gapDuration, participant.AudioInfo.SampleRate, bytesPerSecond, bytesPerSample);
                        if (silenceBytes.Length > 0) await compiledAudioStream.WriteAsync(silenceBytes, 0, silenceBytes.Length);
                    }

                    var audioData = await _audioRepository.RetrieveAudioAsync(chunkInfo.Reference);
                    if (audioData != null && audioData.Length > 0)
                    {
                        await compiledAudioStream.WriteAsync(audioData, 0, audioData.Length);
                        TimeSpan chunkDuration = TimeSpan.FromSeconds((double)audioData.Length / bytesPerSecond);
                        expectedNextStartTime = chunkInfo.Timestamp + chunkDuration;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to retrieve or empty audio for chunk {Reference}, participant {ParticipantId}", chunkInfo.Reference, participant.id);
                    }

                    await Task.Delay(50); // slow down
                }

                if (compiledAudioStream.Length == 0)
                {
                    isSuccess = false;
                    failedReason = "Compiled audio is empty";
                    return null;
                }

                compiledAudioStream.Position = 0;
                byte[] pcmData = compiledAudioStream.ToArray();

                byte[] wavHeader = GenerateWavHeader(
                    participant.AudioInfo.SampleRate,
                    (short)participant.AudioInfo.BitsPerSample,
                    (short)participant.AudioInfo.Channels,
                    pcmData.Length
                );

                byte[] wavData = new byte[wavHeader.Length + pcmData.Length];
                Buffer.BlockCopy(wavHeader, 0, wavData, 0, wavHeader.Length);
                Buffer.BlockCopy(pcmData, 0, wavData, wavHeader.Length, pcmData.Length);

                // Store compiled audio
                string compiledAudioReference = $"{sessionId}/compiled/{participant.id}.wav";

                var compiledMetadata = new Dictionary<string, string>
                {
                    { "ParticipantId", participant.id },
                    { "SessionId", sessionId },
                    { "CompilationTimestamp", DateTime.UtcNow.ToString("o") },
                    { "OriginalChunkCount", chunkInfos.Count.ToString() },
                    { "Format", "wav" }
                };

                bool storedSuccessfully = await _audioRepository.StoreAudioAsync(compiledAudioReference, wavData, compiledMetadata);

                if (storedSuccessfully)
                {
                    await DeleteOriginalChunksAsync(chunkInfos.Select(ci => ci.Reference).ToList(), participant.id, sessionId);

                    isSuccess = true;
                    return compiledAudioReference;
                }
                else
                {
                    isSuccess = false;
                    failedReason = "Failed to store compiled audio";
                    return null;
                }
            }
            finally
            {
                if (isSuccess)
                {
                    await _stateRepository.SetMemberAudioStatusAsync(sessionId, participant.id, ConversationMemberAudioCompilationStatus.Compiled, participant.isAgent);
                }
                else
                {
                    await _stateRepository.SetMemberAudioStatusAsync(sessionId, participant.id, ConversationMemberAudioCompilationStatus.Failed, participant.isAgent, failedReason);
                }
            }
            
        }

        private byte[] GenerateSilence(TimeSpan duration, int sampleRate, int bytesPerSecond, int BytesPerSample)
        {
            if (duration <= TimeSpan.Zero) return Array.Empty<byte>();
            long numBytes = (long)(duration.TotalSeconds * bytesPerSecond);
            if (numBytes % BytesPerSample != 0) numBytes += BytesPerSample - (numBytes % BytesPerSample);
            return numBytes > 0 ? new byte[numBytes] : Array.Empty<byte>();
        }

        private async Task DeleteOriginalChunksAsync(
            List<string> chunkReferences,
            string participantId,
            string sessionId
        ) 
        {
            int successCount = 0;
            int failCount = 0;

            // Consider throttling or limiting concurrency if deleting thousands of files
            var options = new ParallelOptions { MaxDegreeOfParallelism = 10 }; // Limit concurrency
            await Parallel.ForEachAsync(chunkReferences, options, async (reference, cancellationToken) =>
            {
                try
                {
                    if (await _audioRepository.DeleteAudioAsync(reference))
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception deleting chunk {Reference} for participant {ParticipantId}", reference, participantId);
                    Interlocked.Increment(ref failCount);
                }
            });
        }

        private byte[] GenerateWavHeader(int sampleRate, short bitsPerSample, short channels, int pcmDataLength)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8)) // Encoding doesn't matter much for non-char writes
            {
                // RIFF Chunk
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + pcmDataLength); // ChunkSize: 4 (WAVE) + 24 (fmt chunk) + 8 (data chunk header) + pcmDataLength
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt Sub-Chunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk1Size: 16 for PCM
                writer.Write((short)1); // AudioFormat: 1 for PCM (Linear Quantization)
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8)); // ByteRate: SampleRate * NumChannels * BitsPerSample/8
                writer.Write((short)(channels * (bitsPerSample / 8))); // BlockAlign: NumChannels * BitsPerSample/8
                writer.Write(bitsPerSample);

                // data Sub-Chunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(pcmDataLength); // Subchunk2Size: Size of the actual sound data

                return ms.ToArray();
            }
        }
    }
}