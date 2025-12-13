using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using System.Net;

namespace IqraInfrastructure.Managers.SIP
{
    public class SipProxyService : BackgroundService
    {
        private readonly ILogger<SipProxyService> _logger;
        private readonly BusinessManager _businessManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly RegionManager _regionManager;
        private readonly int _sipPort;
        private SIPTransport _sipTransport;
        private bool _isRunning = false;

        public SipProxyService(
            ILogger<SipProxyService> logger,
            int sipPort,
            BusinessManager businessManager,
            ServerSelectionManager serverSelectionManager,
            RegionManager regionManager
        ) {
            _logger = logger;
            _sipPort = sipPort;
            _businessManager = businessManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SIP Proxy Service...");

            try
            {
                _sipTransport = new SIPTransport();

                // IPv4 UDP
                var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, _sipPort));
                _sipTransport.AddSIPChannel(udpChannel);

                // IPv4 TCP (Reliability)
                try
                {
                    var tcpChannel = new SIPTCPChannel(new IPEndPoint(IPAddress.Any, _sipPort));
                    _sipTransport.AddSIPChannel(tcpChannel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not bind SIP TCP channel (continuing UDP only): {Message}", ex.Message);
                }

                // Wire up events
                _sipTransport.SIPTransportRequestReceived += OnRequestReceived;

                _isRunning = true;
                _logger.LogInformation("SIP Proxy listening on port {Port} (UDP/TCP).", _sipPort);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start SIP Proxy Service.");
                throw;
            }

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SIP Proxy Service...");
            _isRunning = false;
            _sipTransport?.Shutdown();
            return base.StopAsync(cancellationToken);
        }

        private async Task OnRequestReceived(SIPEndPoint localSipEndPoint, SIPEndPoint remoteEndPoint, SIPRequest request)
        {
            if (!_isRunning) return;

            try
            {
                // 1. HEALTH CHECK (OPTIONS)
                if (request.Method == SIPMethodsEnum.OPTIONS)
                {
                    await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null));
                    return;
                }

                // 2. INCOMING CALL (INVITE)
                if (request.Method == SIPMethodsEnum.INVITE)
                {
                    await HandleInviteAsync(remoteEndPoint, request);
                }

                // Other methods (REGISTER, BYE) are not handled by the Load Balancer proxy.
                await _sipTransport.SendResponseAsync(
                    SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.MethodNotAllowed, "Proxy should not get any other requests than INVITE")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SIP Request {Method}", request.Method);
            }
        }

        private async Task HandleInviteAsync(SIPEndPoint remoteEndPoint, SIPRequest request)
        {
            string callId = request.Header.CallId;
            string didNumber = request.URI.User;

            string? businessIdStr = request.URI.Parameters.Get("X-Business-Id");
            if (!long.TryParse(businessIdStr, out long businessId))
            {
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.NotFound, "Unable to parse business id to long"));
                return;
            }

            string? phoneId = request.URI.Parameters.Get("X-Phone-Id");
            if (string.IsNullOrEmpty(phoneId)) {
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.NotFound, "Phone id in request empty or not found"));
                return;
            }

            var numberDataResult = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, phoneId);
            if (numberDataResult == null || numberDataResult.Provider != TelephonyProviderEnum.SIP)
            {
                _logger.LogWarning("SIP Proxy: Number {Number} not found/configured.", didNumber);
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.NotFound, "Number Not Found"));
                return;
            }

            var sipConfig = numberDataResult as BusinessNumberSipData;
            if (sipConfig == null)
            {
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.InternalServerError, "Invalid Configuration"));
                return;
            }

            // ACL Check
            string sourceIp = remoteEndPoint.Address.ToString();
            if (sipConfig.AllowedSourceIps != null && sipConfig.AllowedSourceIps.Count > 0)
            {
                if (!sipConfig.AllowedSourceIps.Contains(sourceIp))
                {
                    await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Forbidden, "Unauthorized Source IP"));
                    return;
                }
            }

            // Get the region data
            var regionData = await _regionManager.GetRegionById(sipConfig.RegionId);
            if (regionData == null || regionData.DisabledAt != null)
            {
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.ServiceUnavailable, "Region not enabled"));
                return;
            }

            // Backend Selection
            var serverResult = await _serverSelectionManager.SelectOptimalServerAsync(sipConfig.RegionId);
            if (!serverResult.Success || !serverResult.Data.Any())
            {
                await _sipTransport.SendResponseAsync(SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.ServiceUnavailable, "No Available Servers"));
                return;
            }
            var backend = serverResult.Data.First();

            var backendServerData = regionData.Servers.First(s => s.Id == backend.ServerId);
            string backendHost = $"{backendServerData.Endpoint}:{backendServerData.SIPPort}";

            var redirectResponse = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.MovedTemporarily, "Redirecting to Media Server");
            var contactUri = new SIPURI(didNumber, backendHost, null);
            redirectResponse.Header.Contact = new List<SIPContactHeader> { new SIPContactHeader(null, contactUri) };

            await _sipTransport.SendResponseAsync(redirectResponse);
        }
    }
}
