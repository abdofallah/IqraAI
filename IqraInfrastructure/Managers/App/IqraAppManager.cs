using IqraCore.Constants;
using IqraCore.Entities.App.Configuration;
using IqraCore.Entities.App.Lifecycle;
using IqraCore.Entities.App.Update;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.S3Storage;
using IqraCore.Models.App;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.S3Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace IqraInfrastructure.Managers.App
{
    public class IqraAppManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IqraAppManager> _logger;

        // Thread-safe atomic status
        private IqraAppConfig? _currentConfig = null;
        private AppLifecycleStatus _currentStatus = AppLifecycleStatus.NotInstalled;
        private UpdateCheckResult _currentUpdateCheckResult = new UpdateCheckResult();

        public IqraAppManager(
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            ILogger<IqraAppManager> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Lightweight accessor for Middleware (No DB call)
        public IqraAppConfig? CurrentConfig => _currentConfig;
        public AppLifecycleStatus CurrentStatus => _currentStatus;
        public UpdateCheckResult CurrentUpdateCheckResult => _currentUpdateCheckResult;

        private async Task<IqraAppConfig?> GetConfigSnapshotAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<AppRepository>();
                return await repo.GetIqraAppConfig();
            }
        }

        /// <summary>
        /// Hits the database to update the local cache. 
        /// Called by Background Service.
        /// </summary>
        public async Task RefreshConfigAndStatusAsync()
        {
            if (_currentStatus == AppLifecycleStatus.AwaitingInstallRestart)
            {
                // never allow a refresh while in this state
                return;
            }

            var config = await GetConfigSnapshotAsync();
            _currentConfig = config;

            if (config == null)
            {
                _currentStatus = AppLifecycleStatus.NotInstalled;
                return;
            }

            if (config == null || !config.AppInstalled)
            {
                _currentStatus = AppLifecycleStatus.NotInstalled;
                return;
            }

            if (config.InstalledVersion != IqraGlobalConstants.CurrentAppVersion)
            {
                _currentStatus = AppLifecycleStatus.VersionMismatch;
                return;
            }

            _currentStatus = AppLifecycleStatus.Running;
        }

        /// <summary>
        /// Run logic to upgrade DB schema to match Binary version.
        /// Only run by Frontend on startup.
        /// </summary>
        public async Task PerformAutoMigrationAsync()
        {
            await RefreshConfigAndStatusAsync();

            if (_currentStatus != AppLifecycleStatus.VersionMismatch)
            {
                throw new Exception("Performing Auto Migration without Version Mismatch.");
            }
            if (Version.Parse(_currentConfig!.InstalledVersion) > Version.Parse(IqraGlobalConstants.CurrentAppVersion))
            {
                throw new Exception("Performing Auto Migration but Database installed version is newer than current binary version.");
            }

            _logger.LogInformation("Starting Automatic Migration from DB Version to {TargetVersion}...", IqraGlobalConstants.CurrentAppVersion);

            using (var scope = _serviceProvider.CreateScope())
            {
                var appRepo = scope.ServiceProvider.GetRequiredService<AppRepository>();
                var metricsManager = scope.ServiceProvider.GetRequiredService<ServerMetricsManager>();

                var appConfig = await appRepo.GetIqraAppConfig() ?? new IqraAppConfig();
                var permConfig = await appRepo.GetAppPermissionConfig() ?? new AppPermissionConfig();

                // 1. ACQUIRE LOCKS
                appConfig.IsMigrationInProgress = true;

                if (permConfig.MaintenanceEnabledAt == null)
                {
                    permConfig.MaintenanceEnabledAt = DateTime.UtcNow;
                    permConfig.PublicMaintenanceEnabledReason = "System Upgrade in Progress.";
                    await appRepo.AddUpdateAppPermissionConfig(permConfig);
                }
                await appRepo.AddUpdateIqraAppConfig(appConfig);

                var timeout = DateTime.UtcNow.AddMinutes(30);
                bool isDrained = false;

                while (DateTime.UtcNow < timeout)
                {
                    (bool anyActive, int activeCount) = await metricsManager.AreAnyWorkerNodesRunningAndCount();

                    if (!anyActive)
                    {
                        _logger.LogInformation("Cluster successfully drained. All worker nodes are offline.");
                        isDrained = true;
                        break;
                    }

                    _logger.LogInformation($"Waiting for nodes to shutdown... ({activeCount} Active nodes detected)\nChecking again in 10s...");
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
                if (!isDrained)
                {
                    throw new Exception("Migration aborted: Cluster did not drain. Please manually shutdown all worker nodes and try again.");
                }

                // 3. PERFORM MIGRATION
                _logger.LogInformation("Applying Database Migrations...");
                // TODO: Logic to switch on appConfig.InstalledVersion and apply patches

                // 4. FINALIZE (Partial Unlock)
                appConfig.InstalledVersion = IqraGlobalConstants.CurrentAppVersion;
                appConfig.IsMigrationInProgress = false;

                await appRepo.AddUpdateIqraAppConfig(appConfig);
            }

            _logger.LogInformation("Migration Complete.");
            await RefreshConfigAndStatusAsync();
        }

        /// <summary>
        /// Executes the Day 0 Setup Wizard logic.
        /// </summary>
        public async Task<FunctionReturnResult> PerformFreshInstallAsync(InstallRequestDto request)
        {
            var result = new FunctionReturnResult();

            // Guard: Double check status to prevent overwriting
            if (_currentStatus != AppLifecycleStatus.NotInstalled || _currentStatus == AppLifecycleStatus.AwaitingInstallRestart)
            {
                return result.SetFailureResult("ALREADY_INSTALLED", "Application is already installed.");
            }

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
                    var appRepository = scope.ServiceProvider.GetRequiredService<AppRepository>();
                    var languagesManager = scope.ServiceProvider.GetRequiredService<LanguagesManager>();
                    var integrationsManager = scope.ServiceProvider.GetRequiredService<IntegrationsManager>();

                    var mongoClient = scope.ServiceProvider.GetRequiredService<MongoDB.Driver.IMongoClient>();
                    using (var mongoSession = await mongoClient.StartSessionAsync())
                    {
                        mongoSession.StartTransaction();

                        try
                        {
                            // Test S3 Configuration First
                            var defaultS3Config = new S3StorageConfigData()
                            {
                                Endpoint = request.S3Config.Endpoint,
                                UseSSL = request.S3Config.UseSSL,
                                AccessKey = request.S3Config.AccessKey,
                                SecretKey = request.S3Config.SecretKey,
                                DisabledAt = null
                            };
                            var defaultS3ClientResult = await S3StorageClientFactory.CreateAmazonS3Client(defaultS3Config);
                            if (!defaultS3ClientResult.Success)
                            {
                                await mongoSession.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "PerformFreshInstallAsync:S3_CONFIG_FAILED",
                                    $"Failed to create Amazon S3 client (invalid configuration? or server offline?): {defaultS3ClientResult.Message}"
                                );
                            }

                            var setS3ConfigResult = await appRepository.AddUpdateDefaultS3StorageConfig(defaultS3Config, mongoSession);
                            if (!setS3ConfigResult)
                            {
                                await mongoSession.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "PerformFreshInstallAsync:S3_CONFIG_SAVE_FAILED",
                                    $"Failed to set default S3 configuration in db"
                                );
                            }

                            // Create Super Admin
                            var adminResult = await userManager.CreateAdminUserAsync(request.AdminEmail, request.AdminPassword, mongoSession);
                            if (!adminResult.Success)
                            {
                                await mongoSession.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "PerformFreshInstallAsync:ADMIN_CREATION_FAILED",
                                    $"Failed to create admin: {adminResult.Message}"
                                );
                            }

                            // Seed Default Data
                            string seedingDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seeding");
                            if (Directory.Exists(seedingDir))
                            {
                                var seedFiles = Directory.GetFiles(seedingDir, "*.json");
                                if (seedFiles.Length == 0)
                                {
                                    await mongoSession.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "PerformFreshInstallAsync:SEEDING_DIR_EMPTY",
                                        $"Seeding directory is empty: {seedingDir}"
                                    );
                                }

                                foreach (var file in seedFiles)
                                {
                                    var filename = Path.GetFileNameWithoutExtension(file);
                                    var parts = filename.Split('.');
                                    if (parts.Length == 2)
                                    {
                                        string dbName = parts[0];
                                        string colName = parts[1];

                                        string json = await File.ReadAllTextAsync(file);
                                        if (!string.IsNullOrWhiteSpace(json))
                                        {
                                            var docs = BsonSerializer.Deserialize<List<BsonDocument>>(json);
                                            if (docs != null && docs.Count > 0)
                                            {
                                                var db = mongoClient.GetDatabase(dbName);
                                                var col = db.GetCollection<BsonDocument>(colName);
                                                
                                                try
                                                {
                                                    await col.InsertManyAsync(mongoSession, docs);
                                                }
                                                catch (Exception ex)
                                                {
                                                    await mongoSession.AbortTransactionAsync();
                                                    return result.SetFailureResult(
                                                        "PerformFreshInstallAsync:SEEDING_FILE_FAILED",
                                                        $"Failed to seed file into db (insertMany failed): {file}: {ex.Message}"
                                                    );
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await mongoSession.AbortTransactionAsync();
                                        return result.SetFailureResult(
                                            "PerformFreshInstallAsync:SEEDING_FILE_INVALID",
                                            $"Seeding file is invalid: {file}"
                                        );
                                    }
                                }
                            }
                            else
                            {
                                await mongoSession.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "PerformFreshInstallAsync:SEEDING_DIR_NOT_FOUND",
                                    $"Failed to find seeding directory (should be in project folder): {seedingDir}"
                                );
                            }

                            // Update Configuration
                            var config = await appRepository.GetIqraAppConfig() ?? new IqraAppConfig();

                            config.AppInstalled = true;
                            config.InstalledVersion = IqraGlobalConstants.CurrentAppVersion;
                            config.InstallationDate = DateTime.UtcNow;
                            config.EnableExtraTelemetry = request.EnableExtraTelemetry;
                            if (string.IsNullOrEmpty(config.InstanceId)) config.InstanceId = Guid.NewGuid().ToString();

                            var saveResult = await appRepository.AddUpdateIqraAppConfig(config, mongoSession);
                            if (!saveResult)
                            {
                                await mongoSession.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "PerformFreshInstallAsync:CONFIG_SAVE_FAILED",
                                    "Failed to save final configuration."
                                );
                            }

                            _currentStatus = AppLifecycleStatus.AwaitingInstallRestart; // force set the status for restart - will auto change on next restart

                            await mongoSession.CommitTransactionAsync();

                            _ = SendTelemetryEvent(config, "installation_complete", new Dictionary<string, object>
                            {
                                { "current_version", IqraGlobalConstants.CurrentAppVersion },
                                { "contact", request.AdminEmail },
                                { "hardware", GetSystemSpecs() }
                            });
                        }
                        catch (Exception ex)
                        {
                            await mongoSession.AbortTransactionAsync();
                            _logger.LogError(ex, "Installation failed.");
                            return result.SetFailureResult(
                                "INSTALL_EXCEPTION",
                                $"Installation failed with exception: {ex.Message}"
                            );
                        }
                    }

                    // Force shutdown app so user restarts it manually - to reinitalize everything
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(5); // delay so the result is recieved by the client
                        var appLifetime = _serviceProvider.GetRequiredService<IHostApplicationLifetime>();
                        appLifetime.StopApplication();
                    });
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Installation failed.");
                return result.SetFailureResult(
                    "INSTALL_EXCEPTION",
                    $"Installation failed with exception: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Checks the GitHub repository for updates and security notices.
        /// </summary>
        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<AppRepository>();
                    var usageManager = scope.ServiceProvider.GetRequiredService<UserUsageManager>();

                    IqraAppConfig? config = await repo.GetIqraAppConfig();
                    if (config != null)
                    {
                        var lastUpdateChecked = config.LastUpdateCheck ?? config.InstallationDate;
                        if (DateTime.UtcNow.Subtract(lastUpdateChecked).TotalHours > 24)
                        {
                            var telemetryData = new Dictionary<string, object>
                            {
                                { "current_version", IqraGlobalConstants.CurrentAppVersion },
                                { "hardware", GetSystemSpecs() }
                            };

                            if (config.EnableExtraTelemetry)
                            {
                                var overallUsageResult = await usageManager.GetOverallUsage(
                                    DateTime.Today,
                                    DateTime.Today.AddDays(1).AddTicks(-1)
                                );
                                if (overallUsageResult.Success)
                                {
                                    telemetryData.Add("overall_usage", JsonSerializer.Serialize(overallUsageResult.Data!));
                                }
                            }

                            _ = SendTelemetryEvent(config, "update_heartbeat", telemetryData);
                            _ = UpdateLastCheckTime();
                        }
                    }
                }

                RemoteUpdateManifest? remoteData = null;
                try
                {
                    remoteData = await client.GetFromJsonAsync<RemoteUpdateManifest>(IqraGlobalConstants.RemoteUpdateManifestUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Update check failed. Failed to fetch remote data.");
                    return;
                }

                if (remoteData == null)
                {
                    _logger.LogWarning("Update check failed. Empty response from remote server.");
                    return;
                }

                // Version Comparison
                if (!Version.TryParse(IqraGlobalConstants.CurrentAppVersion, out var currentVer))
                {
                    _logger.LogWarning("Update check failed. Invalid current version.");
                    return;
                }

                if (!Version.TryParse(remoteData.LatestVersion, out var remoteVer))
                {
                    _logger.LogWarning("Update check failed. Invalid remote version.");
                    return;
                }

                var checkResult = new UpdateCheckResult
                {
                    CurrentVersion = IqraGlobalConstants.CurrentAppVersion,
                    LatestVersion = remoteData.LatestVersion,
                    IsUpdateAvailable = remoteVer > currentVer,
                    ChangelogUrl = remoteData.ChangelogUrl
                };

                // Security Notices Logic
                foreach (var notice in remoteData.CriticalSecurityNotices)
                {
                    if (IsVersionAffected(currentVer, notice.MinVersion, notice.MaxVersion))
                    {
                        checkResult.SecurityWarnings.Add(notice);
                    }
                }

                _currentUpdateCheckResult = checkResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update check failed.");
                return;
            }
        }

        private bool IsVersionAffected(Version current, string minStr, string maxStr)
        {
            if (!Version.TryParse(minStr, out var min)) min = new Version(0, 0, 0);
            if (!Version.TryParse(maxStr, out var max)) max = new Version(999, 999, 999);

            return current >= min && current <= max;
        }

        private async Task UpdateLastCheckTime()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<AppRepository>();
                    var config = await repo.GetIqraAppConfig();
                    if (config != null)
                    {
                        config.LastUpdateCheck = DateTime.UtcNow;
                        await repo.AddUpdateIqraAppConfig(config);
                    }
                }
            }
            catch { /* Ignore db errors for this minor update */ }
        }

        private async Task SendTelemetryEvent(IqraAppConfig config, string eventName, Dictionary<string, object>? extraData = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"IqraAI-Backend/{IqraGlobalConstants.CurrentAppVersion}");

                var payloadData = new Dictionary<string, object>();
                if (extraData != null)
                {
                    foreach (var kvp in extraData) payloadData[kvp.Key] = kvp.Value;
                }

                var requestBody = new
                {
                    payload = new
                    {
                        hostname = config.InstanceId,
                        language = "en-US",
                        referrer = "direct",
                        screen = "1x1",
                        title = eventName,
                        url = "/",
                        name = eventName,
                        website = IqraGlobalConstants.TelemetryWebsiteId,
                        data = payloadData
                    },
                    type = "event"
                };

                var response = await client.PostAsJsonAsync(IqraGlobalConstants.TelemetryEndpoint, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send telemetry: {StatusCode}", response.StatusCode);
                }
                //var responseContent = await response.Content.ReadAsStringAsync();
                //if (!string.IsNullOrEmpty(responseContent))
                //{
                //    _logger.LogInformation("Telemetry response: {Response}", responseContent);
                //}
            }
            catch (Exception ex)
            {
                // Telemetry failure must NEVER crash the app
                _logger.LogWarning("Failed to send telemetry: {Message}", ex.Message);
            }
        }

        private object GetSystemSpecs()
        {
            try
            {
                var memInfo = GC.GetGCMemoryInfo();
                var totalMemoryGb = Math.Round((double)memInfo.TotalAvailableMemoryBytes / 1024 / 1024 / 1024, 2);

                return new
                {
                    os_description = RuntimeInformation.OSDescription,
                    os_arch = RuntimeInformation.OSArchitecture.ToString(),
                    process_arch = RuntimeInformation.ProcessArchitecture.ToString(),
                    cores = Environment.ProcessorCount,
                    memory_gb = totalMemoryGb,
                    framework = RuntimeInformation.FrameworkDescription
                };
            }
            catch
            {
                return new { error = "failed_to_retrieve_specs" };
            }
        }
    }
}