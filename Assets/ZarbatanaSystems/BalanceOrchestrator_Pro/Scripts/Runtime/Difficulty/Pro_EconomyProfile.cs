using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    /// <summary>
    /// Configuration profile for level economy: rewards, score/energy actions, and star thresholds.
    /// Scales dynamically with DDA multipliers.
    /// </summary>
    [CreateAssetMenu(fileName = "Pro_EconomyProfile", menuName = "BalanceOrchestrator/Pro Economy Profile", order = 0)]
    public class Pro_EconomyProfile : ScriptableObject
    {
        [TextArea(3, 10)]
        [Tooltip("Description of this economy profile.")]
        public string description;

        [Header("Energy for Player Actions")]
        [Tooltip("Base energy awarded for a correct action/hit (before DDA multipliers).")]
        public int baseEnergyPerCorrectAnimal = 10;

        [Tooltip("Base energy deducted for an incorrect action/hit (usually not scaled by difficulty).")]
        public int baseEnergyDeductedForWrongAnimalHit = 2;

        [Header("Coins & Completion Rewards")]
        [Tooltip("Base coins awarded for level completion (before DDA multipliers).")]
        public int baseCoinsForLevelCompletion = 50;

        [Tooltip("Base bonus coins per star earned (before DDA multipliers).")]
        public int baseBonusCoinsPerStar = 25;

        [Header("Star Thresholds")]
        [Tooltip("Default star conditions (energy/time limits) defined by this economy profile.")]
        public Pro_StarThresholds profileStarThresholds = new Pro_StarThresholds();
    }
}
