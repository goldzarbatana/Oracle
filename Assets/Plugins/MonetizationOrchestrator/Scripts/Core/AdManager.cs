using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncRewardedResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.RewardedAdResult>;
using AsyncInterstitialResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.InterstitialAdResult>;
#else
using AsyncRewardedResult = System.Threading.Tasks.Task<UniversalMonetization.RewardedAdResult>;
using AsyncInterstitialResult = System.Threading.Tasks.Task<UniversalMonetization.InterstitialAdResult>;
#endif

namespace UniversalMonetization
{
    [DefaultExecutionOrder(-50)]
    public class AdManager : MonoBehaviour, IAdManagerService
    {
        public static AdManager Instance { get; private set; }

        [Header("Platform Credentials")]
        [SerializeField] private string androidAppKey = "android_app_key";
        [SerializeField] private string iosAppKey = "ios_app_key";
        
        [Header("Android Ad Unit IDs")]
        [SerializeField] private string androidRewardedId = "Rewarded_Android";
        [SerializeField] private string androidInterstitialId = "Interstitial_Android";
        [SerializeField] private string androidBannerId = "Banner_Android";

        [Header("iOS Ad Unit IDs")]
        [SerializeField] private string iosRewardedId = "Rewarded_iOS";
        [SerializeField] private string iosInterstitialId = "Interstitial_iOS";
        [SerializeField] private string iosBannerId = "Banner_iOS";

        [Space(10)]
        [SerializeField] private bool enableBanners = true;

        private string ActiveRewardedId => Application.platform == RuntimePlatform.IPhonePlayer ? iosRewardedId : androidRewardedId;
        private string ActiveInterstitialId => Application.platform == RuntimePlatform.IPhonePlayer ? iosInterstitialId : androidInterstitialId;
        private string ActiveBannerId => Application.platform == RuntimePlatform.IPhonePlayer ? iosBannerId : androidBannerId;

        [Header("Pacing & UX Cooldowns")]
        [SerializeField] private float minSecondsBetweenInterstitials = 45f;
        [SerializeField] private float rewardedToInterstitialCooldown = 5f;
        [SerializeField] private float sceneWarmupDelay = 5f;
        [SerializeField] private float minSecondsBetweenLoadRequests = 8f;

        [Header("Pacing Game Counters")]
        [SerializeField] private int gameOverAdFrequency = 3;
        [SerializeField] private int levelCompleteAdFrequency = 2;

        [Header("Aesthetics")]
        [SerializeField] private bool bannerAtTop = false;

        [Header("Debug Settings")]
        [SerializeField] private bool useMockProviderInEditor = true;
        [SerializeField] private bool enableTestSuite = false;
        [SerializeField] private bool verboseAdLogs = true;

        // --- Core States ---
        public bool IsInitialized => _adProvider != null && _adProvider.IsInitialized;
        public bool IsAdShowing { get; private set; }
        public bool AreAdsRemoved { get; private set; }

        public bool IsRewardedAdReady => _adProvider != null && _adProvider.IsRewardedAdReady();
        public bool IsInterstitialAdReady => _adProvider != null && _adProvider.IsInterstitialAdReady();

        // --- C# Event Callbacks ---
        public static event Action OnAdStarted;
        public static event Action<int> OnRewardedAdCompleted; // returns reward amount
        public static event Action<string> OnRewardedAdFailed; // returns failure reason
        public static event Action OnInterstitialAdShown;
        public static event Action<bool> OnAdsRemovedChanged;
        public static event Action<float> OnBannerHeightChanged;
        public static event Action<AdImpressionData> OnImpressionDataTracked;

        // --- IAdManagerService Events ---
        public event Action OnSDKInitialized;
        public event Action<string> OnSDKInitializationFailed;
        public event Action<AdImpressionData> OnImpressionDataReceived;

        // --- Dependencies ---
        private IAdProvider _adProvider;
        private AdLoadGuard _adGuard;
        private AdTimerService _timerService;

        // --- Async Flow Synchronization ---
        private TaskCompletionSource<RewardedAdResult> _rewardedCompletionSource;
        private TaskCompletionSource<InterstitialAdResult> _interstitialCompletionSource;
        private CancellationTokenSource _adFlowCts;
        private CancellationTokenSource _loopCts;

        private int _gameOverCount = 0;
        private float _lastBannerHeight = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Clear static events for Domain Reload support
                OnAdStarted = null;
                OnRewardedAdCompleted = null;
                OnRewardedAdFailed = null;
                OnInterstitialAdShown = null;
                OnAdsRemovedChanged = null;
                OnBannerHeightChanged = null;
                OnImpressionDataTracked = null;

                DontDestroyOnLoad(gameObject);
                InitializeInfrastructure();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeAdSDK();
        }

        private void OnDestroy()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _adFlowCts?.Cancel();
            _adFlowCts?.Dispose();
            _adProvider?.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsInitialized)
            {
                ForceRefreshBannerLayout();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _adProvider?.OnApplicationPause(paused);
            if (!paused && IsInitialized)
            {
                ForceRefreshBannerLayout();
            }
        }

        // ========================
        // INITIALIZATION
        // ========================
        private void InitializeInfrastructure()
        {
            _adGuard = new AdLoadGuard
            {
                MinSecondsBetweenLoadRequests = minSecondsBetweenLoadRequests
            };

            _timerService = new AdTimerService();
            _timerService.Initialize(minSecondsBetweenInterstitials, rewardedToInterstitialCooldown, sceneWarmupDelay);
        }

        private void InitializeAdSDK()
        {
            // Pick Provider depending on Editor vs Platforms and defines
#if UNITY_EDITOR
            if (useMockProviderInEditor)
            {
                _adProvider = new MockAdProvider();
            }
#endif
            if (_adProvider == null)
            {
#if MONETIZATION_APPLOVIN_ENABLED
                _adProvider = new AppLovinProvider();
#elif MONETIZATION_LEVELPLAY
                _adProvider = new LevelPlayAdProvider();
#elif MONETIZATION_UNITYADS
                _adProvider = new UnityAdsProvider();
#elif MONETIZATION_ADMOB
                _adProvider = new AdMobProvider();
#else
                Log("⚠️ No ad SDK script defines set (MONETIZATION_APPLOVIN / MONETIZATION_LEVELPLAY / MONETIZATION_UNITYADS / MONETIZATION_ADMOB). Falling back to MockAdProvider.");
                _adProvider = new MockAdProvider();
#endif
            }

            // Wire up callbacks
            _adProvider.OnProviderInitialized += HandleProviderInitialized;
            _adProvider.OnInitializationFailed += HandleProviderInitializationFailed;

            _adProvider.OnRewardedAdLoaded += () => { _adGuard.SetInFlight("rewarded", false); _adGuard.ResetBackoff("rewarded"); };
            _adProvider.OnRewardedAdLoadFailed += err => { _adGuard.SetInFlight("rewarded", false); _adGuard.ApplyFailureBackoff("rewarded", err, LogWarning); };
            _adProvider.OnRewardedAdDisplayed += () => { IsAdShowing = true; OnAdStarted?.Invoke(); _timerService.TrackRewardedDisplayed(); };
            _adProvider.OnRewardedAdDisplayFailed += err => HandleRewardedDisplayFailed(err);
            _adProvider.OnRewardedAdRewarded += () => HandleRewardedEarned();
            _adProvider.OnRewardedAdClosed += () => HandleRewardedClosed();

            _adProvider.OnInterstitialAdLoaded += () => { _adGuard.SetInFlight("interstitial", false); _adGuard.ResetBackoff("interstitial"); };
            _adProvider.OnInterstitialAdLoadFailed += err => { _adGuard.SetInFlight("interstitial", false); _adGuard.ApplyFailureBackoff("interstitial", err, LogWarning); };
            _adProvider.OnInterstitialAdDisplayed += () => { IsAdShowing = true; OnAdStarted?.Invoke(); _timerService.TrackInterstitialDisplayed(); };
            _adProvider.OnInterstitialAdDisplayFailed += err => HandleInterstitialDisplayFailed(err);
            _adProvider.OnInterstitialAdClosed += () => HandleInterstitialClosed();

            _adProvider.OnBannerAdLoaded += () => { _adGuard.SetInFlight("banner", false); _adGuard.ResetBackoff("banner"); UpdateBannerHeight(_adProvider.GetBannerHeight()); };
            _adProvider.OnBannerAdLoadFailed += err => { _adGuard.SetInFlight("banner", false); _adGuard.ApplyFailureBackoff("banner", err, LogWarning); UpdateBannerHeight(0f); };

            _adProvider.OnImpressionDataReady += data => { OnImpressionDataTracked?.Invoke(data); OnImpressionDataReceived?.Invoke(data); };

            string appKey = Application.platform == RuntimePlatform.IPhonePlayer ? iosAppKey : androidAppKey;
            _adProvider.Configure(ActiveRewardedId, ActiveInterstitialId, ActiveBannerId, enableTestSuite, bannerAtTop);
            _adProvider.Initialize(appKey);
        }

        private void HandleProviderInitialized()
        {
            Log("✅ Ad Provider successfully Initialized! Starting preloads...");
            
            // Kick off self-healing cache loop
            _loopCts = new CancellationTokenSource();
            _ = StartAvailabilityLoop(_loopCts.Token);

            // Perform initial manual preloads
            PreloadAll();

            OnSDKInitialized?.Invoke();
        }

        private void HandleProviderInitializationFailed(string error)
        {
            LogError($"❌ Ad Provider failed to initialize: {error}");
            OnSDKInitializationFailed?.Invoke(error);
        }

        private void PreloadAll()
        {
            if (!IsRewardedAdReady && !_adGuard.IsInFlight("rewarded"))
                LoadRewardedAd("init");
            
            if (!IsInterstitialAdReady && !_adGuard.IsInFlight("interstitial"))
                LoadInterstitialAd("init");

            LoadBannerAd("init");
        }

        // ========================
        // PRELOAD LOOP (Self-Healing Cache)
        // ========================
        private async Task StartAvailabilityLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (IsInitialized && !AreAdsRemoved && !IsAdShowing)
                    {
                        if (!IsRewardedAdReady && !_adGuard.IsInFlight("rewarded") && _adGuard.GetBackoffUntil("rewarded") < Time.time)
                            LoadRewardedAd("auto_loop");

                        if (!IsInterstitialAdReady && !_adGuard.IsInFlight("interstitial") && _adGuard.GetBackoffUntil("interstitial") < Time.time)
                            LoadInterstitialAd("auto_loop");
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogError($"Error in ad preload loop: {ex.Message}");
                }

                await MonetizationTimer.DelayAsync(2f, ct);
            }
        }

        // ========================
        // AD LOADING LOGIC
        // ========================
        public void LoadRewardedAd(string source = "manual")
        {
            if (AreAdsRemoved || IsAdShowing) return;
            if (!_adGuard.CanRequestLoad("rewarded", IsInitialized, AreAdsRemoved, false, LogWarning)) return;
            if (IsRewardedAdReady) return;

            _adGuard.RecordAttempt("rewarded");
            Log($"Requesting Rewarded Ad load (source: {source})");
            _adProvider.LoadRewardedAd();
        }

        public void LoadInterstitialAd(string source = "manual")
        {
            if (AreAdsRemoved || IsAdShowing) return;
            if (!_adGuard.CanRequestLoad("interstitial", IsInitialized, AreAdsRemoved, false, LogWarning)) return;
            if (IsInterstitialAdReady) return;

            _adGuard.RecordAttempt("interstitial");
            Log($"Requesting Interstitial Ad load (source: {source})");
            _adProvider.LoadInterstitialAd();
        }

        public void LoadBannerAd(string source = "manual")
        {
            if (!enableBanners) return;
            if (!_adGuard.CanRequestLoad("banner", IsInitialized, AreAdsRemoved, false, LogWarning)) return;

            _adGuard.RecordAttempt("banner");
            Log($"Requesting Banner Ad load (source: {source})");
            _adProvider.LoadBanner(ActiveBannerId);
        }

        // ========================
        // REWARDED SHOW FLOW (ASYNC Spinner & Auto-Buffering Buffer)
        // ========================
        private const float AdBufferTimeoutSeconds = 8f;
        private bool _rewardEarnedInFlow;

        /// <summary>
        /// Shows a Rewarded ad. If the ad is not currently loaded, it shows a buffering overlay spinner 
        /// and waits up to 8s for it to load before giving up. Buttons should ALWAYS remain interactable and call this directly!
        /// </summary>
        public async AsyncRewardedResult ShowRewardedAdAsync(int coinsRewardAmount = 0, CancellationToken ct = default)
        {
            if (AreAdsRemoved)
            {
                OnRewardedAdCompleted?.Invoke(coinsRewardAmount);
                return RewardedAdResult.CreateSuccess(coinsRewardAmount);
            }

            if (IsAdShowing) return RewardedAdResult.CreateFailure(AdResult.AdShowing, "Another ad is showing.");
            if (!IsInitialized) 
            {
                LogError("Cannot show ad. SDK is not initialized! Did you place MonetizationOrchestrator in your scene?");
                return RewardedAdResult.CreateFailure(AdResult.SDKNotInitialized, "SDK not initialized.");
            }

            // Set showing/buffering state immediately to prevent parallel requests during buffering
            IsAdShowing = true;

            // 1. Buffer load if not ready
            if (!IsRewardedAdReady)
            {
                Log("Rewarded ad not cached. Displaying buffering overlay...");
                AdLoadingOverlay.Show();
                LoadRewardedAd("buffering_trigger");

                float elapsed = 0f;
                try
                {
                    while (elapsed < AdBufferTimeoutSeconds)
                    {
                        if (ct.IsCancellationRequested) break;
                        if (IsRewardedAdReady) break;
                        await MonetizationTimer.DelayAsync(0.2f, ct);
                        elapsed += 0.2f;
                    }
                }
                catch
                {
                    AdLoadingOverlay.Hide();
                    IsAdShowing = false;
                    throw;
                }

                AdLoadingOverlay.Hide();

                // Yield a safety frame to let canvas closure focus return to the application
                await MonetizationTimer.DelayAsync(0.2f, ct);

                if (!IsRewardedAdReady)
                {
                    LogWarning("Rewarded ad failed to fill during the buffering window.");
                    AdLoadingOverlay.ShowUnavailableToast();
                    IsAdShowing = false;
                    return RewardedAdResult.CreateFailure(AdResult.NotReady, "ad_no_fill_timeout");
                }
            }

            // 2. Play Ad Flow
            _rewardedCompletionSource?.TrySetCanceled();
            _rewardedCompletionSource = new TaskCompletionSource<RewardedAdResult>();

            _adFlowCts?.Cancel();
            _adFlowCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _adFlowCts.Token.Register(() =>
            {
                if (_rewardedCompletionSource != null && !_rewardedCompletionSource.Task.IsCompleted)
                    _rewardedCompletionSource.TrySetResult(RewardedAdResult.CreateFailure(AdResult.Cancelled, "Cancelled by user/ct"));
            });

            _rewardEarnedInFlow = false;

            await Task.Yield();
            _adProvider.ShowRewardedAd(ActiveRewardedId);

            try
            {
                var outcome = await _rewardedCompletionSource.Task;
                if (outcome.IsSuccess)
                {
                    outcome.RewardAmount = coinsRewardAmount;
                    OnRewardedAdCompleted?.Invoke(coinsRewardAmount);
                }
                return outcome;
            }
            catch
            {
                IsAdShowing = false;
                throw;
            }
            finally
            {
                _rewardedCompletionSource = null;
            }
        }

        private void HandleRewardedDisplayFailed(string error)
        {
            IsAdShowing = false;
            AdLoadingOverlay.ShowUnavailableToast();
            CompleteRewardedFlow(RewardedAdResult.CreateFailure(AdResult.Failed, error));
            LoadRewardedAd("display_failed_refill");
        }

        private void HandleRewardedEarned()
        {
            _rewardEarnedInFlow = true;
        }

        private void HandleRewardedClosed()
        {
            IsAdShowing = false;
            if (_rewardEarnedInFlow)
            {
                CompleteRewardedFlow(RewardedAdResult.CreateSuccess(0, ActiveRewardedId));
            }
            else
            {
                OnRewardedAdFailed?.Invoke("closed_without_reward");
                CompleteRewardedFlow(RewardedAdResult.CreateFailure(AdResult.Cancelled, "User closed ad early."));
            }
            LoadRewardedAd("closed_refill");
        }

        private void CompleteRewardedFlow(RewardedAdResult result)
        {
            if (_rewardedCompletionSource != null && !_rewardedCompletionSource.Task.IsCompleted)
            {
                _rewardedCompletionSource.TrySetResult(result);
            }
        }

        // ========================
        // INTERSTITIAL SHOW FLOW (ASYNC Cooldown pacing)
        // ========================
        public async AsyncInterstitialResult ShowInterstitialAdAsync(string placement = null, CancellationToken ct = default)
        {
            if (AreAdsRemoved) return InterstitialAdResult.CreateSuccess();
            if (IsAdShowing) return InterstitialAdResult.CreateFailure(AdResult.AdShowing, "Another ad is showing.");
            if (!IsInitialized) 
            {
                LogError("Cannot show ad. SDK is not initialized! Did you place MonetizationOrchestrator in your scene?");
                return InterstitialAdResult.CreateFailure(AdResult.SDKNotInitialized, "SDK not initialized.");
            }

            // Verify UX timer constraints
            bool ignorePacing = placement == "test_placement";
            if (!ignorePacing)
            {
                if (!_timerService.HasSceneWarmupPassed()) return InterstitialAdResult.CreateFailure(AdResult.NotReady, "Scene warmup period active.");
                if (!_timerService.IsInterstitialReadyByTimer()) return InterstitialAdResult.CreateFailure(AdResult.NotReady, "Pacing interval active.");
                if (!_timerService.IsPastRewardedCooldown()) return InterstitialAdResult.CreateFailure(AdResult.NotReady, "Cooldown after rewarded active.");
            }

            if (!IsInterstitialAdReady)
            {
                LogWarning("Interstitial ad not cached. Refilling and failing fast.");
                LoadInterstitialAd("fail_fast_refill");
                return InterstitialAdResult.CreateFailure(AdResult.NotReady, "Ad not cached.");
            }

            _interstitialCompletionSource?.TrySetCanceled();
            _interstitialCompletionSource = new TaskCompletionSource<InterstitialAdResult>();

            _adFlowCts?.Cancel();
            _adFlowCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _adFlowCts.Token.Register(() =>
            {
                if (_interstitialCompletionSource != null && !_interstitialCompletionSource.Task.IsCompleted)
                    _interstitialCompletionSource.TrySetResult(InterstitialAdResult.CreateFailure(AdResult.Cancelled, "Cancelled by user/ct"));
            });

            IsAdShowing = true;
            _adProvider.ShowInterstitialAd(placement ?? ActiveInterstitialId);

            try
            {
                return await _interstitialCompletionSource.Task;
            }
            finally
            {
                _interstitialCompletionSource = null;
            }
        }

        private void HandleInterstitialDisplayFailed(string error)
        {
            IsAdShowing = false;
            CompleteInterstitialFlow(InterstitialAdResult.CreateFailure(AdResult.Failed, error));
            LoadInterstitialAd("display_failed_refill");
        }

        private void HandleInterstitialClosed()
        {
            IsAdShowing = false;
            OnInterstitialAdShown?.Invoke();
            CompleteInterstitialFlow(InterstitialAdResult.CreateSuccess(ActiveInterstitialId));
            LoadInterstitialAd("closed_refill");
        }

        private void CompleteInterstitialFlow(InterstitialAdResult result)
        {
            if (_interstitialCompletionSource != null && !_interstitialCompletionSource.Task.IsCompleted)
            {
                _interstitialCompletionSource.TrySetResult(result);
            }
        }

        // ========================
        // REMOTE CONFIGURATION
        // ========================
        public void ApplyRemoteConfig(float? minInterstitial = null, float? rewardedCooldown = null, float? warmupDelay = null, int? gameOverFreq = null, int? levelFreq = null)
        {
            if (minInterstitial.HasValue) minSecondsBetweenInterstitials = minInterstitial.Value;
            if (rewardedCooldown.HasValue) rewardedToInterstitialCooldown = rewardedCooldown.Value;
            if (warmupDelay.HasValue) sceneWarmupDelay = warmupDelay.Value;
            if (gameOverFreq.HasValue) gameOverAdFrequency = gameOverFreq.Value;
            if (levelFreq.HasValue) levelCompleteAdFrequency = levelFreq.Value;

            _timerService?.UpdateParameters(minSecondsBetweenInterstitials, rewardedToInterstitialCooldown, sceneWarmupDelay);
            
            Log("🔄 Remote Config applied to pacing parameters.");
        }

        // ========================
        // GAME PACING CONVENIENCE METHODS
        // ========================
        public void TrackGameOverAdPacing()
        {
            _gameOverCount++;
            if (_gameOverCount % gameOverAdFrequency == 0)
            {
                _ = ShowInterstitialAdAsync("game_over");
            }
        }

        public void TrackLevelCompleteAdPacing(int levelNumber)
        {
            if (levelNumber % levelCompleteAdFrequency == 0)
            {
                _ = ShowInterstitialAdAsync("level_complete");
            }
        }

        // ========================
        // BANNERS
        // ========================
        public void ShowBanner()
        {
            if (!enableBanners || AreAdsRemoved || _adProvider == null) return;
            _adProvider.ShowBanner();
        }

        public void HideBanner()
        {
            _adProvider?.HideBanner();
        }

        private void UpdateBannerHeight(float height)
        {
            if (Mathf.Approximately(_lastBannerHeight, height)) return;
            _lastBannerHeight = height;
            OnBannerHeightChanged?.Invoke(height);
        }

        public void ForceRefreshBannerLayout()
        {
            OnBannerHeightChanged?.Invoke(_lastBannerHeight);
        }

        // ========================
        // MODERATION / REMOVE ADS
        // ========================
        public void DisableAdsPermanently()
        {
            AreAdsRemoved = true;
            HideBanner();
            OnAdsRemovedChanged?.Invoke(true);
            Log("Ads disabled permanently.");
        }

        public void RestoreAdsState(bool adsRemoved)
        {
            if (adsRemoved)
            {
                DisableAdsPermanently();
            }
        }

        // ========================
        // REGULATORY COMPLIANCE
        // ========================
        public void SetConsent(bool gdprConsent)
        {
            _adProvider?.SetConsent(gdprConsent);
        }

        public void SetMetaData(string key, string value)
        {
            _adProvider?.SetMetaData(key, value);
        }

        public void OpenTestSuite()
        {
            _adProvider?.LaunchTestSuite();
        }

        // ========================
        // IAdManagerService EXPLICIT IMPLEMENTATIONS
        // ========================
        bool IAdManagerService.IsRewardedAdReady(string placement) => IsRewardedAdReady;
        bool IAdManagerService.IsInterstitialAdReady(string placement) => IsInterstitialAdReady;
        int IAdManagerService.GetBannerHeight() => _adProvider != null ? (int)_adProvider.GetBannerHeight() : 0;
        void IAdManagerService.LaunchTestSuite() => OpenTestSuite();
        void IAdManagerService.SetConsent(bool consent) => SetConsent(consent);

        // ========================
        // LOGGER UTILITY
        // ========================
        private void Log(string msg)
        {
            if (verboseAdLogs) Debug.Log($"<color=#38c8ff>[AdManager]</color> {msg}");
        }

        private void LogWarning(string msg)
        {
            if (verboseAdLogs) Debug.LogWarning($"<color=yellow>[AdManager] ⚠️</color> {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[AdManager] ❌ {msg}");
        }
    }
}
