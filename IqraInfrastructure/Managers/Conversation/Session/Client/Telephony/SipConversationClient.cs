using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Conversation.Session.Client.Transport;
using Microsoft.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class SipConversationClient : BaseTelephonyConversationClient
    {
        private readonly SIPUserAgent _userAgent;
        private readonly SIPServerUserAgent? _uas;
        private VoIPMediaSession? _rtpSession;
        private readonly ILogger _logger;

        public SipConversationClient(
            string sessionId,
            string clientId,
            ConversationClientConfiguration clientConfig,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            SIPUserAgent userAgent,
            SIPServerUserAgent uas,
            DeferredClientTransport deferredTransport,
            ILogger<SipConversationClient> logger
            ) : base(sessionId, clientId, clientConfig, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, deferredTransport, logger)
        {
            _userAgent = userAgent;
            _uas = uas;
            _logger = logger;
            ClientTelephonyProviderType = TelephonyProviderEnum.SIP;

            _userAgent.OnDtmfTone += (value, duration) =>
            {
                string conValue = value.ToString();
                if (conValue == "10") conValue = "*";
                if (conValue == "11") conValue = "#";
                RaiseDTMFReceived(conValue);
            };
        }

        public async Task Answer()
        {
            if (_uas == null) throw new InvalidOperationException("Cannot Answer: No Inbound Transaction (UAS) provided.");

            var encoder = new AudioEncoder(true, true);
            var mediaEndpoints = new MediaEndPoints
            {
                AudioSource = new AudioExtrasSource(
                    encoder,
                    new AudioSourceOptions {
                        AudioSource = AudioSourcesEnum.None
                    }
                )
            };

            _rtpSession = new VoIPMediaSession(mediaEndpoints);
            _rtpSession.AcceptRtpFromAny = true;

            var realTransport = new SipClientTransport(_userAgent, _rtpSession, _logger, CancellationToken.None);
            if (Transport is DeferredClientTransport deferred)
            {
                deferred.Activate(realTransport);
            }

            SIPURI contactSIPURI = _uas.CallRequest.URI.CopyOf();
            SIPContactHeader contactHeader = new SIPContactHeader(null, contactSIPURI);

            await _userAgent.Answer(_uas, _rtpSession, null, contactHeader);

            var selectedFormat = _rtpSession.AudioStream.RemoteTrack.Capabilities.FirstOrDefault().ToAudioFormat();

            var (encoding, rate, bits) = MapSipCodecToIqra(selectedFormat);

            UpdateAudioConfiguration(encoding, rate, bits);

            // 5. Start RTP Flow
            await _rtpSession.Start();
        }

        private (AudioEncodingTypeEnum, int, int) MapSipCodecToIqra(AudioFormat format)
        {
            string formatName = format.FormatName.ToLower();
            if (format.Codec != null)
            {
                switch (format.Codec)
                {
                    case AudioCodecsEnum.PCM_S16LE:
                    case AudioCodecsEnum.L16:
                        formatName = "l16";
                        break;
                    case AudioCodecsEnum.PCMU:
                        formatName = "mulaw";
                        break;
                    case AudioCodecsEnum.PCMA:
                        formatName = "alaw";
                        break;
                    case AudioCodecsEnum.G722:
                        formatName = "g722";
                        break;
                    case AudioCodecsEnum.OPUS:
                        formatName = "opus";
                        break;
                    case AudioCodecsEnum.G729:
                        formatName = "g729";
                        break;
                    default:
                        break;
                }
            }

            switch (formatName)
            {
                case "pcm":
                case "lpcm":
                case "l16":
                    return (AudioEncodingTypeEnum.PCM, format.ClockRate, 16);
                case "pcmu":
                case "mulaw":
                    return (AudioEncodingTypeEnum.MULAW, 8000, 8);
                case "alaw":
                case "pcma":
                    return (AudioEncodingTypeEnum.ALAW, 8000, 8);
                case "g.722":
                case "g722":
                    return (AudioEncodingTypeEnum.G722, 16000, 16);
                case "opus":
                    return (AudioEncodingTypeEnum.OPUS, format.ClockRate, 16);
                case "g.729":
                case "g729":
                    return (AudioEncodingTypeEnum.G729, 8000, 8);
                default:
                    throw new ArgumentException($"Unsupported audio format: {format.FormatName}");
            }
        }

        public override async Task SendDTMFAsync(List<char> digits, CancellationToken cancellationToken)
        {
            foreach (var digit in digits)
            {
                if (_rtpSession != null && _rtpSession.IsAudioStarted)
                {
                    byte sendDigit;

                    if (char.IsDigit(digit))
                    {
                        // Convert char '5' to byte 5.
                        // We subtract the ASCII value of '0' (48) from the digit char.
                        sendDigit = (byte)(digit - '0');
                    }
                    else if (digit == '*')
                    {
                        sendDigit = 10;
                    }
                    else if (digit == '#')
                    {
                        sendDigit = 11;
                    }
                    else if (digit >= 'A' && digit <= 'D') // Optional: A, B, C, D keys
                    {
                        sendDigit = (byte)(12 + (digit - 'A'));
                    }
                    else
                    {
                        // Invalid digit
                        continue;
                    }

                    await _rtpSession.SendDtmf(sendDigit, cancellationToken);
                }
            }
        }

        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            RaiseAudioReceived(data);
        }

        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            RaiseTextReceived(message);
        }

        public override Task SendAudioAsync(byte[] audioData, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken)
        {
            return Transport.SendBinaryAsync(audioData, sampleRate, bitsPerSample, frameDurationMs, cancellationToken);
        }
    }
}