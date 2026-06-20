namespace TimeAura.Core.Services
{
    public static class ConfigKeys
    {
        // CONFIG
        public const string DemoMode = "demo_mode";
        public const string AppVersionMin = "app_version_min";
        public const string MaintenanceMode = "maintenance_mode";
        public const string OracleDailyLimitFree = "oracle_daily_limit_free";
        public const string OracleDailyLimitEnlightened = "oracle_daily_limit_enlightened";
        public const string CacheTimeoutMinutes = "cache_timeout_minutes";
        public const string TelemetryEnabled = "telemetry_enabled";

        // ECONOMY
        public const string QuantToUsdRate = "quant_to_usd_rate";
        public const string HorasToUsdRate = "horas_to_usd_rate";
        public const string CommissionPercent = "commission_percent";
        public const string MinCashoutThreshold = "min_cashout_threshold";
        public const string MaxCashoutThreshold = "max_cashout_threshold";
        public const string QuantPack100Price = "quant_pack_100_price";
        public const string QuantPack500Price = "quant_pack_500_price";
        public const string QuantPack1000Price = "quant_pack_1000_price";
        public const string EnlightenedSubscriptionPrice = "enlightened_subscription_price";
        public const string FreeUserDailyBonus = "free_user_daily_bonus";
        public const string HoraDisplayThreshold = "hora_display_threshold";
        public const string QuantToWavesRate = "quant_to_waves_rate";
        public const string PostBoostCostWaves = "post_boost_cost_waves";
        public const string TipMinWaves = "tip_min_waves";
        public const string StripeCommissionPercent = "stripe_commission_percent";
        public const string StripeTestMode = "stripe_test_mode";

        // ORACLE_PROMPTS
        public const string ToneMystic = "tone_mystic";
        public const string ToneBusiness = "tone_business";
        public const string ToneCasual = "tone_casual";
        public const string ToneTech = "tone_tech";
        public const string GreetingUa = "greeting_ua";
        public const string GreetingEn = "greeting_en";
        public const string ArbitrationPrompt = "arbitration_prompt";
        public const string IntentParserPrompt = "intent_parser_prompt";

        // FEATURE_FLAGS
        public const string VoiceSearch = "voice_search";
        public const string MatterRealm = "matter_realm";
        public const string Arbitration = "arbitration";
        public const string Cashout = "cashout";
        public const string DeepLinking = "deep_linking";
        public const string OraclePersonalityEngine = "oracle_personality_engine";
        public const string AnalyticsLogging = "analytics_logging";
        public const string DemoModeToggle = "demo_mode_toggle";
    }
}
