namespace IqraCore.Entities.Business
{
    public class BusinessAppContextBranch
    {
        public long Id { get; set; }
        public BusinessAppContextBranchGeneral General { get; set; } = new BusinessAppContextBranchGeneral();
        public BusinessAppContextBranchWorkingHours WorkingHours { get; set; } = new BusinessAppContextBranchWorkingHours();
        public List<BusinessAppContextBranchTeam> Team { get; set; } = new List<BusinessAppContextBranchTeam>();
    }

    public class BusinessAppContextBranchGeneral
    {
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Address { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Website { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }

    public class BusinessAppContextBranchWorkingHours
    {
        public DayOfWeek Day { get; set; }
        public bool IsClosed { get; set; } = false;
        public List<(TimeOnly, TimeOnly)> Timings { get; set; } = new List<(TimeOnly, TimeOnly)>();
    }

    public class BusinessAppContextBranchTeam
    {
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Role { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Information { get; set; } = new Dictionary<string, string>();
    }
}
