using System;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    [DefaultExecutionOrder(-500)]
    public class Pro_DifficultyManager : MonoBehaviour
    {
        public static Pro_DifficultyManager Instance { get; private set; }

        [SerializeField] private Pro_DifficultyProfile profile;
        [SerializeField, Range(0, 100)] private float difficultyPercentageOverride = -1f; // <0 => ignore, read from data adapter

        [Header("DDA (Adaptive Difficulty)")]
        [SerializeField] private bool ddaEnabled = false;
        [SerializeField, Range(0, 100)] private float ddaMinPercent = 10f;
        [SerializeField, Range(0, 100)] private float ddaMaxPercent = 90f;
        [SerializeField, Range(0, 20)] private float ddaStep = 5f;
        [SerializeField, Min(0f)] private float ddaCooldownSeconds = 120f;
        [SerializeField, Range(0, 1)] private float ddaDeadzone01 = 0.05f;
        [SerializeField, Range(0, 1)] private float ddaTargetSafety01 = 0.5f;
        [SerializeField, Range(0, 1)] private float ddaTargetWinRate01 = 0.6f;

        private Pro_DifficultySnapshot _snapshot;
        public event Action<Pro_DifficultySnapshot> OnChanged;

        private float _currentPercent; // 0..100 — last evaluated value
        private float _lastAdjustTime = -9999f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(Pro_DifficultyProfile p, float percent)
        {
            profile = p;
            Evaluate(percent);
        }

        public void Evaluate(float percent)
        {
            if (profile == null)
            {
                Debug.LogWarning("[Pro DDA] Pro_DifficultyProfile is missing. Using identity multipliers.");
                _snapshot = Pro_DifficultySnapshot.Identity;
                OnChanged?.Invoke(_snapshot);
                return;
            }

            var x = Mathf.Clamp(percent < 0 ? profile.defaultDifficultyPercentage : percent, 0f, 100f);
            _currentPercent = x;
            _snapshot = new Pro_DifficultySnapshot
            (
                enemyHealthMul: profile.EvaluateEnemyHealth(x),
                enemySpeedMul: profile.EvaluateEnemySpeed(x),
                enemySpawnChanceMul: profile.EvaluateEnemySpawnChance(x),
                waveDelayMul: profile.EvaluateWaveDelay(x),
                rewardMul: profile.EvaluateReward(x),
                abilityIntensityMul: profile.EvaluateAbilityIntensity(x)
            );
            OnChanged?.Invoke(_snapshot);
        }

        public Pro_DifficultySnapshot Get() => _snapshot;
        public float GetCurrentPercent() => _currentPercent;

        // Setters for external runtime control
        public void SetDdaEnabled(bool enabled) { ddaEnabled = enabled; }
        public void SetDdaRange(float minPercent, float maxPercent) { ddaMinPercent = Mathf.Clamp(minPercent, 0, 100); ddaMaxPercent = Mathf.Clamp(maxPercent, 0, 100); }
        public void SetDdaStep(float step) { ddaStep = Mathf.Clamp(step, 0f, 20f); }
        public void SetDdaCooldown(float seconds) { ddaCooldownSeconds = Mathf.Max(0f, seconds); }
        public void SetDdaDeadzone(float deadzone01) { ddaDeadzone01 = Mathf.Clamp01(deadzone01); }
        public void SetDdaTargets(float safety01, float winRate01) { ddaTargetSafety01 = Mathf.Clamp01(safety01); ddaTargetWinRate01 = Mathf.Clamp01(winRate01); }

        // Bootstrap from a decoupled level data adapter
        public void BootstrapFrom(Pro_LevelDataAdapter levelDataAdapter)
        {
            if (profile == null && levelDataAdapter != null)
                profile = levelDataAdapter.ResolveProfile();

            var percent = difficultyPercentageOverride >= 0 ? difficultyPercentageOverride : (levelDataAdapter?.GetDifficultyPercentage() ?? -1f);
            Evaluate(percent);
        }

        // ===== DDA Adaptive Difficulty Correction =====
        public void TryReport(Pro_DdaEvent evt, float value01, float weight = 1f)
        {
            if (!ddaEnabled) return;

            float now = Time.unscaledTime;
            if (now - _lastAdjustTime < ddaCooldownSeconds)
            {
                return; // cooldown
            }

            float target = 0.5f;
            switch (evt)
            {
                case Pro_DdaEvent.SafetyRatio01:
                    target = Mathf.Clamp01(ddaTargetSafety01);
                    break;
                case Pro_DdaEvent.Win:
                    target = Mathf.Clamp01(ddaTargetWinRate01);
                    break;
                case Pro_DdaEvent.Lose:
                    target = Mathf.Clamp01(ddaTargetWinRate01);
                    break;
                case Pro_DdaEvent.Stars01:
                    target = 0.66f; // target ~2/3 of stars on average
                    break;
            }

            float measurement = Mathf.Clamp01(value01);
            float dz = Mathf.Clamp01(ddaDeadzone01);
            float delta = 0f;

            if (measurement > target + dz)
            {
                // Too easy -> increase difficulty
                delta = Mathf.Abs(ddaStep) * Mathf.Clamp01(weight);
            }
            else if (measurement < target - dz)
            {
                // Too hard -> decrease difficulty
                delta = -Mathf.Abs(ddaStep) * Mathf.Clamp01(weight);
            }
            else
            {
                return; // within deadzone — no changes
            }

            float newPercent = Mathf.Clamp(_currentPercent + delta, ddaMinPercent, ddaMaxPercent);
            if (!Mathf.Approximately(newPercent, _currentPercent))
            {
                Evaluate(newPercent);
                _lastAdjustTime = now;
                Debug.Log($"[Pro DDA] {evt} value={measurement:0.00} target={target:0.00} -> percentage {_currentPercent:0.0}");
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticData()
        {
            Instance = null;
        }
#endif
    }

    [Serializable]
    public struct Pro_DifficultySnapshot
    {
        public float enemyHealthMul;
        public float enemySpeedMul;
        public float enemySpawnChanceMul;
        public float waveDelayMul;
        public float rewardMul;
        public float abilityIntensityMul;

        public Pro_DifficultySnapshot(float enemyHealthMul, float enemySpeedMul, float enemySpawnChanceMul, float waveDelayMul, float rewardMul, float abilityIntensityMul)
        {
            this.enemyHealthMul = enemyHealthMul;
            this.enemySpeedMul = enemySpeedMul;
            this.enemySpawnChanceMul = enemySpawnChanceMul;
            this.waveDelayMul = waveDelayMul;
            this.rewardMul = rewardMul;
            this.abilityIntensityMul = abilityIntensityMul;
        }

        public static readonly Pro_DifficultySnapshot Identity = new Pro_DifficultySnapshot(1f, 1f, 1f, 1f, 1f, 1f);
    }

    // Decoupled abstract adapter for integrating DDA into any game's level data structure
    public abstract class Pro_LevelDataAdapter
    {
        public abstract float GetDifficultyPercentage(); // 0..100
        public abstract Pro_DifficultyProfile ResolveProfile();
    }

    public enum Pro_DdaEvent
    {
        Win,
        Lose,
        SafetyRatio01,
        Stars01,
    }
}
