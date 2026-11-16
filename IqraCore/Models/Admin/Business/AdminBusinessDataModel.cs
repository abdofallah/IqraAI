using IqraCore.Entities.Business;

namespace IqraCore.Models.Admin.Business
{
    public class AdminBusinessDataModel
    {
        public AdminBusinessDataModel() { }
        public AdminBusinessDataModel(BusinessData data)
        {
            Id = data.Id;
            MasterUserEmail = data.MasterUserEmail;
            Name = data.Name;
            DefaultLanguage = data.DefaultLanguage;
            Languages = data.Languages;
            Tutorials = data.Tutorials;
            Permission = data.Permission;
            WhiteLabelAssignedCustomerEmail = data.WhiteLabelAssignedCustomerEmail;
        }

        public long Id { get; set; }
        public string MasterUserEmail { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string DefaultLanguage { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new List<string>();

        public Dictionary<string, object> Tutorials { get; set; } = new Dictionary<string, object>();

        public BusinessModulePermission Permission { get; set; } = new BusinessModulePermission();

        public string? WhiteLabelAssignedCustomerEmail { get; set; } = null;
    }
}
