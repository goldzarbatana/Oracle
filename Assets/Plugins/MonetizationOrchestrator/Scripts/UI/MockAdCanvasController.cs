using System;
using System.Collections;
using UnityEngine;

#if HAS_UGUI
using UnityEngine.UI;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Controls the on-screen UI overlay that simulates an ad playing.
    /// Used inside the Unity Editor or in developmental builds to test ad integrations without SDKs.
    /// </summary>
    public class MockAdCanvasController : MonoBehaviour
    {
#if HAS_UGUI
        [Header("UI Elements")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text timerText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider progressSlider;
#endif

        private Action _onAdRewarded;
        private Action _onAdClosed;
        private int _countdownDuration = 5;
        private Coroutine _countdownCoroutine;
        private bool _isRewardedAd;

        private float _previousTimeScale;
        private bool _previousAudioPauseState;

        private void Start()
        {
#if HAS_UGUI
            if (claimButton != null) claimButton.onClick.AddListener(OnClaimClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
#endif
        }

        /// <summary>
        /// Initializes and shows the simulated ad playback overlay.
        /// </summary>
        public void ShowAd(bool isRewarded, int duration, Action onRewarded, Action onClosed)
        {
            _isRewardedAd = isRewarded;
            _countdownDuration = duration;
            _onAdRewarded = onRewarded;
            _onAdClosed = onClosed;

            // Pause game audio and timescale to simulate native ad overlays
            _previousTimeScale = Time.timeScale;
            _previousAudioPauseState = AudioListener.pause;
            
            Time.timeScale = 0f;
            AudioListener.pause = true;

            gameObject.SetActive(true);

#if HAS_UGUI
            // Configure button visibilities
            if (titleText != null) titleText.text = isRewarded ? "SIMULATED REWARDED AD" : "SIMULATED INTERSTITIAL AD";
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);

            if (progressSlider != null)
            {
                progressSlider.maxValue = duration;
                progressSlider.value = duration;
            }
#endif

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(AdCountdownRoutine());
        }

        private IEnumerator AdCountdownRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _countdownDuration)
            {
                // We use RealtimeSinceStartup because timeScale is 0
                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;

#if HAS_UGUI
                float remaining = Mathf.Max(0f, _countdownDuration - elapsed);
                if (timerText != null) timerText.text = $"Ad closes in {remaining:F1}s";

                if (progressSlider != null)
                {
                    progressSlider.value = remaining;
                }
#endif
            }

#if HAS_UGUI
            if (timerText != null) timerText.text = "Playback Complete";

            if (_isRewardedAd)
            {
                // User must click "Claim" to get the reward
                if (claimButton != null) claimButton.gameObject.SetActive(true);
                if (closeButton != null) closeButton.gameObject.SetActive(true);
            }
            else
            {
                // Interstitial can just be closed
                if (closeButton != null) closeButton.gameObject.SetActive(true);
            }
#else
            // If no UI system is available, complete the mock ad automatically
            if (_isRewardedAd)
            {
                OnClaimClicked();
            }
            else
            {
                OnCloseClicked();
            }
#endif
        }

        public void OnClaimClicked()
        {
            _onAdRewarded?.Invoke();
            CloseAd();
        }

        public void OnCloseClicked()
        {
            CloseAd();
        }

        private void CloseAd()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            // Restore engine state
            Time.timeScale = _previousTimeScale;
            AudioListener.pause = _previousAudioPauseState;

            gameObject.SetActive(false);
            _onAdClosed?.Invoke();
        }
    }
}
