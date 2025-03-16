using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraInfrastructure.Repositories.Region;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Managers.Region
{
    public class RegionManager
    {
        private readonly ILogger<RegionManager> _logger;

        private readonly RegionRepository _regionRepository;
        public RegionManager(ILogger<RegionManager> logger, RegionRepository regionRepository)
        {
            _logger = logger;

            _regionRepository = regionRepository;
        }

        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<RegionData>?>();
            result.Data = null;

            var businesses = await _regionRepository.GetRegions(page, pageSize);
            if (businesses == null)
            {
                result.Code = "GetRegions:1";
                _logger.LogError("[RegionManager] Null - Regions not found");
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions()
        {
            var result = new FunctionReturnResult<List<RegionData>?>();

            var businesses = await _regionRepository.GetRegions();
            if (businesses == null)
            {
                result.Code = "GetRegions:1";
                _logger.LogError("[RegionManager] Null - Regions not found");
            }
            else
            {
                result.Success = true;
                result.Data = businesses;
            }

            return result;
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

            regionData.CountryRegion = regionData.CountryCode + "-" + regionData.CountryRegion;
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
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Endpoint == ipAddress)
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
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Endpoint == ipAddress)
            );

            var updateDefinition = Builders<RegionData>.Update.Set(
                r => r.Servers.FirstMatchingElement().DisabledAt,
                null
            );

            return await _regionRepository.UpdateRegion(filterDefinition, updateDefinition);
        }

        public async Task<RegionData?> GetRegionById(string regionId)
        {
            return await _regionRepository.GetRegionById(regionId);
        }
    }
}
