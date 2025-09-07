using IqraCore.Entities.Helper.Call.Outbound;

namespace IqraCore.Models.Business.MakeCalls
{
    public class MakeCallRequestDto
    {
        public string? CampaignId { get; set; } = null;

        public MakeCallNumberDetailsDto Number { get; set; } = new MakeCallNumberDetailsDto();
        public MakeCallScheduleDto Schedule { get; set; } = new MakeCallScheduleDto();

        public Dictionary<string, string> DynamicVariables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class MakeCallNumberDetailsDto
    {
        public OutboundCallNumberType? Type { get; set; } = null;

        // If Single Call
        public string? ToNumber { get; set; } = null;
    }

    public class MakeCallScheduleDto
    {
        public OutboundCallScheduleType? Type { get; set; } = null;
        public DateTime? DateTimeUTC { get; set; } = null;
    }
}
