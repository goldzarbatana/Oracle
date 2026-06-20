using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    /// <summary>
    /// Configuration asset containing AnimationCurves for difficulty scaling.
    /// Maps difficulty percentage (0..100) to various parameter multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "Pro_DifficultyProfile", menuName = "BalanceOrchestrator/Pro Difficulty Profile", order = 0)]
    public class Pro_DifficultyProfile : ScriptableObject
    {
        [TextArea(3, 10)]
        [Tooltip("Description of this difficulty profile.")]
        public string description;

        [Tooltip("Default economy profile for this difficulty profile. Can be overridden in LevelData.")]
        public Pro_EconomyProfile defaultEconomyProfile;

        [Header("Multiplier Curves (X: 0..100 difficulty percentage)")]
        [Tooltip("Enemy health multiplier curve.")]
        public AnimationCurve enemyHealthMul = AnimationCurve.Linear(0, 1f, 100, 1.5f);

        [Tooltip("Enemy movement speed multiplier curve.")]
        public AnimationCurve enemySpeedMul = AnimationCurve.Linear(0, 1f, 100, 1.3f);

        [Tooltip("Enemy spawn chance/density multiplier curve.")]
        public AnimationCurve enemySpawnChanceMul = AnimationCurve.Linear(0, 1f, 100, 1.4f);

        [Tooltip("Wave delay multiplier curve (smaller values mean faster waves).")]
        public AnimationCurve waveDelayMul = AnimationCurve.Linear(0, 1.2f, 100, 0.6f);

        [Header("Optional Multipliers")]
        [Tooltip("Completion reward (coins/resources) multiplier curve.")]
        public AnimationCurve rewardMul = AnimationCurve.Linear(0, 1f, 100, 1.25f);

        [Tooltip("Enemy ability/AI intensity multiplier curve.")]
        public AnimationCurve abilityIntensityMul = AnimationCurve.Linear(0, 1f, 100, 1.2f);

        [Space]
        [Range(0, 100)] 
        public float defaultDifficultyPercentage = 30f;

        public float EvaluateEnemyHealth(float percent) => EvaluateClamped(enemyHealthMul, percent);
        public float EvaluateEnemySpeed(float percent) => EvaluateClamped(enemySpeedMul, percent);
        public float EvaluateEnemySpawnChance(float percent) => EvaluateClamped(enemySpawnChanceMul, percent);
        public float EvaluateWaveDelay(float percent) => EvaluateClamped(waveDelayMul, percent);
        public float EvaluateReward(float percent) => EvaluateClamped(rewardMul, percent);
        public float EvaluateAbilityIntensity(float percent) => EvaluateClamped(abilityIntensityMul, percent);

        private static float EvaluateClamped(AnimationCurve curve, float percent)
        {
            if (curve == null) return 1f;
            var x = Mathf.Clamp(percent, 0f, 100f);
            var y = curve.Evaluate(x);
            return Mathf.Max(0f, y);
        }
    }
}
