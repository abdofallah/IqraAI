using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IqraCore.Interfaces.Modules
{
    public interface ICloudFrontendAppInitalizer
    {
        void SetupConfiguration(IServiceCollection services, IConfiguration config);
        void SetupManagers(IServiceCollection services, IConfiguration appConfig);
        void SetupRepositories(IServiceCollection services, IConfiguration appConfig);
        void UseWhiteLabelResolver(IApplicationBuilder app);
        void ConfigureStaticFiles(IWebHostEnvironment env);
    }
}
