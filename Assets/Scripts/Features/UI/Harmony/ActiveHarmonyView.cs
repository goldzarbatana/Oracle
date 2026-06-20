using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using TimeAura.Features.Harmony;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TimeAura.Features.UI.Harmony
{
    /// <summary>
    /// Active Harmony Cycle View - displays live timer and Horas flow.
    /// "Witness the sacred exchange as time bends between two souls."
    /// </summary>
    public class ActiveHarmonyView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI horasText;
        [SerializeField] private Image progressBar;
        [SerializeField] private LineRenderer connectionLine;
        [SerializeField] private Button sealHarmonyButton;
        [SerializeField] private Button dissolveButton;

        [Header("Aura Positions")]
        [SerializeField] private Transform initiatorAuraPoint;
        [SerializeField] private Transform recipientAuraPoint;

        [Header("VFX")]
        [SerializeField] private ParticleSystem auraFlowParticles;
        [SerializeField] private Material goldenLineMaterial;

        [Inject] private HarmonyManager _harmonyManager;
        [Inject] private Core.Services.AudioService _audioService;
        [Inject] private LocalizationManager _localizationManager;

        private HarmonySession _currentSession;
        private bool _isActive;

        private void Start()
        {
            sealHarmonyButton?.onClick.AddListener(OnSealHarmonyClicked);
            dissolveButton?.onClick.AddListener(OnDissolveClicked);

            if (_harmonyManager != null)
            {
                _harmonyManager.OnSessionStarted += OnSessionStarted;
                _harmonyManager.OnProgressUpdated += OnProgressUpdated;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_harmonyManager != null)
            {
                _harmonyManager.OnSessionStarted -= OnSessionStarted;
                _harmonyManager.OnProgressUpdated -= OnProgressUpdated;
            }

            sealHarmonyButton?.onClick.RemoveListener(OnSealHarmonyClicked);
            dissolveButton?.onClick.RemoveListener(OnDissolveClicked);
        }

        private void Update()
        {
            if (_isActive && _currentSession != null)
            {
                UpdateTimer();
                AnimateConnection();
            }
        }

        private void OnSessionStarted(HarmonySession session)
        {
            _currentSession = session;
            _isActive = true;
            gameObject.SetActive(true);

            InitializeVisuals();
            _audioService?.PlayTransformationAmbience(fadeIn: true);

            Debug.Log("[ActiveHarmonyView] 🌟 Harmony cycle view activated.");
        }

        private void OnProgressUpdated(float progress)
        {
            if (progressBar != null) progressBar.fillAmount = progress;
        }

        private void InitializeVisuals()
        {
            if (horasText != null)
            {
                horasText.text = _localizationManager?.GetFormatted(AuraTerms.UI_VECTOR_DISPLAY, _currentSession.horasExchanged) ?? $"{_currentSession.horasExchanged} Horas";
            }

            if (connectionLine != null && initiatorAuraPoint != null && recipientAuraPoint != null)
            {
                connectionLine.positionCount = 2;
                connectionLine.SetPosition(0, initiatorAuraPoint.position);
                connectionLine.SetPosition(1, recipientAuraPoint.position);
                connectionLine.material = goldenLineMaterial;
                connectionLine.startWidth = 0.1f;
                connectionLine.endWidth = 0.1f;
                connectionLine.enabled = true;
            }

            if (auraFlowParticles != null) auraFlowParticles.Play();
        }

        private void UpdateTimer()
        {
            if (timerText != null && _currentSession != null)
            {
                float duration = _currentSession.Duration;
                int minutes = Mathf.FloorToInt(duration / 60f);
                int seconds = Mathf.FloorToInt(duration % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void AnimateConnection()
        {
            if (connectionLine != null && connectionLine.enabled)
            {
                float pulse = 0.05f + Mathf.Sin(Time.time * 2f) * 0.05f;
                connectionLine.startWidth = pulse;
                connectionLine.endWidth = pulse;

                Color baseColor = new Color(1f, 0.84f, 0.4f);
                float intensity = 1f + Mathf.Sin(Time.time * 1.5f) * 0.3f;
                connectionLine.startColor = baseColor * intensity;
                connectionLine.endColor = baseColor * intensity;
            }
        }

        private async void OnSealHarmonyClicked()
        {
            _audioService?.PlayButtonClick();
            var resonance = await ShowResonanceSelectionAsync();

            bool success = await _harmonyManager.CompleteHarmonyAsync(resonance);
            if (success)
            {
                _audioService?.PlayResonanceChime((int)resonance);
                CloseView();
            }
        }

        private async void OnDissolveClicked()
        {
            _audioService?.PlayButtonClick();
            bool confirmed = await ShowDissolveConfirmationAsync();

            if (confirmed)
            {
                await _harmonyManager.DissolveHarmonyAsync();
                CloseView();
            }
        }

        private void CloseView()
        {
            _isActive = false;
            _currentSession = null;

            if (connectionLine != null) connectionLine.enabled = false;
            if (auraFlowParticles != null) auraFlowParticles.Stop();

            _audioService?.StopTransformationAmbience(fadeOut: true);
            gameObject.SetActive(false);

            Debug.Log("[ActiveHarmonyView] 🌙 Harmony view closed.");
        }

        private async UniTask<ResonanceLevel> ShowResonanceSelectionAsync()
        {
            await UniTask.Delay(500);
            return HarmonyResonanceCalculator.CalculateResonance(_currentSession);
        }

        private async UniTask<bool> ShowDissolveConfirmationAsync()
        {
            await UniTask.Delay(100);
            return true;
        }
    }
}
