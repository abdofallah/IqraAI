using IqraCore.Entities.App.Enum;
using IqraCore.Entities.App.Lifecycle;
using IqraInfrastructure.Managers.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.HostedServices.Lifecycle
{
    public class StartupIntegrityCheckService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartupIntegrityCheckService> _logger;
        private readonly AppNodeTypeEnum _appNodeType;

        public StartupIntegrityCheckService(
            IServiceProvider serviceProvider,
            ILogger<StartupIntegrityCheckService> logger,
            AppNodeTypeEnum appNodeType
        ) {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _appNodeType = appNodeType;
        }

        public async Task CheckAsync()
        {
            _logger.LogInformation("Performing Startup Integrity Check for {AppNodeType}...", _appNodeType.ToString());

            using (var scope = _serviceProvider.CreateScope())
            {
                var appManager = scope.ServiceProvider.GetRequiredService<IqraAppManager>();

                try
                {
                    // Force a refresh of the app status
                    await appManager.RefreshConfigAndStatusAsync();
                    var status = appManager.CurrentStatus;
                    var config = appManager.CurrentConfig;

                    // 1. Install Check
                    if (status == AppLifecycleStatus.NotInstalled)
                    {
                        if (_appNodeType == AppNodeTypeEnum.Frontend) return; // Allow Frontend to start Installer

                        _logger.LogCritical("CRITICAL: Application not installed. {AppNodeType} cannot start.", _appNodeType.ToString());
                        throw new InvalidOperationException($"App not installed. Run setup on Frontend.");
                    }

                    // 2. Migration Check
                    if (config != null && config.IsMigrationInProgress)
                    {
                        if (_appNodeType == AppNodeTypeEnum.Frontend) return; // In case of Frontend mid migration crash or exit, continue migration

                        _logger.LogCritical("CRITICAL: Migration in progress. {AppNodeType} cannot start.", _appNodeType.ToString());
                        throw new InvalidOperationException("Migration locked.");
                    }

                    // 3. Version Check
                    if (status == AppLifecycleStatus.VersionMismatch)
                    {
                        if (_appNodeType == AppNodeTypeEnum.Frontend) return; // Frontend will Auto-Migrate

                        _logger.LogCritical("CRITICAL: Version Mismatch. {AppNodeType} is obsolete.", _appNodeType.ToString());
                        throw new InvalidOperationException("Version mismatch.");
                    }

                    _logger.LogInformation("Integrity Check Passed. System is Running.");
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    // Handle DB connection errors gracefully-ish (letting it crash is also fine for Docker)
                    _logger.LogCritical(ex, "Failed to connect to database during Integrity Check.");
                    throw;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
