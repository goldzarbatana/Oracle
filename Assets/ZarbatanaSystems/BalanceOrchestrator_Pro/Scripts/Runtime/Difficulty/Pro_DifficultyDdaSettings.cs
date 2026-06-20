using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    [CreateAssetMenu(fileName = "Pro_DifficultyDdaSettings", menuName = "BalanceOrchestrator/Pro DDA Settings", order = 1)]
    public class Pro_DifficultyDdaSettings : ScriptableObject
    {
        [Header("Enable DDA")]
        public bool enabled = false;

        [Header("Correction Range (0..100)")]
        [Tooltip("Minimum allowed difficulty percentage after correction")][Range(0, 100)] public float minPercent = 10f;
        [Tooltip("Maximum allowed difficulty percentage after correction")][Range(0, 100)] public float maxPercent = 90f;

        [Header("Step and Cooldown")]
        [Tooltip("How much difficulty percentage changes per adjustment step")][Range(0, 20)] public float step = 5f;
        [Tooltip("Minimum cooldown in seconds between difficulty adjustments")][Min(0f)] public float cooldownSeconds = 120f;

        [Header("Hysteresis")]
        [Tooltip("Sensitivity deadzone. If the metric is within deadzone from target, no changes are made")][Range(0, 1)] public float deadzone01 = 0.05f;

        [Header("Metric Targets (0..1)")]
        [Tooltip("Target safety ratio. 1=very easy, 0=very hard")][Range(0, 1)] public float targetSafety01 = 0.5f;
        [Tooltip("Target level completion win rate")][Range(0, 1)] public float targetWinRate01 = 0.6f;
    }
}
