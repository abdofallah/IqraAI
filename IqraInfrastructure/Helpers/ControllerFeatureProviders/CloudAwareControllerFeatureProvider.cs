using IqraCore.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace IqraInfrastructure.Helpers.Providers
{
    public class CloudAwareControllerFeatureProvider : ControllerFeatureProvider
    {
        private readonly bool _isCloudVersion;

        public CloudAwareControllerFeatureProvider(bool isCloudVersion)
        {
            _isCloudVersion = isCloudVersion;
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            var isController = base.IsController(typeInfo);
            if (!isController) return false;

            if (_isCloudVersion)
            {
                if (typeInfo.GetCustomAttribute<OpenSourceOnlyAttribute>() != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
