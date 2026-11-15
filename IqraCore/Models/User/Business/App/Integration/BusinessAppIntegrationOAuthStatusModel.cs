using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Integration
{
    public class BusinessAppIntegrationOAuthStatusModel
    {
        public BusinessAppIntegrationOAuthStatusModel(BusinessAppIntegration data)
        {
            IntegrationId = data.Id;
            HasConnection = !string.IsNullOrEmpty(data.OAuth?.EncryptedRefreshToken);
            Message = data.OAuth?.StatusMessage;
            CheckedAt = data.OAuth?.StatusCheckedAt;

            if (data.OAuth?.Status == BusinessAppIntegrationOAuthData.StatusFailed)
            {
                Status = BusinessAppIntegrationOAuthData.StatusFailed;
            }
            else
            {
                Status = HasConnection ? BusinessAppIntegrationOAuthData.StatusConnected : BusinessAppIntegrationOAuthData.StatusNotConnected;
            }
        }

        public string IntegrationId { get; set; }
        public string Status { get; set; }
        public bool HasConnection { get; set; }
        public string? Message { get; set; }
        public DateTime? CheckedAt { get; set; }
    }
}
