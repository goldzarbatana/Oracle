using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimeAura.Core.Services
{
    public sealed class RemoteConfigService
    {
        private readonly IRemoteConfigSource _source;

        public RemoteConfigService(IRemoteConfigSource source)
        {
            _source = source;
        }

        // Properties mapped to Google Sheets keys
        public bool IsDemoMode => _source.GetValue(ConfigKeys.DemoMode, true);
        public string AppVersionMin => _source.GetValue(ConfigKeys.AppVersionMin, "1.0.0");
        public bool IsMaintenanceMode => _source.GetValue(ConfigKeys.MaintenanceMode, false);
        public int OracleDailyLimitFree => _source.GetValue(ConfigKeys.OracleDailyLimitFree, 10);
        public int OracleDailyLimitEnlightened => _source.GetValue(ConfigKeys.OracleDailyLimitEnlightened, -1);
        public int CacheTimeoutMinutes => _source.GetValue(ConfigKeys.CacheTimeoutMinutes, 5);
        public bool IsTelemetryEnabled => _source.GetValue(ConfigKeys.TelemetryEnabled, true);

        // Economy
        public float QuantToUsdRate => _source.GetValue(ConfigKeys.QuantToUsdRate, 0.01f);
        public float HorasToUsdRate => _source.GetValue(ConfigKeys.HorasToUsdRate, 0.15f);
        public float CommissionPercent => _source.GetValue(ConfigKeys.CommissionPercent, 7f);
        public float MinCashoutThreshold => _source.GetValue(ConfigKeys.MinCashoutThreshold, 50f);
        public float MaxCashoutThreshold => _source.GetValue(ConfigKeys.MaxCashoutThreshold, 1000f);
        public float QuantPack100Price => _source.GetValue(ConfigKeys.QuantPack100Price, 0.99f);
        public float QuantPack500Price => _source.GetValue(ConfigKeys.QuantPack500Price, 4.99f);
        public float QuantPack1000Price => _source.GetValue(ConfigKeys.QuantPack1000Price, 9.99f);
        public float EnlightenedSubscriptionPrice => _source.GetValue(ConfigKeys.EnlightenedSubscriptionPrice, 4.99f);
        public int FreeUserDailyBonus => _source.GetValue(ConfigKeys.FreeUserDailyBonus, 50);

        // Oracle Prompts
        public string ToneMystic => _source.GetValue(ConfigKeys.ToneMystic, "");
        public string ToneBusiness => _source.GetValue(ConfigKeys.ToneBusiness, "");
        public string ToneCasual => _source.GetValue(ConfigKeys.ToneCasual, "");
        public string ToneTech => _source.GetValue(ConfigKeys.ToneTech, "");
        public string GreetingUa => _source.GetValue(ConfigKeys.GreetingUa, "");
        public string GreetingEn => _source.GetValue(ConfigKeys.GreetingEn, "");
        public string ArbitrationPrompt => _source.GetValue(ConfigKeys.ArbitrationPrompt, "");
        public string IntentParserPrompt => _source.GetValue(ConfigKeys.IntentParserPrompt, "");

        // Feature Flags
        public bool EnableVoiceSearch => _source.GetValue(ConfigKeys.VoiceSearch, true);
        public bool EnableMatterRealm => _source.GetValue(ConfigKeys.MatterRealm, true);
        public bool EnableArbitration => _source.GetValue(ConfigKeys.Arbitration, false);
        public bool EnableCashout => _source.GetValue(ConfigKeys.Cashout, false);
        public bool EnableDeepLinking => _source.GetValue(ConfigKeys.DeepLinking, false);
        public bool EnableOraclePersonalityEngine => _source.GetValue(ConfigKeys.OraclePersonalityEngine, true);
        public bool EnableAnalyticsLogging => _source.GetValue(ConfigKeys.AnalyticsLogging, true);
        public bool EnableDemoModeToggle => _source.GetValue(ConfigKeys.DemoModeToggle, true);

        // Fallback for OracleDailyLimit (previously used in old code)
        public int OracleDailyLimit => OracleDailyLimitFree;

        public IRemoteConfigSource Source => _source;

        public async UniTask InitializeAsync()
        {
            Debug.Log("[RemoteConfigService] Initializing remote config source...");
            try
            {
                await _source.FetchConfigAsync(forceReload: false);
                Debug.Log($"[RemoteConfigService] Initialization complete. Demo Mode: {IsDemoMode}, Oracle Daily Limit: {OracleDailyLimit}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigService] Failed to initialize RemoteConfig: {ex.Message}");
            }
        }

        public async UniTask ReloadAsync()
        {
            Debug.Log("[RemoteConfigService] Force-reloading Remote Config from Sheets...");
            await _source.FetchConfigAsync(forceReload: true);
            Debug.Log("[RemoteConfigService] Reload complete.");
        }
    }
}
