using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Auth;
using TimeAura.Features.Localization;
using VContainer;
using System;

namespace TimeAura.Features.UI.Auth
{
    /// <summary>
    /// The entry screen of the application. Handles login, phone input, and visual rituals.
    /// </summary>
    public class InitiationView : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image logoImage;
        [SerializeField] private Image backgroundVignette;
        [SerializeField] private ParticleSystem auraMist;

        [Header("Input Controls")]
        [SerializeField] private TMP_InputField phoneInputField;
        [SerializeField] private TMP_Text phoneLabel;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button initiateButton;
        [SerializeField] private TMP_Text initiateButtonText;
        [SerializeField] private Image initiateButtonGlow;

        [Header("Settings")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private float logoRotationSpeed = 5f;
        [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f, 1f);

        private AuthManager _authManager;
        private LocalizationManager _localization;

        private bool _isBusy;

        [Inject]
        public void Construct(AuthManager authManager, LocalizationManager localization)
        {
            _authManager = authManager;
            _localization = localization;
            Debug.Log("[InitiationView] 💉 Injected via Temple Ritual.");
        }

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0;
            if (initiateButton != null) initiateButton.onClick.AddListener(OnInitiateClicked);
        }

        private async void Start()
        {
            await ShowAsync();
            UpdateLocalization();
        }

        private void Update()
        {
            if (logoImage != null)
            {
                logoImage.transform.Rotate(Vector3.forward, logoRotationSpeed * Time.deltaTime);
            }
        }

        public async UniTask ShowAsync()
        {
            if (canvasGroup == null) return;
            
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
                await UniTask.Yield();
            }
            canvasGroup.alpha = 1;
        }

        private void UpdateLocalization()
        {
            if (_localization == null) return;
            
            if (statusText != null) statusText.text = _localization.Get("INIT_WELCOME", "The temple awaits your presence.");
            if (phoneLabel != null) phoneLabel.text = _localization.Get("INIT_PHONE_LABEL", "NEXUS IDENTITY (PHONE)");
            if (initiateButtonText != null) initiateButtonText.text = _localization.Get("INIT_BUTTON", "INITIATE RITE");
        }

        private async void OnInitiateClicked()
        {
            if (_isBusy || _authManager == null) return;
            _isBusy = true;

            try
            {
                string phoneNumber = phoneInputField?.text;
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    ShowStatus("Invalid Identity Number", Color.red);
                    _isBusy = false;
                    return;
                }

                ShowStatus("Communing with the ether...", goldenColor);
                
                // Trigger Authentication
                var result = await _authManager.VerifyPhone(phoneNumber);
                
                if (result.Profile != null)
                {
                    ShowStatus("Rite Accepted", goldenColor);
                }
                else
                {
                    ShowStatus("Rite Denied. Try again.", Color.red);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InitiationView] Error: {ex.Message}");
                ShowStatus("Connection Failed", Color.red);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
    }
}
