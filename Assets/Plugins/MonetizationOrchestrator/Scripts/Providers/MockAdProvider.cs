#pragma warning disable 67

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if HAS_UGUI
using UnityEngine.UI;
#endif

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncResultTask = Cysharp.Threading.Tasks.UniTask<bool>;
#else
using AsyncResultTask = System.Threading.Tasks.Task<bool>;
#endif

namespace UniversalMonetization
{
    public class MockAdProvider : IAdProvider
    {
        public bool IsInitialized { get; private set; }

        // Lifecycle Events
        public event Action OnProviderInitialized;
        public event Action<string> OnInitializationFailed;

        // Rewarded Events
        public event Action OnRewardedAdLoaded;
        public event Action<string> OnRewardedAdLoadFailed;
        public event Action OnRewardedAdDisplayed;
        public event Action<string> OnRewardedAdDisplayFailed;
        public event Action OnRewardedAdClicked;
        public event Action OnRewardedAdRewarded;
        public event Action OnRewardedAdClosed;

        // Interstitial Events
        public event Action OnInterstitialAdLoaded;
        public event Action<string> OnInterstitialAdLoadFailed;
        public event Action OnInterstitialAdDisplayed;
        public event Action<string> OnInterstitialAdDisplayFailed;
        public event Action OnInterstitialAdClicked;
        public event Action OnInterstitialAdClosed;

        // Banner Events
        public event Action OnBannerAdLoaded;
        public event Action<string> OnBannerAdLoadFailed;
        public event Action OnBannerAdDisplayed;

        public event Action<AdImpressionData> OnImpressionDataReady;

        private MockAdCanvasController _uiInstance;
        private bool _isRewardedLoaded;
        private bool _isInterstitialLoaded;
        private bool _isBannerLoaded;
        private bool _bannerVisible;
        private bool _bannerAtTop;
        private GameObject _bannerGo;

        public void Configure(string rewardedId, string interstitialId, string bannerId, bool enableTestSuite, bool bannerAtTop)
        {
            _bannerAtTop = bannerAtTop;
            Debug.Log("[MockAdProvider] ⚙️ Configuring Mock Provider.");
        }

        public void Initialize(string appKey)
        {
            Debug.Log("[MockAdProvider] ⚡ Initializing Mock Provider (Simulated).");
            
            // Simulate initialization delay
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await MonetizationTimer.DelayAsync(1f);
            IsInitialized = true;
            Debug.Log("<color=green>[MockAdProvider] ✅ Mock Ad SDK Initialized successfully!</color>");
            OnProviderInitialized?.Invoke();
        }

        private void EnsureUIInstance()
        {
            if (_uiInstance != null) return;

            // Load from Resources dynamically for "Zero-Setup"
            var prefab = Resources.Load<GameObject>("MockAdCanvas");
            if (prefab == null)
            {
                Debug.LogError("[MockAdProvider] ❌ MockAdCanvas prefab not found in 'Resources' directory! Please ensure it is present.");
                return;
            }

            var go = UnityEngine.Object.Instantiate(prefab);
            UnityEngine.Object.DontDestroyOnLoad(go);
            _uiInstance = go.GetComponent<MockAdCanvasController>();

            if (_uiInstance == null)
            {
                Debug.LogError("[MockAdProvider] ❌ MockAdCanvasController component missing from the MockAdCanvas prefab!");
            }

#if HAS_UGUI
            // Ensure EventSystem exists in the scene so UI buttons can be clicked
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();

                // Check for New Input System module assembly
                var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemGo.AddComponent(inputModuleType);
                    Debug.Log("[MockAdProvider] 🌐 Created EventSystem with New Input System UI module.");
                }
                else
                {
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("[MockAdProvider] 🌐 Created EventSystem with legacy Standalone Input module.");
                }

                UnityEngine.Object.DontDestroyOnLoad(eventSystemGo);
            }
#endif
        }

        // ========================
        // REWARDED ADS
        // ========================
        public bool IsRewardedAdReady() => IsInitialized && _isRewardedLoaded;

        public void LoadRewardedAd()
        {
            if (!IsInitialized || _isRewardedLoaded) return;
            _ = LoadRewardedAsync();
        }

        private async Task LoadRewardedAsync()
        {
            await MonetizationTimer.DelayAsync(0.8f);
            _isRewardedLoaded = true;
            Debug.Log("[MockAdProvider] 📺 Rewarded Ad Loaded (Simulated)");
            OnRewardedAdLoaded?.Invoke();
        }

        public void ShowRewardedAd(string placementId = null)
        {
            if (!IsRewardedAdReady())
            {
                Debug.LogWarning("[MockAdProvider] ⚠️ Rewarded ad not ready.");
                OnRewardedAdDisplayFailed?.Invoke("ad_not_ready");
                return;
            }

            EnsureUIInstance();
            if (_uiInstance == null) return;

            _isRewardedLoaded = false;
            OnRewardedAdDisplayed?.Invoke();

            _uiInstance.ShowAd(
                isRewarded: true,
                duration: 4,
                onRewarded: () =>
                {
                    Debug.Log("<color=green>[MockAdProvider] ✅ Rewarded Ad Completed. Granting simulated reward.</color>");
                    OnRewardedAdRewarded?.Invoke();
                    
                    // Simulate ILRD Impression Data
                    OnImpressionDataReady?.Invoke(new AdImpressionData
                    {
                        AdNetwork = "MockNetwork",
                        AdUnit = "Rewarded",
                        InstanceName = placementId ?? "default_rewarded",
                        Revenue = 0.015,
                        Currency = "USD"
                    });
                },
                onClosed: () =>
                {
                    OnRewardedAdClosed?.Invoke();
                }
            );
        }

        public bool IsRewardedPlacementCapped(string placementId) => false;

        public async AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default)
        {
            if (!IsRewardedAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(false)))
            {
                Action rewardHandler = () => tcs.TrySetResult(true);
                Action closeHandler = () => tcs.TrySetResult(false);

                OnRewardedAdRewarded += rewardHandler;
                OnRewardedAdClosed += closeHandler;

                ShowRewardedAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    OnRewardedAdRewarded -= rewardHandler;
                    OnRewardedAdClosed -= closeHandler;
                }
            }
        }

        // ========================
        // INTERSTITIAL ADS
        // ========================
        public bool IsInterstitialAdReady() => IsInitialized && _isInterstitialLoaded;

        public void LoadInterstitialAd()
        {
            if (!IsInitialized || _isInterstitialLoaded) return;
            _ = LoadInterstitialAsync();
        }

        private async Task LoadInterstitialAsync()
        {
            await MonetizationTimer.DelayAsync(0.8f);
            _isInterstitialLoaded = true;
            Debug.Log("[MockAdProvider] 📂 Interstitial Ad Loaded (Simulated)");
            OnInterstitialAdLoaded?.Invoke();
        }

        public void ShowInterstitialAd(string placementId = null)
        {
            if (!IsInterstitialAdReady())
            {
                Debug.LogWarning("[MockAdProvider] ⚠️ Interstitial ad not ready.");
                OnInterstitialAdDisplayFailed?.Invoke("ad_not_ready");
                return;
            }

            EnsureUIInstance();
            if (_uiInstance == null) return;

            _isInterstitialLoaded = false;
            OnInterstitialAdDisplayed?.Invoke();

            _uiInstance.ShowAd(
                isRewarded: false,
                duration: 3,
                onRewarded: null,
                onClosed: () =>
                {
                    OnInterstitialAdClosed?.Invoke();
                    
                    // Simulate ILRD Impression Data
                    OnImpressionDataReady?.Invoke(new AdImpressionData
                    {
                        AdNetwork = "MockNetwork",
                        AdUnit = "Interstitial",
                        InstanceName = placementId ?? "default_interstitial",
                        Revenue = 0.008,
                        Currency = "USD"
                    });
                }
            );
        }

        public async AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default)
        {
            if (!IsInterstitialAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(true)))
            {
                Action closeHandler = () => tcs.TrySetResult(true);
                OnInterstitialAdClosed += closeHandler;

                ShowInterstitialAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    OnInterstitialAdClosed -= closeHandler;
                }
            }
        }

        // ========================
        // BANNERS
        // ========================
        public void LoadBanner(string unitId)
        {
            if (_isBannerLoaded) return;
            _isBannerLoaded = true;
            Debug.Log("[MockAdProvider] 🏷️ Banner Loaded (Simulated)");
            OnBannerAdLoaded?.Invoke();
        }

        public void ShowBanner()
        {
            _bannerVisible = true;
            Debug.Log("[MockAdProvider] 📺 Showing Banner Ad (Simulated)");
            
#if HAS_UGUI
            if (_bannerGo == null)
            {
                _bannerGo = new GameObject("MockBannerCanvas");
                UnityEngine.Object.DontDestroyOnLoad(_bannerGo);
                
                Canvas canvas = _bannerGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 8999;
                
                CanvasScaler scaler = _bannerGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);

                GameObject bg = new GameObject("BannerBackground");
                bg.transform.SetParent(_bannerGo.transform, false);
                Image img = bg.AddComponent<Image>();
                img.color = new Color(0.15f, 0.16f, 0.22f, 1f); // Slightly lighter slate
                
                Outline border = bg.AddComponent<Outline>();
                border.effectColor = new Color(0.9f, 0.6f, 0.15f, 1f); // Gold border
                border.effectDistance = new Vector2(4, -4);
                
                RectTransform rt = bg.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(1080, 150); // Standard 50dp equivalent
                
                if (_bannerAtTop)
                {
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.anchoredPosition = Vector2.zero;
                }
                
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(bg.transform, false);
                
                Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpType != null)
                {
                    Component tmpText = textGo.AddComponent(tmpType);
                    tmpType.GetProperty("text").SetValue(tmpText, "TEST BANNER AD (320x50)");
                    tmpType.GetProperty("fontSize").SetValue(tmpText, 32f);
                    
                    Type alignType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
                    if (alignType != null)
                    {
                        try
                        {
                            var centerValue = Enum.Parse(alignType, "Center");
                            tmpType.GetProperty("alignment").SetValue(tmpText, centerValue);
                        }
                        catch
                        {
                            // Safe fallback if parsing fails
                        }
                    }
                    tmpType.GetProperty("color").SetValue(tmpText, Color.white);
                }
                else
                {
                    Text txt = textGo.AddComponent<Text>();
                    txt.text = "TEST BANNER AD (320x50)";
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.fontSize = 42;
                    txt.fontStyle = FontStyle.Bold;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.white; // White text for better contrast against gold border
                }
                
                RectTransform txtRt = textGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
            }
            _bannerGo.SetActive(true);
#endif
            OnBannerAdDisplayed?.Invoke();
        }

        public void HideBanner()
        {
            _bannerVisible = false;
            Debug.Log("[MockAdProvider] 📺 Hiding Banner Ad (Simulated)");
            if (_bannerGo != null) _bannerGo.SetActive(false);
        }

        public int GetBannerHeight() => _bannerVisible ? 50 : 0;

        // ========================
        // MISC / DEBUG / LIFECYCLE
        // ========================
        public void LaunchTestSuite()
        {
            Debug.Log("[MockAdProvider] 🛠️ Launching Test Suite... Not supported in Mock Mode.");
        }

        public void SetConsent(bool consent)
        {
            Debug.Log($"[MockAdProvider] 🛡️ Regulatory consent set to: {consent}");
        }

        public void SetMetaData(string key, string value)
        {
            Debug.Log($"[MockAdProvider] 🛡️ Ad Metadata key '{key}' set to: '{value}'");
        }

        public void OnApplicationPause(bool isPaused)
        {
            Debug.Log($"[MockAdProvider] 🔄 Application Pause State: {isPaused}");
        }

        public void Dispose()
        {
            if (_uiInstance != null)
            {
                UnityEngine.Object.Destroy(_uiInstance.transform.root.gameObject);
                _uiInstance = null;
            }
        }
    }
}
