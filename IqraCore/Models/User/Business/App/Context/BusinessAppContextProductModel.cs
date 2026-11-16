using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Context
{
    public class BusinessAppContextProductModel
    {
        public BusinessAppContextProductModel(BusinessAppContextProduct data)
        {
            Id = data.Id;
            Name = data.Name;
            ShortDescription = data.ShortDescription;
            LongDescription = data.LongDescription;
            AvailableAtBranches = data.AvailableAtBranches;
            OtherInformation = data.OtherInformation;
        }

        public string Id { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> ShortDescription { get; set; }
        public Dictionary<string, string> LongDescription { get; set; }
        public List<string> AvailableAtBranches { get; set; }
        public Dictionary<string, Dictionary<string, string>> OtherInformation { get; set; }
    }
}
