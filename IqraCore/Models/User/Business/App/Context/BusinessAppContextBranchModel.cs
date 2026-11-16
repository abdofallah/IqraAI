using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;

namespace IqraCore.Models.User.Business.App.Context
{
    public class BusinessAppContextBranchModel
    {
        public BusinessAppContextBranchModel(BusinessAppContextBranch data)
        {
            Id = data.Id;
            General = data.General;
            WorkingHours = data.WorkingHours;
            Team = data.Team;
        }

        public string Id { get; set; }
        public BusinessAppContextBranchGeneral General { get; set; }
        public DictionaryStringEnumValue<string, AllDaysOfWeekEnum, BusinessAppContextBranchWorkingHours> WorkingHours { get; set; }
        public List<BusinessAppContextBranchTeam> Team { get; set; }
    }
}
