using System;
using UnityEngine;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// Calculates and manages Resonance between Masters.
    /// "Resonance is the echo of synchronicity across the threads of time."
    /// </summary>
    public static class HarmonyResonanceCalculator
    {
        /// <summary>
        /// Calculates automatic Resonance based on Harmony cycle metrics.
        /// </summary>
        public static ResonanceLevel CalculateResonance(HarmonySession session)
        {
            if (session == null) return ResonanceLevel.Neutral;

            float score = 0f;

            // Duration factor (optimal: 3-7 minutes for a deep cycle)
            float duration = session.Duration;
            if (duration >= 180 && duration <= 420) score += 2f;
            else if (duration >= 120 && duration <= 600) score += 1f;

            // Horas exchanged (higher = stronger energetic alignment)
            if (session.horasExchanged >= 50) score += 2f;
            else if (session.horasExchanged >= 30) score += 1f;

            // Cosmic alignment (Evening peak)
            var hour = DateTime.UtcNow.Hour;
            if (hour >= 18 && hour <= 22) score += 1f;

            // Convert score to ResonanceLevel
            if (score >= 5f) return ResonanceLevel.Transcendent;
            if (score >= 4f) return ResonanceLevel.Synchronized;
            if (score >= 3f) return ResonanceLevel.Harmonious;
            if (score >= 2f) return ResonanceLevel.Neutral;
            return ResonanceLevel.Dissonant;
        }

        public static string GetResonanceName(ResonanceLevel level)
        {
            return level switch
            {
                ResonanceLevel.Dissonant => "Дисонанс",
                ResonanceLevel.Neutral => "Нейтральний",
                ResonanceLevel.Harmonious => "Гармонійний",
                ResonanceLevel.Synchronized => "Синхронізований",
                ResonanceLevel.Transcendent => "Трансцендентний",
                _ => "Невідомий"
            };
        }

        public static Color GetResonanceColor(ResonanceLevel level)
        {
            return level switch
            {
                ResonanceLevel.Dissonant => new Color(0.8f, 0.2f, 0.2f),
                ResonanceLevel.Neutral => new Color(0.7f, 0.7f, 0.7f),
                ResonanceLevel.Harmonious => new Color(0.4f, 0.8f, 0.9f),
                ResonanceLevel.Synchronized => new Color(0.9f, 0.7f, 0.3f),
                ResonanceLevel.Transcendent => new Color(1f, 0.9f, 0.5f),
                _ => Color.white
            };
        }

        public static float GetResonanceMultiplier(ResonanceLevel level)
        {
            return level switch
            {
                ResonanceLevel.Dissonant => 0.5f,
                ResonanceLevel.Neutral => 1.0f,
                ResonanceLevel.Harmonious => 1.2f,
                ResonanceLevel.Synchronized => 1.5f,
                ResonanceLevel.Transcendent => 2.0f,
                _ => 1.0f
            };
        }

        public static int CalculateExperience(HarmonySession session, ResonanceLevel resonance)
        {
            int baseXP = (int)(session.horasExchanged / 2);
            float multiplier = GetResonanceMultiplier(resonance);
            return Mathf.RoundToInt(baseXP * multiplier);
        }

        public static bool ShouldPromoteMaster(int completedSessions, float averageResonance)
        {
            // Initiate → Master: 10 cycles, avg 3.0+
            if (completedSessions >= 10 && averageResonance >= 3.0f) return true;

            // Master → Grandmaster: 50 cycles, avg 4.0+
            if (completedSessions >= 50 && averageResonance >= 4.0f) return true;

            return false;
        }
    }
}
