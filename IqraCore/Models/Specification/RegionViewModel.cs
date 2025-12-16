using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Region;

namespace IqraCore.Models.Specification
{
    public class RegionViewModel
    {
        public string CountryRegion { get; set; }
        public string CountryCode { get; set; }
        public DateTime? DisabledAt { get; set; }

        public List<RegionServerViewModel> Servers { get; set; }

        public static RegionViewModel BuildViewModelFromEntity(RegionData entity, bool includeBackendServers = false, bool includeDevelopmentServers = false)
        {
            var model = new RegionViewModel();

            model.CountryRegion = entity.CountryRegion;
            model.CountryCode = entity.CountryCode;
            model.DisabledAt = entity.DisabledAt;

            model.Servers = new List<RegionServerViewModel>();
            foreach (var server in entity.Servers)
            {
                if (server.Type == ServerTypeEnum.Backend && !includeBackendServers)
                {
                    continue;
                }

                if (server.IsDevelopmentServer && !includeDevelopmentServers)
                {
                    continue;
                }

                var serverModel = new RegionServerViewModel()
                {
                    Id = server.Id,
                    Endpoint = server.Endpoint,
                    UseSSL = server.UseSSL,
                    SIPPort = server.SIPPort,
                    DisabledAt = server.DisabledAt,
                    IsDevelopmentServer = server.IsDevelopmentServer
                };

                model.Servers.Add(serverModel);
            }

            return model;
        }
    }

    public class RegionServerViewModel
    {
        public string Id { get; set; } 
        public string Endpoint { get; set; }
        public bool UseSSL { get; set; }
        public int SIPPort { get; set; }

        public DateTime? DisabledAt { get; set; }
        public bool IsDevelopmentServer { get; set; }
    }
}
