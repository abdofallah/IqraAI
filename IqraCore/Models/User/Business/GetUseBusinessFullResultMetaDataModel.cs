using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business
{
    public class GetUseBusinessFullResultMetaDataModel
    {
        public GetUseBusinessFullResultMetaDataModel(BusinessData data)
        {
            Id = data.Id;
            MasterUserEmail = data.MasterUserEmail;
            Name = data.Name;
            // LogoUrl must be filled manually using a presigned url
            DefaultLanguage = data.DefaultLanguage;
            Languages = data.Languages;
            Tutorials = data.Tutorials;
            Permission = data.Permission;
            WhiteLabelAssignedCustomerEmail = data.WhiteLabelAssignedCustomerEmail;
        }

        public long Id { get; set; }
        public string MasterUserEmail { get; set; }

        public string Name { get; set; }
        public string? LogoUrl { get; set; }

        public string DefaultLanguage { get; set; }
        public List<string> Languages { get; set; }

        public Dictionary<string, object> Tutorials { get; set; }

        public BusinessPermission Permission { get; set; }

        public string? WhiteLabelAssignedCustomerEmail { get; set; }
    }
}
