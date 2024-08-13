using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;

namespace IqraCore.Models.AppUser
{
    public class GetUserBusinessFullReturnModel
    {
        public BusinessData BusinessData { get; set; }
        public BusinessApp BusinessApp { get; set; }
        public List<BusinessWhiteLabelDomain> BusinessWhiteLabelDomain { get; set; }
    }
}
