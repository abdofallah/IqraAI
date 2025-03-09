using IqraCore.Entities.Helper.Region;

namespace IqraCore.Entities.Region
{
    public class RegionBackendServerData : RegionServerData
    {
        public override RegionServerTypeEnum Type { get; set; } = RegionServerTypeEnum.Backend;
    }
}
