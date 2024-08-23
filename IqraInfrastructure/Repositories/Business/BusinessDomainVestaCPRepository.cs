using IqraCore.Entities.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessDomainVestaCPRepository
    {  
        private string _hostname;
        private string _adminUsername;
        private string _businessesUsername;
        private string _password;

        private string _domainDefaultIP;
        private string _businessDomain;

        private HttpClient _httpClient;

        public BusinessDomainVestaCPRepository(string hostname, string adminUsername, string businessesUsername, string password, string domainDefaultIP, string businessDomain)
        {  
            _hostname = hostname;
            _adminUsername = adminUsername;
            _businessesUsername = businessesUsername;
            _password = password;

            _domainDefaultIP = domainDefaultIP;
            _businessDomain = businessDomain;

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            _httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(hostname),
                Timeout = TimeSpan.FromSeconds(30),
            };

            ValidateVestaCPAccounts().GetAwaiter().GetResult();
        }

        private async Task ValidateVestaCPAccounts()
        {
            // Admin Check
            FunctionReturnResult<string?> adminUserStringData = await GetUserDetails(_adminUsername);
            if (!adminUserStringData.Success || string.IsNullOrEmpty(adminUserStringData.Data))
            {
                throw new Exception("Unable to retrieve VestaCP Admin User data for API Initalization test.\nDid you forget to allow your IP for API use?");
            }
            JsonDocument adminUserJsonData = JsonSerializer.Deserialize<JsonDocument>(adminUserStringData.Data);
            if (!adminUserJsonData.RootElement.TryGetProperty("username", out _))
            {
                throw new Exception($"{_adminUsername} not found in VestaCP users");
            }

            // Businesses Check
            FunctionReturnResult<string?> businessesUserStringData = await GetUserDetails(_businessesUsername);
            if (!businessesUserStringData.Success || string.IsNullOrEmpty(businessesUserStringData.Data))
            {
                throw new Exception("Unable to retrieve VestaCP Businesses User data for API Initalization test.\nDid you forget to allow your IP for API use?");
            }
            JsonDocument businessesUserJsonData = JsonSerializer.Deserialize<JsonDocument>(businessesUserStringData.Data);
            if (!businessesUserJsonData.RootElement.TryGetProperty("username", out _))
            {
                throw new Exception($"{_businessesUsername} not found in VestaCP users");
            }
        }

        /** 
         * 
         * Publicize Variables
         * 
        **/

        public string GetBusinessDomain()
        {
            return _businessDomain;
        }

        /** 
         * 
         * Server Actions
         * 
        **/

        private async Task<FunctionReturnResult<string?>> SendRequest(string command, params (string key, string value)[] args)
        {
            var result = new FunctionReturnResult<string>();

            var formData = new Dictionary<string, string>
            {
                { "user", _adminUsername },
                { "password", _password },
                { "cmd", command },
            };

            foreach (var (key, value) in args)
            {
                formData.Add(key, value);
            }

            try
            {
                var response = await _httpClient.PostAsync(_hostname, new FormUrlEncodedContent(formData));

                if (!response.IsSuccessStatusCode)
                {
                    result.Success = false;
                    result.Code = "SendRequest:1";
                    result.Message = "Failed with status code " + response.StatusCode.ToString();
                    return result;
                }

                var stringResult = await response.Content.ReadAsStringAsync();

                result.Success = true;
                result.Data = stringResult;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Code = "SendRequest:2";
                result.Message = "Exception occured " + ex.Message;
            }

            return result;
        }

        // General
        public async Task<FunctionReturnResult<string?>> GetUserDetails(string username)
        {
            return await SendRequest("v-list-user",
                ("arg1", username),
                ("arg2", "json")
            );
        }

        // Custom Business Domain
        public async Task<FunctionReturnResult<string?>> GetCustomBusinessDomainDetails(string domain)
        {
            return await SendRequest("v-list-web-domain",
                ("arg1", _businessesUsername), ("arg2", domain),
                ("arg3", "json")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddCustomBusinessDomain(string domain, bool restart, string aliases, string proxyExtensions)
        {
            return await SendRequest("v-add-web-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", _domainDefaultIP),
                ("arg4", restart ? "yes" : "no"),
                ("arg5", aliases),
                ("arg6", proxyExtensions)
            );
        }

        public async Task<FunctionReturnResult<string?>> AddCustomBusinessDomainSSL(string domain, string sslDir, string sslHome)
        {
            return await SendRequest("v-add-web-domain-ssl",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", sslDir),
                ("arg4", sslHome)
            );
        }

        public async Task<FunctionReturnResult<string?>> AddCustomBusinessDomainLetsEncryptSSL(string domain)
        {
            return await SendRequest("v-add-letsencrypt-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", "")
            );
        }

        public async Task<FunctionReturnResult<string?>> ChangeCustomBusinessDomainProxy(string domain, string proxyTemplate, bool restart)
        {
            return await SendRequest("v-change-web-domain-proxy-tpl",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", proxyTemplate),
                ("arg4", ""),
                ("arg5", restart == true ? "yes" : "no")
            );
        }
        
        public async Task<FunctionReturnResult<string?>> DeleteCustomBusinessDomain(string domain)
        {
            return await SendRequest("v-delete-web-domain",
                ("arg1", _businessesUsername),
                ("arg2", domain)
            );
        }

        // Iqra Business Subdomain
        public async Task<FunctionReturnResult<string?>> GetIqraBusinessSubDomainDetails(string subdomain)
        {
            return await SendRequest("v-list-web-domain",
                ("arg1", _businessesUsername), ("arg2", (subdomain + "." + _businessDomain)),
                ("arg3", "json")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddIqraBusinessSubDomain(string subdomain, bool restart)
        {
            return await SendRequest("v-add-web-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain)),
                ("arg3", _domainDefaultIP),
                ("arg4", restart ? "yes" : "no"),
                ("arg5", ""),
                ("arg6", "")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddIqraBusinessSubDomainDNSRecord(string subdomain, bool restart)
        {
            return await SendRequest("v-add-dns-record",
                ("returncode", "OK"),
                ("arg1", _adminUsername),
                ("arg2", _businessDomain),
                ("arg3", subdomain),
                ("arg4", "A"),
                ("arg5", _domainDefaultIP),
                ("arg6", ""),
                ("arg7", ""),
                ("arg8", restart ? "yes" : "no")
            );
        }

        public async Task<FunctionReturnResult<string?>> ChangeIqraBusinessSubDomainProxy(string subdomain, string proxyTemplate, bool restart)
        {
            return await SendRequest("v-change-web-domain-proxy-tpl",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain)),
                ("arg3", proxyTemplate),
                ("arg4", ""),
                ("arg5", restart == true ? "yes" : "no")
            );
        }

        public async Task<FunctionReturnResult<string?>> DeleteIqraBusinessSubDomain(string subdomain)
        {
            return await SendRequest("v-delete-web-domain",
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain))
            );
        }
    }
}
