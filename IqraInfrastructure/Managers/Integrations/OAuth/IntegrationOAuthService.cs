using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.Integrations.OAuth
{
    public class IntegrationOAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AES256EncryptionService _integrationEncryptionService;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RedisConnectionFactory _lockRedisFactory;
        private readonly ILogger<IntegrationOAuthService> _logger;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

        private static readonly TimeSpan TokenRequestTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RefreshLockExpiry = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan RefreshLockWaitTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan AccessTokenExpirySkew = TimeSpan.FromSeconds(120);

        private static readonly Regex UrlTemplateFieldRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        private static readonly HashSet<string> RejectedGrantErrors = new()
        {
            "invalid_grant",
            "invalid_client",
            "unauthorized_client"
        };

        public IntegrationOAuthService(
            IHttpClientFactory httpClientFactory,
            AES256EncryptionService integrationEncryptionService,
            BusinessAppRepository businessAppRepository,
            IntegrationsManager integrationsManager,
            RedisConnectionFactory lockRedisFactory,
            ILogger<IntegrationOAuthService> logger
        ) {
            _httpClientFactory = httpClientFactory;
            _integrationEncryptionService = integrationEncryptionService;
            _businessAppRepository = businessAppRepository;
            _integrationsManager = integrationsManager;
            _lockRedisFactory = lockRedisFactory;
            _logger = logger;
        }

        public class TokenResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ProviderError { get; set; }
            public bool IsRejected { get; set; }
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public int ExpiresInSeconds { get; set; } = 3600;

            public Dictionary<string, string> ExtraFields { get; set; } = new();
        }

        public async Task<IntegrationOAuthData?> GetOAuthConfigAsync(string integrationType)
        {
            var integrationTypeResult = await _integrationsManager.getIntegrationData(integrationType);
            return integrationTypeResult.Success ? integrationTypeResult.Data!.OAuth : null;
        }

        public static string BuildAuthorizationUrl(IntegrationOAuthData config, BusinessAppIntegration integration, string clientId, string redirectUri, string scopes, string state)
        {
            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = scopes,
                ["state"] = state
            };

            foreach (var extra in config.AdditionalAuthorizationParameters)
            {
                query[extra.Key] = extra.Value;
            }

            var authorizationUrl = ResolveUrl(config.AuthorizationUrl, integration);
            var encoded = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            var separator = authorizationUrl.Contains('?') ? "&" : "?";
            return $"{authorizationUrl}{separator}{encoded}";
        }

        public Task<TokenResult> ExchangeCodeAsync(IntegrationOAuthData config, BusinessAppIntegration integration, string clientId, string clientSecret, string code, string redirectUri)
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            };
            return PostTokenRequestAsync(config, integration, clientId, clientSecret, form);
        }

        public async Task<bool> SaveConnectionAsync(long businessId, BusinessAppIntegration integration, TokenResult tokenResult)
        {
            var oauthData = new BusinessAppIntegrationOAuthData
            {
                EncryptedRefreshToken = _integrationEncryptionService.Encrypt(tokenResult.RefreshToken!),
                EncryptedAccessToken = _integrationEncryptionService.Encrypt(tokenResult.AccessToken!),
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresInSeconds),
                ExtraFields = new Dictionary<string, string>(tokenResult.ExtraFields),
                Status = BusinessAppIntegrationOAuthData.StatusConnected,
                StatusMessage = null,
                StatusCheckedAt = DateTime.UtcNow
            };

            var saved = await _businessAppRepository.UpdateBusinessIntegrationOAuth(businessId, integration.Id, oauthData);
            if (saved)
            {
                integration.OAuth = oauthData;
            }
            return saved;
        }

        public async Task<bool> MarkConnectionFailedAsync(long businessId, BusinessAppIntegration integration, string message)
        {
            var currentOAuth = integration.OAuth;
            var oauthData = new BusinessAppIntegrationOAuthData
            {
                EncryptedRefreshToken = currentOAuth?.EncryptedRefreshToken,
                EncryptedAccessToken = currentOAuth?.EncryptedAccessToken,
                AccessTokenExpiresAt = currentOAuth?.AccessTokenExpiresAt,
                ExtraFields = currentOAuth?.ExtraFields ?? new Dictionary<string, string>(),
                Status = BusinessAppIntegrationOAuthData.StatusFailed,
                StatusMessage = message,
                StatusCheckedAt = DateTime.UtcNow
            };

            var saved = await _businessAppRepository.UpdateBusinessIntegrationOAuthIfRefreshTokenUnchanged(businessId, integration.Id, oauthData, currentOAuth?.EncryptedRefreshToken);
            if (saved)
            {
                integration.OAuth = oauthData;
            }
            return saved;
        }

        public async Task<FunctionReturnResult> EnsureFreshTokenAsync(long businessId, BusinessAppIntegration integration, bool forceRefresh = false)
        {
            var result = new FunctionReturnResult();

            try
            {
                var oauthConfig = await GetOAuthConfigAsync(integration.Type);
                if (oauthConfig == null)
                {
                    return result.SetSuccessResult();
                }

                if (!forceRefresh && HasValidAccessToken(integration))
                {
                    return result.SetSuccessResult();
                }

                var lockKey = $"integration:oauth:refresh:{businessId}:{integration.Id}";
                var localLock = _refreshLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

                await localLock.WaitAsync();
                try
                {
                    var storedIntegration = await _businessAppRepository.getBusinessIntegrationById(businessId, integration.Id);
                    if (storedIntegration == null)
                    {
                        return result.SetFailureResult("OAUTH_INTEGRATION_NOT_FOUND", "The integration no longer exists.");
                    }

                    if (!forceRefresh && HasValidAccessToken(storedIntegration))
                    {
                        integration.OAuth = storedIntegration.OAuth;
                        return result.SetSuccessResult();
                    }

                    var lockDatabase = _lockRedisFactory.GetDatabase();
                    var lockToken = ObjectId.GenerateNewId().ToString();
                    if (!await TakeRefreshLockAsync(lockDatabase, lockKey, lockToken))
                    {
                        return result.SetFailureResult("OAUTH_REFRESH_BUSY", "Another refresh of this connection did not finish in time. Try again.");
                    }

                    try
                    {
                        storedIntegration = await _businessAppRepository.getBusinessIntegrationById(businessId, integration.Id);
                        if (storedIntegration == null)
                        {
                            return result.SetFailureResult("OAUTH_INTEGRATION_NOT_FOUND", "The integration no longer exists.");
                        }

                        if (!forceRefresh && HasValidAccessToken(storedIntegration))
                        {
                            integration.OAuth = storedIntegration.OAuth;
                            return result.SetSuccessResult();
                        }

                        return await RefreshStoredTokenAsync(oauthConfig, storedIntegration, integration, businessId);
                    }
                    finally
                    {
                        await lockDatabase.LockReleaseAsync(lockKey, lockToken);
                    }
                }
                finally
                {
                    localLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error ensuring fresh OAuth token for integration {IntegrationId}", integration.Id);
                return result.SetFailureResult("OAUTH_REFRESH_EXCEPTION", ex.Message);
            }
        }

        public string? DecryptAccessToken(BusinessAppIntegration integration)
        {
            return string.IsNullOrEmpty(integration.OAuth?.EncryptedAccessToken) ? null : _integrationEncryptionService.Decrypt(integration.OAuth.EncryptedAccessToken);
        }

        private async Task<FunctionReturnResult> RefreshStoredTokenAsync(IntegrationOAuthData config, BusinessAppIntegration storedIntegration, BusinessAppIntegration integration, long businessId)
        {
            var result = new FunctionReturnResult();

            var storedOAuth = storedIntegration.OAuth;
            var clientId = DecryptFieldIfPresent(storedIntegration, IntegrationOAuthData.ClientIdField);
            var clientSecret = DecryptFieldIfPresent(storedIntegration, IntegrationOAuthData.ClientSecretField);

            if (string.IsNullOrEmpty(storedOAuth?.EncryptedRefreshToken) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return result.SetFailureResult(
                    "OAUTH_NOT_CONNECTED",
                    "This integration has not completed the OAuth connection (missing client credentials or refresh token). Use the Connect button on the integration."
                );
            }

            var refreshToken = _integrationEncryptionService.Decrypt(storedOAuth.EncryptedRefreshToken);
            var tokenResult = await PostTokenRequestAsync(config, storedIntegration, clientId, clientSecret, new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                if (tokenResult.IsRejected && await MarkConnectionFailedAsync(businessId, storedIntegration, tokenResult.ProviderError ?? "The provider rejected the connection."))
                {
                    integration.OAuth = storedIntegration.OAuth;
                }

                return result.SetFailureResult("OAUTH_REFRESH_FAILED", $"Failed to refresh access token: {tokenResult.ErrorMessage}");
            }

            var extraFields = new Dictionary<string, string>(storedOAuth.ExtraFields);
            foreach (var extra in tokenResult.ExtraFields)
            {
                extraFields[extra.Key] = extra.Value;
            }

            var refreshedOAuth = new BusinessAppIntegrationOAuthData
            {
                EncryptedRefreshToken = string.IsNullOrEmpty(tokenResult.RefreshToken) ? storedOAuth.EncryptedRefreshToken : _integrationEncryptionService.Encrypt(tokenResult.RefreshToken),
                EncryptedAccessToken = _integrationEncryptionService.Encrypt(tokenResult.AccessToken),
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresInSeconds),
                ExtraFields = extraFields,
                Status = BusinessAppIntegrationOAuthData.StatusConnected,
                StatusMessage = null,
                StatusCheckedAt = DateTime.UtcNow
            };

            var saved = await _businessAppRepository.UpdateBusinessIntegrationOAuthIfRefreshTokenUnchanged(businessId, integration.Id, refreshedOAuth, storedOAuth.EncryptedRefreshToken);
            if (!saved)
            {
                var latestIntegration = await _businessAppRepository.getBusinessIntegrationById(businessId, integration.Id);
                if (latestIntegration != null && HasValidAccessToken(latestIntegration))
                {
                    integration.OAuth = latestIntegration.OAuth;
                    return result.SetSuccessResult();
                }

                return result.SetFailureResult("OAUTH_REFRESH_CONFLICT", "The connection changed while it was being refreshed. Try again.");
            }

            integration.OAuth = refreshedOAuth;
            return result.SetSuccessResult();
        }

        private async Task<bool> TakeRefreshLockAsync(IDatabase lockDatabase, string lockKey, string lockToken)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < RefreshLockWaitTimeout)
            {
                if (await lockDatabase.LockTakeAsync(lockKey, lockToken, RefreshLockExpiry))
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static bool HasValidAccessToken(BusinessAppIntegration integration)
        {
            return !string.IsNullOrEmpty(integration.OAuth?.EncryptedAccessToken) &&
                integration.OAuth.AccessTokenExpiresAt.HasValue &&
                integration.OAuth.AccessTokenExpiresAt.Value > DateTime.UtcNow.Add(AccessTokenExpirySkew);
        }

        private string? DecryptFieldIfPresent(BusinessAppIntegration integration, string fieldKey)
        {
            return integration.EncryptedFields.TryGetValue(fieldKey, out var encryptedValue) ? _integrationEncryptionService.Decrypt(encryptedValue) : null;
        }

        private static string ResolveUrl(string urlTemplate, BusinessAppIntegration integration)
        {
            return UrlTemplateFieldRegex.Replace(urlTemplate, match =>
                integration.Fields.TryGetValue(match.Groups[1].Value, out var value) ? Uri.EscapeDataString(value) : match.Value);
        }

        private async Task<TokenResult> PostTokenRequestAsync(IntegrationOAuthData config, BusinessAppIntegration integration, string clientId, string clientSecret, Dictionary<string, string> form)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, ResolveUrl(config.TokenUrl, integration));

                if (config.UseBasicAuthForTokenEndpoint)
                {
                    var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                }
                else
                {
                    form["client_id"] = clientId;
                    form["client_secret"] = clientSecret;
                }

                request.Content = new FormUrlEncodedContent(form);

                using var timeout = new CancellationTokenSource(TokenRequestTimeout);
                var response = await client.SendAsync(request, timeout.Token);
                var content = await response.Content.ReadAsStringAsync(timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OAuth token request for {IntegrationType} failed: {Status} - {Body}", integration.Type, response.StatusCode, content);

                    var failure = new TokenResult { Success = false, ErrorMessage = $"{response.StatusCode}: {content}", ProviderError = $"HTTP {(int)response.StatusCode}" };
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(content);
                        if (errorDoc.RootElement.ValueKind == JsonValueKind.Object && errorDoc.RootElement.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                        {
                            var errorCode = errorElement.GetString() ?? string.Empty;
                            var errorDescription = errorDoc.RootElement.TryGetProperty("error_description", out var descriptionElement) && descriptionElement.ValueKind == JsonValueKind.String
                                ? descriptionElement.GetString()
                                : null;

                            failure.ProviderError = string.IsNullOrEmpty(errorDescription) ? errorCode : $"{errorCode}: {errorDescription}";
                            failure.IsRejected =
                                (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized) &&
                                RejectedGrantErrors.Contains(errorCode);
                        }
                    }
                    catch (JsonException)
                    {
                    }

                    return failure;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var result = new TokenResult { Success = true };

                if (root.TryGetProperty("access_token", out var at))
                {
                    result.AccessToken = at.GetString();
                }

                if (root.TryGetProperty("refresh_token", out var rt))
                {
                    result.RefreshToken = rt.GetString();
                }

                if (root.TryGetProperty("expires_in", out var ei))
                {
                    if (ei.ValueKind == JsonValueKind.Number && ei.TryGetInt32(out var seconds))
                    {
                        result.ExpiresInSeconds = seconds;
                    }
                    else if (ei.ValueKind == JsonValueKind.String && int.TryParse(ei.GetString(), out var secondsStr))
                    {
                        result.ExpiresInSeconds = secondsStr;
                    }
                }

                foreach (var extraKey in config.ExtraResponseFields)
                {
                    if (root.TryGetProperty(extraKey, out var extraVal) && extraVal.ValueKind == JsonValueKind.String)
                    {
                        var v = extraVal.GetString();
                        if (!string.IsNullOrEmpty(v))
                        {
                            result.ExtraFields[extraKey] = v;
                        }
                    }
                }

                if (string.IsNullOrEmpty(result.AccessToken))
                {
                    return new TokenResult { Success = false, ErrorMessage = "Token endpoint returned no access_token." };
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during OAuth token request for {IntegrationType}", integration.Type);
                return new TokenResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
