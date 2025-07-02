using IqraCore.Entities.Billing.Plan;
using IqraCore.Entities.Helper.Billing;
using System.Text.Json.Serialization;

namespace IqraCore.Models.User.GetUserPlanDetailsModel
{
    public class GetUserPlanDetailsModel
    {
        public GetUserPlanDetailsModel() { }
        public GetUserPlanDetailsModel(PlanDefinitionBase planDefinitionBase)
        {
            Id = planDefinitionBase.Id;
            Name = planDefinitionBase.Name;
            Description = planDefinitionBase.Description;
            Type = planDefinitionBase.Type;
            PricingModel = planDefinitionBase.PricingModel;
            AdditionalConcurrencySlotPrice = planDefinitionBase.AdditionalConcurrencySlotPrice;
            MinimumTopUpAmount = planDefinitionBase.MinimumTopUpAmount;
            BaseConcurrency = planDefinitionBase.GetBaseIncludedConcurrency();

            if (planDefinitionBase is StandardPlanDefinition standardPlan)
            {
                BaseMinutePrice = standardPlan.BaseMinutePrice;
            }
            else if (planDefinitionBase is VolumeTieredPlanDefinition volumePlan)
            {
                BaseMinutePrice = volumePlan.BaseMinutePriceBeforeDiscount;

                VolumeDiscountTiers = volumePlan.VolumeDiscountTiers
                    .Select(t => new VolumeDiscountTierModel(t))
                    .ToList();
            }
            else if (planDefinitionBase is FixedPackagePlanDefinition fixedPlan)
            {
                BaseMinutePrice = (decimal)(fixedPlan.FixedMonthlyPrice / ((decimal)fixedPlan.IncludedMinutes));

                FixedMonthlyPrice = fixedPlan.FixedMonthlyPrice;
                IncludedMinutes = fixedPlan.IncludedMinutes;
                OverageMinutePrice = fixedPlan.OverageMinutePrice;
            }
        }

        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PlanType Type { get; set; } = PlanType.Public;
        public PlanPricingModel PricingModel { get; set; } = PlanPricingModel.StandardPayAsYouGo;
        public decimal AdditionalConcurrencySlotPrice { get; set; } = 0.00m;
        public decimal MinimumTopUpAmount { get; set; } = 0.00m;

        // Common Properties for All Plan Types
        public decimal BaseMinutePrice { get; set; } = 0.00m;
        public int BaseConcurrency { get; set; } = 0;

        // Volume Tiered Plan Specific Properties
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<VolumeDiscountTierModel>? VolumeDiscountTiers { get; set; } = null;

        // Fixed Package Plan Specific Properties
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? FixedMonthlyPrice { get; set; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? IncludedMinutes { get; set; } = null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? OverageMinutePrice { get; set; } = null;
    }

    public class VolumeDiscountTierModel
    {
        public VolumeDiscountTierModel() { }
        public VolumeDiscountTierModel(VolumeDiscountTier volumeDiscountTier)
        {
            MinimumMonthlyMinutesThreshold = volumeDiscountTier.MinimumMonthlyMinutesThreshold;
            DiscountPercentage = volumeDiscountTier.DiscountPercentage;
        }

        public int MinimumMonthlyMinutesThreshold { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
