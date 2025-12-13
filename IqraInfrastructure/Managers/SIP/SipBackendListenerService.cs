using IqraCore.Entities.Server;
using IqraInfrastructure.Managers.Call.Backend;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly BackendAppConfig _appConfig;

        private SIPTransport _sipTransport;
        private bool _isRunning = false;

        public const int SIP_PORT = 6261;

        public SipBackendListenerService(
            ILogger<SipBackendListenerService> logger,
            IServiceProvider serviceProvider,
            BackendAppConfig appConfig)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appConfig = appConfig;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SIP Backend Listener Service...");

            try
            {
                _sipTransport = new SIPTransport();

                // 1. Bind IPv4 UDP (Primary)
                var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, SIP_PORT));
                _sipTransport.AddSIPChannel(udpChannel);

                // 2. Bind IPv4 TCP (Reliability for large packets)
                try
                {
                    var tcpChannel = new SIPTCPChannel(new IPEndPoint(IPAddress.Any, SIP_PORT));
                    _sipTransport.AddSIPChannel(tcpChannel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not bind SIP TCP channel (port likely busy, continuing with UDP only): {Message}", ex.Message);
                }

                // 3. Wire up Request Handler
                _sipTransport.SIPTransportRequestReceived += OnRequestReceived;

                _isRunning = true;
                _logger.LogInformation("SIP Backend Listener active on port {Port} (UDP/TCP).", SIP_PORT);
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

            _logger.LogInformation("Processing Inbound SIP Call: To={DID} From={From} Call-ID={CallId}",
                didNumber, request.Header.From.FromURI.User, callId);

            // 1. Create Transaction & User Agent
            // We need to establish the SIP Transaction wrapper immediately to send "100 Trying".
            var uasTransaction = new UASInviteTransaction(_sipTransport, request, null);
            var serverUserAgent = new SIPServerUserAgent(_sipTransport, null, uasTransaction, null);

            // 2. Send "100 Trying"
            // This stops the carrier from re-sending the INVITE immediately.
            serverUserAgent.Progress(SIPResponseStatusCodesEnum.Trying, null, null, null, null);

            // 3. Hand off to Processor Manager
            // We create a scope because BackendCallProcessorManager relies on DB Contexts (Scoped).
            using (var scope = _serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<BackendCallProcessorManager>();

                // This method will:
                // a. Lookup the business/route.
                // b. Create the Session Orchestrator & Mixer.
                // c. Create the SipConversationClient (wrapping this serverUserAgent).
                // d. Answer the call (200 OK).
                var result = await processor.ProcessInboundSipCallAsync(serverUserAgent, didNumber);

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
}