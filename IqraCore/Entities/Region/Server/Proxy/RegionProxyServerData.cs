using IqraCore.Entities.Helper.Region;

namespace IqraCore.Entities.Region
{
    public class RegionProxyServerData : RegionServerData
    {
        public override RegionServerTypeEnum Type { get; set; } = RegionServerTypeEnum.Proxy;
    }
}
