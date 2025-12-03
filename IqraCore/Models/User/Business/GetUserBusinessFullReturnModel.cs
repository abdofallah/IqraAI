using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business
{
    public class GetUserBusinessFullReturnModel
    {
        public GetUseBusinessFullResultMetaDataModel BusinessData { get; set; }
        public BusinessApp BusinessApp { get; set; }
    }
}
