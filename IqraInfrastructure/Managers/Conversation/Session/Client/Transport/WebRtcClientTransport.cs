using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    public class WebRtcClientTransport : IConversationClientTransport
    {
        private const uint PCMU_SAMPLES_PER_20MS_PACKET = 160;

        private readonly ILogger _logger;
        private readonly RTCPeerConnection _peerConnection;
        private readonly WebSocket _signalingSocket; // For SDP and ICE exchange
        private CancellationTokenSource _transportCts;

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived; // For data channel text messages
        public event EventHandler<string> Disconnected;

        public WebRtcClientTransport(WebSocket signalingSocket, ILogger logger, CancellationToken sessionCts)
        {
            _logger = logger;
            _signalingSocket = signalingSocket;
            _transportCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts);

            // 1. Configure and create the RTCPeerConnection
            var pcConfig = new RTCConfiguration { iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } } };
            _peerConnection = new RTCPeerConnection(pcConfig);

            // 2. Wire up all the necessary events from the peer connection
            _peerConnection.OnRtpPacketReceived += OnRtpPacketHandler;
            _peerConnection.onicecandidate += OnIceCandidateHandler;
            _peerConnection.onconnectionstatechange += OnConnectionStateChangeHandler;

            // Create a data channel for text chat (optional but good practice)
            var dataChannel = _peerConnection.createDataChannel("chat").GetAwaiter().GetResult();
            dataChannel.onmessage += OnDataChannelMessageHandler;

            // 3. Start listening on the WebSocket for signaling messages
            Task.Run(() => StartSignalingLoop(_transportCts.Token), _transportCts.Token);
        }

        // Main loop to process incoming signaling messages (offers, candidates)
        private async Task StartSignalingLoop(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            while (_signalingSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _signalingSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                        await HandleSignalingMessageAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in signaling loop.");
                    Disconnected?.Invoke(this, $"Signaling error: {ex.Message}");
                    break;
                }
            }
        }

        // Processes the JSON signaling messages
        private async Task HandleSignalingMessageAsync(string json)
        {
            var jsonMsg = JsonDocument.Parse(json).RootElement;
            string type = jsonMsg.GetProperty("type").GetString();

            if (type == "offer")
            {
                var sdp = jsonMsg.GetProperty("sdp").GetString();
                var offer = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = sdp };
                _logger.LogInformation("Received WebRTC Offer.");

                var result = _peerConnection.setRemoteDescription(offer);
                if (result == SetDescriptionResultEnum.OK)
                {
                    var answer = _peerConnection.createAnswer(null);
                    await _peerConnection.setLocalDescription(answer);

                    var answerJson = JsonSerializer.Serialize(new { type = "answer", sdp = answer.sdp });
                    await SendSignalingMessageAsync(answerJson);
                }
                else
                {
                    _logger.LogError("Failed to set remote description from offer. Result: {result}", result);
                }
            }
            else if (type == "candidate")
            {
                var candidateInit = JsonSerializer.Deserialize<RTCIceCandidateInit>(jsonMsg.GetProperty("candidate").ToString());
                _peerConnection.addIceCandidate(candidateInit);
            }
        }

        // Sends a signaling message (answer/candidate) back to the client
        private Task SendSignalingMessageAsync(string jsonMessage)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonMessage);
            return _signalingSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // --- Event Handlers for RTCPeerConnection ---

        private void OnRtpPacketHandler(IPEndPoint remoteEndPoint, SDPMediaTypesEnum media, RTPPacket rtpPacket)
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                // The raw payload of the RTP packet is our audio data.
                BinaryMessageReceived?.Invoke(this, rtpPacket.Payload);
            }
        }

        private void OnDataChannelMessageHandler(RTCDataChannel dc, DataChannelPayloadProtocols protocol, byte[] data)
        {
            // We assume the data is a UTF8 encoded string for chat messages.
            TextMessageReceived?.Invoke(this, Encoding.UTF8.GetString(data));
        }

        private void OnIceCandidateHandler(RTCIceCandidate candidate)
        {
            if (candidate != null)
            {
                _logger.LogInformation("Sending ICE candidate to remote peer.");
                var candidateJson = JsonSerializer.Serialize(new { type = "candidate", candidate = candidate });
                _ = SendSignalingMessageAsync(candidateJson);
            }
        }

        private void OnConnectionStateChangeHandler(RTCPeerConnectionState state)
        {
            _logger.LogInformation("WebRTC connection state changed to {State}", state);
            if (state == RTCPeerConnectionState.failed || state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.disconnected)
            {
                Disconnected?.Invoke(this, $"WebRTC connection state: {state}");
            }
        }

        // --- IConversationClientTransport Implementation ---

        public Task SendBinaryAsync(byte[] data, CancellationToken cancellationToken)
        {
            // This sends audio received from the AI out to the browser client
            _peerConnection.SendAudio(PCMU_SAMPLES_PER_20MS_PACKET, data);
            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            var dataChannel = _peerConnection.DataChannels.FirstOrDefault();
            if (dataChannel != null && dataChannel.readyState == RTCDataChannelState.open)
            {
                dataChannel.send(text);
            }
            else
            {
                _logger.LogWarning("Could not send text message, data channel is not open.");
            }
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason)
        {
            if (!_peerConnection.IsClosed)
            {
                _peerConnection.Close(reason);
            }
            if (_signalingSocket.State == WebSocketState.Open)
            {
                await _signalingSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            }
        }

        public void Dispose()
        {
            _peerConnection.OnRtpPacketReceived -= OnRtpPacketHandler;
            _peerConnection.onicecandidate -= OnIceCandidateHandler;
            _peerConnection.onconnectionstatechange -= OnConnectionStateChangeHandler;

            var dc = _peerConnection.DataChannels.FirstOrDefault();
            if (dc != null) dc.onmessage -= OnDataChannelMessageHandler;

            DisconnectAsync("Disposing").Wait();
            _transportCts?.Dispose();
        }
    }
}
