using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Context
{
    public class BusinessAppContextModel
    {
        public BusinessAppContextModel(BusinessAppContext data)
        {
            Branding = new BusinessAppContextBrandingModel(data.Branding);
            Branches = data.Branches.Select(x => new BusinessAppContextBranchModel(x)).ToList();
            Services = data.Services.Select(x => new BusinessAppContextServiceModel(x)).ToList();
            Products = data.Products.Select(x => new BusinessAppContextProductModel(x)).ToList();
        }

        public BusinessAppContextBrandingModel Branding { get; set; }
        public List<BusinessAppContextBranchModel> Branches { get; set; }
        public List<BusinessAppContextServiceModel> Services { get; set; }
        public List<BusinessAppContextProductModel> Products { get; set; }
    }
}
