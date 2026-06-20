using UnityEngine;

namespace TimeAura.Core.Data.SO
{
    [CreateAssetMenu(fileName = "ResonanceEconomy", menuName = "TimeAura/Data/ResonanceEconomy")]
    public class ResonanceEconomySO : ScriptableObject
    {
        public float InitialHorasBonus;
        public float EscrowFeePercent;
        public float dailyLoginReward;
        public float burstCost;
        public float maxNegativeHoras;
    }
}
