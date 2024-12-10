using IqraCore.Attributes;
using IqraCore.Entities.Helper;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace IqraCore.Entities.Business
{
    public class BusinessAppContextBranch
    {
        public string Id { get; set; }
        public BusinessAppContextBranchGeneral General { get; set; } = new BusinessAppContextBranchGeneral();

        [BsonDictionaryOptions(DictionaryRepresentation.Document)]
        public DictionaryStringEnumValue<string, DayOfWeek, BusinessAppContextBranchWorkingHours> WorkingHours { get; set; } = new DictionaryStringEnumValue<string, DayOfWeek, BusinessAppContextBranchWorkingHours>();
        public List<BusinessAppContextBranchTeam> Team { get; set; } = new List<BusinessAppContextBranchTeam>();
    }

    public class BusinessAppContextBranchGeneral
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Address { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Website { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, Dictionary<string, string>> OtherInformation { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }

    public class BusinessAppContextBranchWorkingHours
    {
        public bool IsClosed { get; set; } = false;
        public List<(TimeOnly, TimeOnly)> Timings { get; set; } = new List<(TimeOnly, TimeOnly)>();
    }

    public class BusinessAppContextBranchTeam
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Role { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Information { get; set; } = new Dictionary<string, string>();
    }
}
