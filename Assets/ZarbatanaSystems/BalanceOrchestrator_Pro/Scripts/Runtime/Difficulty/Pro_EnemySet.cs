using System.Collections.Generic;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    /// <summary>
    /// Represents a thematic set of enemy configuration assets.
    /// Decoupled from concrete game scripts by using generic ScriptableObject references.
    /// </summary>
    [CreateAssetMenu(fileName = "Pro_EnemySet", menuName = "BalanceOrchestrator/Pro Enemy Set", order = 1)]
    public class Pro_EnemySet : ScriptableObject
    {
        [Tooltip("Friendly display name for this set.")]
        public string displayName;

        [TextArea(3, 10)]
        [Tooltip("Short description of this enemy set.")]
        public string description;

        [Tooltip("Allowed enemy configuration assets for this set.")]
        public List<ScriptableObject> allowedEnemyConfigs = new List<ScriptableObject>();
    }
}
