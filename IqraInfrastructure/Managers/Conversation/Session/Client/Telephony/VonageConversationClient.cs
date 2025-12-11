using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class VonageConversationClient : BaseTelephonyConversationClient
    {
        private readonly VonageManager _vonageManager;
        private readonly string _callUuid;
        private readonly string _jwt;

        public VonageConversationClient(
            string clientId,
            ConversationWebClientConfiguration clientConfig,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string callUuid,
            string jwt,
            VonageManager vonageManager,
            IConversationClientTransport transport,
            ILogger<VonageConversationClient> logger
        ) : base(clientId, clientConfig, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _callUuid = callUuid;
            _jwt = jwt;
            _vonageManager = vonageManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.Vonage;
        }

        /// <summary>
        /// Vonage does not send distinct events like 'start' or 'stop' over the WebSocket.
        /// It primarily sends audio payloads and DTMF tones as JSON objects.
        /// We will parse incoming text messages for these payloads.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            JsonNode? jsonMessage;
            try
            {
                jsonMessage = JsonNode.Parse(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse incoming JSON from Vonage: {Message}", message);
                return;
            }

            string? digit = jsonMessage["digit"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(digit))
            {
                _logger.LogInformation("Received DTMF digit from Vonage: {Digit}", digit);
                RaiseDTMFReceived(digit);
            }
            else
            {
                _logger.LogWarning("Received an unexpected text message on a Vonage client stream: {Message}", message);
            }
        }

        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            RaiseAudioReceived(data);
        }

        /// <summary>
        /// Sends audio to Vonage by wrapping it in their expected JSON format.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, CancellationToken cancellationToken)
        {
            return Transport.SendBinaryAsync(audioData, sampleRate, bitsPerSample, cancellationToken);
        }

        /// <summary>
        /// Sends DTMF digits by wrapping them in Vonage's expected JSON format.
        /// </summary>
        public override Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            //var dtmfMessage = $"{{\"dtmf\":\"{digits}\"}}";
            //return Transport.SendTextAsync(dtmfMessage, cancellationToken);

            return _vonageManager.SendDtmfAsync(_jwt, _callUuid, string.Join("", digits));
        }

        /// <summary>
        /// Disconnects the call by closing the transport and then using the Vonage API to hang up.
        /// </summary>
        public override async Task DisconnectAsync(string reason)
        {
            await base.DisconnectAsync(reason); // Closes WebSocket
            _logger.LogInformation("Ending Vonage call {CallUuid} via API.", _callUuid);
            await _vonageManager.EndCallAsync(_jwt, _callUuid);
        }
    }
}
