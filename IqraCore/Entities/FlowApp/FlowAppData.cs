using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.FlowApp
{
    public class FlowAppData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Matches the IFlowApp.AppKey (e.g. "cal_com")
        /// Unique Index should be applied on this field.
        /// </summary>
        [BsonElement("appKey")]
        public string AppKey { get; set; } = string.Empty;

        /// <summary>
        /// If not null, the entire app is disabled.
        /// </summary>
        [BsonElement("disabledAt")]
        public DateTime? DisabledAt { get; set; }

        /// <summary>
        /// Reason shown to admins (Internal).
        /// </summary>
        [BsonElement("disabledPrivateReason")]
        public string? DisabledPrivateReason { get; set; }

        /// <summary>
        /// Reason shown to users (e.g., "Under Maintenance").
        /// </summary>
        [BsonElement("disabledPublicReason")]
        public string? DisabledPublicReason { get; set; }

        /// <summary>
        /// Granular control for Actions within the app.
        /// Key = ActionKey (e.g. "BookMeeting")
        /// </summary>
        [BsonElement("actionPermissions")]
        public Dictionary<string, FlowItemPermission> ActionPermissions { get; set; } = new();

        /// <summary>
        /// Granular control for Fetchers.
        /// Key = FetcherKey (e.g. "GetEventTypes")
        /// </summary>
        [BsonElement("fetcherPermissions")]
        public Dictionary<string, FlowItemPermission> FetcherPermissions { get; set; } = new();
    }

    public class FlowItemPermission
    {
        [BsonElement("disabledAt")]
        public DateTime? DisabledAt { get; set; }

        [BsonElement("disabledPrivateReason")]
        public string? DisabledPrivateReason { get; set; }

        [BsonElement("disabledPublicReason")]
        public string? DisabledPublicReason { get; set; }
    }
}