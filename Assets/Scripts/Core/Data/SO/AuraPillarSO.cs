using UnityEngine;
using System.Collections.Generic;

namespace TimeAura.Core.Data.SO
{
    [CreateAssetMenu(fileName = "AuraPillar", menuName = "TimeAura/Data/AuraPillar")]
    public class AuraPillarSO : ScriptableObject
    {
        public string Id;
        public string LocalizationKey;
        public Sprite Icon;
        public Color ThemeColor;
        public int weight;
        public bool isActive;
        public List<string> tags;
    }
}
