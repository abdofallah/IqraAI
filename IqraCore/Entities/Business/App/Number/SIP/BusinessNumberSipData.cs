using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberSipData : BusinessNumberData
    {
        public BusinessNumberSipData() { }

        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.SIP;

        public bool IsE164Number { get; set; } = false;

        public string? OverrideSipUsername { get; set; }
        public string? OverrideSipPassword { get; set; }

        public List<string> AllowedSourceIps { get; set; } = new List<string>();
    }
}