using IqraInfrastructure.Managers.Call.Backend;
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

        private SIPTransport _sipTransport;
        private bool _isRunning = false;

        public SipBackendListenerService(
            ILogger<SipBackendListenerService> logger,
            int sipPort,
            BackendCallProcessorManager backendCallProcessorManager
        ) {
            _logger = logger;
            _sipPort = sipPort;
            _backendCallProcessorManager = backendCallProcessorManager;
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
            string callId = request.Header.CallId;
            string didNumber = request.URI.User;

            // Create Transaction & User Agent
            var uasTransaction = new UASInviteTransaction(_sipTransport, request, null);
            var serverUserAgent = new SIPServerUserAgent(_sipTransport, null, uasTransaction, null);

            // Send "100 Trying"
            serverUserAgent.Progress(SIPResponseStatusCodesEnum.Trying, null, null, null, null);

            string? businessIdStr = request.URI.Parameters.Get("X-Business-Id");
            if (!long.TryParse(businessIdStr, out long businessId))
            {
                serverUserAgent.Reject(SIPResponseStatusCodesEnum.NotFound, "Unable to parse business id to long", null);
                return;
            }

            string? phoneId = request.URI.Parameters.Get("X-Phone-Id");
            if (string.IsNullOrEmpty(phoneId))
            {
                serverUserAgent.Reject(SIPResponseStatusCodesEnum.NotFound, "Phone id in request empty or not found", null);
                return;
            }

            // Hand off to Processor Manager
            var result = await _backendCallProcessorManager.ProcessInboundSipCallAsync(serverUserAgent, businessId, phoneId);
            if (!result.Success)
            {
                _logger.LogWarning("Rejecting SIP Call {CallId}: [{Code}] {Message}", callId, result.Code, result.Message);

                // Map internal errors to SIP Response Codes
                var sipResponseCode = SIPResponseStatusCodesEnum.ServiceUnavailable;
                if (result.Code.Contains("NOT_FOUND") || result.Code.Contains("NO_ROUTE"))
                {
                    sipResponseCode = SIPResponseStatusCodesEnum.NotFound;
                }
                else if (result.Code.Contains("BUSY") || result.Code.Contains("CAPACITY"))
                {
                    sipResponseCode = SIPResponseStatusCodesEnum.BusyHere;
                }

                // Reject the call
                serverUserAgent.Reject(sipResponseCode, null, null);
            }
        }
    }
}