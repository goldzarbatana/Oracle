using UnityEngine;
#if HAS_UGUI
using UnityEngine.UI;
#endif
using System.Threading.Tasks;

namespace UniversalMonetization.Demo
{
    /// <summary>
    /// A simple demonstration helper script to test the MonetizationOrchestrator features.
    /// Wire this up to the MonetizationDemoCanvas prefab.
    /// </summary>
#if HAS_UGUI
    public class MonetizationTest : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button rewardedButton;
        [SerializeField] private Button interstitialButton;
        [SerializeField] private Button iapButton;
        
        [Header("LiveOps UI")]
        [Tooltip("Optional: Assign a UI Text component here to display LiveOps values from Google Sheets")]
        [SerializeField] private Text liveOpsDataText;

        private async void Start()
        {
            if (rewardedButton != null)
                rewardedButton.onClick.AddListener(() => _ = TriggerRewardedAd());

            if (interstitialButton != null)
                interstitialButton.onClick.AddListener(() => _ = TriggerInterstitialAd());

            if (iapButton != null)
                iapButton.onClick.AddListener(TriggerIAP);
            
            Debug.Log("[MonetizationTest] Test panel active. UI Button listeners successfully registered.");

            // Auto-show banner for demonstration purposes once the SDK is ready
            while (MonetizationOrchestrator.Instance == null || !MonetizationOrchestrator.Instance.Ads.IsInitialized)
            {
                await Task.Delay(100);
            }
            
            MonetizationOrchestrator.Instance.Ads.ShowBanner();
            
            // Show LiveOps magic on screen
            _ = CreateLiveOpsOverlayAsync();
        }

        private async Task CreateLiveOpsOverlayAsync()
        {
            RemoteConfigManager rcm = UnityEngine.Object.FindFirstObjectByType<RemoteConfigManager>();
            if (rcm == null) return;

            // Wait until data is loaded
            int waitTime = 0;
            while (rcm.ConfigData.Count == 0 && waitTime < 5000)
            {
                await Task.Delay(200);
                waitTime += 200;
            }

            if (rcm.ConfigData.Count == 0) return; // Fetch failed or no data

            string display = "<color=#38ff8e><b>☁️ LiveOps Connected!</b></color>\n<size=30><i>(Values pulled from Google Sheets)</i></size>\n\n";
            display += $"<color=white>interstitial_interval:</color> <color=#ffda38>{rcm.GetStringConfig("interstitial_min_interval")}</color>s\n";
            display += $"<color=white>rewarded_cooldown:</color> <color=#ffda38>{rcm.GetStringConfig("rewarded_cooldown_seconds")}</color>s\n";
            display += $"<color=white>initial_ad_delay:</color> <color=#ffda38>{rcm.GetStringConfig("initial_ad_delay")}</color>s\n";
            display += $"<color=white>game_over_freq:</color> <color=#ffda38>{rcm.GetStringConfig("interstitial_game_over_freq")}</color>\n";
            display += $"<color=white>level_complete_freq:</color> <color=#ffda38>{rcm.GetStringConfig("interstitial_level_complete_freq")}</color>\n";
            display += $"<color=white>rewarded_coins:</color> <color=#ffda38>{rcm.GetStringConfig("rewarded_coins")}</color>\n";
            
            if (liveOpsDataText != null)
            {
                liveOpsDataText.text = display;
            }
            else
            {
                Debug.Log($"[MonetizationTest] LiveOps Data downloaded but no UI Text assigned to display it:\n{display.Replace("<color=white>", "").Replace("</color>", "")}");
            }
        }

        private async Task TriggerRewardedAd()
        {
            Debug.Log("[MonetizationTest] Requesting Rewarded Ad for 100 coins...");
            if (MonetizationOrchestrator.Instance == null) return;
            var outcome = await MonetizationOrchestrator.Instance.Ads.ShowRewardedAdAsync(100);

            if (outcome.IsSuccess)
            {
                Debug.Log($"<color=green>[MonetizationTest] ✅ Success! Awarding player {outcome.RewardAmount} coins.</color>");
                AdLoadingOverlay.ShowToast($"🎁 Reward Claimed!\n+{outcome.RewardAmount} Coins", new Color(0.18f, 0.8f, 0.44f, 1f));
            }
            else
            {
                Debug.LogWarning($"[MonetizationTest] ⚠️ Ad failure or skipped. Reason: {outcome.FailureReason}");
                AdLoadingOverlay.ShowToast($"⚠️ Ad Skipped\n{outcome.FailureReason}", new Color(0.9f, 0.6f, 0.15f, 1f));
            }
        }

        private async Task TriggerInterstitialAd()
        {
            Debug.Log("[MonetizationTest] Requesting Interstitial Ad...");
            if (MonetizationOrchestrator.Instance == null) return;
            var outcome = await MonetizationOrchestrator.Instance.Ads.ShowInterstitialAdAsync("test_placement");

            if (outcome.IsSuccess)
            {
                Debug.Log("<color=green>[MonetizationTest] ✅ Interstitial played and closed.</color>");
            }
            else
            {
                Debug.LogWarning($"[MonetizationTest] ⚠️ Interstitial skipped. Reason: {outcome.FailureReason}");
            }
        }

        private void TriggerIAP()
        {
            string productId = "com.game.offer.bundle";
            Debug.Log($"[MonetizationTest] Requesting purchase of: {productId}...");
            if (MonetizationOrchestrator.Instance == null) return;
            MonetizationOrchestrator.Instance.Iap.BuyProduct(productId);
        }

        private void OnEnable()
        {
            // Subscribe to generic IAP events
            IAPManager.OnPurchaseCompleted += HandlePurchaseSuccess;
            IAPManager.OnPurchaseError += HandlePurchaseFailed;
        }

        private void OnDisable()
        {
            // Unsubscribe to avoid memory leaks
            IAPManager.OnPurchaseCompleted -= HandlePurchaseSuccess;
            IAPManager.OnPurchaseError -= HandlePurchaseFailed;
        }

        private void HandlePurchaseSuccess(IapPurchaseResult purchase)
        {
            Debug.Log($"<color=green>[MonetizationTest] 💰 IAP Purchase Completed!</color>\n" +
                      $"Product: {purchase.ProductId}\n" +
                      $"Transaction ID: {purchase.TransactionId}");
                      
            AdLoadingOverlay.ShowToast($"💰 Purchase Completed!\n{purchase.ProductId}", new Color(0.18f, 0.8f, 0.44f, 1f));
        }

        private void HandlePurchaseFailed(string productId, string error)
        {
            Debug.LogError($"[MonetizationTest] ❌ IAP Purchase Failed for product: {productId}. Error: {error}");
            AdLoadingOverlay.ShowToast($"❌ Purchase Failed!\n{productId}", new Color(0.9f, 0.3f, 0.3f, 1f));
        }
    }
#endif
}
