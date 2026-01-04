using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IqraCore.Interfaces.Modules
{
    public interface ICloudProxyAppInitalizer
    {
        void SetupManagers(IServiceCollection services, IConfiguration appConfig);
        void SetupRepositories(IServiceCollection services, IConfiguration appConfig);
    }
}
