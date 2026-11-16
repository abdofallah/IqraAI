using IqraCore.Entities.Business;
using IqraCore.Models.S3Storage;

namespace IqraCore.Models.User.Business
{
    public class GetUserBusinessFullResultMetaDataModel
    {
        public GetUserBusinessFullResultMetaDataModel(BusinessData data)
        {
            Id = data.Id;
            Name = data.Name;
            DefaultLanguage = data.DefaultLanguage;
            Languages = data.Languages;
            Tutorials = data.Tutorials;
            Permission = data.Permission;
        }

        public long Id { get; set; }

        public string Name { get; set; }
        public S3SignedFileModel? LogoUrl { get; set; }

        public string DefaultLanguage { get; set; }
        public List<string> Languages { get; set; }

        public Dictionary<string, object> Tutorials { get; set; }

        public BusinessModulePermission Permission { get; set; }
    }
}
