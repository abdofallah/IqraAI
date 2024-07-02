namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUserPermissionMakeCalls
    {
        public bool MakeCallsTabEnabled { get; set; }
        public bool MakeSingleCallEnabled { get; set; }
        public bool MakeBulkCallEnabled { get; set; }
        public int MaxBulkCallNumbersCount { get; set; }
    }
}
