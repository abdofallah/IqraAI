using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Agent.Script.Node.FlowAppNode;
using IqraCore.Entities.FlowApp;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Integration;
using IqraCore.Models.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.FlowApp;
using IqraInfrastructure.Utilities.Templating;
using IqraInfrastructure.Utilities.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp
{
    public class FlowAppManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ScribanRenderService _scribanService;
        private readonly NJsonSchemaValidator _schemaValidator;
        private readonly AES256EncryptionService _integrationEncryptionService;
        private readonly FlowAppRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FlowAppManager> _logger;

        // The in-memory registry of all discovered apps
        private readonly Dictionary<string, IFlowApp> _apps = new();

        private const string CacheKeyPermissions = "FlowApp_Permissions_Cache";

        public FlowAppManager(
            IServiceProvider serviceProvider,
            ScribanRenderService scribanService,
            NJsonSchemaValidator schemaValidator,
            AES256EncryptionService integrationEncryptionService,
            FlowAppRepository repository,
            IMemoryCache cache,
            ILogger<FlowAppManager> logger)
        {
            _serviceProvider = serviceProvider;
            _scribanService = scribanService;
            _schemaValidator = schemaValidator;
            _integrationEncryptionService = integrationEncryptionService;
            _repository = repository;
            _cache = cache;
            _logger = logger;

            // Discover apps immediately upon instantiation
            InitializeApps();
        }

        /// <summary>
        /// Uses Reflection to find and instantiate all IFlowApp implementations.
        /// </summary>
        private void InitializeApps()
        {
            try
            {
                var appType = typeof(IFlowApp);
                var types = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(p => appType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

                foreach (var type in types)
                {
                    try
                    {
                        // Use ActivatorUtilities to allow Apps to have DI dependencies (like HttpClient)
                        var appInstance = (IFlowApp)ActivatorUtilities.CreateInstance(_serviceProvider, type);

                        if (_apps.ContainsKey(appInstance.AppKey))
                        {
                            _logger.LogWarning("Duplicate Flow App Key detected: {AppKey}. Skipping {Type}.", appInstance.AppKey, type.Name);
                            continue;
                        }

                        _apps.Add(appInstance.AppKey, appInstance);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to instantiate Flow App: {Type}", type.Name);
                    }
                }

                _logger.LogInformation("Initialized {Count} Flow Apps.", _apps.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during FlowApp discovery.");
            }
        }

        /// <summary>
        /// Returns App Definitions merged with Database Permissions.
        /// Used by the Script Builder UI and Admin Dashboard.
        /// </summary>
        public async Task<List<FlowAppDefWithPermissionModel>> GetAllAppDefinitionsWithPermissions()
        {
            // 1. Get Permissions (Cached)
            var permissions = await GetCachedPermissionsAsync();

            var result = new List<FlowAppDefWithPermissionModel>();

            foreach (var app in _apps.Values)
            {
                var perm = permissions.FirstOrDefault(p => p.AppKey == app.AppKey);

                var model = new FlowAppDefWithPermissionModel
                {
                    AppKey = app.AppKey,
                    Name = app.Name,
                    IconUrl = app.IconUrl,
                    IntegrationType = app.IntegrationType,
                    IsDisabled = perm?.DisabledAt != null,
                    DisabledReason = perm?.DisabledPublicReason
                };

                // Map Actions with Permissions
                foreach (var action in app.Actions)
                {
                    var actionPerm = perm?.ActionPermissions.GetValueOrDefault(action.ActionKey);
                    model.Actions.Add(new FlowActionDefWithPermissionModel
                    {
                        ActionKey = action.ActionKey,
                        Name = action.Name,
                        Description = action.Description,
                        RequiresIntegration = action.RequiresIntegration,
                        InputSchemaJson = action.GetInputSchemaJson(),
                        OutputPorts = action.GetOutputPorts().ToList(),
                        IsDisabled = actionPerm?.DisabledAt != null,
                        DisabledReason = actionPerm?.DisabledPublicReason
                    });
                }

                // Map Fetchers with Permissions
                foreach (var fetcher in app.DataFetchers)
                {
                    var fetcherPerm = perm?.FetcherPermissions.GetValueOrDefault(fetcher.FetcherKey);
                    model.Fetchers.Add(new FlowFetcherDefWithPermissionModel
                    {
                        FetcherKey = fetcher.FetcherKey,
                        RequiresIntegration = fetcher.RequiresIntegration,
                        IsDisabled = fetcherPerm?.DisabledAt != null,
                        DisabledReason = fetcherPerm?.DisabledPublicReason
                    });
                }

                result.Add(model);
            }

            return result;
        }

        /// <summary>
        /// Lightweight lookup for script validation. Returns the raw App interface.
        /// </summary>
        public IFlowApp? GetApp(string appKey)
        {
            _apps.TryGetValue(appKey, out var app);
            return app;
        }

        /// <summary>
        /// Retrieves a single App Definition merged with its Database Permissions.
        /// Used for validation during script saving.
        /// </summary>
        public async Task<FlowAppDefWithPermissionModel?> GetAppDefinitionWithPermissionsAsync(string appKey)
        {
            // 1. Look up Code Definition
            if (!_apps.TryGetValue(appKey, out var app))
            {
                return null;
            }

            // 2. Look up Permissions (Cached)
            // We reuse the existing cache method to avoid hitting DB per node
            var permissionsList = await GetCachedPermissionsAsync();
            var perm = permissionsList.FirstOrDefault(p => p.AppKey == appKey);

            // 3. Map to Model
            var model = new FlowAppDefWithPermissionModel
            {
                AppKey = app.AppKey,
                Name = app.Name,
                IconUrl = app.IconUrl,
                IntegrationType = app.IntegrationType,
                IsDisabled = perm?.DisabledAt != null,
                DisabledReason = perm?.DisabledPublicReason
            };

            foreach (var action in app.Actions)
            {
                var actionPerm = perm?.ActionPermissions.GetValueOrDefault(action.ActionKey);

                model.Actions.Add(new FlowActionDefWithPermissionModel
                {
                    ActionKey = action.ActionKey,
                    Name = action.Name,
                    Description = action.Description,
                    RequiresIntegration = action.RequiresIntegration,
                    // We don't necessarily need the full schema string for validation check, 
                    // but it doesn't hurt to include it for completeness.
                    InputSchemaJson = action.GetInputSchemaJson(),
                    OutputPorts = action.GetOutputPorts().ToList(),
                    IsDisabled = actionPerm?.DisabledAt != null,
                    DisabledReason = actionPerm?.DisabledPublicReason
                });
            }

            foreach (var fetcher in app.DataFetchers)
            {
                var fetcherPerm = perm?.FetcherPermissions.GetValueOrDefault(fetcher.FetcherKey);

                model.Fetchers.Add(new FlowFetcherDefWithPermissionModel
                {
                    FetcherKey = fetcher.FetcherKey,
                    RequiresIntegration = fetcher.RequiresIntegration,
                    IsDisabled = fetcherPerm?.DisabledAt != null,
                    DisabledReason = fetcherPerm?.DisabledPublicReason
                });
            }

            return model;
        }

        /// <summary>
        /// The main execution pipeline called by the Script Engine.
        /// 1. Resolve Templates -> 2. Validate Schema -> 3. Execute Action.
        /// </summary>
        public async Task<ActionExecutionResult> ExecuteActionAsync(
            string appKey,
            string actionKey,
            Dictionary<string, object?> rawInputs,
            Dictionary<string, object?> sessionContext,
            BusinessAppIntegration? integration
        ) {
            // 1. Locate App and Action
            if (!_apps.TryGetValue(appKey, out var app))
            {
                return ActionExecutionResult.Failure("APP_NOT_FOUND", $"App with key '{appKey}' not found.");
            }

            var action = app.Actions.FirstOrDefault(a => a.ActionKey == actionKey);
            if (action == null)
            {
                return ActionExecutionResult.Failure("ACTION_NOT_FOUND", $"Action '{actionKey}' not found in app '{appKey}'.");
            }

            // Permission Check
            var permissions = await GetCachedPermissionsAsync();
            var appPerm = permissions.FirstOrDefault(p => p.AppKey == appKey);
            if (appPerm?.DisabledAt != null)
            {
                return ActionExecutionResult.Failure("APP_DISABLED", appPerm.DisabledPublicReason ?? "Integration temporarily disabled.");
            }
            if (appPerm != null && appPerm.ActionPermissions.TryGetValue(actionKey, out var actPerm) && actPerm.DisabledAt != null)
            {
                return ActionExecutionResult.Failure("ACTION_DISABLED", actPerm.DisabledPublicReason ?? "Action temporarily disabled.");
            }

            // Auth Check
            if (action.RequiresIntegration && integration == null)
            {
                return ActionExecutionResult.Failure("AUTH_REQUIRED", "This action requires a valid integration.");
            }

            try
            {
                // 2. Resolve Scriban Templates in Inputs
                // The user might have entered "{{ customer.name }}" in the "Title" field.
                var renderResult = await _scribanService.RenderDictionaryAsync(rawInputs, sessionContext);

                if (!renderResult.Success)
                {
                    return ActionExecutionResult.Failure("TEMPLATE_ERROR", $"Failed to render inputs: {renderResult.Message}");
                }

                var resolvedDictionary = renderResult.Data;

                // 3. Convert to JsonElement for Validation/Execution
                // System.Text.Json handles the conversion from Dictionary -> JsonElement nicely
                var jsonElement = JsonSerializer.SerializeToElement(resolvedDictionary);

                // 4. Validate against Schema
                var schemaJson = action.GetInputSchemaJson();
                var validationResult = await _schemaValidator.ValidateAsync(
                    jsonElement,
                    schemaJson,
                    $"{appKey}_{actionKey}" // Cache Key
                );

                if (!validationResult.Success)
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", validationResult.Message);
                }

                // 5. Execute Action Logic
                // We pass the resolved, validated JSON to the logic class
                return await action.ExecuteAsync(jsonElement, GetDescryptedIntegration(integration));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {AppKey}:{ActionKey}", appKey, actionKey);
                return ActionExecutionResult.Failure("EXECUTION_EXCEPTION", ex.Message);
            }
        }

        /// <summary>
        /// Orchestrates dynamic data fetching for UI dropdowns
        /// </summary>
        public async Task<FunctionReturnResult<List<DynamicOption>>> FetchOptionsAsync(
            string appKey,
            string fetcherKey,
            JsonElement context, // The current state of the frontend form
            BusinessAppIntegration? integration)
        {
            var result = new FunctionReturnResult<List<DynamicOption>>();

            if (!_apps.TryGetValue(appKey, out var app))
            {
                return result.SetFailureResult("APP_NOT_FOUND", "App not found.");
            }

            var fetcher = app.DataFetchers.FirstOrDefault(f => f.FetcherKey == fetcherKey);
            if (fetcher == null)
            {
                return result.SetFailureResult("FETCHER_NOT_FOUND", $"Fetcher '{fetcherKey}' not found.");
            }

            // Permission Check
            var permissions = await GetCachedPermissionsAsync();
            var appPerm = permissions.FirstOrDefault(p => p.AppKey == appKey);
            if (appPerm?.DisabledAt != null)
            {
                return result.SetFailureResult("APP_DISABLED", "Integration disabled.");
            }
            if (appPerm != null && appPerm.FetcherPermissions.TryGetValue(fetcherKey, out var fetchPerm) && fetchPerm.DisabledAt != null)
            {
                return result.SetFailureResult("FETCHER_DISABLED", "Data source disabled.");
            }

            // Auth Check for Fetcher
            if (fetcher.RequiresIntegration && integration == null)
            {
                return result.SetFailureResult("AUTH_REQUIRED", "This fetcher requires a valid integration.");
            }

            try
            {
                // Execute Fetcher
                var options = await fetcher.FetchOptionsAsync(GetDescryptedIntegration(integration), context);
                return result.SetSuccessResult(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching options for {AppKey}:{FetcherKey}", appKey, fetcherKey);
                return result.SetFailureResult("FETCHER_EXCEPTION", ex.Message);
            }
        }

        /*
         * 
         * HELPERS
         * 
        **/

        public async Task<bool> UpdatePermissionAsync(string appKey, string? itemKey, string type, bool disabled, string? privateReason, string? publicReason)
        {
            bool success = false;

            if (type == "App")
                success = await _repository.UpdateAppPermissionAsync(appKey, disabled, privateReason, publicReason);
            else if (type == "Action" && itemKey != null)
                success = await _repository.UpdateActionPermissionAsync(appKey, itemKey, disabled, privateReason, publicReason);
            else if (type == "Fetcher" && itemKey != null)
                success = await _repository.UpdateFetcherPermissionAsync(appKey, itemKey, disabled, privateReason, publicReason);

            if (success)
            {
                _cache.Remove(CacheKeyPermissions); // Force refresh on next read
            }
            return success;
        }

        private BusinessAppIntegrationDecryptedModel? GetDescryptedIntegration(BusinessAppIntegration? integration)
        {
            if (integration == null) return null;

            BusinessAppIntegrationDecryptedModel decryptedIntegration = new BusinessAppIntegrationDecryptedModel()
            {
                Id = integration.Id,
                Type = integration.Type,
                FriendlyName = integration.FriendlyName,
                Fields = integration.Fields,
                DecryptedFields = new Dictionary<string, string>()
            };

            foreach (var encryptedField in integration.EncryptedFields)
            {
                var decryptedValue = _integrationEncryptionService.Decrypt(encryptedField.Value);
                decryptedIntegration.DecryptedFields.Add(encryptedField.Key, decryptedValue);
            }

            return decryptedIntegration;
        }

        /// <summary>
        /// Converts a flat list of inputs with dot-notation keys into a nested dictionary.
        /// Example: "attendee.name": "Ali" -> { "attendee": { "name": "Ali" } }
        /// </summary>
        public static Dictionary<string, object?> ExpandInputs(List<FlowAppNodeInput> inputs)
        {
            var root = new Dictionary<string, object?>();

            foreach (var input in inputs)
            {
                if (string.IsNullOrWhiteSpace(input.Key)) continue;

                var keys = input.Key.Split('.');
                var currentDict = root;

                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    bool isLast = i == keys.Length - 1;

                    if (isLast)
                    {
                        // Assign value (Static value or AI Generated marker/template)
                        // At runtime, if IsAiGenerated is true, this value might be overwritten 
                        // or used as a hint/default.
                        currentDict[key] = input.Value;
                    }
                    else
                    {
                        // Traverse deeper
                        if (!currentDict.ContainsKey(key) || currentDict[key] is not Dictionary<string, object?>)
                        {
                            currentDict[key] = new Dictionary<string, object?>();
                        }
                        currentDict = (Dictionary<string, object?>)currentDict[key]!;
                    }
                }
            }

            return root;
        }
        private async Task<List<FlowAppData>> GetCachedPermissionsAsync()
        {
            return await _cache.GetOrCreateAsync(CacheKeyPermissions, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // Refresh DB state every 10 mins
                return await _repository.GetAllAppDataAsync();
            }) ?? new List<FlowAppData>();
        }
    }
}