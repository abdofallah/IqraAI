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
        private readonly int _sampleRate;
        private readonly int _bitsPerSample;

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public WebRtcClientTransport(
            WebSocket signalingSocket,
            AudioEncodingTypeEnum targetEncoding,
            int sampleRate,
            int bitsPerSample,
            ILogger logger,
            CancellationToken sessionCts)
        {
            _logger = logger;
            _signalingSocket = signalingSocket;
            _targetEncoding = targetEncoding;
            _sampleRate = sampleRate;
            _transportCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts);

            // 1. Configure SIPSorcery
            var pcConfig = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
            };
            _peerConnection = new RTCPeerConnection(pcConfig);

            // 2. Setup Audio Track based on Configuration
            // We tell SIPSorcery exactly what format we support
            var audioFormat = MapToSipAudioFormat(_targetEncoding, _sampleRate);

            // Add a Send/Recv Audio Track
            var track = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPAudioVideoMediaFormat> { new SDPAudioVideoMediaFormat(audioFormat) });
            _peerConnection.addTrack(track);

            // 3. Events
            _peerConnection.OnRtpPacketReceived += OnRtpPacketHandler;
            _peerConnection.onicecandidate += OnIceCandidateHandler;
            _peerConnection.onconnectionstatechange += OnConnectionStateChangeHandler;

            var dataChannel = _peerConnection.createDataChannel("chat").GetAwaiter().GetResult();
            dataChannel.onmessage += OnDataChannelMessageHandler;

            // 4. Start Signaling
            Task.Run(() => StartSignalingLoop(_transportCts.Token), _transportCts.Token);
        }

        private AudioFormat MapToSipAudioFormat(AudioEncodingTypeEnum encoding, int rate)
        {
            return encoding switch
            {
                AudioEncodingTypeEnum.PCM => new AudioFormat(AudioCodecsEnum.PCM_S16LE, 0, rate, 1, ""),
                AudioEncodingTypeEnum.OPUS => new AudioFormat(AudioCodecsEnum.OPUS, 111, rate, 2, "minptime=10;useinbandfec=1"),
                AudioEncodingTypeEnum.MULAW => new AudioFormat(AudioCodecsEnum.PCMU, 0, 8000, 1, ""),
                AudioEncodingTypeEnum.ALAW => new AudioFormat(AudioCodecsEnum.PCMA, 8, 8000, 1, ""),
                AudioEncodingTypeEnum.G722 => new AudioFormat(AudioCodecsEnum.G722, 9, 16000, 1, ""),
                AudioEncodingTypeEnum.G729 => new AudioFormat(AudioCodecsEnum.G729, 0, 8000, 1, ""),
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

                // Flexible parsing to handle different client JSON structures
                string type = jsonMsg.TryGetProperty("type", out var prop) ? prop.GetString() : "";

                if (type == "offer")
                {
                    var sdp = jsonMsg.GetProperty("sdp").GetString();
                    _peerConnection.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = sdp });

                    var answer = _peerConnection.createAnswer(null);
                    await _peerConnection.setLocalDescription(answer);

                    var resp = new { type = "answer", sdp = answer.sdp };
                    await SendSignalingMessageAsync(JsonSerializer.Serialize(resp));
                }
                else if (type == "candidate")
                {
                    // Handle candidate JSON structure from frontend
                    var candidateObj = jsonMsg.GetProperty("candidate");
                    var candidateInit = JsonSerializer.Deserialize<RTCIceCandidateInit>(candidateObj.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    _peerConnection.addIceCandidate(candidateInit);
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
                // This is the Raw Encoded Payload (e.g., Opus frames or MuLaw bytes)
                // We pass this up. The BaseConversationClient's Decoder (OpusStreamDecoder) handles it.
                BinaryMessageReceived?.Invoke(this, pkt.Payload);
            }
        }

        public Task SendBinaryAsync(byte[] data, CancellationToken cancellationToken)
        {
            // data is PCM. 
            // We tell SIPSorcery to send it. SIPSorcery handles the Opus/MuLaw encoding internally based on the track setup.

            // Samples calculation: 
            // SIPSorcery SendAudio expects samples per duration.
            // But SendAudio only accepts raw bytes for G711 usually. 
            // For Opus, we typically need to use SendAudioRaw if we did encoding, OR pass Short[] if we want it to encode.

            // NOTE: SIPSorcery's RTCPeerConnection.SendAudio(uint duration, byte[] sample) is a helper.
            // Ideally, we push raw RTP if we already encoded.
            // BUT, since we agreed to let SIPSorcery handle encoding for WebRTC to keep RTP clean:

            // We need to assume 'data' is PCM here (because BaseClient used PcmStreamEncoder).
            // We pass it to SIPSorcery.

            // Calculate duration of this chunk
            // Duration = (Bytes / (Rate * Channels * BytesPerSample)) * 90000 (Video) or SampleRate (Audio)
            // Simplified: If backend sends 20ms chunks.

            // SIPSorcery needs Sample Count for timestamping
            uint samples = (uint)(data.Length / 2); // Assuming 16-bit
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
                var msg = new { type = "candidate", candidate = cand };
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