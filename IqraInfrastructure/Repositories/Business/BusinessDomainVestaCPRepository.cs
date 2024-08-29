using FluentFTP;
using IqraCore.Entities.Helpers;
using System.Text;
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

        private readonly string _proxyTemplateFTPHostname;
        private readonly string _proxyTemplateFTPUsername;
        private readonly string _proxyTemplateFTPPassword;

        private readonly string _defaultHTTPSProxyTemplateName = "iqrabusiness-https";
        private readonly string _maintenanceProxyTemplateFile = "iqrabusiness-maintenance";

        private HttpClient _httpClient;

        public BusinessDomainVestaCPRepository(
            string hostname,
            string adminUsername,
            string businessesUsername,
            string password,
            string domainDefaultIP,
            string businessDomain,
            string proxyTemplateFTPHostname,
            string proxyTemplateFTPUsername,
            string proxyTemplateFTPPassword
        )
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("[BusinessDomainVestaCPRepository] Initializing...");
            Console.ResetColor();

            _hostname = hostname;
            _adminUsername = adminUsername;
            _businessesUsername = businessesUsername;
            _password = password;

            _domainDefaultIP = domainDefaultIP;
            _businessDomain = businessDomain;

            _proxyTemplateFTPHostname = proxyTemplateFTPHostname;
            _proxyTemplateFTPUsername = proxyTemplateFTPUsername;
            _proxyTemplateFTPPassword = proxyTemplateFTPPassword;

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

            // Validate API Works Fine
            ValidateVestaCPAccounts().GetAwaiter().GetResult();
            
            // Upload the latest templates files
            UpdateTemplatesFiles();

            // Rebuild all the web with the latest templates files
            var rebuildAdminResult = RebuildAdminWeb(true).GetAwaiter().GetResult();
            if (!rebuildAdminResult.Success)
            {
                throw new Exception(rebuildAdminResult.Message);
            }
            var rebuildBusinessesResult = RebuildBusinessesWeb(true).GetAwaiter().GetResult();
            if (!rebuildBusinessesResult.Success)
            {
                throw new Exception(rebuildBusinessesResult.Message);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("[BusinessDomainVestaCPRepository] Initialization Complete");
            Console.ResetColor();
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
            if (!adminUserJsonData.RootElement.TryGetProperty(_adminUsername, out _))
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
            if (!businessesUserJsonData.RootElement.TryGetProperty(_businessesUsername, out _))
            {
                throw new Exception($"{_businessesUsername} not found in VestaCP users");
            }
        }

        private void UpdateTemplatesFiles()
        {
            string templatesFolderFTPPath = "vestacpnginxtemplates";
            string templatesFolderLocalPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "VestaCP", "ProxyTemplates");

            using (var ftp = new FtpClient(_proxyTemplateFTPHostname, _proxyTemplateFTPUsername, _proxyTemplateFTPPassword))
            {
                ftp.Connect();

                ftp.UploadBytes(
                    File.ReadAllBytes(Path.Combine(templatesFolderLocalPath, (_defaultHTTPSProxyTemplateName + ".tpl"))),
                    Path.Combine(templatesFolderFTPPath, (_defaultHTTPSProxyTemplateName + ".tpl")),
                    FtpRemoteExists.Overwrite
                );
                ftp.UploadBytes(
                    File.ReadAllBytes(Path.Combine(templatesFolderLocalPath, (_defaultHTTPSProxyTemplateName + ".stpl"))),
                    Path.Combine(templatesFolderFTPPath, (_defaultHTTPSProxyTemplateName + ".stpl")),
                    FtpRemoteExists.Overwrite
                );

                ftp.UploadBytes(
                    File.ReadAllBytes(Path.Combine(templatesFolderLocalPath, (_maintenanceProxyTemplateFile + ".tpl"))),
                    Path.Combine(templatesFolderFTPPath, (_maintenanceProxyTemplateFile + ".tpl")),
                    FtpRemoteExists.Overwrite
                );
                ftp.UploadBytes(
                    File.ReadAllBytes(Path.Combine(templatesFolderLocalPath, (_maintenanceProxyTemplateFile + ".stpl"))),
                    Path.Combine(templatesFolderFTPPath, (_maintenanceProxyTemplateFile + ".stpl")),
                    FtpRemoteExists.Overwrite
                );

                ftp.Disconnect();
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

        public string GetBusinessDomainDefaultIP()
        {
            return _domainDefaultIP;
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
                ("returncode", "OK"),
                ("arg1", username),
                ("arg2", "json")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddWebDomainFTP(string domain, string ftpUser, string ftpPass, string ftpPath)
        {
            return await SendRequest("v-add-web-domain-ftp",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", ftpUser),
                ("arg4", ftpPass),
                ("arg5", ftpPath)
            );
        }

        public async Task<FunctionReturnResult<string?>> DeleteWebDomainFTP(string domain, string ftpUser)
        {
            return await SendRequest("v-add-web-domain-ftp",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", ftpUser)
            );
        }

        public async Task<FunctionReturnResult<string?>> RebuildAdminWeb(bool restart)
        {
            return await SendRequest("v-rebuild-web-domains",
                ("arg1", _adminUsername),
                ("arg2", restart ? "yes" : "no")
            );
        }

        public async Task<FunctionReturnResult<string?>> RebuildBusinessesWeb(bool restart)
        {
            return await SendRequest("v-rebuild-web-domains",
                ("arg1", _businessesUsername),
                ("arg2", restart ? "yes" : "no")
            );
        }

        public async Task<FunctionReturnResult<string?>> ChangeWebProxy(string username, string domain, string proxyTemplate, string extensions, bool restart)
        {
            return await SendRequest("v-change-web-domain-proxy-tpl",
                ("returncode", "OK"),
                ("arg1", username),
                ("arg2", domain),
                ("arg3", proxyTemplate),
                ("arg4", extensions),
                ("arg5", restart == true ? "yes" : "no")
            );
        }

        /**
         * 
         * Custom Business Domain
         * 
        **/
        public async Task<FunctionReturnResult<string?>> GetCustomBusinessDomainDetails(string domain)
        {
            return await SendRequest("v-list-web-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", "json")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddCustomBusinessDomain(string domain, bool restart)
        {
            return await SendRequest("v-add-web-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", _domainDefaultIP),
                ("arg4", restart ? "yes" : "no"),
                ("arg5", "none"),
                ("arg6", "")
            );
        }

        public async Task<FunctionReturnResult<string?>> AddCustomBusinessDomainSSL(string domain, string sslCertificate, string sslPrivateKey, bool restart)
        {
            var result = new FunctionReturnResult<string?>();

            string ftpUsername = (domain + "_ssl");
            string ftpPass = Guid.NewGuid().ToString();

            string sslDir = Path.Combine("home", _businessesUsername, "web", domain, "private");

            var addFTPResult = await AddWebDomainFTP(domain, ftpUsername, ftpPass, "private");
            if (!addFTPResult.Success) {

                result.Code = "AddCustomBusinessDomainSSL:" + addFTPResult.Code;
                result.Message = addFTPResult.Message;
                result.Data = addFTPResult.Data;
                return result;
            }

            var ftpUploadResult = new FunctionReturnResult<string?>();
            try
            {
                using (var ftp = new FtpClient(domain, ftpUsername, ftpPass))
                {
                    ftp.Connect();

                    ftp.UploadBytes(Encoding.Default.GetBytes(sslCertificate), (Path.Combine(sslDir, (domain + ".crt"))), FtpRemoteExists.Overwrite);
                    ftp.UploadBytes(Encoding.Default.GetBytes(sslPrivateKey), (Path.Combine(sslDir, (domain + ".key"))), FtpRemoteExists.Overwrite);

                    ftp.Disconnect();
                }

                ftpUploadResult.Success = true;
            }
            catch (Exception ex)
            {
                ftpUploadResult.Code = "AddCustomBusinessDomainSSL:1";
                ftpUploadResult.Message = ex.Message;
            }

            var deleteFTPResult = await DeleteWebDomainFTP(domain, ftpUsername);
            if (!deleteFTPResult.Success) {
                result.Code = "AddCustomBusinessDomainSSL:" + deleteFTPResult.Code;
                result.Message = deleteFTPResult.Message;
                result.Data = deleteFTPResult.Data;
                return result;
            }

            if (!ftpUploadResult.Success) {
                result.Code = "AddCustomBusinessDomainSSL:" + ftpUploadResult.Code;
                result.Message = ftpUploadResult.Message;
                return result;
            }

            return await SendRequest("v-add-web-domain-ssl",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain),
                ("arg3", sslDir),
                ("arg4", ""),
                ("arg5", (restart ? "yes" : "no"))
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
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", domain)
            );
        }

        public async Task<FunctionReturnResult<string?>> SetCustomDomainDefaultProxyTemplate(string domain, bool restart)
        {
            return await ChangeWebProxy(
                _businessesUsername,
                domain,
                _defaultHTTPSProxyTemplateName,
                "",
                restart
            );
        }

        public async Task<FunctionReturnResult<string?>> SetCustomDomainMaintenanceProxyTemplate(string domain, bool restart)
        {
            return await ChangeWebProxy(
                _businessesUsername,
                domain,
                _maintenanceProxyTemplateFile,
                "",
                restart
            );
        }

        /**
         * 
         * Iqra Business Subdomain
         * 
        **/
        public async Task<FunctionReturnResult<string?>> GetIqraBusinessSubDomainDetails(string subdomain)
        {
            return await SendRequest("v-list-web-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain)),
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
                ("arg5", "none"),
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
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain))
            );
        }

        public async Task<FunctionReturnResult<string?>> AddIqraBusinessSubDomainLetsEncryptSSL(string subdomain)
        {
            return await SendRequest("v-add-letsencrypt-domain",
                ("returncode", "OK"),
                ("arg1", _businessesUsername),
                ("arg2", (subdomain + "." + _businessDomain)),
                ("arg3", "")
            );
        }

        public async Task<FunctionReturnResult<string?>> SetIqraSubDomainDefaultProxyTemplate(string subdomain, bool restart)
        {
            return await ChangeWebProxy(
                _businessesUsername,
                (subdomain + "." + _businessDomain),
                _defaultHTTPSProxyTemplateName,
                "",
                restart
            );
        }

        public async Task<FunctionReturnResult<string?>> SetIqraSubDomainMaintenanceProxyTemplate(string subdomain, bool restart)
        {
            return await ChangeWebProxy(
                _businessesUsername,
                (subdomain + "." + _businessDomain),
                _maintenanceProxyTemplateFile,
                "",
                restart
            );
        }
    }
}
