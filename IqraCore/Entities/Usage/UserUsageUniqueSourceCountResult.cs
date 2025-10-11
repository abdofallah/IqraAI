using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Entities.Usage
{
    public class UserUsageUniqueSourceCountResult
    {
        public long BusinessId { get; set; }
        public UserUsageSourceTypeEnum SourceType { get; set; }
        public int Count { get; set; }
    }
}
