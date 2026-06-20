using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace TimeAura.Features.UI.Social
{
    /// <summary>
    /// Aura Shader Controller - Manages mystical pulsation effects for FateCards.
    /// Integrates with Unity Shader Graph materials to create dynamic visual feedback.
    /// "The aura breathes with the rhythm of convergence."
    /// </summary>
    public class AuraShaderController : MonoBehaviour
    {
        [Header("Shader Configuration")]
        [SerializeField] private Material auraMaterial;
        [SerializeField] private Renderer targetRenderer;

        [Header("Pulsation Settings")]
        [SerializeField] private float pulseSpeed = 1.0f;
        [SerializeField] private float pulseAmplitude = 0.3f;
        [SerializeField] private float baseIntensity = 0.7f;

        [Header("Color Themes")]
        [SerializeField] private Color goldenAura = new Color(1f, 0.84f, 0f, 1f); // Default golden
        [SerializeField] private Color mysticalAura = new Color(0.5f, 0.2f, 0.8f, 1f); // Purple for special states
        [SerializeField] private Color transformedAura = new Color(0f, 1f, 0.8f, 1f); // Cyan for active transformation

        [Header("Scroll-Based Intensity")]
        [SerializeField] private bool modulateByScrollDistance = true;
        [SerializeField] private float maxScrollIntensityBoost = 0.5f;

        private Material materialInstance;
        private bool isPulsing = true;
        private float currentIntensity;
        private Color currentColor;
        private float scrollIntensityMultiplier = 1f;

        // Shader property IDs (cache for performance)
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int AuraColorID = Shader.PropertyToID("_AuraColor");
        private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
        private static readonly int GlowRadiusID = Shader.PropertyToID("_GlowRadius");

        private void Awake()
        {
            if (auraMaterial != null)
            {
                // Create material instance to avoid affecting other objects
                materialInstance = new Material(auraMaterial);

                if (targetRenderer != null)
                {
                    targetRenderer.material = materialInstance;
                }
            }

            currentColor = goldenAura;
            currentIntensity = baseIntensity;
        }

        private void OnDestroy()
        {
            // Cancel any pending burst effects
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();

            // Clean up material instance
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }
        }

        private void Update()
        {
            if (!isPulsing || materialInstance == null) return;

            // Calculate pulsation wave
            float phase = Time.time * pulseSpeed;
            float wave = Mathf.Sin(phase * Mathf.PI * 2f);
            float normalizedWave = (wave + 1f) * 0.5f; // 0 to 1

            // Apply amplitude and base intensity
            float targetIntensity = baseIntensity + (normalizedWave * pulseAmplitude);

            // Apply scroll-based modulation
            if (modulateByScrollDistance)
            {
                targetIntensity *= scrollIntensityMultiplier;
            }

            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 5f);

            // Update shader properties
            materialInstance.SetFloat(IntensityID, currentIntensity);
            materialInstance.SetColor(AuraColorID, currentColor * currentIntensity);
            materialInstance.SetFloat(PulseSpeedID, pulseSpeed);
        }

        #region Public API

        /// <summary>
        /// Start aura pulsation (called when card becomes visible).
        /// </summary>
        public void StartPulsing()
        {
            isPulsing = true;

            // Fade in the aura
            currentIntensity = 0f;
        }

        /// <summary>
        /// Stop aura pulsation (called when card is pooled or hidden).
        /// </summary>
        public void StopPulsing()
        {
            isPulsing = false;

            // Fade out
            if (materialInstance != null)
            {
                materialInstance.SetFloat(IntensityID, 0f);
            }
        }

        /// <summary>
        /// Change aura color theme (e.g., for transformation state).
        /// </summary>
        public void SetAuraTheme(AuraTheme theme)
        {
            switch (theme)
            {
                case AuraTheme.Golden:
                    currentColor = goldenAura;
                    break;
                case AuraTheme.Mystical:
                    currentColor = mysticalAura;
                    break;
                case AuraTheme.Transformed:
                    currentColor = transformedAura;
                    break;
            }
        }

        /// <summary>
        /// Set custom aura color.
        /// </summary>
        public void SetAuraColor(Color color)
        {
            currentColor = color;
        }

        /// <summary>
        /// Adjust pulse speed (e.g., speed up during transformation).
        /// </summary>
        public void SetPulseSpeed(float speed)
        {
            pulseSpeed = Mathf.Clamp(speed, 0.1f, 5f);
        }

        /// <summary>
        /// Set intensity multiplier based on scroll distance from center.
        /// Cards near center of viewport get higher intensity.
        /// </summary>
        public void SetScrollIntensity(float distanceFromCenter)
        {
            if (!modulateByScrollDistance) return;

            // distanceFromCenter: 0 = center, 1 = edge of screen
            float normalized = Mathf.Clamp01(1f - distanceFromCenter);
            scrollIntensityMultiplier = 1f + (normalized * maxScrollIntensityBoost);
        }

        /// <summary>
        /// Trigger burst effect (brief intensity spike).
        /// </summary>
        public void TriggerBurst(float duration = 0.5f)
        {
            if (materialInstance == null) return;

            _ = BurstEffectAsync(duration);
        }

        #endregion

        #region Effects

        private async Cysharp.Threading.Tasks.UniTask BurstEffectAsync(float duration)
        {
            float originalAmplitude = pulseAmplitude;
            float originalSpeed = pulseSpeed;

            // Spike amplitude and speed
            pulseAmplitude = 0.7f;
            pulseSpeed = 3f;

            try
            {
                await Cysharp.Threading.Tasks.UniTask.Delay(
                    System.TimeSpan.FromSeconds(duration),
                    cancellationToken: destroyCancellationToken
                );
            }
            catch (System.OperationCanceledException)
            {
                // Component destroyed during burst
            }

            // Restore original values
            pulseAmplitude = originalAmplitude;
            pulseSpeed = originalSpeed;
        }

        private new CancellationToken destroyCancellationToken
        {
            get
            {
                if (_destroyCts == null)
                {
                    _destroyCts = new CancellationTokenSource();
                }
                return _destroyCts.Token;
            }
        }

        private CancellationTokenSource _destroyCts;

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Preview Golden Aura")]
        private void PreviewGoldenAura()
        {
            SetAuraTheme(AuraTheme.Golden);
        }

        [ContextMenu("Preview Mystical Aura")]
        private void PreviewMysticalAura()
        {
            SetAuraTheme(AuraTheme.Mystical);
        }

        [ContextMenu("Preview Transformed Aura")]
        private void PreviewTransformedAura()
        {
            SetAuraTheme(AuraTheme.Transformed);
        }

        [ContextMenu("Test Burst")]
        private void TestBurst()
        {
            TriggerBurst(1f);
        }
#endif

        #endregion
    }

    /// <summary>
    /// Predefined aura color themes for different states.
    /// </summary>
    public enum AuraTheme
    {
        Golden,      // Default state — enlightenment, luxury
        Mystical,    // Special state — magic, mystery (purple)
        Transformed  // Active transformation — energy flow (cyan/teal)
    }
}
