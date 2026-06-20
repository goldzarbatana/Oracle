using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalMonetization
{
    [CreateAssetMenu(fileName = "NewShopOffer", menuName = "Monetization/Shop Offer Bundle")]
    public class ShopOfferSO : ScriptableObject
    {
        [Header("General Info")]
        public string offerId = "com.game.offer.bundle";
        public string iapProductId = "com.game.iap.bundle";
        public string displayTitle = "Starter Pack";
        [TextArea(2, 4)]
        public string displayDescription = "Get a massive jump start with coins and boosts!";
        
        [Header("Product Type")]
        public IapProductType productType = IapProductType.Consumable;
        public bool oneTimePurchase = false;

        [Header("Pricing Falls")]
        [Tooltip("Fallback price formatted as text, displayed if store metadata failed to fetch.")]
        public string fallbackPriceDisplay = "$4.99";
        public float fallbackPriceAmount = 4.99f;

        [Header("Rewards Payload (Generic Layout)")]
        public int coinAmount = 10000;
        
        [Tooltip("Custom reward definitions that your inventory manager can process upon successful purchase callback.")]
        public List<CustomRewardPayload> customRewards = new();
    }

    [Serializable]
    public struct CustomRewardPayload
    {
        public string rewardType; // e.g. "Shield", "Potion", "Skin"
        public string rewardId;   // e.g. "shield_basic", "gold_skin"
        public int amount;
    }
}
