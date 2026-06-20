using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using CommerceOrchestrator.Core;
using CommerceOrchestrator.Providers;
using UniversalMonetization;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TimeAura.Features.Economy
{
    public class MonetizationEconomyAdapter : IStartable, IDisposable
    {
        private readonly IDataService _dataService;
        private readonly AuthManager _authManager;
        private readonly AppConfig _appConfig;

        [Inject]
        public MonetizationEconomyAdapter(IDataService dataService, AuthManager authManager, AppConfig appConfig)
        {
            _dataService = dataService;
            _authManager = authManager;
            _appConfig = appConfig;
        }

        public void Start()
        {
            AdManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
            StoreManager.OnPurchaseSuccess += HandlePurchaseSuccess;
            
            InitializeStoreAsync().Forget();
            
            Debug.Log("[Economy] 💰 Monetization Economy Adapter (Commerce Orchestrator) initialized. Listening for rewards and purchases.");
        }

        public void Dispose()
        {
            AdManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;
            StoreManager.OnPurchaseSuccess -= HandlePurchaseSuccess;
        }

        private async UniTaskVoid InitializeStoreAsync()
        {
            // 0. Initialize Unity Gaming Services
            try
            {
                Debug.Log("[Economy] Initializing Unity Gaming Services for In-App Purchasing...");
                await Unity.Services.Core.UnityServices.InitializeAsync();
                Debug.Log("[Economy] Unity Gaming Services initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Economy] Failed to initialize Unity Gaming Services: {ex.Message}");
            }

            // 1. Find or create StoreManager in the scene
            var storeManager = StoreManager.Instance;
            if (storeManager == null)
            {
                var go = new GameObject("StoreManager");
                storeManager = go.AddComponent<StoreManager>();
            }

            // 2. Load the fallback StoreDatabase asset from Resources
            var fallbackDb = Resources.Load<StoreDatabase>("Settings/StoreDatabase");
            if (fallbackDb == null)
            {
                Debug.LogWarning("[Economy] Fallback StoreDatabase asset not found in 'Assets/Resources/Settings/StoreDatabase.asset'. Please create one.");
            }

            // 3. Register pricing provider
            string sheetUrl = _appConfig != null ? _appConfig.GoogleSheetBaseUrl : "";
            var provider = new GoogleSheetsPricingProvider(sheetUrl, fallbackDb);
            storeManager.SetPricingProvider(provider);

            // 4. Trigger Store initialization
            try
            {
                await storeManager.InitializeStoreAsync();
                Debug.Log("[Economy] 🛒 Commerce Orchestrator Store initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Economy] ❌ Commerce Orchestrator Store initialization failed: {ex.Message}");
            }
        }

        private async void HandleRewardedAdCompleted(int rewardAmount)
        {
            if (_authManager.CurrentProfile == null) return;
            
            Debug.Log($"[Economy] 🎁 Player finished an ad. Granting +1 Extra Contract slot.");

            var profile = _authManager.CurrentProfile;
            profile.ExtraContracts++;
            
            await _dataService.SaveUserProfileAsync(profile, default);
        }

        private async void HandlePurchaseSuccess(string productId, List<RewardEntry> rewards)
        {
            if (_authManager.CurrentProfile == null) return;

            var profile = _authManager.CurrentProfile;
            bool addedAny = false;

            if (rewards != null && rewards.Count > 0)
            {
                foreach (var reward in rewards)
                {
                    string name = reward.resourceId.Trim().ToLower();
                    int amount = reward.amount;

                    if (name.Contains("quant"))
                    {
                        // 1 Quant = 100 Waves
                        profile.AddQuants(amount);
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Granted {amount} Quants ({amount * 100} Waves) to {profile.Nickname}.");
                        addedAny = true;
                    }
                    else if (name.Contains("wave"))
                    {
                        profile.AddWaves(amount);
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Granted {amount} Waves to {profile.Nickname}.");
                        addedAny = true;
                    }
                    else if (name.Contains("hora"))
                    {
                        // 1 Hora = 60 minutes
                        profile.AddHoras(amount);
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Granted {amount} Horas ({amount * 60} Atoms) to {profile.Nickname}.");
                        addedAny = true;
                    }
                    else if (name.Contains("minute") || name.Contains("atom"))
                    {
                        profile.AddMinutes(amount);
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Granted {amount} Minutes (Atoms) to {profile.Nickname}.");
                        addedAny = true;
                    }
                    else if (name.Contains("enlightened") || name.Contains("ascension") || name.Contains("sub"))
                    {
                        profile.IsAscensionSubscribed = true;
                        var currentExpiry = profile.AscensionExpiresAt ?? DateTime.UtcNow;
                        if (currentExpiry < DateTime.UtcNow) currentExpiry = DateTime.UtcNow;
                        
                        // Extend by 30 days
                        profile.SetAscensionExpiresAt(currentExpiry.AddDays(30));
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Activated/extended Enlightened subscription for 30 days for {profile.Nickname}.");
                        addedAny = true;
                    }
                    else if (name.Contains("fiat"))
                    {
                        profile.AddFiat(amount);
                        Debug.Log($"[Economy] 🛒 Dynamic IAP Success! Granted {amount} Fiat to {profile.Nickname}.");
                        addedAny = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[Economy] Unknown reward resource '{reward.resourceId}' in purchase. Attempting to match as raw Waves.");
                        profile.AddWaves(amount);
                        addedAny = true;
                    }
                }
            }
            else
            {
                // Fallback to static mapping if no dynamic rewards found (for backward compatibility)
                switch (productId)
                {
                    case "com.timeaura.horas.pack1":
                    case "com.timeaura.quants.pack100":
                        profile.AddQuants(100f);
                        Debug.Log($"[Economy] 🛒 Fallback IAP Success! Granted 100 Quants (10,000 Waves) to {profile.Nickname}.");
                        addedAny = true;
                        break;
                    case "com.timeaura.quants.pack500":
                        profile.AddQuants(500f);
                        Debug.Log($"[Economy] 🛒 Fallback IAP Success! Granted 500 Quants (50,000 Waves) to {profile.Nickname}.");
                        addedAny = true;
                        break;
                    case "com.timeaura.quants.pack1000":
                        profile.AddQuants(1000f);
                        Debug.Log($"[Economy] 🛒 Fallback IAP Success! Granted 1000 Quants (100,000 Waves) to {profile.Nickname}.");
                        addedAny = true;
                        break;
                    case "com.timeaura.subscription.enlightened":
                        profile.IsAscensionSubscribed = true;
                        var currentExpiry = profile.AscensionExpiresAt ?? DateTime.UtcNow;
                        if (currentExpiry < DateTime.UtcNow) currentExpiry = DateTime.UtcNow;
                        profile.SetAscensionExpiresAt(currentExpiry.AddDays(30));
                        Debug.Log($"[Economy] 🛒 Fallback IAP Success! Activated/extended Enlightened subscription for 30 days for {profile.Nickname}.");
                        addedAny = true;
                        break;
                    default:
                        Debug.LogWarning($"[Economy] Unknown product purchased (no rewards specified): {productId}.");
                        break;
                }
            }

            if (addedAny)
            {
                await _dataService.SaveUserProfileAsync(profile, default);
            }
        }
    }
}
