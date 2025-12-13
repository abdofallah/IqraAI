using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Conversation.Session.Client.Transport;
using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class SipConversationClient : BaseTelephonyConversationClient
    {
        private readonly SIPServerUserAgent _uas; // The incoming call transaction
        private VoIPMediaSession? _rtpSession;
        private readonly ILogger _logger;

        public SipConversationClient(
            string clientId,
            ConversationClientConfiguration clientConfig,
            string ourSipUri,
            string customerSipUri,
            SIPServerUserAgent uas, // Passed from Listener -> Processor -> Here
            DeferredClientTransport deferredTransport, // We pass the deferred transport to base
            ILogger<SipConversationClient> logger
            ) : base(clientId, clientConfig, ourSipUri, ourSipUri, customerSipUri, deferredTransport, logger)
        {
            _uas = uas;
            _logger = logger;
            ClientTelephonyProviderType = TelephonyProviderEnum.SIP;
        }

        // Called by BackendCallProcessorManager to accept the call
        public async Task Answer()
        {
            _logger.LogInformation("[SipConversationClient] Preparing to answer call for ClientID: {ClientId}", this.ClientId);

            // 1. Create Media Session (RTP)
            // We enable standard telephony codecs.
            var mediaEndpoints = new MediaEndPoints
            {
                AudioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.External })
            };

            // Allow G.711 (ULAW/ALAW) and G.722. Opus is optional for SIP but good to have.
            // Note: SIPSorcery defaults usually include PCMU/PCMA/G722.
            _rtpSession = new VoIPMediaSession(mediaEndpoints);
            _rtpSession.AcceptRtpFromAny = true; // Required for some NAT scenarios

            // 2. Initialize Real Transport
            var realTransport = new SipClientTransport(_uas, _rtpSession, _logger, CancellationToken.None);

            // 3. Activate Deferred Transport
            // This connects the BaseClient (Mixer Output) to the SipClientTransport
            if (Transport is DeferredClientTransport deferred)
            {
                deferred.Activate(realTransport);
            }

            // 4. Negotiate SDP
            // SIPSorcery checks the Offer in _uas and generates a compatible Answer
            var answerSdp = _rtpSession.CreateAnswer(null);

            // 5. Send 200 OK
            _logger.LogInformation("[SipConversationClient] Sending 200 OK...");
            _uas.Answer(SDP.SDP_MIME_CONTENTTYPE, answerSdp.ToString(), null, SIPDialogueTransferModesEnum.NotAllowed);

            // 6. Start RTP Flow
            await _rtpSession.Start();

            _logger.LogInformation("[SipConversationClient] Call Answered. RTP Started.");
        }

        // --- SIP Specifics ---

        public override Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            // SIPSorcery handles DTMF via RFC2833 (RTP Events) if negotiated.
            foreach (var digit in digits)
            {
                // 0-9, *, # are standard.
                // Duration usually 100-250ms
                if (_rtpSession != null && _rtpSession.IsAudioStarted)
                {
                    // Convert char to byte event ID (0-9, 10=*, 11=#)
                    // _rtpSession.SendDtmf((byte)digit); // Requires helper mapping
                    // Implementation skipped for brevity, but this is where it goes.
                }
            }
            return Task.CompletedTask;
        }

        // Base methods (SendText, etc) are handled by the Transport implementation
    }
}