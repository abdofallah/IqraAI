using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System.Net;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    public class SipClientTransport : IConversationClientTransport
    {
        private readonly ILogger _logger;
        private readonly SIPUserAgent _userAgent;
        private readonly VoIPMediaSession _rtpSession;
        private readonly CancellationTokenSource _transportCts;

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public SipClientTransport(
            SIPUserAgent userAgent,
            VoIPMediaSession rtpSession,
            ILogger logger,
            CancellationToken sessionToken)
        {
            _logger = logger;
            _userAgent = userAgent;
            _rtpSession = rtpSession;
            _transportCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);

            // Wire up Signaling Events
            _userAgent.OnCallHungup += OnCallHungupHandler;

            // Wire up Media Events
            _rtpSession.OnRtpPacketReceived += OnRtpPacketHandler;
        }

        private void OnCallHungupHandler(SIPDialogue dialogue)
        {
            Disconnected?.Invoke(this, "Call hung up by remote party.");
        }

        private void OnRtpPacketHandler(IPEndPoint remoteEndPoint, SDPMediaTypesEnum media, RTPPacket rtpPacket)
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                BinaryMessageReceived?.Invoke(this, rtpPacket.Payload);
            }
        }

        public Task SendBinaryAsync(byte[] data, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken)
        {
            if (!_rtpSession.IsAudioStarted || _rtpSession.IsClosed)
            {
                // This can happen during startup or shutdown race conditions. Safe to ignore.
                return Task.CompletedTask;
            }

            uint durationRtpUnits = (uint)(sampleRate * frameDurationMs) / 1000;

            _rtpSession.SendAudio(durationRtpUnits, data);

            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            // SIP doesn't support "Text Frames" like WebSockets.
            // We could implement SIP MESSAGE method here if needed later.
            _logger.LogWarning("[SipClientTransport] SendTextAsync called but ignored (SIP).");
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason)
        {
            _logger.LogInformation("[SipClientTransport] Disconnecting... Reason: {Reason}", reason);

            if (_userAgent.IsCallActive)
            {
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
            if (_userAgent != null) _userAgent.OnCallHungup -= OnCallHungupHandler;
            if (_rtpSession != null)
            {
                _rtpSession.OnRtpPacketReceived -= OnRtpPacketHandler;
            }
            _transportCts?.Cancel();
            _transportCts?.Dispose();
        }
    }
}