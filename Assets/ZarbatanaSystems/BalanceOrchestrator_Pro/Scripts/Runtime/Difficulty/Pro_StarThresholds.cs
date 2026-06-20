using System;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    /// <summary>
    /// Represents thresholds (e.g. energy score and time limits) for earning stars on a level.
    /// </summary>
    [Serializable]
    public class Pro_StarThresholds
    {
        [Header("Energy Thresholds")]
        [Tooltip("Energy score required for 1 star.")]
        [Min(1)]
        public int energyFor1Star = 50;

        [Tooltip("Energy score required for 2 stars.")]
        [Min(1)]
        public int energyFor2Stars = 100;

        [Tooltip("Energy score required for 3 stars.")]
        [Min(1)]
        public int energyFor3Stars = 150;

        [Header("Time Thresholds")]
        [Tooltip("Should time be evaluated to award stars?")]
        public bool evaluateTimeForStars = false;

        [Tooltip("Time limit in seconds to achieve 3 stars.")]
        [Min(0.1f)]
        public float timeFor3StarsInSeconds = 60f;

        [Tooltip("Time limit in seconds to achieve 2 stars.")]
        [Min(0.1f)]
        public float timeFor2StarsInSeconds = 90f;

        [Tooltip("Time limit in seconds to achieve 1 star.")]
        [Min(0.1f)]
        public float timeFor1StarInSeconds = 120f;
    }
}
