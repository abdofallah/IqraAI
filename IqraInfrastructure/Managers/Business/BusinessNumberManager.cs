using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Business;
using MongoDB.Bson;
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

        public async Task<FunctionReturnResult<BusinessNumberData?>> AddOrUpdateBusinessNumber(JsonDocument changes, string countryCode, string number, string integrationId, TelephonyProviderEnum provider, string postType, BusinessNumberData? existingNumberData, long businessId, RegionManager regionManager, bool isUserAdmin)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            BusinessNumberData newNumberData;
            switch (provider)
            {
                case TelephonyProviderEnum.SIP:
                    newNumberData = new BusinessNumberSipData();
                    break;
                case TelephonyProviderEnum.Twilio:
                    newNumberData = new BusinessNumberTwilioData();
                    break;
                case TelephonyProviderEnum.ModemTel:
                    newNumberData = new BusinessNumberModemTelData();
                    break;
                default:
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:INVALID_PROVIDER",
                        "Provider not supported."
                    );
            }

            newNumberData.Id = postType == "new" ? ObjectId.GenerateNewId().ToString() : existingNumberData!.Id;
            newNumberData.CountryCode = countryCode;
            newNumberData.Number = number;
            newNumberData.Provider = provider;
            newNumberData.RouteId = postType == "new" ? null : existingNumberData?.RouteId;
            newNumberData.IntegrationId = integrationId;

            // Get Integration Data
            var integrationDataResult = await _parentBusinessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, newNumberData.IntegrationId);
            if (!integrationDataResult.Success)
            {
                return result.SetFailureResult(
                    $"AddOrUpdateBusinessNumber:{integrationDataResult.Code}",
                    integrationDataResult.Message
                );
            }

            // Get region ID
            if (!changes.RootElement.TryGetProperty("regionId", out var regionIdElement))
            {
                return result.SetFailureResult(
                    $"AddOrUpdateBusinessNumber:REGION_ID_NOT_FOUND",
                    "Region ID not found in changes."
                );
            }
            string? regionId = regionIdElement.GetString();
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:REGION_ID_EMPTY",
                    "Region ID cannot be empty."
                );
            }

            // Validate region exists
            var regionData = await regionManager.GetRegionById(regionId);
            if (regionData == null)
            {
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:REGION_NOT_FOUND",
                    "Region not found."
                );
            }
            if (regionData.DisabledAt != null)
            {
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:REGION_DISABLED",
                    "Region is disabled."
                );
            }

            newNumberData.RegionId = regionId;

            // Get Regions's Webhook
            var getRegionProxyServer = regionData.Servers.Find(x => x.Type == ServerTypeEnum.Proxy && (isUserAdmin || !x.IsDevelopmentServer));
            if (getRegionProxyServer == null)
            {
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:REGION_NO_PROXY_SERVER",
                    "Region does not have a proxy server."
                );
            }
            newNumberData.RegionServerId = getRegionProxyServer.Id;

            if (provider == TelephonyProviderEnum.SIP)
            {
                var sipData = (BusinessNumberSipData)newNumberData;

                // E.164 Flag
                if (
                    !changes.RootElement.TryGetProperty("isE164Number", out var e164El) ||
                    (e164El.ValueKind != JsonValueKind.True && e164El.ValueKind != JsonValueKind.False)
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:E164_FLAG_NOT_FOUND",
                        "E.164 flag not found in changes."
                    );
                }
                sipData.IsE164Number = e164El.GetBoolean();

                // Allowed Source IPs
                if (
                    !changes.RootElement.TryGetProperty("allowedSourceIps", out var ipsEl) ||
                    ipsEl.ValueKind != JsonValueKind.Array
                ) {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:ALLOWED_SOURCE_IPS_NOT_FOUND",
                        "Allowed source IPs not found in changes."
                    );
                }
                var ipsEnum = ipsEl.EnumerateArray();
                foreach (var ip in ipsEnum)
                {
                    if (ip.ValueKind != JsonValueKind.String)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateBusinessNumber:ALLOWED_SOURCE_IPS_INVALID_TYPE",
                            "Allowed source IPs must be an array of strings."
                        );
                    }

                    if (string.IsNullOrWhiteSpace(ip.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateBusinessNumber:ALLOWED_SOURCE_IPS_EMPTY",
                            "Allowed source IPs cannot be empty."
                        );
                    }

                    sipData.AllowedSourceIps.Add(ip.GetString()!);
                }
            }
            else if (provider == TelephonyProviderEnum.ModemTel)
            {
                var modemTelData = (BusinessNumberModemTelData)newNumberData;

                var decryptedKey = _integrationsManager.DecryptField(integrationDataResult.Data.EncryptedFields["apikey"]);

                var phoneNumberData = await _modemTelManager.GetPhoneNumberByCountryCodeAndNumberAsync(decryptedKey, integrationDataResult.Data.Fields["endpoint"], countryCode, number);
                if (!phoneNumberData.Success)
                {
                    return result.SetFailureResult(
                        $"AddOrUpdateBusinessNumber:{phoneNumberData.Code}",
                        phoneNumberData.Message
                    );
                }

                if (!phoneNumberData.Data.IsActive)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_NOT_ACTIVE",
                        "Phone number is not active."
                    );
                }

                if (!phoneNumberData.Data.CanMakeCalls)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_CANNOT_MAKE_CALLS",
                        "Phone number cannot make calls."
                    );
                }

                if (!phoneNumberData.Data.CanSendSms)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_CANNOT_SEND_SMS",
                        "Phone number cannot send SMS."
                    );
                }

                modemTelData.ModemTelPhoneNumberId = phoneNumberData.Data.Id;

                // TODO update the webhook url in-app
            }
            else if (provider == TelephonyProviderEnum.Twilio)
            {
                var twilioData = (BusinessNumberTwilioData)newNumberData;

                var accountSid = integrationDataResult.Data.Fields["sid"];
                var accountAuthToken = _integrationsManager.DecryptField(integrationDataResult.Data.EncryptedFields["auth"]);

                var phoneNumberData = await _twilioManager.GetPhoneNumbersByNumberAsync(accountSid, accountAuthToken, PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(countryCode).ToString(), number);
                if (!phoneNumberData.Success)
                {
                    return result.SetFailureResult(
                        $"AddOrUpdateBusinessNumber:{phoneNumberData.Code}",
                        phoneNumberData.Message
                    );
                }

                if (phoneNumberData.Data.Count == 0)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_NOT_FOUND",
                        "Phone number not found."
                    );
                }
                var firstNumber = phoneNumberData.Data.FirstOrDefault();

                if (!firstNumber.Capabilities.Voice)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_CANNOT_MAKE_CALLS",
                        "Phone number cannot make calls."
                    );
                }

                if (!firstNumber.Capabilities.SMS)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:PHONE_NUMBER_CANNOT_SEND_SMS",
                        "Phone number cannot send SMS."
                    );
                }

                twilioData.TwilioPhoneNumberId = firstNumber.Sid;

                var regionProxyServerBaseURI = new Uri((getRegionProxyServer.UseSSL ? "https://" : "http://") + getRegionProxyServer.Endpoint);
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
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:PROVIDER_TYPE_NOT_IMPLEMENTED",
                    "Provider type currently not implemented."
                );
            }
            else
            {
                return result.SetFailureResult(
                    "AddOrUpdateBusinessNumber:PROVIDER_TYPE_INVALID",
                    "Provider type is invalid."
                );
            }

            if (postType == "new")
            {
                bool addNumberResult = await _businessAppRepository.AddBusinessNumber(businessId, newNumberData);
                if (!addNumberResult)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:FAILED_TO_ADD_NUMBER_TO_BUSINESS",
                        "Failed to add number to business."
                    );
                }
            }
            else
            {
                bool updateNumberResult = await _businessAppRepository.UpdateBusinessNumber(businessId, newNumberData);
                if (!updateNumberResult)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateBusinessNumber:FAILED_TO_UPDATE_NUMBER_TO_BUSINESS",
                        "Failed to update number to business."
                    );
                }
            }

            return result.SetSuccessResult(newNumberData);
        }
    }
}
