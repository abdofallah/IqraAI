namespace IqraCore.Entities.User
{
    public class UserPaymentMethod
    {
        public string ProviderName { get; set; } // e.g., "Stripe", "PayPal", "YourCustomGateway"
        public string ProviderPaymentMethodId { get; set; } // The ID from the payment provider
        public string DisplayName { get; set; } // e.g., "Visa ending in 4242"
        public string Type { get; set; } // e.g., "Card", "BankAccount"
        public bool IsDefault { get; set; } = false; // Only one should be default per user
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true; // Can be marked inactive if removed or expired

        // You might store more details depending on the provider,
        // but often the provider's ID is enough to reference it for charges.
        // public string CardLastFour {get; set;}
        // public string CardExpiryMonth {get; set;}
        // public string CardExpiryYear {get; set;}
    }
}
