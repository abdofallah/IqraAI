namespace IqraCore.Entities.ProviderBase
{
    public class ProviderDataBase<TModel> where TModel : ProviderModelBase
    {
        public DateTime? DisabledAt { get; set; } = null;
        public string IntegrationId { get; set; } = "";
        public List<TModel> Models { get; set; } = new List<TModel>();
        public List<ProviderFieldBase> UserIntegrationFields { get; set; } = new List<ProviderFieldBase>();
    }
}
