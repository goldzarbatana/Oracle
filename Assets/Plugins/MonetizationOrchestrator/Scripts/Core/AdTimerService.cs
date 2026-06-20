using UnityEngine;

namespace UniversalMonetization
{
    /// <summary>
    /// Service for managing timers, intervals, and pacing rules for showing advertisements.
    /// Helps respect user experience by preventing ads from appearing too frequently.
    /// </summary>
    public class AdTimerService
    {
        private float _lastInterstitialShowTime;
        private float _lastRewardedShowTime;
        private float _sceneStartTime;

        // Default UX Pacing Rules
        private float _minInterstitialInterval = 45f;
        private float _rewardedToInterstitialCooldown = 5f;
        private float _sceneWarmupDelay = 5f;

        public void Initialize(float minInterstitialInterval, float rewardedToInterstitialCooldown, float sceneWarmupDelay)
        {
            _minInterstitialInterval = minInterstitialInterval;
            _rewardedToInterstitialCooldown = rewardedToInterstitialCooldown;
            _sceneWarmupDelay = sceneWarmupDelay;
            _sceneStartTime = Time.unscaledTime;
            
            // Allow the first interstitial soon after warmup, but set the timer back
            _lastInterstitialShowTime = -minInterstitialInterval + 5f;
        }

        public void UpdateParameters(float minInterstitialInterval, float rewardedToInterstitialCooldown, float sceneWarmupDelay)
        {
            _minInterstitialInterval = minInterstitialInterval;
            _rewardedToInterstitialCooldown = rewardedToInterstitialCooldown;
            _sceneWarmupDelay = sceneWarmupDelay;
        }

        public void TrackInterstitialDisplayed()
        {
            _lastInterstitialShowTime = Time.time;
        }

        public void TrackRewardedDisplayed()
        {
            _lastRewardedShowTime = Time.time;
        }

        public void TrackSceneStart()
        {
            _sceneStartTime = Time.unscaledTime;
        }

        public bool IsInterstitialReadyByTimer()
        {
            return (Time.time - _lastInterstitialShowTime) >= _minInterstitialInterval;
        }

        public bool IsPastRewardedCooldown()
        {
            return (Time.time - _lastRewardedShowTime) >= _rewardedToInterstitialCooldown;
        }

        public bool HasSceneWarmupPassed()
        {
            return (Time.unscaledTime - _sceneStartTime) >= _sceneWarmupDelay;
        }

        public float GetSecondsUntilNextInterstitial()
        {
            float elapsed = Time.time - _lastInterstitialShowTime;
            return Mathf.Max(0, _minInterstitialInterval - elapsed);
        }
    }
}
