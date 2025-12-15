using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class ModemTelConversationClient : BaseTelephonyConversationClient
    {
        private readonly ModemTelManager _modemTelManager;
        private readonly string? _providerCallId;
        private readonly string _apiKey;
        private readonly string _apiBaseUrl;

        public ModemTelConversationClient(
            string sessionId,
            string clientId,
            ConversationClientConfiguration clientConfig,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string? providerCallId,
            string apiBaseUrl,
            string apiKey,
            ModemTelManager modemTelManager,
            IConversationClientTransport transport,
            ILogger<ModemTelConversationClient> logger
        ) : base(sessionId, clientId, clientConfig, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _providerCallId = providerCallId;
            _apiBaseUrl = apiBaseUrl;
            _apiKey = apiKey;
            _modemTelManager = modemTelManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.ModemTel;
        }

        /// <summary>
        /// For ModemTel, binary messages from the transport are raw audio.
        /// </summary>
        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            RaiseAudioReceived(data); // Directly raise the audio event
        }

        /// <summary>
        /// For ModemTel, text messages are used for DTMF.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            if (message.StartsWith("DTMF:", StringComparison.OrdinalIgnoreCase))
            {
                var digit = message.Substring("DTMF:".Length);
                if (!string.IsNullOrEmpty(digit))
                {
                    RaiseDTMFReceived(digit);
                }
            }
        }

        /// <summary>
        /// Sends audio data by sending it directly as a binary message via the transport.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken)
        {
            // Delegate sending to the transport
            return Transport.SendBinaryAsync(audioData, sampleRate, bitsPerSample, frameDurationMs, cancellationToken);
        }

        public override Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            string dtmfMessage = $"DTMF:{string.Join("", digits)}";
            // Delegate sending to the transport
            return Transport.SendTextAsync(dtmfMessage, cancellationToken);
        }

        public Task ClearBufferedAudioAync(CancellationToken cancellationToken)
        {
            string clearMessage = "clear";
            // Delegate sending to the transport
            return Transport.SendTextAsync(clearMessage, cancellationToken);
        }

        /// <summary>
        /// Overrides the base disconnect to add ModemTel-specific logic (hanging up the call via API).
        /// </summary>
        public override async Task DisconnectAsync(string reason)
        {
            // First, disconnect the transport
            await base.DisconnectAsync(reason);

            // Then, perform the provider-specific cleanup
            if (!string.IsNullOrEmpty(_providerCallId))
            {
                _logger.LogInformation("Hanging up ModemTel call {CallId} via API.", _providerCallId);
                await _modemTelManager.HangupCallAsync(_apiKey, _apiBaseUrl, _providerCallId);
            }
        }
    }
}