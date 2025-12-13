using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Business
{
    public class BusinessNumberSipData : BusinessNumberData
    {
        public BusinessNumberSipData() { }

        public BusinessNumberSipData(BusinessNumberData data) : base(data)
        {
        }

        public override TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.SIP;

        public string? SipUsername { get; set; }
        public string? SipPassword { get; set; }

        public List<string> AllowedSourceIps { get; set; } = new List<string>();

        public List<AudioEncodingTypeEnum> PreferredCodecs { get; set; } = new List<AudioEncodingTypeEnum>();
    }
}