using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Number
{
    public class BusinessNumberSipModel : BusinessNumberModel
    {
        public BusinessNumberSipModel(BusinessNumberSipData data) : base(data)
        {
            IsE164Number = data.IsE164Number;
            AllowedSourceIps = data.AllowedSourceIps;
        }

        public bool IsE164Number { get; set; }
        public List<string> AllowedSourceIps { get; set; }
    }
}
