using UnityEngine;
using System.Collections.Generic;

namespace TimeAura.Core.Data.SO
{
    /// <summary>
    /// OracleSO — The Soul of an Oracle Companion.
    /// Each Oracle has a unique personality, resonant skill tags, and a base system instruction.
    /// Masters equip Oracles to augment their Aura Build in the Chamber.
    /// "Every Oracle carries the echo of a thousand rituals."
    /// </summary>
    [CreateAssetMenu(fileName = "Oracle", menuName = "TimeAura/Data/Oracle")]
    public class OracleSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;                        // e.g. "oracle_coder"
        public string LocalizationKey;           // e.g. "ORACLE_CODER" → localized display name
        public string DisplayName;               // Fallback name if localization not found

        [Header("Personality")]
        [TextArea(3, 8)]
        public string BasePersonality;           // Base system instruction fragment
        // e.g. "You are a strict, concise AI architect. Output only clean C# patterns."

        [Header("Resonance")]
        public List<string> ResonantTags;        // Tags that trigger high resonance, e.g. ["programming","code","architecture"]

        [Header("Visuals")]
        public Color ThemeColor = Color.yellow;
        public Sprite Icon;

        [Header("Economy")]
        public int Tier = 0;                     // 0 = Free, 1 = Advanced, 2 = Legendary
    }
}
