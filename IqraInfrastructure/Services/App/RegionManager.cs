using IqraCore.Entities.Region;
using IqraInfrastructure.Repositories.App;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Services.App
{
    public class RegionManager
    {
        private readonly RegionRepository _regionRepository;
        public RegionManager(RegionRepository regionRepository)
        {
            _regionRepository = regionRepository;
        }

        public async Task<List<RegionData>> GetRegions()
        {
            return await _regionRepository.GetRegions();
        }

        public async Task<RegionData?> GetRegion(string countryCode, string countryRegion)
        {
            return await _regionRepository.GetRegion(countryCode, countryRegion);
        }

        public async Task<bool> AddRegion(RegionData regionData)
        {
            if (string.IsNullOrWhiteSpace(regionData.CountryCode) || string.IsNullOrWhiteSpace(regionData.CountryRegion))
            {
                return false;
            }

            regionData.Id = regionData.CountryCode + "-" + regionData.CountryRegion;
            return await _regionRepository.AddRegion(regionData);
        }

        public async Task<bool> DisableRegion(string countryCode, string countryRegion)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.DisabledAt, DateTime.UtcNow);

            return await _regionRepository.UpdateRegion(countryCode, countryRegion, updateDefinition);
        }

        public async Task<bool> EnableRegion(string countryCode, string countryRegion)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.DisabledAt, null);

            return await _regionRepository.UpdateRegion(countryCode, countryRegion, updateDefinition);
        }

        public async Task<bool> DisableRegionServer(string countryCode, string countryRegion, string ipAddress)
        {
            var filterDefinition = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.CountryCode, countryCode),
                Builders<RegionData>.Filter.Eq(r => r.CountryRegion, countryRegion),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.IpAddress == ipAddress)
            );

            var updateDefinition = Builders<RegionData>.Update.Set(
                r => r.Servers.FirstMatchingElement().DisabledAt,
                DateTime.UtcNow
            ); ;

            return await _regionRepository.UpdateRegion(filterDefinition, updateDefinition);
        }

        public async Task<bool> EnableRegionServer(string countryCode, string countryRegion, string ipAddress)
        {
            var filterDefinition = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.CountryCode, countryCode),
                Builders<RegionData>.Filter.Eq(r => r.CountryRegion, countryRegion),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.IpAddress == ipAddress)
            );

            var updateDefinition = Builders<RegionData>.Update.Set(
                r => r.Servers.FirstMatchingElement().DisabledAt,
                null
            );

            return await _regionRepository.UpdateRegion(filterDefinition, updateDefinition);
        }
    }
}
