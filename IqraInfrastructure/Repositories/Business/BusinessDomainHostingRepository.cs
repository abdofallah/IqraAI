using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessDomainHostingRepository
    {
        public async Task<FunctionReturnResult<bool?>> CheckDomainsExistsInHosting(BusinessUserWhiteLabelDomainTypeEnum domainType, string domainName)
        {
            var result = new FunctionReturnResult<bool?>();

            // todo

            return result;
        }

        public async Task<FunctionReturnResult<bool?>> AddDomainToHosting(BusinessWhiteLabelDomain domainData)
        {
            var result = new FunctionReturnResult<bool?>();

            // todo

            return result;
        }

        public async Task<FunctionReturnResult<bool?>> UpdateDomainToHosting(BusinessWhiteLabelDomain oldDomainData, BusinessWhiteLabelDomain updatedDomainData)
        {
            var result = new FunctionReturnResult<bool?>();

            // todo

            return result;
        }
    }
}
