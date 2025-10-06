using IqraInfrastructure.Managers.Call.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using System.Net;

namespace IqraInfrastructure.Managers.Telephony
{
    public class SipCallManager : IHostedService
    {
        private readonly ILogger<SipCallManager> _logger;
        private readonly BackendCallProcessorManager _callProcessorManager;
        private SIPTransport _sipTransport;

        public SipCallManager(ILogger<SipCallManager> logger, BackendCallProcessorManager callProcessorManager)
        {
            _logger = logger;
            _callProcessorManager = callProcessorManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[SipCallManager] Starting SIP Transport...");
            _sipTransport = new SIPTransport();
            _sipTransport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 5060)));
            _sipTransport.AddSIPChannel(new SIPTCPChannel(new IPEndPoint(IPAddress.Any, 5060)));
            _sipTransport.SIPTransportRequestReceived += OnSipTransportRequestReceived;
            _logger.LogInformation("[SipCallManager] SIP listener started.");
            return Task.CompletedTask;
        }

        private async Task OnSipTransportRequestReceived(SIPEndPoint localSipEndPoint, SIPEndPoint remoteSipEndPoint, SIPRequest sipRequest)
        {
            // This is the dispatcher logic from SIPTransportManager.cs
            if (sipRequest.Method == SIPMethodsEnum.INVITE)
            {
                if (sipRequest.Header.To.ToTag == null) // Initial INVITE
                {
                    _logger.LogInformation("[SipCallManager] INVITE received, dispatching to CallProcessorManager.");
                    // Let the application layer (CallProcessorManager) handle it.
                    // We pass the raw transport and request so the app layer can create the User Agent.
                    //await _callProcessorManager.HandleIncomingSipCall(_sipTransport, sipRequest);
                }
            }
        }

        // Public method for our app to make an outbound call.
        public SIPUserAgent CreateOutboundUserAgent()
        {
            return new SIPUserAgent(_sipTransport, null);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[SipCallManager] Stopping SIP Transport.");
            _sipTransport?.Shutdown();
            return Task.CompletedTask;
        }
    }
}
