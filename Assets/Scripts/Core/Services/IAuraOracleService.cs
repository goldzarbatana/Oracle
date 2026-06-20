using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Data;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// AuraOracleVerdict - The ШІ's decree on a Master's soul spectrum.
    /// </summary>
    public struct AuraOracleVerdict
    {
        public string ColorHex;
        public string Title;
        public string Reason;
    }

    public struct OracleSuggestion
    {
        public List<string> SuggestedTags;
        public string Whisper; // Mystical insight message
    }

    public enum OracleState
    {
        Closed,
        Active,
        Processing,
        Alert,
        Privacy,
        DebtAlert
    }

    public struct OracleStatusUpdate
    {
        public OracleState State;
        public string Message;
    }

    public interface IAuraOracleService
    {
        event Action<OracleStatusUpdate> OnStatusUpdate;
        void Whisper(string message, OracleState state = OracleState.Active);

        UniTask<AuraOracleVerdict> PredictAuraAsync(List<string> gifts);
        UniTask<string> GetProphecyAsync(string chatContext);

        /// <summary>
        /// Provides a mystical explanation of the symmetry between two Masters.
        /// </summary>
        UniTask<string> ExplainSymmetryAsync(UserProfile currentUser, UserProfile targetUser);

        /// <summary>
        /// Analyzes chosen tags and optional context to suggest the next resonance point.
        /// </summary>
        UniTask<OracleSuggestion> SuggestResonanceAsync(List<string> currentGifts, List<string> currentSeeks, string context = "", OracleTone tone = OracleTone.Mystic, string activePillarId = "");
        UniTask<string> GetPhilosophicalGuidanceAsync(string userMessage, string context = "Nexus");
        UniTask<AuraOracleVerdict> PredictEvolutionaryTitleAsync(UserProfile profile);
    }
}
