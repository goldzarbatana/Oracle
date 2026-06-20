using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Localization;
using TimeAura.Features.Data;
using UnityEngine;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// AuraOracleMock - A temporary implementation of the Aura Oracle.
    /// Provides random mystical results until Gemini API is fully integrated.
    /// </summary>
    public class AuraOracleMock : IAuraOracleService
    {
        public event System.Action<OracleStatusUpdate> OnStatusUpdate;

        public void Whisper(string message, OracleState state = OracleState.Active)
        {
            OnStatusUpdate?.Invoke(new OracleStatusUpdate { Message = message, State = state });
            Debug.Log($"[Oracle Mock] Whisper: {message} ({state})");
        }

        public async UniTask<AuraOracleVerdict> PredictAuraAsync(List<string> gifts)
        {
            await UniTask.Delay(1500);

            string titleKey = DetermineTitleKey(gifts);
            
            var verdict = new AuraOracleVerdict
            {
                Title = titleKey, // Returning localization key instead of raw string
                ColorHex = "#FFD700", // Classic Gold
                Reason = "Your profile is being analyzed for optimal Harmony."
            };

            Debug.Log($"[Oracle Mock] Decree: {titleKey}");
            return verdict;
        }

        public async UniTask<AuraOracleVerdict> PredictEvolutionaryTitleAsync(UserProfile profile)
        {
            await UniTask.Delay(1500);
            return new AuraOracleVerdict
            {
                Title = "Лицар Багів і Конфліктів",
                ColorHex = "#8A2BE2",
                Reason = "Мобільний оракул-симулятор бачить твою містичну роботу!"
            };
        }

        public async UniTask<string> GetProphecyAsync(string chatContext)
        {
            // Placeholder: In production, this sends the last few messages to Gemini
            await UniTask.Delay(800);
            return "Prophecy: The Oracle estimates a fair exchange at 1.5 - 2.5 Horas.";
        }

        public async UniTask<string> ExplainSymmetryAsync(UserProfile currentUser, UserProfile targetUser)
        {
            await UniTask.Delay(500);
            return "👁️ The patterns of fate suggest a rare resonance between your paths.";
        }

        public async UniTask<OracleSuggestion> SuggestResonanceAsync(List<string> currentGifts, List<string> currentSeeks, string context = "", OracleTone tone = OracleTone.Mystic, string activePillarId = "")
        {
            await UniTask.Delay(1000);
            return new OracleSuggestion
            {
                SuggestedTags = new List<string> { "Innovation" },
                Whisper = "The Oracle senses a path of creative discovery."
            };
        }
        
        public async UniTask<string> GetPhilosophicalGuidanceAsync(string userMessage, string context = "Nexus")
        {
            await UniTask.Delay(1000);
            return "The stars suggest that your path is clear, but the shadows of doubt must be cast aside.";
        }

        private string DetermineTitleKey(List<string> gifts)
        {
            if (gifts == null || gifts.Count == 0) return AuraTerms.TITLE_EXPLORER;

            // Categorize gifts
            int knowledge = 0;
            int creative = 0;
            int action = 0;

            foreach (var tag in gifts)
            {
                if (tag == AuraTerms.TAG_CODE || tag == AuraTerms.TAG_STRATEGY || tag == AuraTerms.TAG_CRYPTO) 
                    knowledge++;
                else if (tag == AuraTerms.TAG_DESIGN || tag == AuraTerms.TAG_ART || tag == AuraTerms.TAG_MUSIC || tag == AuraTerms.TAG_WRITING) 
                    creative++;
                else if (tag == AuraTerms.TAG_HEALING || tag == AuraTerms.TAG_VISIONARY) 
                    action++;
            }

            if (knowledge >= creative && knowledge >= action) return AuraTerms.TITLE_MENTOR;
            if (creative >= knowledge && creative >= action) return AuraTerms.TITLE_CREATOR;
            if (action >= knowledge && action >= creative) return AuraTerms.TITLE_PRACTITIONER;

            return AuraTerms.TITLE_SEEKER;
        }
    }
}
