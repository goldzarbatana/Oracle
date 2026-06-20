using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CommerceOrchestrator.Core
{
    [Serializable]
    public struct RewardEntry
    {
        public string resourceId;
        public int amount;

        public RewardEntry(string resourceId, int amount)
        {
            this.resourceId = resourceId;
            this.amount = amount;
        }
    }

    [Serializable]
    public class ProductConfig
    {
        public string productId;
        public float priceUsd;
        public string rewardString;
        public float discountPercent;
        public bool isSubscription;
        public List<RewardEntry> parsedRewards;

        public ProductConfig()
        {
            parsedRewards = new List<RewardEntry>();
        }
    }

    public interface IStorePricingProvider
    {
        event Action OnPricingUpdated;
        bool IsConnected { get; }
        UniTask FetchProductsAsync(bool forceReload = false);
        ProductConfig GetProductConfig(string productId);
        IReadOnlyDictionary<string, ProductConfig> GetAllProductConfigs();
    }
}
