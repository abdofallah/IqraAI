using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System.Net;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    /// <summary>
    /// Implements the client transport layer for a single SIP call. It encapsulates
    /// the SIPSorcery User Agent and handles RTP media streams.
    /// This class can wrap both a SIPServerUserAgent (for inbound) and a SIPClientUserAgent (for outbound).
    /// </summary>
    public class SipClientTransport : IConversationClientTransport
    {
        // for an 8kHz codec like PCMU. (8000 samples/sec * 0.020s = 160).
        private const uint PCMU_SAMPLES_PER_FRAME = 160;

        private readonly ILogger _logger;
        private readonly SIPUserAgent _userAgent;
        private readonly VoIPMediaSession _rtpSession;

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public SipClientTransport(SIPUserAgent userAgent, VoIPMediaSession rtpSession, ILogger logger)
        {
            _logger = logger;
            _userAgent = userAgent;
            _rtpSession = rtpSession;

            _userAgent.OnCallHungup += OnCallHungupHandler;
            _rtpSession.OnRtpPacketReceived += OnRtpPacketHandler;
        }

        private void OnCallHungupHandler(SIPDialogue dialogue)
        {
            _logger.LogInformation("[SipClientTransport] Call hung up by signaling (BYE received). Call-ID: {CallID}", _userAgent.CallDescriptor.CallId);
            Disconnected?.Invoke(this, "Call hung up by remote party.");
        }

        private void OnRtpPacketHandler(IPEndPoint remoteEndPoint, SDPMediaTypesEnum media, RTPPacket rtpPacket)
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                BinaryMessageReceived?.Invoke(this, rtpPacket.Payload);
            }
        }

        public Task SendBinaryAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (!_rtpSession.IsAudioStarted || _rtpSession.IsClosed)
            {
                _logger.LogWarning("[SipClientTransport] Attempted to send audio on a non-active RTP session for Call-ID: {CallID}", _userAgent.CallDescriptor.CallId);
                return Task.CompletedTask;
            }

            _rtpSession.SendAudio(PCMU_SAMPLES_PER_FRAME, data);
            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            _logger.LogWarning("[SipClientTransport] SendTextAsync is not supported on a SIP transport.");
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason)
        {
            if (_userAgent.IsHangingUp) return;

            if (!_userAgent.IsCallActive)
            {
                _logger.LogInformation("[SipClientTransport] DisconnectAsync called. Hanging up call {CallID}.", _userAgent.CallDescriptor.CallId);
                _userAgent.Hangup();
            }

            if (!_rtpSession.IsClosed)
            {
                _rtpSession.Close(reason);
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogDebug("[SipClientTransport] Disposing transport for Call-ID: {CallID}", _userAgent.CallDescriptor.CallId);

            if (_userAgent != null)
            {
                _userAgent.OnCallHungup -= OnCallHungupHandler;
            }

            if (_rtpSession != null)
            {
                _rtpSession.OnRtpPacketReceived -= OnRtpPacketHandler;
            }

            _ = DisconnectAsync("Disposing");
        }
    }
}
