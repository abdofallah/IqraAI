using IqraCore.Entities.Helper.Server;

namespace IqraCore.Entities.Region
{
    public class RegionBackendServerData : RegionServerData
    {
        public override ServerTypeEnum Type { get; set; } = ServerTypeEnum.Backend;
    }
}
