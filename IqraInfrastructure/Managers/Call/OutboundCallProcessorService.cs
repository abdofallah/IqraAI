using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server.Configuration;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Call
{
    public class OutboundCallProcessorService : IHostedService, IDisposable
    {
        private readonly ILogger<OutboundCallProcessorService> _logger;
        private readonly OutboundCallProcessingOrchestrator _outboundCallProcessingOrchestrator;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private Task _pollTask;
        private PaginationCursor<PaginationCursorNoFilterHelper>? _currentRegionQueueCursor;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private readonly ProxyAppConfig _proxyAppConfig;

        private readonly SemaphoreSlim _parallelProcessingSemaphore;

        public OutboundCallProcessorService(
            ILogger<OutboundCallProcessorService> logger,
            ProxyAppConfig proxyAppConfig,
            OutboundCallProcessingOrchestrator callProcessingOrchestrator,
            OutboundCallQueueRepository outboundCallQueueRepo
        )
        {
            _logger = logger;
            _outboundCallProcessingOrchestrator = callProcessingOrchestrator;
            _proxyAppConfig = proxyAppConfig;
            _outboundCallQueueRepo = outboundCallQueueRepo;

            _parallelProcessingSemaphore = new SemaphoreSlim(
                _proxyAppConfig.OutboundProcessing.ProcessingBatchSize, // initialCount
                _proxyAppConfig.OutboundProcessing.ProcessingBatchSize  // maxCount
            );
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stoppingCts.Token);

            _pollTask = Task.Run(() => PollForWorkAsync(linkedCts.Token), linkedCts.Token);

            return Task.CompletedTask;
        }

        private async Task PollForWorkAsync(CancellationToken token)
        {
            _logger.LogInformation("OutboundCallProcessorService polling loop started for region {Region}.", _proxyAppConfig.RegionId);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var scheduleThreshold = DateTime.UtcNow.AddMinutes(_proxyAppConfig.OutboundProcessing.ScheduleWindowMinutes);

                    var (callsSuccessfullyMarked, nextCursor) = await _outboundCallQueueRepo.GetProcessableOutboundCallsAndMarkAsync(
                        _proxyAppConfig.RegionId,
                        _proxyAppConfig.OutboundProcessing.DbFetchBatchSize,
                        scheduleThreshold,
                        _currentRegionQueueCursor
                    );

                    if (token.IsCancellationRequested) return;

                    if (callsSuccessfullyMarked.Any())
                    {
                        var processingTasks = new List<Task>();

                        foreach (var call in callsSuccessfullyMarked)
                        {
                            if (token.IsCancellationRequested) break;

                            await _parallelProcessingSemaphore.WaitAsync(token);
                            if (token.IsCancellationRequested)
                            {
                                _parallelProcessingSemaphore.Release(); 
                                break;
                            }

                            processingTasks.Add(ProcessSingleCallAsync(call, token));
                        }

                        if (processingTasks.Any()) await Task.WhenAll(processingTasks);
                    }

                    if (nextCursor != null)
                    {
                        _currentRegionQueueCursor = nextCursor;
                    }
                    else
                    {
                        if (_currentRegionQueueCursor != null)
                        {
                            _currentRegionQueueCursor = null;
                        }
                    }

                    if (!callsSuccessfullyMarked.Any())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_proxyAppConfig.OutboundProcessing.PollingIntervalSeconds > 0 ? _proxyAppConfig.OutboundProcessing.PollingIntervalSeconds : 1), token);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { /** Ignore **/ }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical unhandled error in PollForWorkAsync for region {Region}. Polling loop stopping.", _proxyAppConfig.RegionId);
                // This is a critical failure of the polling loop. The service might become unhealthy.
                // Consider how to handle this (e.g., allow app to crash and restart by orchestrator if unrecoverable).
                _currentRegionQueueCursor = null; // Reset cursor on major error.
                                                  // Re-throwing might be appropriate if you want the Task to show as faulted.
                                                  // throw;
            }
        }

        private async Task ProcessSingleCallAsync(OutboundCallQueueData call, CancellationToken token)
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await _outboundCallProcessingOrchestrator.ProcessCallAsync(call);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { /** Ignore **/ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during ProcessSingleCallAsync for call {QueueId}.", call.Id);

                try
                {
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(
                        call.Id,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLog {
                            Type = CallQueueLogTypeEnum.Error,
                            Message = $"Unhandled error during processing: {ex.Message}"
                        },
                        completedAt: DateTime.UtcNow
                    );
                }
                catch (Exception finalEx)
                {
                    _logger.LogError(finalEx, "Failed to even mark call {QueueId} as failed after unhandled processing error.", call.Id);
                }
            }
            finally
            {
                _parallelProcessingSemaphore.Release();
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboundCallProcessorService is stopping for region {Region}.", _proxyAppConfig.RegionId);
            _stoppingCts.Cancel();
            _pollTask?.Wait();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("OutboundCallProcessorService is disposing for region {Region}.", _proxyAppConfig.RegionId);
            _stoppingCts.Dispose();
            _pollTask?.Dispose();
        }
    }
}
