using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class TelnyxConversationClient : BaseTelephonyConversationClient
    {
        private readonly TelnyxManager _telnyxManager;
        private readonly string _apiKey;
        private readonly string _callControlId;

        // This is the critical ID received from the 'start' event, needed for sending media.
        private string _streamId;

        public TelnyxConversationClient(
            string clientId, // This would typically be the call_control_id
            ConversationWebClientConfiguration clientConfig,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string apiKey,
            TelnyxManager telnyxManager,
            IConversationClientTransport transport,
            ILogger<TelnyxConversationClient> logger
        ) : base(clientId, clientConfig, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _callControlId = clientId; // The client ID is the call control ID for Telnyx.
            _apiKey = apiKey;
            _telnyxManager = telnyxManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.Telnyx;
        }

        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            JsonNode jsonMessage;
            try
            {
                jsonMessage = JsonNode.Parse(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse incoming JSON from Telnyx: {Message}", message);
                return;
            }

            string eventType = jsonMessage["event"]?.GetValue<string>();

            switch (eventType)
            {
                case "start":
                    _streamId = jsonMessage["stream_id"]?.GetValue<string>();
                    _logger.LogInformation("Telnyx stream started with stream_id: {StreamId}", _streamId);
                    break;

                case "media":
                    string payload = jsonMessage["media"]?["payload"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        try
                        {
                            byte[] audioData = Convert.FromBase64String(payload);
                            RaiseAudioReceived(audioData);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Received malformed Base64 audio payload from Telnyx.");
                        }
                    }
                    break;

                case "dtmf":
                    string digit = jsonMessage["dtmf"]?["digit"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(digit))
                    {
                        RaiseDTMFReceived(digit);
                    }
                    break;

                case "stop":
                    _logger.LogInformation("Received 'stop' event from Telnyx for stream {StreamId}.", _streamId);
                    // The WebSocket connection will be closed by Telnyx, triggering the transport's disconnect.
                    break;
            }
        }

        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            _logger.LogWarning("Received an unexpected binary message on a Telnyx client stream. Ignoring {DataLength} bytes.", data.Length);
        }

        public override Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_streamId))
            {
                _logger.LogWarning("Cannot send audio, Telnyx stream_id has not been received yet.");
                return Task.CompletedTask;
            }

            var mediaPayload = new { payload = Convert.ToBase64String(audioData) };
            var mediaMessage = new { @event = "media", stream_id = _streamId, media = mediaPayload };
            string jsonPayload = JsonSerializer.Serialize(mediaMessage, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            return Transport.SendTextAsync(jsonPayload, cancellationToken);
        }

        public Task ClearBufferedAudioAync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_streamId))
            {
                _logger.LogWarning("Cannot clear buffer, Telnyx stream_id has not been received yet.");
                return Task.CompletedTask;
            }

            var clearMessage = new { @event = "clear", stream_id = _streamId };
            string jsonPayload = JsonSerializer.Serialize(clearMessage);
            return Transport.SendTextAsync(jsonPayload, cancellationToken);
        }

        public override Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            // Sending DTMF is a REST API call, not a WebSocket message for Telnyx.
            // This requires adding a 'SendDtmfAsync' method to the TelnyxManager.
            _logger.LogInformation("Sending DTMF via Telnyx API for call {CallControlId}", _callControlId);
            // return _telnyxManager.SendDtmfAsync(_apiKey, _callControlId, digits);
            _logger.LogWarning("SendDTMFAsync for Telnyx is not yet implemented in the manager.");
            return Task.CompletedTask;
        }

        public override async Task DisconnectAsync(string reason)
        {
            await base.DisconnectAsync(reason); // Closes WebSocket via transport
            _logger.LogInformation("Ending Telnyx call {CallControlId} via API.", _callControlId);
            await _telnyxManager.EndCallAsync(_apiKey, _callControlId);
        }
    }
}
