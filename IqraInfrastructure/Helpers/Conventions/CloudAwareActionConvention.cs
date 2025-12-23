using IqraCore.Attributes;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace IqraInfrastructure.Helpers.Conventions
{
    public class CloudAwareActionConvention : IApplicationModelConvention
    {
        private readonly bool _isCloudVersion;

        public CloudAwareActionConvention(bool isCloudVersion)
        {
            _isCloudVersion = isCloudVersion;
        }

        public void Apply(ApplicationModel application)
        {
            if (!_isCloudVersion) return;

            foreach (var controller in application.Controllers)
            {
                var actionsToRemove = controller.Actions
                    .Where(a => a.Attributes.OfType<OpenSourceOnlyAttribute>().Any())
                    .ToList();

                foreach (var action in actionsToRemove)
                {
                    controller.Actions.Remove(action);
                }
            }
        }
    }
}
