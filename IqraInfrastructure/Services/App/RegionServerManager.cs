namespace IqraInfrastructure.Services.App
{
    public class RegionServerManager
    {
        private readonly RegionManager _regionManager;

        public RegionServerManager(RegionManager regionManager)
        {
            _regionManager = regionManager;
        }
    }
}
