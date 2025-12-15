using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Server;
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
        public readonly BackendAppConfig _backendAppConfig;
        private readonly BackendCallProcessorManager _backendCallProcessorManager;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;

        private SIPUserAgent _sipUserAgent;
        private SIPTransport _sipTransport;

        public SipBackendListenerService(
            ILogger<SipBackendListenerService> logger,
            BackendAppConfig backendAppConfig,
            BackendCallProcessorManager backendCallProcessorManager,
            InboundCallQueueRepository inboundCallQueueRepository
        ) {
            _logger = logger;
            _backendAppConfig = backendAppConfig;
            _backendCallProcessorManager = backendCallProcessorManager;
            _inboundCallQueueRepository = inboundCallQueueRepository;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting SIP Backend Listener Service...");

            try
            {
                _sipTransport = new SIPTransport();

                var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, _backendAppConfig.SIPPort));
                _sipTransport.AddSIPChannel(udpChannel);

                try
                {
                    var tcpChannel = new SIPTCPChannel(new IPEndPoint(IPAddress.Any, _backendAppConfig.SIPPort));
                    _sipTransport.AddSIPChannel(tcpChannel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not bind SIP TCP channel (port likely busy, continuing with UDP only): {Message}", ex.Message);
                }

                //_sipTransport.SIPTransportRequestReceived += OnRequestReceived;

                _sipUserAgent = new SIPUserAgent(_sipTransport, null, false, null);

                _sipUserAgent.OnIncomingCall += async (sipUserAgent, request) => await OnIncomingCall(sipUserAgent, request);

                _logger.LogInformation("SIP Backend Listener active on port {Port} (UDP/TCP).", _backendAppConfig.SIPPort);
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

            if (_sipUserAgent != null)
            {
                _sipUserAgent.OnIncomingCall -= async (sipUserAgent, request) => await OnIncomingCall(sipUserAgent, request);
                _sipUserAgent.Dispose();
            }

            if (_sipTransport != null)
            {
                _sipTransport.Shutdown();
                _sipTransport.Dispose();
            }

            return base.StopAsync(cancellationToken);
        }

        private async Task OnIncomingCall(SIPUserAgent sipUserAgent, SIPRequest request)
        {
            string callId = request.Header.CallId;
            string didNumber = request.URI.User;

            var uas = sipUserAgent.AcceptCall(request);

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

            if (inboundQueueData.RegionId != _backendAppConfig.RegionId || inboundQueueData.ProcessingBackendServerId != _backendAppConfig.Id)
            {
                uas.Reject(SIPResponseStatusCodesEnum.Forbidden, "Not authorized for this region/server", null);
                return;
            }

            if (inboundQueueData.Status != CallQueueStatusEnum.ProcessedProxy)
            {
                uas.Reject(SIPResponseStatusCodesEnum.Forbidden, "Call queue not in processed proxy state", null);
                return;
            }

            // Hand off to Processor Manager
            var result = await _backendCallProcessorManager.ProcessInboundCallAsync(queueId, inboundQueueData, sipUserAgent, uas);
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