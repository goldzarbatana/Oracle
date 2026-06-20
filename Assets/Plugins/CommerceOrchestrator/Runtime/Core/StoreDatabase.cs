using System;
using System.Collections.Generic;
using UnityEngine;

namespace CommerceOrchestrator.Core
{
    [Serializable]
    public class ProductLocalInfo
    {
        [Tooltip("Ідентифікатор продукту в Google Play / App Store")]
        public string productId;

        [Tooltip("Ключ локалізації для імені товару")]
        public string itemNameKey;

        [Tooltip("Ключ локалізації для опису товару")]
        public string itemDescriptionKey;

        [Tooltip("Спрайт-іконка товару для відображення в UI")]
        public Sprite itemIcon;

        [Tooltip("Дефолтна ціна в USD (використовується як фоллбек)")]
        public float fallbackPriceUsd = 0.99f;

        [Tooltip("Ціна в м'якій валюті (монетах), якщо купується не за реальні гроші")]
        public int fallbackPriceCoins = 0;

        [Tooltip("Дефолтний рядок нагород (наприклад, '100 Quants + 50 Horas')")]
        public string fallbackRewardString;

        [Tooltip("Чи є цей товар періодичною підпискою")]
        public bool isSubscription = false;

        [Tooltip("Чи є цей товар одноразовою покупкою")]
        public bool isOneTimePurchase = false;
    }

    [CreateAssetMenu(fileName = "StoreDatabase", menuName = "Commerce Orchestrator/Store Database", order = 1)]
    public class StoreDatabase : ScriptableObject
    {
        public List<ProductLocalInfo> products = new List<ProductLocalInfo>();

        public ProductLocalInfo GetProduct(string productId)
        {
            if (products == null) return null;
            return products.Find(p => p != null && p.productId == productId);
        }
    }
}
