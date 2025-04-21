using IqraCore.Entities.Helper.Server;

namespace IqraCore.Entities.Region
{
    public class RegionProxyServerData : RegionServerData
    {
        public override ServerTypeEnum Type { get; set; } = ServerTypeEnum.Proxy;
    }
}
