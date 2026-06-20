using System;
using UnityEngine;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// Represents an active Harmony session between two Masters.
    /// "Through the exchange of time, we weave the tapestry of shared destiny."
    /// </summary>
    [Serializable]
    public class HarmonySession
    {
        public string sessionId;
        public string initiatorUserId;
        public string recipientUserId;
        public DateTime startTime;
        public DateTime? endTime;
        public long horasExchanged;
        public HarmonyStatus status;
        public ResonanceLevel? finalResonance;
        public string contractTerms; // The conditions of the Harmony
        public TimeAura.Core.ContractRealm realm;
        public long fiatAmountCents;
        public long lockedMinutes;    // Minutes frozen in Escrow
        public ContractType contractType = ContractType.TimeBarter;
        public DateTime? lastInteractionTime; // For 24h timeout tracking

        /// <summary>
        /// Duration of the session in seconds.
        /// </summary>
        public float Duration => endTime.HasValue ?
            (float)(endTime.Value - startTime).TotalSeconds :
            (float)(DateTime.UtcNow - startTime).TotalSeconds;

        /// <summary>
        /// Progress of the Harmony exchange (0-1).
        /// </summary>
        public float Progress => Mathf.Clamp01(Duration / 300f); // 5 minutes standard cycle

        public HarmonySession(string initiatorId, string recipientId, long horas)
        {
            sessionId = Guid.NewGuid().ToString();
            initiatorUserId = initiatorId;
            recipientUserId = recipientId;
            startTime = DateTime.UtcNow;
            horasExchanged = horas;
            status = HarmonyStatus.ActiveChannel;
        }

        public void Complete(ResonanceLevel resonance)
        {
            endTime = DateTime.UtcNow;
            finalResonance = resonance;
            status = HarmonyStatus.Completed;
        }

        public void Dissolve()
        {
            endTime = DateTime.UtcNow;
            status = HarmonyStatus.Dissolved;
        }
    }

    public enum HarmonyStatus
    {
        PendingMatch,      // Awaiting mutual resonance (Step 1)
        ActiveChannel,     // Harmony Channel is open (Step 2)
        OfferingSubmitted, // Client submitted offering, waiting for Master review (Step 2.05)
        RitualConfirmed,   // Master accepted, Terms agreed, Horas locked (Escrow - Step 2.1)
        ResultSubmitted,   // Master submitted result, waiting for Client review (Step 3.0)
        Completed,         // Successful exchange (Step 3.1)
        Dissolved,         // Peaceful separation (Step 2.2)
        Disputed,          // Conflict detected, Oracle summoned (Step 3.3)
        Breached           // Violation of the cycle (Step 2.3)
    }

    public enum ResonanceLevel
    {
        Dissonant = 1,      // Out of sync
        Neutral = 2,        // Static exchange
        Harmonious = 3,     // In rhythm
        Synchronized = 4,   // Unified flow
        Transcendent = 5    // Perfect symmetry (Master level)
    }

    public enum ContractType
    {
        Fiat,
        TimeBarter
    }
}
