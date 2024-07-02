namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppContextBranch
    {
        public long Id { get; set; }
        public BusinessAppContextBranchGeneral General { get; set; }
        public BusinessAppContextBranchWorkingHours WorkingHours { get; set; }
        public List<BusinessAppContextBranchTeam> Team { get; set; }
    }

    public class BusinessAppContextBranchGeneral
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Address { get; set; }
        public Dictionary<string, string> Phone { get; set; }
        public Dictionary<string, string> Email { get; set; }
        public Dictionary<string, string> Website { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; }
    }

    public class BusinessAppContextBranchWorkingHours
    {
        public DayOfWeek Day { get; set; }
        public bool IsClosed { get; set; }
        public List<(TimeOnly, TimeOnly)> Timings { get; set; }
    }

    public class BusinessAppContextBranchTeam
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Role { get; set; }
        public Dictionary<string, string> Email { get; set; }
        public Dictionary<string, string> Phone { get; set; }
        public Dictionary<string, string> Information { get; set; }
    }
}
