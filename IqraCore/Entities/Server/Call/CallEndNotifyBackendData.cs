using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Server.Call
{
    public class CallEndNotifyBackendData
    {
        public TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Unknown;
        public string PhoneNumberId { get; set; } = "";
    }
}
