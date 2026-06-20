using UnityEngine;
using System.Collections.Generic;

namespace TimeAura.Core.Data.SO
{
    [CreateAssetMenu(fileName = "LegacyTier", menuName = "TimeAura/Data/LegacyTier")]
    public class LegacyTierSO : ScriptableObject
    {
        public string TierNameKey;
        public int RequiredLevel;
        public Sprite BadgeIcon;
        public GameObject AuraVFX;
        public List<string> TierPerks;
    }
}
