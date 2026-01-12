using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IqraCore.Interfaces.Modules
{
    public interface ICloudBackgroundAppInitalizer
    {
        void SetupConfiguration(IServiceCollection services, IConfiguration appConfig);
        void SetupManagers(IServiceCollection services, IConfiguration appConfig);
        void SetupRepositories(IServiceCollection services, IConfiguration appConfig);
        void SetupHostedServices(IServiceCollection services);
    }
}
