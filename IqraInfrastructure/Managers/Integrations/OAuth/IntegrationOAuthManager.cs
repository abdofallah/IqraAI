using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Models.Integrations;
using IqraCore.Models.User.Business.App.Integration;
using IqraInfrastructure.Helpers.Integrations;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Integrations.OAuth
{
    public class IntegrationOAuthManager
    {
        private readonly ILogger<IntegrationOAuthManager> _logger;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly IntegrationsManager _integrationsManager;
        private readonly IntegrationOAuthService _oauthService;

        private const string CallbackPath = "/app/integrations/oauth/callback";
        private const long StateMaxAgeSeconds = 900;

        public IntegrationOAuthManager(
            ILogger<IntegrationOAuthManager> logger,
            BusinessAppRepository businessAppRepository,
            IntegrationsManager integrationsManager,
            IntegrationOAuthService oauthService
        ) {
            _logger = logger;
            _businessAppRepository = businessAppRepository;
            _integrationsManager = integrationsManager;
            _oauthService = oauthService;
        }

        public async Task<FunctionReturnResult<string?>> BeginConnect(long businessId, string integrationId, string origin)
        {
            var result = new FunctionReturnResult<string?>();

            try
            {
                var integration = await _businessAppRepository.getBusinessIntegrationById(businessId, integrationId);
                if (integration == null)
                {
                    return result.SetFailureResult(
                        "BeginConnect:INTEGRATION_NOT_FOUND",
                        "Integration not found."
                    );
                }

                var oauthConfig = await _oauthService.GetOAuthConfigAsync(integration.Type);
                if (oauthConfig == null)
                {
                    return result.SetFailureResult(
                        "BeginConnect:NOT_OAUTH",
                        "This integration does not connect with OAuth."
                    );
                }

                if (!integration.EncryptedFields.TryGetValue(IntegrationOAuthData.ClientIdField, out var encryptedClientId))
                {
                    return result.SetFailureResult(
                        "BeginConnect:MISSING_CLIENT_ID",
                        "The integration has no saved client id."
                    );
                }
                var clientId = _integrationsManager.DecryptField(encryptedClientId);

                string scopes = oauthConfig.Scopes;
                if (integration.Fields.TryGetValue(IntegrationOAuthData.ScopesField, out var scopesOverride) && !string.IsNullOrWhiteSpace(scopesOverride))
                {
                    scopes = scopesOverride;
                }

                var state = BuildState(businessId, integrationId, integration.Type);
                var authorizationUrl = IntegrationOAuthService.BuildAuthorizationUrl(oauthConfig, integration, clientId, BuildRedirectUri(origin), scopes, state);

                return result.SetSuccessResult(authorizationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin the OAuth connection for integration {IntegrationId}", integrationId);
                return result.SetFailureResult(
                    "BeginConnect:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        public FunctionReturnResult<IntegrationOAuthStateModel?> ReadState(string? state)
        {
            var result = new FunctionReturnResult<IntegrationOAuthStateModel?>();

            if (string.IsNullOrEmpty(state))
            {
                return result.SetFailureResult(
                    "ReadState:MISSING_STATE",
                    "The OAuth state is missing."
                );
            }

            try
            {
                var encrypted = Encoding.UTF8.GetString(FromUrlSafeBase64(state));
                var json = _integrationsManager.DecryptField(encrypted);
                var parsed = JsonSerializer.Deserialize<IntegrationOAuthStateModel>(json);
                if (parsed == null)
                {
                    return result.SetFailureResult(
                        "ReadState:INVALID_STATE",
                        "The OAuth state is invalid."
                    );
                }

                var ageSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - parsed.IssuedAtUnix;
                if (ageSeconds < 0 || ageSeconds > StateMaxAgeSeconds)
                {
                    return result.SetFailureResult(
                        "ReadState:EXPIRED_STATE",
                        "The OAuth state has expired."
                    );
                }

                return result.SetSuccessResult(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read the OAuth state.");
                return result.SetFailureResult(
                    "ReadState:INVALID_STATE",
                    "The OAuth state is invalid."
                );
            }
        }

        public async Task<FunctionReturnResult> CompleteConnect(IntegrationOAuthStateModel state, string? code, string? providerError, string? providerErrorDescription, string origin)
        {
            var result = new FunctionReturnResult();

            try
            {
                var integration = await _businessAppRepository.getBusinessIntegrationById(state.BusinessId, state.IntegrationId);
                if (integration == null)
                {
                    return result.SetFailureResult(
                        "CompleteConnect:INTEGRATION_NOT_FOUND",
                        "Integration not found."
                    );
                }

                var oauthConfig = await _oauthService.GetOAuthConfigAsync(integration.Type);
                if (oauthConfig == null)
                {
                    return result.SetFailureResult(
                        "CompleteConnect:NOT_OAUTH",
                        "This integration does not connect with OAuth."
                    );
                }

                if (!string.IsNullOrEmpty(providerError))
                {
                    var providerReason = string.IsNullOrEmpty(providerErrorDescription) ? providerError : $"{providerError}: {providerErrorDescription}";
                    _logger.LogWarning("OAuth provider returned an error for integration {IntegrationId}: {Error}", state.IntegrationId, ProviderReplyHelper.CapForLog(providerReason));
                    return await RecordConnectFailure(state.BusinessId, integration, result.SetFailureResult(
                        "CompleteConnect:PROVIDER_ERROR",
                        providerReason
                    ));
                }

                if (string.IsNullOrEmpty(code))
                {
                    return await RecordConnectFailure(state.BusinessId, integration, result.SetFailureResult(
                        "CompleteConnect:MISSING_CODE",
                        "The provider returned no authorization code."
                    ));
                }

                if (!integration.EncryptedFields.TryGetValue(IntegrationOAuthData.ClientIdField, out var encryptedClientId) ||
                    !integration.EncryptedFields.TryGetValue(IntegrationOAuthData.ClientSecretField, out var encryptedClientSecret))
                {
                    return await RecordConnectFailure(state.BusinessId, integration, result.SetFailureResult(
                        "CompleteConnect:MISSING_CLIENT_CREDENTIALS",
                        "The integration has no saved client id or client secret."
                    ));
                }
                var clientId = _integrationsManager.DecryptField(encryptedClientId);
                var clientSecret = _integrationsManager.DecryptField(encryptedClientSecret);

                var tokenResult = await _oauthService.ExchangeCodeAsync(oauthConfig, integration, clientId, clientSecret, code, BuildRedirectUri(origin));
                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _logger.LogWarning("OAuth code exchange failed for integration {IntegrationId}: {Error}", state.IntegrationId, tokenResult.ErrorMessage);
                    return await RecordConnectFailure(state.BusinessId, integration, result.SetFailureResult(
                        "CompleteConnect:TOKEN_EXCHANGE_FAILED",
                        tokenResult.ProviderError ?? "The provider did not exchange the authorization code."
                    ));
                }

                if (string.IsNullOrEmpty(tokenResult.RefreshToken))
                {
                    _logger.LogWarning("OAuth code exchange for integration {IntegrationId} returned no refresh token.", state.IntegrationId);
                    return await RecordConnectFailure(state.BusinessId, integration, result.SetFailureResult(
                        "CompleteConnect:NO_REFRESH_TOKEN",
                        "The provider returned no refresh token."
                    ));
                }

                var saved = await _oauthService.SaveConnectionAsync(state.BusinessId, integration, tokenResult);
                if (!saved)
                {
                    return result.SetFailureResult(
                        "CompleteConnect:PERSIST_FAILED",
                        "Failed to save the OAuth tokens."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete the OAuth connection for integration {IntegrationId}", state.IntegrationId);
                return result.SetFailureResult(
                    "CompleteConnect:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<BusinessAppIntegrationOAuthStatusModel?>> TestConnection(long businessId, string integrationId)
        {
            var result = new FunctionReturnResult<BusinessAppIntegrationOAuthStatusModel?>();

            try
            {
                var integration = await _businessAppRepository.getBusinessIntegrationById(businessId, integrationId);
                if (integration == null)
                {
                    return result.SetFailureResult(
                        "TestConnection:INTEGRATION_NOT_FOUND",
                        "Integration not found."
                    );
                }

                var oauthConfig = await _oauthService.GetOAuthConfigAsync(integration.Type);
                if (oauthConfig == null)
                {
                    return result.SetFailureResult(
                        "TestConnection:NOT_OAUTH",
                        "This integration does not connect with OAuth."
                    );
                }

                if (string.IsNullOrEmpty(integration.OAuth?.EncryptedRefreshToken))
                {
                    return result.SetFailureResult(
                        "TestConnection:NOT_CONNECTED",
                        "This integration is not connected yet.",
                        new BusinessAppIntegrationOAuthStatusModel(integration)
                    );
                }

                var refreshResult = await _oauthService.EnsureFreshTokenAsync(businessId, integration, forceRefresh: true);
                if (!refreshResult.Success)
                {
                    return result.SetFailureResult(
                        $"TestConnection:{refreshResult.Code}",
                        refreshResult.Message,
                        new BusinessAppIntegrationOAuthStatusModel(integration)
                    );
                }

                return result.SetSuccessResult(new BusinessAppIntegrationOAuthStatusModel(integration));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test the OAuth connection for integration {IntegrationId}", integrationId);
                return result.SetFailureResult(
                    "TestConnection:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<List<BusinessAppIntegrationOAuthStatusModel>?>> GetConnectionStatuses(long businessId)
        {
            var result = new FunctionReturnResult<List<BusinessAppIntegrationOAuthStatusModel>?>();

            try
            {
                var integrationTypesResult = await _integrationsManager.GetIntegrationsList();
                if (!integrationTypesResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetConnectionStatuses:{integrationTypesResult.Code}",
                        integrationTypesResult.Message
                    );
                }

                var oauthIntegrationTypes = integrationTypesResult.Data!
                    .Where(i => i.OAuth != null)
                    .Select(i => i.Id)
                    .ToHashSet();

                var integrations = await _businessAppRepository.GetBusinessIntegrations(businessId);

                return result.SetSuccessResult(
                    integrations
                        .Where(i => oauthIntegrationTypes.Contains(i.Type))
                        .Select(i => new BusinessAppIntegrationOAuthStatusModel(i))
                        .ToList()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read the OAuth connection statuses of business {BusinessId}", businessId);
                return result.SetFailureResult(
                    "GetConnectionStatuses:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        public IntegrationOAuthResultViewModel BuildConnectResult(long? businessId, string? failureReason)
        {
            return new IntegrationOAuthResultViewModel
            {
                RedirectUrl = BuildIntegrationsTabUrl(businessId, failureReason),
                Success = failureReason == null,
                Reason = failureReason ?? string.Empty
            };
        }

        public string BuildRedirectUri(string origin)
        {
            return $"{origin}{CallbackPath}";
        }

        public string BuildIntegrationsTabUrl(long? businessId, string? failureReason)
        {
            var basePath = businessId.HasValue ? $"/business/{businessId.Value}/integrations" : "/";
            if (failureReason == null)
            {
                return $"{basePath}?oauth=success";
            }

            return $"{basePath}?oauth=error&reason={Uri.EscapeDataString(failureReason)}";
        }

        public static bool RequiresReconnectOnEdit(IntegrationData integrationTypeData, BusinessAppIntegration previousIntegration, BusinessAppIntegration newIntegration)
        {
            if (integrationTypeData.OAuth == null || previousIntegration.OAuth == null)
            {
                return false;
            }

            foreach (var field in integrationTypeData.Fields.Where(f => f.RequiresOAuthReconnect))
            {
                var previousValues = field.IsEncrypted ? previousIntegration.EncryptedFields : previousIntegration.Fields;
                var newValues = field.IsEncrypted ? newIntegration.EncryptedFields : newIntegration.Fields;

                previousValues.TryGetValue(field.Id, out var previousValue);
                newValues.TryGetValue(field.Id, out var newValue);

                if ((previousValue ?? string.Empty) != (newValue ?? string.Empty))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<FunctionReturnResult> RecordConnectFailure(long businessId, BusinessAppIntegration integration, FunctionReturnResult failure)
        {
            if (string.IsNullOrEmpty(integration.OAuth?.EncryptedRefreshToken))
            {
                await _oauthService.MarkConnectionFailedAsync(businessId, integration, failure.Message ?? "The connection failed.");
            }

            return failure;
        }


        private string BuildState(long businessId, string integrationId, string integrationType)
        {
            var payload = new IntegrationOAuthStateModel
            {
                BusinessId = businessId,
                IntegrationId = integrationId,
                IntegrationType = integrationType,
                IssuedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            var json = JsonSerializer.Serialize(payload);
            var encrypted = _integrationsManager.EncryptField(json);
            return ToUrlSafeBase64(Encoding.UTF8.GetBytes(encrypted));
        }

        private static string ToUrlSafeBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static byte[] FromUrlSafeBase64(string value)
        {
            var s = value.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
