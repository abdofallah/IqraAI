using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Server
{
    public class TelephonyStatusNotifyToBackendModel
    {
        public TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Unknown;
        public string? PhoneNumberId { get; set; } = null;
        public string? Status { get; set; } = null;
    }
}
