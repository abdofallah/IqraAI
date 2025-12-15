using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraInfrastructure.Managers.Call.Backend;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System.Net;

namespace IqraInfrastructure.Managers.SIP
{
    public class SipBackendListenerService : BackgroundService
    {
        private readonly ILogger<SipBackendListenerService> _logger;
        public readonly int _sipPort;
        private readonly BackendCallProcessorManager _backendCallProcessorManager;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;

        private SIPTransport _sipTransport;
        private bool _isRunning = false;

        public SipBackendListenerService(
            ILogger<SipBackendListenerService> logger,
            int sipPort,
            BackendCallProcessorManager backendCallProcessorManager,
            InboundCallQueueRepository inboundCallQueueRepository
        ) {
            _logger = logger;
            _sipPort = sipPort;
            _backendCallProcessorManager = backendCallProcessorManager;
            _inboundCallQueueRepository = inboundCallQueueRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SIP Backend Listener Service...");

            try
            {
                _sipTransport = new SIPTransport();

                // 1. Bind IPv4 UDP (Primary)
                var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, _sipPort));
                _sipTransport.AddSIPChannel(udpChannel);

                // 2. Bind IPv4 TCP (Reliability for large packets)
                try
                {
                    var tcpChannel = new SIPTCPChannel(new IPEndPoint(IPAddress.Any, _sipPort));
                    _sipTransport.AddSIPChannel(tcpChannel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not bind SIP TCP channel (port likely busy, continuing with UDP only): {Message}", ex.Message);
                }

                // 3. Wire up Request Handler
                _sipTransport.SIPTransportRequestReceived += OnRequestReceived;

                _isRunning = true;
                _logger.LogInformation("SIP Backend Listener active on port {Port} (UDP/TCP).", _sipPort);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start SIP Backend Listener.");
                throw; // Fatal error
            }

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Keep service alive
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SIP Backend Listener...");
            _isRunning = false;

            if (_sipTransport != null)
            {
                _sipTransport.SIPTransportRequestReceived -= OnRequestReceived;
                _sipTransport.Shutdown();
            }

            return base.StopAsync(cancellationToken);
        }

        private async Task OnRequestReceived(SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint, SIPRequest request)
        {
            if (!_isRunning) return;

            try
            {
                // 1. HEALTH CHECK (OPTIONS)
                // Carriers send this to check if the media server is alive.
                if (request.Method == SIPMethodsEnum.OPTIONS)
                {
                    var response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
                    await _sipTransport.SendResponseAsync(response);
                    return;
                }

                // 2. INCOMING CALL (INVITE)
                if (request.Method == SIPMethodsEnum.INVITE)
                {
                    await HandleInviteAsync(request);
                    return;
                }

                if (request.Method == SIPMethodsEnum.ACK)
                {
                    var response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
                    await _sipTransport.SendResponseAsync(response);
                    return;
                }

                // Note: BYE, CANCEL, ACK are handled by the specific SIPServerUserAgent 
                // instances created inside the session logic, routed automatically by SIPSorcery 
                // based on the Transaction/Call ID.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SIP Request {Method} from {Remote}", request.Method, remoteEndPoint);
            }
        }

        private async Task HandleInviteAsync(SIPRequest request)
        {
            // if its a new invite
            string callId = request.Header.CallId;
            string didNumber = request.URI.User;

            var userAgent = new SIPUserAgent(_sipTransport, null, true);
            var uas = userAgent.AcceptCall(request);

            var queueId = request.URI.Parameters.Get("X-CallQueue-Id");
            if (string.IsNullOrEmpty(queueId))
            {
                uas.Reject(SIPResponseStatusCodesEnum.NotFound, "Call queue id in request empty or not found", null);
                return;
            }

            var inboundQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(queueId);
            if (inboundQueueData == null)
            {
                uas.Reject(SIPResponseStatusCodesEnum.NotFound, "Call queue not found", null);
                return;
            }

            if (!string.IsNullOrEmpty(inboundQueueData.SessionId))
            {
                // most likely a re-invite
                // just ack/ok for now, later we can update the userAgent/uas for the client
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null));
                return;
            }

            // Hand off to Processor Manager
            var result = await _backendCallProcessorManager.ProcessInboundCallAsync(queueId, inboundQueueData, userAgent, uas);
            if (!result.Success)
            {
                var sipResponseCode = SIPResponseStatusCodesEnum.ServiceUnavailable;
                if (result.Code.Contains("NOT_FOUND") || result.Code.Contains("NO_ROUTE"))
                {
                    sipResponseCode = SIPResponseStatusCodesEnum.NotFound;
                }
                else if (result.Code.Contains("BUSY") || result.Code.Contains("CAPACITY"))
                {
                    sipResponseCode = SIPResponseStatusCodesEnum.BusyHere;
                }

                if (!string.IsNullOrWhiteSpace(queueId))
                {
                    await _inboundCallQueueRepository.SetInboundCallQueueFailedStatusAsync(queueId, new CallQueueLogEntry() { CreatedAt = DateTime.UtcNow, Message = $"[{result.Code}] {result.Message}", Type = CallQueueLogTypeEnum.Error });
                }

                uas.Reject(sipResponseCode, result.Message, null);
                return;
            }

            var finalResult = await _backendCallProcessorManager.AnswerPrimarySIPClientAndNotifyStarted(result.Data!.SessionId);
            if (!finalResult.Success)
            {
                uas.Reject(SIPResponseStatusCodesEnum.ServiceUnavailable, $"[{finalResult.Code}] {finalResult.Message}", null);
                return;
            }
        }
    }
}