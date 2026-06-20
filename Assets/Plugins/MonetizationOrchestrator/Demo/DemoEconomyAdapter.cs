using UnityEngine;
using UniversalMonetization;

namespace UniversalMonetization.Demo
{
    /// <summary>
    /// This is an EXAMPLE script demonstrating how to connect the SDK's global events 
    /// to your game's specific economy or saving system.
    /// 
    /// Usage:
    /// 1. Copy this script into your game's scripts folder.
    /// 2. Rename it to "MonetizationEconomyAdapter" or similar.
    /// 3. Replace the Debug.Log statements with your actual GameManager or PlayerPrefs saving logic.
    /// 4. Attach it to a persistent GameObject in your first scene.
    /// </summary>
    public class DemoEconomyAdapter : MonoBehaviour
    {
        private void OnEnable()
        {
            // Subscribe to global reward and purchase events
            AdManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
            IAPManager.OnPurchaseCompleted += HandlePurchaseCompleted;
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks when this object is destroyed
            AdManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;
            IAPManager.OnPurchaseCompleted -= HandlePurchaseCompleted;
        }

        /// <summary>
        /// Called automatically when a player finishes watching a Rewarded Ad.
        /// </summary>
        /// <param name="rewardAmount">The amount configured in your Ad Network / Remote Config.</param>
        private void HandleRewardedAdCompleted(int rewardAmount)
        {
            Debug.Log($"[DemoEconomyAdapter] 🎁 Player watched an ad! Giving them {rewardAmount} coins.");
            
            // TODO: Replace with your game's logic!
            // Example:
            // GameManager.Instance.AddCoins(rewardAmount);
            // PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + rewardAmount);
        }

        /// <summary>
        /// Called automatically when a player successfully completes an In-App Purchase.
        /// </summary>
        /// <param name="result">Contains ProductId and TransactionId.</param>
        private void HandlePurchaseCompleted(IapPurchaseResult result)
        {
            Debug.Log($"[DemoEconomyAdapter] 🛒 Player bought product: {result.ProductId}. Transaction ID: {result.TransactionId}");
            
            // TODO: Replace with your game's logic!
            // Example:
            /*
            switch (result.ProductId)
            {
                case "com.mygame.starterpack":
                    GameManager.Instance.AddGems(100);
                    GameManager.Instance.RemoveAds();
                    break;
                case "com.mygame.noads":
                    GameManager.Instance.RemoveAds();
                    break;
            }
            */
        }
    }
}
