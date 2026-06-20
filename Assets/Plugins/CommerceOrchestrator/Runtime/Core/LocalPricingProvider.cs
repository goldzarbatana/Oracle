using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CommerceOrchestrator.Core
{
    public class LocalPricingProvider : IStorePricingProvider
    {
        private readonly StoreDatabase _database;
        private readonly Dictionary<string, ProductConfig> _configs = new Dictionary<string, ProductConfig>();

        public event Action OnPricingUpdated;
        public bool IsConnected => true;

        public LocalPricingProvider(StoreDatabase database)
        {
            _database = database;
            BuildConfigs();
        }

        public UniTask FetchProductsAsync(bool forceReload = false)
        {
            BuildConfigs();
            OnPricingUpdated?.Invoke();
            return UniTask.CompletedTask;
        }

        public ProductConfig GetProductConfig(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            _configs.TryGetValue(productId, out var config);
            return config;
        }

        public IReadOnlyDictionary<string, ProductConfig> GetAllProductConfigs()
        {
            return _configs;
        }

        private void BuildConfigs()
        {
            _configs.Clear();
            if (_database == null || _database.products == null) return;

            foreach (var product in _database.products)
            {
                if (product == null || string.IsNullOrEmpty(product.productId)) continue;

                var config = new ProductConfig
                {
                    productId = product.productId,
                    priceUsd = product.fallbackPriceUsd,
                    rewardString = product.fallbackRewardString,
                    isSubscription = product.isSubscription,
                    discountPercent = 0
                };

                config.parsedRewards = ParseRewards(product.fallbackRewardString);
                _configs[product.productId] = config;
            }
        }

        private List<RewardEntry> ParseRewards(string rewardStr)
        {
            var list = new List<RewardEntry>();
            if (string.IsNullOrEmpty(rewardStr)) return list;

            var parts = rewardStr.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^([\d,]+)\s+(.+)$");
                if (match.Success)
                {
                    string numStr = match.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(numStr, out int amount))
                    {
                        list.Add(new RewardEntry(match.Groups[2].Value.Trim(), amount));
                    }
                }
            }
            return list;
        }
    }
}
