using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalMonetization
{
    /// <summary>
    /// Guard class responsible for throttling ad load requests.
    /// Uses Exponential Backoff after a failure to load:
    ///   Attempt 1 -> 5s, Attempt 2 -> 15s, Attempt 3 -> 30s, Attempt 4+ -> 60s (max)
    /// On successful load -> failure counter resets to 0.
    /// </summary>
    public class AdLoadGuard
    {
        private readonly Dictionary<string, float> _lastAttemptTimes = new();
        private readonly Dictionary<string, float> _backoffUntilTimes = new();
        private readonly Dictionary<string, bool> _inFlightLoads = new();
        private readonly Dictionary<string, int> _failureCount = new();

        // Configurable constraints
        public float MinSecondsBetweenLoadRequests { get; set; } = 5f;

        // Exponential backoff steps: failure count index -> wait seconds
        private static readonly float[] BackoffSteps = { 5f, 15f, 30f, 60f };

        public bool CanRequestLoad(
            string adType,
            bool isProviderReady,
            bool areAdsRemoved,
            bool consentBlocked,
            Action<string> logWarn = null)
        {
            if (!isProviderReady)
            {
                logWarn?.Invoke($"[AdLoadGuard] {adType} load skipped: provider unavailable or not initialized.");
                return false;
            }
            if (consentBlocked)
            {
                logWarn?.Invoke($"[AdLoadGuard] {adType} load skipped: consent blocked.");
                return false;
            }
            if (areAdsRemoved) return false;

            float now = Time.time;

            if (IsInFlight(adType))
            {
                logWarn?.Invoke($"[AdLoadGuard] {adType} load skipped: already in flight.");
                return false;
            }

            float backoffUntil = GetBackoffUntil(adType);
            if (backoffUntil > now)
            {
                logWarn?.Invoke($"[AdLoadGuard] {adType} load skipped: exponential backoff {backoffUntil - now:F1}s remaining (attempt #{GetFailureCount(adType)}).");
                return false;
            }

            float lastAttempt = GetLastAttemptTime(adType);
            if (now - lastAttempt < MinSecondsBetweenLoadRequests)
            {
                logWarn?.Invoke($"[AdLoadGuard] {adType} load skipped: min interval {MinSecondsBetweenLoadRequests:F1}s not elapsed.");
                return false;
            }

            return true;
        }

        public void RecordAttempt(string adType)
        {
            _lastAttemptTimes[adType] = Time.time;
            _inFlightLoads[adType] = true;
        }

        /// <summary>Resets the failure/backoff state for an ad type after a successful load.</summary>
        public void ResetBackoff(string adType)
        {
            _backoffUntilTimes[adType] = -1f;
            _failureCount[adType] = 0;
        }

        public void SetInFlight(string adType, bool inFlight) =>
            _inFlightLoads[adType] = inFlight;

        public bool IsInFlight(string adType) =>
            _inFlightLoads.TryGetValue(adType, out bool val) && val;

        public float GetBackoffUntil(string adType) =>
            _backoffUntilTimes.TryGetValue(adType, out float v) ? v : -1f;

        public int GetFailureCount(string adType) =>
            _failureCount.TryGetValue(adType, out int v) ? v : 0;

        /// <summary>
        /// Apply exponential backoff. Fires on any load failure.
        /// Makes the system robust against brief network dropouts.
        /// </summary>
        public void ApplyFailureBackoff(string adType, string error, Action<string> logWarn = null)
        {
            int count = GetFailureCount(adType);
            count++;
            _failureCount[adType] = count;

            int idx = Mathf.Clamp(count - 1, 0, BackoffSteps.Length - 1);
            float duration = BackoffSteps[idx];

            _backoffUntilTimes[adType] = Time.time + duration;
            logWarn?.Invoke($"[AdLoadGuard] {adType} backoff applied: attempt #{count} -> waiting {duration:F0}s (error: {error})");
        }

        private float GetLastAttemptTime(string adType) =>
            _lastAttemptTimes.TryGetValue(adType, out float v) ? v : -100f;
    }
}
