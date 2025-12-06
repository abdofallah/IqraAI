using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.Audio;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Audio.Encoders;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public abstract class BaseConversationClient : IConversationClient
    {
        public IConversationClientTransport Transport { get; protected set; }
        protected readonly ILogger _logger;
        private bool _hasDisconnected = false;

        public string ClientId { get; }
        public ConversationWebClientConfiguration ClientConfig { get; }
        public abstract ConversationClientType ClientType { get; }

        protected IAudioStreamEncoder? _audioEncoder;

        public event EventHandler<ConversationAudioReceivedEventArgs> AudioReceived;
        public event EventHandler<ConversationTextReceivedEventArgs> TextReceived;
        public event EventHandler<ConversationClientDisconnectedEventArgs> Disconnected;

        protected BaseConversationClient(string clientId, ConversationWebClientConfiguration clientConfig, IConversationClientTransport transport, ILogger logger)
        {
            ClientId = clientId;
            ClientConfig = clientConfig;
            Transport = transport; // Use the public property
            _logger = logger;

            Transport.BinaryMessageReceived += OnTransportBinaryMessageReceived;
            Transport.TextMessageReceived += OnTransportTextMessageReceived;
            Transport.Disconnected += OnTransportDisconnected;

            InitializeEncoder();
        }

        private void InitializeEncoder()
        {
            try
            {
                _audioEncoder = AudioEncoderFactory.CreateEncoder(
                    ClientConfig.AudioEncodingType,
                    ClientConfig.SampleRate,
                    ClientConfig.BitsPerSample,
                    ClientConfig.FrameDurationMs
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client {ClientId}: Failed to initialize audio encoder for format {Format}", ClientId, ClientConfig.AudioEncodingType);
                // We don't throw here to allow the client to exist, but audio sending might fail later.
            }
        }

        /// <summary>
        /// The main entry point for audio coming FROM the AI Agent to be sent TO the User.
        /// This method handles the "Edge Encoding" logic.
        /// </summary>
        public virtual async Task ProcessDownstreamAudioAsync(ReadOnlyMemory<byte> masterAudioData, int masterSampleRate, int masterBitsPerSample)
        {
            if (masterAudioData.IsEmpty) return;

            byte[] dataToSend;
            if (ClientConfig.AudioEncodingType == AudioEncodingTypeEnum.PCM &&
                ClientConfig.SampleRate == masterSampleRate &&
                ClientConfig.BitsPerSample == masterBitsPerSample)
            {
                dataToSend = masterAudioData.ToArray();
            }
            else
            {
                if (_audioEncoder != null)
                {
                    dataToSend = _audioEncoder.Encode(masterAudioData.Span, masterSampleRate, masterBitsPerSample);
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
                await SendAudioAsync(dataToSend, CancellationToken.None);
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
        public abstract Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        public virtual Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            return Transport.SendTextAsync(text, cancellationToken);
        }
        public virtual Task DisconnectAsync(string reason)
        {
            return Transport.DisconnectAsync(reason);
        }

        // Protected event invokers for subclasses to raise the public-facing events
        protected void RaiseAudioReceived(byte[] audioData) => AudioReceived?.Invoke(this, new(audioData));
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
