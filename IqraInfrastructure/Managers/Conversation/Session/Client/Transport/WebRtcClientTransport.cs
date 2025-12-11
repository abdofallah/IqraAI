using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    public class WebRtcClientTransport : IConversationClientTransport
    {
        private readonly ILogger _logger;
        private readonly RTCPeerConnection _peerConnection;
        private readonly WebSocket _signalingSocket;
        private readonly CancellationTokenSource _transportCts;

        // Configuration
        private readonly AudioEncodingTypeEnum _targetEncoding;

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public WebRtcClientTransport(
            WebSocket signalingSocket,
            AudioEncodingTypeEnum targetEncoding,
            ILogger logger,
            CancellationToken sessionCts)
        {
            _logger = logger;
            _signalingSocket = signalingSocket;
            _targetEncoding = targetEncoding;
            _transportCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts);

            // 1. Configure SIPSorcery
            var pcConfig = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
            };
            _peerConnection = new RTCPeerConnection(pcConfig);

            // 2. Setup Audio Track based on Configuration
            // We tell SIPSorcery exactly what format we support
            var audioFormat = MapToSipAudioFormat(_targetEncoding);

            // Add a Send/Recv Audio Track
            // This ensures the SDP we generate in the Answer includes this media section
            var track = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPAudioVideoMediaFormat> { new SDPAudioVideoMediaFormat(audioFormat) });
            _peerConnection.addTrack(track);

            // 3. Events
            _peerConnection.OnRtpPacketReceived += OnRtpPacketHandler;
            _peerConnection.onicecandidate += OnIceCandidateHandler;
            _peerConnection.onconnectionstatechange += OnConnectionStateChangeHandler;

            _peerConnection.ondatachannel += (dc) =>
            {
                _logger.LogInformation($"Data channel established: {dc.label}");
                dc.onmessage += OnDataChannelMessageHandler;

                // Send a hello to confirm
                dc.send("Chat Data Channel: Backend Connected"); 
            };

            // 4. Start Signaling
            Task.Run(() => StartSignalingLoop(_transportCts.Token), _transportCts.Token);
        }

        private AudioFormat MapToSipAudioFormat(AudioEncodingTypeEnum encoding)
        {
            return encoding switch
            {
                AudioEncodingTypeEnum.OPUS => new AudioFormat(AudioCodecsEnum.OPUS, 111, 48000, 2, "minptime=10;useinbandfec=1"),
                AudioEncodingTypeEnum.MULAW => new AudioFormat(AudioCodecsEnum.PCMU, 0, 8000, 1, ""),
                AudioEncodingTypeEnum.ALAW => new AudioFormat(AudioCodecsEnum.PCMA, 8, 8000, 1, ""),
                AudioEncodingTypeEnum.G722 => new AudioFormat(AudioCodecsEnum.G722, 9, 16000, 1, ""),
                _ => throw new NotImplementedException($"MapToSipAudioFormat:Encoding {encoding} is not supported."),
            };
        }

        private async Task StartSignalingLoop(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            try
            {
                while (_signalingSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _signalingSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                        await HandleSignalingMessageAsync(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync("Client closed WebSocket");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signaling loop error");
                Disconnected?.Invoke(this, "Signaling Error");
            }
        }

        private async Task HandleSignalingMessageAsync(string json)
        {
            try
            {
                var jsonMsg = JsonDocument.Parse(json).RootElement;
                string type = jsonMsg.TryGetProperty("type", out var prop) ? prop.GetString()?.ToLower() : "";

                if (type == "offer")
                {
                    var sdp = jsonMsg.GetProperty("sdp").GetString();

                    // Create Offer Object
                    var offerInit = new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = sdp };

                    // Check Result
                    var setResult = _peerConnection.setRemoteDescription(offerInit);
                    if (setResult != SetDescriptionResultEnum.OK)
                    {
                        _logger.LogError($"Failed to set remote description: {setResult}");
                        await DisconnectAsync($"SDP Error: {setResult}");
                        return;
                    }

                    var answer = _peerConnection.createAnswer(null);
                    await _peerConnection.setLocalDescription(answer);

                    // Send Answer back to client
                    var resp = new { type = "answer", sdp = answer.sdp };
                    await SendSignalingMessageAsync(JsonSerializer.Serialize(resp));
                }
                else if (type == "candidate")
                {
                    if (jsonMsg.TryGetProperty("candidate", out var candidateProp))
                    {
                        // Handle candidate JSON structure from frontend
                        var candidateInit = JsonSerializer.Deserialize<RTCIceCandidateInit>(
                            candidateProp.ToString(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (candidateInit != null)
                        {
                            _peerConnection.addIceCandidate(candidateInit);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling signaling message");
            }
        }

        private Task SendSignalingMessageAsync(string jsonMessage)
        {
            if (_signalingSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(jsonMessage);
                return _signalingSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            return Task.CompletedTask;
        }

        // --- Data Flow ---
        private void OnRtpPacketHandler(IPEndPoint ep, SDPMediaTypesEnum media, RTPPacket pkt)
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                // Pass Raw Encoded Payload to BaseConversationClient -> Decoder
                BinaryMessageReceived?.Invoke(this, pkt.Payload);
            }
        }

        public Task SendBinaryAsync(byte[] data, int sampleRate, int bitsPerSample, CancellationToken cancellationToken)
        {
            // data is PCM (from BaseClient). SIPSorcery encodes it based on the Track we added.

            int bytesPerSample = bitsPerSample / 8;

            uint samples = (uint)(data.Length / bytesPerSample);

            // SIPSorcery SendAudio expects PCM data.
            // It will encode this data based on the Track we added in the constructor (Opus/PCMU).
            _peerConnection.SendAudio(samples, data);

            return Task.CompletedTask;
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            // Use Data Channel for text
            var dc = _peerConnection.DataChannels.FirstOrDefault();
            if (dc != null && dc.readyState == RTCDataChannelState.open)
                dc.send(text);
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason)
        {
            _peerConnection.Close(reason);
            if (_signalingSocket.State == WebSocketState.Open)
                await _signalingSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
        }

        private void OnDataChannelMessageHandler(RTCDataChannel dc, DataChannelPayloadProtocols proto, byte[] data)
        {
            TextMessageReceived?.Invoke(this, Encoding.UTF8.GetString(data));
        }
        private void OnIceCandidateHandler(RTCIceCandidate cand)
        {
            if (cand != null)
            {
                // 1. Fix the Candidate String
                // SIPSorcery might give "12345 udp...", but Browser needs "candidate:12345 udp..."
                string cleanCandidateStr = cand.candidate;
                if (!string.IsNullOrEmpty(cleanCandidateStr) && !cleanCandidateStr.StartsWith("candidate:"))
                {
                    cleanCandidateStr = "candidate:" + cleanCandidateStr;
                }

                // 2. Create a clean anonymous object matching RTCIceCandidateInit
                var candidateInit = new
                {
                    candidate = cleanCandidateStr,
                    sdpMid = cand.sdpMid,
                    sdpMLineIndex = cand.sdpMLineIndex,
                    usernameFragment = cand.usernameFragment
                };

                // 3. Wrap in the message structure
                var msg = new { type = "candidate", candidate = candidateInit };

                // 4. Send
                _ = SendSignalingMessageAsync(JsonSerializer.Serialize(msg));
            }
        }
        private void OnConnectionStateChangeHandler(RTCPeerConnectionState state)
        {
            if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                Disconnected?.Invoke(this, $"WebRTC State: {state}");
        }

        public void Dispose()
        {
            _peerConnection.Close("disposing");
            _transportCts.Cancel();
        }
    }
}