using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Call
{
    public class OutboundCallProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger<OutboundCallProcessorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly IConfiguration _configuration;
        private Timer _pollTimer;
        private Timer _cleanupTimer;
        private PaginationCursor? _currentRegionQueueCursor;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private string _myProxyRegionId;
        private string _myProxyInstanceId;
        private int _pollingIntervalMs;
        private int _dbFetchBatchSize;
        private int _processingBatchSize;
        private int _scheduleWindowMinutes;
        private int _stuckCallCleanupIntervalMs;
        private int _stuckCallThresholdMinutes;

        public OutboundCallProcessorService(
            ILogger<OutboundCallProcessorService> logger,
            IServiceScopeFactory scopeFactory,
            OutboundCallQueueRepository outboundCallQueueRepo,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _outboundCallQueueRepo = outboundCallQueueRepo;
            _configuration = configuration;

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            _myProxyRegionId = _configuration["Proxy:RegionId"] ?? throw new ArgumentException("Proxy:RegionId not configured.");
            _myProxyInstanceId = _configuration["Proxy:InstanceId"] ?? $"{System.Net.Dns.GetHostName()}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            _pollingIntervalMs = _configuration.GetValue<int>("OutboundProcessing:PollingIntervalSeconds", 5) * 1000;
            _dbFetchBatchSize = _configuration.GetValue<int>("OutboundProcessing:DbFetchBatchSize", 50);
            _processingBatchSize = _configuration.GetValue<int>("OutboundProcessing:ProcessingBatchSize", 10);
            _scheduleWindowMinutes = _configuration.GetValue<int>("OutboundProcessing:ScheduleWindowMinutes", 2); // Look 2 mins ahead
            _stuckCallCleanupIntervalMs = _configuration.GetValue<int>("OutboundProcessing:StuckCallCleanupIntervalMinutes", 15) * 60 * 1000;
            _stuckCallThresholdMinutes = _configuration.GetValue<int>("OutboundProcessing:StuckCallThresholdMinutes", 5);

            _logger.LogInformation("OutboundCallProcessorService Configured for Region: {Region}, Instance: {Instance}, PollInterval: {PollMs}ms, DbFetchBatch: {DbFetch}, ProcessingBatch: {ProcBatch}",
               _myProxyRegionId, _myProxyInstanceId, _pollingIntervalMs, _dbFetchBatchSize, _processingBatchSize);
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboundCallProcessorService for region {Region} (Instance: {Instance}) is starting.", _myProxyRegionId, _myProxyInstanceId);

            // Combine external token with internal for stopping
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);

            _pollTimer = new Timer(async (s) => await PollForWorkAsync(linkedCts.Token), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_pollingIntervalMs));

            if (_stuckCallCleanupIntervalMs > 0)
            {
                _cleanupTimer = new Timer(async (s) => await CleanupStuckCallsAsync(linkedCts.Token), null, TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(_stuckCallCleanupIntervalMs));
            }

            return Task.CompletedTask;
        }

        private async Task PollForWorkAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            _logger.LogDebug("Polling for outbound calls in region {Region}. Cursor: {CursorId} @ {CursorTs}",
                _myProxyRegionId, _currentRegionQueueCursor?.Id, _currentRegionQueueCursor?.Timestamp);

            try
            {
                var scheduleThreshold = DateTime.UtcNow.AddMinutes(_scheduleWindowMinutes);

                var (callsToProcess, nextCursor) = await _outboundCallQueueRepo.GetProcessableOutboundCallsAndMarkAsync(
                    _myProxyRegionId,
                    _dbFetchBatchSize,
                    _processingBatchSize,
                    scheduleThreshold,
                    _myProxyInstanceId,
                    _currentRegionQueueCursor);

                if (token.IsCancellationRequested) return;

                if (callsToProcess.Any())
                {
                    _logger.LogInformation("Proxy {Instance} picked {Count} calls in region {Region}. Processing...",
                        _myProxyInstanceId, callsToProcess.Count, _myProxyRegionId);

                    // Process calls one by one to avoid overwhelming this single timer thread execution
                    // For parallel processing, a more robust task management/producer-consumer pattern would be needed.
                    foreach (var call in callsToProcess)
                    {
                        if (token.IsCancellationRequested) break;
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var orchestrator = scope.ServiceProvider.GetRequiredService<IOutboundCallProcessingOrchestrator>();
                            await orchestrator.ProcessCallAsync(call);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing call {QueueId} via orchestrator. Call might be left in WaitingForProcessing by this proxy.", call.Id);
                            // Consider requeuing or marking as failed if orchestrator fails catastrophically
                            // For now, orchestrator handles its own final state updates.
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("Proxy {Instance} found no new calls to process in region {Region} for this batch scan.", _myProxyInstanceId, _myProxyRegionId);
                }

                // Update cursor logic
                if (nextCursor != null)
                {
                    if (_currentRegionQueueCursor == null ||
                        nextCursor.Timestamp != _currentRegionQueueCursor.Timestamp ||
                        nextCursor.Id != _currentRegionQueueCursor.Id)
                    {
                        _logger.LogDebug("Updating cursor for region {Region} to: {Id} @ {Ts}", _myProxyRegionId, nextCursor.Id, nextCursor.Timestamp);
                    }
                    _currentRegionQueueCursor = nextCursor;
                }
                else
                {
                    if (_currentRegionQueueCursor != null) // Only log/reset if we previously had a cursor
                    {
                        _logger.LogInformation("Resetting queue cursor for region {Region} as end of scannable queue was reached or indicated by repository.", _myProxyRegionId);
                        _currentRegionQueueCursor = null;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("Polling for work was canceled for region {Region}.", _myProxyRegionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PollForWorkAsync for region {Region}.", _myProxyRegionId);
                // On critical error, maybe reset cursor to avoid getting stuck on a problematic range.
                _currentRegionQueueCursor = null;
            }
        }

        private async Task CleanupStuckCallsAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            _logger.LogInformation("Running cleanup for stuck outbound calls in region {Region}.", _myProxyRegionId);
            try
            {
                int cleanedCount = await _outboundCallQueueRepo.CleanupStuckOutboundCallsAsync(
                    TimeSpan.FromMinutes(_stuckCallThresholdMinutes),
                    _myProxyInstanceId); // Don't clean up own calls if this proxy is just slow
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} stuck outbound calls in region {Region}.", cleanedCount, _myProxyRegionId);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogInformation("Stuck call cleanup was canceled for region {Region}.", _myProxyRegionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stuck call cleanup for region {Region}.", _myProxyRegionId);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboundCallProcessorService is stopping for region {Region}.", _myProxyRegionId);
            _stoppingCts.Cancel(); // Signal async loops to stop
            _pollTimer?.Change(Timeout.Infinite, 0);
            _cleanupTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _stoppingCts.Dispose();
            _pollTimer?.Dispose();
            _cleanupTimer?.Dispose();
        }
    }
}
