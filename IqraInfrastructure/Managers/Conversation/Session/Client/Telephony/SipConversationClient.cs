using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using System.Net;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class SipConversationClient : BaseTelephonyConversationClient
    {
        private const uint PCMU_SAMPLES_PER_FRAME = 160;

        private readonly SIPUserAgent _userAgent;

        private VoIPMediaSession _rtpSession;

        public SipConversationClient(
            string clientId,
            ConversationClientConfiguration clientConfig,
            string ourSipUri,
            string customerSipUri,
            SIPUserAgent userAgent,
            ILogger<SipConversationClient> logger
            ) : base(clientId, clientConfig, ourSipUri, ourSipUri, customerSipUri, null, logger) // Transport is null initially.
        {
            _userAgent = userAgent;
            ClientTelephonyProviderType = TelephonyProviderEnum.SIPTrunking;

            // Wire up the events from the underlying SIPUserAgent.
            _userAgent.OnCallHungup += OnCallHungupHandler;
        }

        private void OnCallHungupHandler(SIPDialogue dialogue)
        {
            _logger.LogInformation("[SipConversationClient] Call hung up for ClientID: {ClientId}", this.ClientId);
            // This will trigger the Disconnected event on the base class, which is what we want.
            // We use the base class's disconnect handler to ensure proper event raising.
            base.OnTransportDisconnected(this, "Call hung up by remote party or timed out.");
        }

        private void OnRtpPacketHandler(IPEndPoint remoteEndPoint, SDPMediaTypesEnum media, RTPPacket rtpPacket)
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                // This will trigger the AudioReceived event on the base class.
                base.RaiseAudioReceived(rtpPacket.Payload);
            }
        }

        // This method is called by the CallProcessorManager to answer an incoming call.
        public async Task<bool> Answer(SIPServerUserAgent uas)
        {
            _logger.LogInformation("[SipConversationClient] Answering call for ClientID: {ClientId}", this.ClientId);
            _rtpSession = CreateAiMediaSession();

            SIPDialogue result = uas.Answer(SDP.SDP_MIME_CONTENTTYPE, _rtpSession.CreateAnswer(null).ToString(), SIPDialogueTransferModesEnum.NotAllowed);

            if (result != null)
            {
                await StartMediaSession();
            }

            return true;
        }

        // This method is called by the CallProcessorManager to initiate an outbound call.
        public async Task<bool> Call(string destination)
        {
            _logger.LogInformation("[SipConversationClient] Initiating call to {Destination} for ClientID: {ClientId}", destination, this.ClientId);
            _rtpSession = CreateAiMediaSession();

            SIPCallDescriptor sIPCallDescriptor = new SIPCallDescriptor(
                null, null, destination,
                $"sip:{this.ClientTelephonyPhoneNumber}", null, null, null, null,
                SIPCallDirection.Out,
                SDP.SDP_MIME_CONTENTTYPE,
                _rtpSession.CreateOffer(null).ToString(), null
            );

            await _userAgent.InitiateCallAsync(sIPCallDescriptor, _rtpSession);
            return true;
        }

        private async Task StartMediaSession()
        {
            _rtpSession.OnRtpPacketReceived += OnRtpPacketHandler;
            await _rtpSession.Start();
        }

        // --- Overriding base class methods ---

        public override Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, CancellationToken cancellationToken)
        {
            if (_rtpSession?.IsAudioStarted == true)
            {
                _rtpSession.SendAudio(PCMU_SAMPLES_PER_FRAME, audioData);
            }
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync(string reason)
        {
            if (_userAgent.IsHangingUp) return Task.CompletedTask;

            if (_userAgent.IsCallActive)
            {
                _userAgent.Hangup();
            }
            if (_rtpSession?.IsClosed == false)
            {
                _rtpSession.Close(reason);
            }
            return Task.CompletedTask;
        }

        // DTMF implementation would go here, similar to the Softphone example.
        public override Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            _logger.LogWarning("SendDTMFAsync for SIP is not yet implemented.");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _logger.LogDebug("[SipConversationClient] Disposing client for Call-ID: {ClientId}", this.ClientId);
            if (_userAgent != null)
            {
                _userAgent.OnCallHungup -= OnCallHungupHandler;
            }
            if (_rtpSession != null)
            {
                _rtpSession.OnRtpPacketReceived -= OnRtpPacketHandler;
                _rtpSession.Close("Disposing");
            }
            base.Dispose();
        }

        private VoIPMediaSession CreateAiMediaSession()
        {
            var audioExtrasSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions());
            var mediaSession = new VoIPMediaSession(new MediaEndPoints { AudioSource = audioExtrasSource });
            mediaSession.AcceptRtpFromAny = true;
            return mediaSession;
        }

        /// <summary>
        /// This method is required by the base class but will not be called for the SIP client,
        /// as it manages its own media events directly from the RTP session.
        /// </summary>
        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            _logger.LogWarning("[SipConversationClient] OnTransportBinaryMessageReceived was called unexpectedly.");
        }

        /// <summary>
        /// This method is required by the base class but will not be called for the SIP client.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            _logger.LogWarning("[SipConversationClient] OnTransportTextMessageReceived was called unexpectedly.");
        }
    }
}
