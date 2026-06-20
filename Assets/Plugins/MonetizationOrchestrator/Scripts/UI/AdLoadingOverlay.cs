using System.Collections;
using UnityEngine;
#if HAS_UGUI
using UnityEngine.UI;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Manages the visual buffering screen (spinner) and error toasts.
    /// This prevents users from clicking other UI elements while an ad is fetching from the network.
    /// </summary>
    public class AdLoadingOverlay : MonoBehaviour
    {
        private static AdLoadingOverlay _instance;

#if HAS_UGUI
        [Header("UI References")]
        [SerializeField] private GameObject spinnerContainer;
        [SerializeField] private GameObject toastContainer;
        [SerializeField] private Text toastText;
#endif

#if HAS_UGUI
        private Coroutine _toastCoroutine;
#endif

        private static AdLoadingOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    var prefab = Resources.Load<GameObject>("AdLoadingOverlay");
                    if (prefab == null)
                    {
                        Debug.LogError("[AdLoadingOverlay] ❌ AdLoadingOverlay prefab not found in Resources folder!");
                        return null;
                    }
                    var go = Instantiate(prefab);
                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<AdLoadingOverlay>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

#if HAS_UGUI
            if (spinnerContainer != null) spinnerContainer.SetActive(false);
            if (toastContainer != null) toastContainer.SetActive(false);
#endif
        }

        /// <summary>Shows the loading spinner overlay.</summary>
        public static void Show()
        {
            if (Instance == null) return;
            Instance.ShowSpinnerInternal();
        }

        /// <summary>Hides the loading spinner overlay.</summary>
        public static void Hide()
        {
            if (Instance == null) return;
            Instance.HideSpinnerInternal();
        }

        public static void ShowUnavailableToast(string message = "Ad not available. Try again later.")
        {
            if (Instance == null) return;
            Instance.ShowToastInternal(message, new Color(0.9f, 0.3f, 0.3f, 1f));
        }

        public static void ShowToast(string message, Color color)
        {
            if (Instance == null) return;
            Instance.ShowToastInternal(message, color);
        }

        private void ShowSpinnerInternal()
        {
#if HAS_UGUI
            if (spinnerContainer != null) spinnerContainer.SetActive(true);
#endif
        }

        private void HideSpinnerInternal()
        {
#if HAS_UGUI
            if (spinnerContainer != null) spinnerContainer.SetActive(false);
#endif
        }

        private void ShowToastInternal(string message, Color color)
        {
#if HAS_UGUI
            if (toastContainer == null || toastText == null) return;

            toastText.color = color;

            if (_toastCoroutine != null)
            {
                StopCoroutine(_toastCoroutine);
            }

            _toastCoroutine = StartCoroutine(ToastSequenceRoutine(message));
#endif
        }

        private IEnumerator ToastSequenceRoutine(string message)
        {
#if HAS_UGUI
            toastText.text = message;
            toastContainer.SetActive(true);

            // Fade in and stay
            CanvasGroup canvasGroup = toastContainer.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.25f)
                {
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.25f);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            yield return new WaitForSecondsRealtime(2f);

            // Fade out
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            toastContainer.SetActive(false);
            _toastCoroutine = null;
#endif
            yield break;
        }
    }
}
