using UnityEngine;
using Cysharp.Threading.Tasks;
using Firebase.Analytics;
using Firebase.Crashlytics;

namespace TimeAura.Core.Services
{
    public sealed class TelemetryService
    {
        public async UniTask InitializeAsync()
        {
            Debug.Log("[Telemetry] Initializing Analytics & Crashlytics...");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            
            await UniTask.CompletedTask;
            Debug.Log("[Telemetry] Ready.");
        }

        public void LogOracleRequestMade(string promptType)
        {
            Debug.Log($"[Telemetry] Event: oracle_request_made | type: {promptType}");
            FirebaseAnalytics.LogEvent("oracle_request_made", new Parameter("prompt_type", promptType));
        }

        public void LogHarmonyStarted(string partnerId)
        {
            Debug.Log($"[Telemetry] Event: harmony_started | partner: {partnerId}");
            FirebaseAnalytics.LogEvent("harmony_started", new Parameter("partner_id", partnerId));
        }

        public void LogQuantPurchased(int amount)
        {
            Debug.Log($"[Telemetry] Event: quant_purchased | amount: {amount}");
            FirebaseAnalytics.LogEvent("quant_purchased", new Parameter("amount", amount));
        }
    }
}
