using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.Audio;
using IqraCore.Interfaces.Audio.Decoders;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Audio.Decoders;
using IqraInfrastructure.Managers.Audio.Encoders;
using IqraInfrastructure.Managers.Conversation.Session.Client.Transport;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public abstract class BaseConversationClient : IConversationClient
    {
        public IConversationClientTransport Transport { get; protected set; }
        protected readonly ILogger _logger;
        private bool _hasDisconnected = false;

        public string SessionId { get; }
        public string ClientId { get; }
        public ConversationClientConfiguration ClientConfig { get; }
        public abstract ConversationClientType ClientType { get; }

        protected IAudioStreamEncoder? _audioEncoder;
        protected IAudioStreamDecoder? _audioDecoder;

        public event EventHandler<ConversationAudioReceivedEventArgs> AudioReceived;
        public event EventHandler<ConversationTextReceivedEventArgs> TextReceived;
        public event EventHandler<ConversationClientDisconnectedEventArgs> Disconnected;

        protected BaseConversationClient(string sessionId, string clientId, ConversationClientConfiguration clientConfig, IConversationClientTransport transport, ILogger logger)
        {
            SessionId = sessionId;
            ClientId = clientId;
            ClientConfig = clientConfig;
            Transport = transport;
            _logger = logger;

            Transport.BinaryMessageReceived += OnTransportBinaryMessageReceived;
            Transport.TextMessageReceived += OnTransportTextMessageReceived;
            Transport.Disconnected += OnTransportDisconnected;

            InitializeEncoder();
            InitializeDecoder();
        }

        private void InitializeEncoder()
        {
            try
            {
                _audioEncoder = AudioEncoderFactory.CreateEncoder(
                    ClientConfig.AudioOutputConfiguration.AudioEncodingType,
                    ClientConfig.AudioOutputConfiguration.SampleRate,
                    ClientConfig.AudioOutputConfiguration.BitsPerSample,
                    ClientConfig.AudioOutputConfiguration.FrameDurationMs
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client {ClientId}: Failed to initialize audio encoder for format {Format}", ClientId, ClientConfig.AudioOutputConfiguration.AudioEncodingType);
                // We don't throw here to allow the client to exist, but audio sending might fail later.
            }
        }

        private void InitializeDecoder()
        {
            try
            {
                _audioDecoder = AudioDecoderFactory.CreateDecoder(
                    ClientConfig.AudioInputConfiguration.AudioEncodingType,
                    ClientConfig.AudioInputConfiguration.SampleRate,
                    ClientConfig.AudioInputConfiguration.BitsPerSample
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client {ClientId}: Failed to initialize audio decoder for format {Format}", ClientId, ClientConfig.AudioInputConfiguration.AudioEncodingType);
                // We don't throw here to allow the client to exist, but audio sending might fail later.
            }
        }

        public void UpdateAudioConfiguration(AudioEncodingTypeEnum inputEncoding, int inputRate, int inputBits)
        {
            // 1. Update Config Object
            ClientConfig.AudioInputConfiguration.AudioEncodingType = inputEncoding;
            ClientConfig.AudioInputConfiguration.SampleRate = inputRate;
            ClientConfig.AudioInputConfiguration.BitsPerSample = inputBits;

            ClientConfig.AudioOutputConfiguration.AudioEncodingType = inputEncoding;
            ClientConfig.AudioOutputConfiguration.SampleRate = inputRate;
            ClientConfig.AudioOutputConfiguration.BitsPerSample = inputBits;

            // 2. Re-Initialize Decoder
            _audioDecoder?.Dispose();
            InitializeDecoder();

            // 3. Re-Initialize Encoder
            _audioEncoder?.Dispose();
            InitializeEncoder();
        }

        /// <summary>
        /// The main entry point for audio coming FROM the AI Agent to be sent TO the User.
        /// This method handles the "Edge Encoding" logic.
        /// </summary>
        public virtual async Task ProcessDownstreamAudioAsync(ReadOnlyMemory<byte> masterAudioData, int masterSampleRate, int masterBitsPerSample, int frameDurationMs)
        {
            if (masterAudioData.IsEmpty) return;

            bool isDeferredTransport = false;
            bool isDeferredActivated = false;

            if (Transport is DeferredClientTransport deferredClientTransport)
            {
                isDeferredTransport = true;
                isDeferredActivated = deferredClientTransport.IsActivated;
            }

            if (isDeferredTransport && !isDeferredActivated)
            {
                return;
            }

            byte[] dataToSend;
            int dataSampleRate;
            int dataBitsPerSample;

            // Special case for OPUS where we define custom frame duration
            if (ClientConfig.AudioOutputConfiguration.AudioEncodingType == AudioEncodingTypeEnum.OPUS)
            {
                frameDurationMs = ClientConfig.AudioOutputConfiguration.FrameDurationMs;
            }

            if (
                (
                    ClientConfig.AudioOutputConfiguration.AudioEncodingType == AudioEncodingTypeEnum.PCM &&
                    ClientConfig.AudioOutputConfiguration.SampleRate == masterSampleRate &&
                    ClientConfig.AudioOutputConfiguration.BitsPerSample == masterBitsPerSample
                )
            ) {
                dataToSend = masterAudioData.ToArray();
                dataSampleRate = masterSampleRate;
                dataBitsPerSample = masterBitsPerSample;
            }
            else
            {
                if (_audioEncoder != null)
                {
                    dataToSend = _audioEncoder.Encode(masterAudioData.Span, masterSampleRate, masterBitsPerSample);
                    dataSampleRate = ClientConfig.AudioOutputConfiguration.SampleRate;
                    dataBitsPerSample = ClientConfig.AudioOutputConfiguration.BitsPerSample;
                }
                else
                {
                    // Fallback if encoder failed to init (send raw or drop? dropping safest to avoid ear blasting)
                    _logger.LogWarning("Client {ClientId}: Encoder not initialized, dropping audio frame.", ClientId);
                    return;
                }
            }

            if (dataToSend.Length > 0)
            {
                await SendAudioAsync(dataToSend, dataSampleRate, dataBitsPerSample, frameDurationMs, CancellationToken.None);
            }
        }

        // Abstract handlers for subclasses to implement protocol-specific logic
        protected abstract void OnTransportBinaryMessageReceived(object sender, byte[] data);
        protected abstract void OnTransportTextMessageReceived(object sender, string message);

        protected virtual void OnTransportDisconnected(object sender, string reason)
        {
            if (_hasDisconnected) return;
            _hasDisconnected = true;

            _logger.LogInformation("Client {ClientId} transport disconnected. Reason: {Reason}", ClientId, reason);
            Disconnected?.Invoke(this, new ConversationClientDisconnectedEventArgs(reason));
        }

        // Implement IConversationClient methods by delegating to the transport
        public abstract Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken);
        public virtual Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            return Transport.SendTextAsync(text, cancellationToken);
        }
        public virtual Task DisconnectAsync(string reason)
        {
            return Transport.DisconnectAsync(reason);
        }

        // Protected event invokers for subclasses to raise the public-facing events
        private const AudioEncodingTypeEnum TargetAudioEncodingType = AudioEncodingTypeEnum.PCM;
        private const int TargetSampleRate = 16000;
        private const int TargetBitsPerSample = 32;
        protected void RaiseAudioReceived(byte[] audioData)
        {
            if (audioData.Length == 0) return;

            byte[] decodedData;
            if (ClientConfig.AudioInputConfiguration.AudioEncodingType == TargetAudioEncodingType &&
                ClientConfig.AudioInputConfiguration.SampleRate == TargetSampleRate &&
                ClientConfig.AudioInputConfiguration.BitsPerSample == TargetBitsPerSample)
            {
                decodedData = audioData;
            }
            else
            {
                if (_audioDecoder != null)
                {
                    decodedData = _audioDecoder.Decode(audioData);
                }
                else
                {
                    // Fallback if decoder failed to init (send raw or drop? dropping safest to avoid ear blasting)
                    _logger.LogWarning("Client {ClientId}: Decoder not initialized, dropping audio frame.", ClientId);
                    return;
                }
            }

            if (decodedData.Length > 0)
            {
                AudioReceived?.Invoke(this, new(decodedData, TargetSampleRate, TargetBitsPerSample));
            }
        }
        protected void RaiseTextReceived(string text) => TextReceived?.Invoke(this, new(text));

        public virtual void Dispose()
        {
            Transport.BinaryMessageReceived -= OnTransportBinaryMessageReceived;
            Transport.TextMessageReceived -= OnTransportTextMessageReceived;
            Transport.Disconnected -= OnTransportDisconnected;
            Transport.Dispose();

            _audioEncoder?.Dispose();
        }
    }
}
