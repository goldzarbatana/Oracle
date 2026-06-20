using UnityEngine;

namespace TimeAura.Core.Data.SO
{
    public enum OracleTone 
    { 
        Mystic, 
        Casual, 
        Business, 
        Tech 
    }

    [CreateAssetMenu(fileName = "OraclePersona_", menuName = "TimeAura/Data/OraclePersona")]
    public class OraclePersonaSO : ScriptableObject
    {
        [Header("Persona Identity")]
        public OracleTone Tone;

        [Header("AI Configuration")]
        [TextArea(5, 10)]
        public string SystemPrompt;
        
        [Range(0f, 1f)]
        public float Temperature = 0.7f;
        public int MaxTokens = 250;
        
        [Header("Responses")]
        public string[] GreetingVariations;
        public string FallbackMessage;
    }
}
