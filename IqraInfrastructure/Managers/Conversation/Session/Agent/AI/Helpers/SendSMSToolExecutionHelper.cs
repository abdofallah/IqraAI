using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using PhoneNumbers;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers
{
    public class SendSMSToolExecutionHelper
    {
        private readonly ILogger _logger;

        private BusinessApp? _businessApp;

        private readonly IntegrationsManager _integrationsManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;

        public SendSMSToolExecutionHelper(
            ILoggerFactory loggerFactory,
            IntegrationsManager integrationsManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager
        )
        {
            _logger = loggerFactory.CreateLogger<SendSMSToolExecutionHelper>();

            _integrationsManager = integrationsManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
        }

        public void Initalize(BusinessApp businessApp)
        {
            _businessApp = businessApp;
        }

        public async Task<FunctionReturnResult> SendMessageAsync(BusinessAppAgentScriptSendSMSToolNode sendSMSNode, string message, string toNumber, CancellationToken cancellationToken)
        {
            var result = new FunctionReturnResult();

            BusinessNumberData? numberData = _businessApp.Numbers.Find(n => n.Id == sendSMSNode.PhoneNumberId);
            if (numberData == null)
            {
                return result.SetFailureResult(
                    "SendMessageAsync:NUMBER_NOT_FOUND",
                    $"Phone number with id {sendSMSNode.PhoneNumberId} not found in Business App Data."
                );
            }

            PhoneNumber toSendNumberParsed;
            try
            {
                if (toNumber.StartsWith("+"))
                {
                    toSendNumberParsed = PhoneNumberUtil.GetInstance().Parse(toNumber, "ZZ");
                }
                else
                {
                    toSendNumberParsed = PhoneNumberUtil.GetInstance().Parse("+" + toNumber, "ZZ");
                }
            }
            catch (NumberParseException)
            {
                // if parsing failed, it could be that it is missing country code, could be possible if a national call to national number
                // workaround, we know our client telephopny country code so we will try to map it once again and see if it works

                try
                {
                    int countryCodeForTelephony = PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(numberData.CountryCode);

                    toSendNumberParsed = PhoneNumberUtil.GetInstance().Parse("+" + countryCodeForTelephony + toNumber, "ZZ");
                }
                catch (NumberParseException)
                {
                    return result.SetFailureResult(
                        "SendMessageAsync:INVALID_NUMBER",
                        $"Phone number {toNumber} is not valid, must be in the format of E.164 (+[country code][phone number])."
                    );
                }
            }
            if (!PhoneNumberUtil.GetInstance().IsValidNumber(toSendNumberParsed))
            {
                return result.SetFailureResult(
                    "SendMessageAsync:INVALID_NUMBER",
                    $"Phone number {toNumber} is not valid, must be in the format of E.164 (+[country code][phone number])."
                );
            }

            BusinessAppIntegration? numberIntegrationData = _businessApp.Integrations.Find(i => i.Id == numberData.IntegrationId);
            if (numberIntegrationData == null)
            {
                return result.SetFailureResult(
                    "SendMessageAsync:INTEGRATION_NOT_FOUND",
                    $"Integration with id {numberData.IntegrationId} not found in Business App Data for phone number with id {sendSMSNode.PhoneNumberId}."
                );
            }

            if (numberData.Provider == TelephonyProviderEnum.ModemTel)
            {
                if (numberData is BusinessNumberModemTelData modemTelNumberData)
                {
                    string? APIKey = _integrationsManager.DecryptField(numberIntegrationData.EncryptedFields["apikey"]);
                    string? APIBaseUrl = numberIntegrationData.Fields["endpoint"];

                    if (string.IsNullOrEmpty(APIBaseUrl) || string.IsNullOrEmpty(APIKey))
                    {
                        return result.SetFailureResult(
                            "SendMessageAsync:MODEMTEL_INTEGRATION_ERROR",
                            $"ModemTel integration is not configured correctly for phone number with id {sendSMSNode.PhoneNumberId}."
                        );
                    }

                    var sendModemtelSMSResult = await _modemTelManager.SendSmsAsync(APIKey, APIBaseUrl, modemTelNumberData.ModemTelPhoneNumberId, toNumber, message);
                    if (!sendModemtelSMSResult.Success)
                    {
                        return result.SetFailureResult(
                            "SendMessageAsync:MODEMTEL_SEND_ERROR",
                            $"Error sending SMS using ModemTel: {sendModemtelSMSResult.Message}"
                        );
                    }

                    return result.SetSuccessResult();
                }
                else
                {
                    return result.SetFailureResult(
                        "SendMessageAsync:MODEMTEL_MAPPING_ERROR",
                        $"Failed to map phone number with id {sendSMSNode.PhoneNumberId} to ModemTel number data."
                    );
                }
            }
            else if (numberData.Provider == TelephonyProviderEnum.Twilio)
            {
                if (numberData is BusinessNumberTwilioData twilioNumberData)
                {
                    throw new NotImplementedException("Twilio SMS sending is not yet implemented.");
                }
                else
                {
                    return result.SetFailureResult(
                        "SendMessageAsync:TWILIO_MAPPING_ERROR",
                        $"Failed to map phone number with id {sendSMSNode.PhoneNumberId} to Twilio number data."
                    );
                }
            }
            else
            {
                return result.SetFailureResult(
                    "SendMessageAsync:UNKNOWN_PROVIDER",
                    $"Phone number with id {sendSMSNode.PhoneNumberId} has provider {numberData.Provider}, which is not yet supported."
                );
            }
        }
    }
}
