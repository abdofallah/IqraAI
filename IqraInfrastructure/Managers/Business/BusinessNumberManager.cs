using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Business;
using PhoneNumbers;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessNumberManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;

        private readonly IntegrationsManager _integrationsManager;

        public BusinessNumberManager(
            BusinessManager businessManager,
            BusinessAppRepository businessAppRepository,
            BusinessRepository businessRepository,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager
        )
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;

            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;

            _integrationsManager = integrationsManager;
        }

        public async Task<BusinessNumberData?> GetBusinessNumberById(long businessId, string numberId)
        {
            var numberData = await _businessAppRepository.GetBusinessNumberById(businessId, numberId);
            return numberData;
        }

        public async Task<bool> CheckBusinessNumberExistsByNumber(string numberCountryCode, string phoneNumber, long businessId)
        {
            return await _businessAppRepository.CheckBusinessNumberExistsByNumber(numberCountryCode, phoneNumber, businessId);
        }

        public async Task<bool> CheckBusinessNumberExistsById(string exisitingNumberId, long businessId)
        {
            return await _businessAppRepository.CheckBusinessNumberExistsById(exisitingNumberId, businessId);
        }

        public async Task<FunctionReturnResult<BusinessNumberData?>> AddOrUpdateBusinessNumber(JsonDocument? changes, string countryCode, string number, string integrationId, TelephonyProviderEnum provider, string postType, BusinessNumberData? exisitingNumberData, long businessId, RegionManager regionManager)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            BusinessNumberData newNumberData = new BusinessNumberData()
            {
                Id = postType == "new" ? Guid.NewGuid().ToString() : exisitingNumberData.Id,
                CountryCode = countryCode,
                Number = number,
                Provider = provider,
                RouteId = exisitingNumberData.RouteId
            };
   
            // Get Integration Data
            var integrationData = await _parentBusinessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, integrationId);
            if (!integrationData.Success)
            {
                result.Code = "AddOrUpdateBusinessNumber:" + integrationData.Code;
                result.Message = integrationData.Message;
                return result;
            }

            newNumberData.IntegrationId = integrationId;

            // Get region ID
            if (!changes.RootElement.TryGetProperty("regionId", out var regionIdElement))
            {
                result.Code = "AddOrUpdateBusinessNumber:1";
                result.Message = "Region ID not found in changes.";
                return result;
            }
            string? regionId = regionIdElement.GetString();
            if (string.IsNullOrWhiteSpace(regionId))
            {
                result.Code = "AddOrUpdateBusinessNumber:2";
                result.Message = "Region ID cannot be empty.";
                return result;
            }

            // Validate region exists
            var regionData = await regionManager.GetRegionById(regionId);
            if (regionData == null)
            {
                result.Code = "AddOrUpdateBusinessNumber:3";
                result.Message = "Region not found.";
                return result;
            }
            if (regionData.DisabledAt != null)
            {
                result.Code = "AddOrUpdateBusinessNumber:4";
                result.Message = "Region is disabled.";
                return result;
            }

            newNumberData.RegionId = regionId;

            // Get Regions's Webhook
            var getRegionWebhookServer = regionData.Servers.Find(x => x.Type == ServerTypeEnum.Proxy);
            if (getRegionWebhookServer == null)
            {
                result.Code = "AddOrUpdateBusinessNumber:5";
                result.Message = "Region does not have a proxy server.";
                return result;
            }

            newNumberData.RegionWebhookEndpoint = getRegionWebhookServer.Endpoint;

            if (provider == TelephonyProviderEnum.Unknown)
            {
                result.Code = "AddOrUpdateBusinessNumber:6";
                result.Message = "Invalid provider type.";
                return result;
            }

            if (provider == TelephonyProviderEnum.ModemTel)
            {
                newNumberData = new BusinessNumberModemTelData(newNumberData);

                var decryptedKey = _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["apikey"]);

                var phoneNumberData = await _modemTelManager.GetPhoneNumberByCountryCodeAndNumberAsync(decryptedKey, integrationData.Data.Fields["endpoint"], countryCode, number);
                if (!phoneNumberData.Success)
                {
                    result.Code = "AddOrUpdateBusinessNumber:" + phoneNumberData.Code;
                    result.Message = phoneNumberData.Message;
                    return result;
                }

                if (!phoneNumberData.Data.IsActive)
                {
                    result.Code = "AddOrUpdateBusinessNumber:7";
                    result.Message = "Phone number is not active.";
                    return result;
                }

                if (!phoneNumberData.Data.CanMakeCalls)
                {
                    result.Code = "AddOrUpdateBusinessNumber:8";
                    result.Message = "Phone number cannot make calls.";
                    return result;
                }

                if (!phoneNumberData.Data.CanSendSms)
                {
                    result.Code = "AddOrUpdateBusinessNumber:9";
                    result.Message = "Phone number cannot make SMS.";
                    return result;
                }

                ((BusinessNumberModemTelData)newNumberData).ModemTelPhoneNumberId = phoneNumberData.Data.Id;

                // TODO update the webhook url in-app
            }
            else if (provider == TelephonyProviderEnum.Twilio)
            {
                newNumberData = new BusinessNumberTwilioData(newNumberData);

                var accountSid = integrationData.Data.Fields["sid"];
                var accountAuthToken = _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["auth"]);

                var phoneNumberData = await _twilioManager.GetPhoneNumbersByNumberAsync(accountSid, accountAuthToken, PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(countryCode).ToString(), number);
                if (!phoneNumberData.Success)
                {
                    result.Code = "AddOrUpdateBusinessNumber:" + phoneNumberData.Code;
                    result.Message = phoneNumberData.Message;
                    return result;
                }

                if (phoneNumberData.Data.Count == 0)
                {
                    result.Code = "AddOrUpdateBusinessNumber:7";
                    result.Message = "Phone number not found.";
                    return result;
                }
                var firstNumber = phoneNumberData.Data.FirstOrDefault();

                if (!firstNumber.Capabilities.Voice)
                {
                    result.Code = "AddOrUpdateBusinessNumber:8";
                    result.Message = "Phone number cannot make calls.";
                    return result;
                }

                if (!firstNumber.Capabilities.SMS)
                {
                    result.Code = "AddOrUpdateBusinessNumber:9";
                    result.Message = "Phone number cannot make SMS.";
                    return result;
                }

                ((BusinessNumberTwilioData)newNumberData).TwilioPhoneNumberId = firstNumber.Sid;

                var regionProxyServerBaseURI = new Uri((getRegionWebhookServer.UseSSL ? "https://" : "http://") + getRegionWebhookServer.Endpoint);
                var regionProxyABSPATH = (regionProxyServerBaseURI.AbsolutePath != "/" ? regionProxyServerBaseURI.AbsolutePath : "");

                string statusCallbackUrl = new Uri(regionProxyServerBaseURI, $"{regionProxyABSPATH}/api/twilio/webhook/voice/status/{businessId}/{newNumberData.Id}").ToString();
                string voiceUrl = new Uri(regionProxyServerBaseURI, $"{regionProxyABSPATH}/api/twilio/webhook/voice/incoming/{businessId}/{newNumberData.Id}").ToString();

                var updateWebhookResult = await _twilioManager.UpdatePhoneNumberVoiceConfigurationAsync(accountSid, accountAuthToken, firstNumber.Sid, voiceUrl, statusCallbackUrl);
                if (!updateWebhookResult.Success)
                {
                    // do nothing for now?
                    // do let the user know webhooks were not updated but the number was added eitherways
                }
            }
            else if (provider == TelephonyProviderEnum.Vonage || provider == TelephonyProviderEnum.Telnyx)
            {
                result.Code = "AddOrUpdateBusinessNumber:10";
                result.Message = "Provider type currently not implemented.";
                return result;
            }
            else
            {
                result.Code = "AddOrUpdateBusinessNumber:11";
                result.Message = "Invalid provider type.";
                return result;
            }

            if (postType == "new")
            {
                bool addNumberResult = await _businessAppRepository.AddBusinessNumber(businessId, newNumberData);
                if (!addNumberResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:12";
                    result.Message = $"Failed to add number to business.";
                    return result;
                }
            }
            else
            {
                bool updateNumberResult = await _businessAppRepository.UpdateBusinessNumber(businessId, newNumberData);
                if (!updateNumberResult)
                {
                    result.Code = "AddOrUpdateBusinessNumber:13";
                    result.Message = $"Failed to update number.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newNumberData;
            return result;
        }
    }
}
