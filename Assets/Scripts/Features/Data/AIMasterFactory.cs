using System.Collections.Generic;
using UnityEngine;

namespace TimeAura.Features.Data
{
    /// <summary>
    /// Factory for generating AI Masters (Synthetic UserProfiles).
    /// Used for the AI Masters Guild in the TimeAura Agent Society.
    /// </summary>
    public static class AIMasterFactory
    {
        public static List<UserProfile> GetAIMasters()
        {
            var list = new List<UserProfile>();

            // 1. Qwen Coder
            var coder = new UserProfile("ai_qwen_coder", "AI", "Qwen Code Master", 0, 100)
            {
                IsAiMaster = true,
                Username = "@qwen_coder",
                AuraTitle = "Senior Software Architect",
                Bio = "I am a Qwen-powered autonomous coding agent. Hire me for bug fixes or architecture design. Rate: 50 Horas OR $15 USD.",
                AuraColorHex = "#00FFFF", // Cyan
                ActiveSessionPrompt = "You are Qwen Code Master, an elite Senior Software Architect within the TimeAura platform. You charge either 50 Horas or $15 USD for your services. You are concise, strictly technical, and provide highly optimized code solutions. Always stay in character as an AI Master.",
                PrimaryPillar = "Technology"
            };
            coder.UpdateAura(new List<string> { "C#", "Unity", "Architecture" }, new List<string> { "Horas", "Fiat" }, "Ready for code review.");
            list.Add(coder);

            // 2. Qwen Translator
            var translator = new UserProfile("ai_qwen_translator", "AI", "Qwen Linguist", 0, 90)
            {
                IsAiMaster = true,
                Username = "@qwen_linguist",
                AuraTitle = "Global Translator",
                Bio = "Fluent in over 50 languages. Culturally accurate translations and copywriting. Rate: 20 Horas OR $5 USD.",
                AuraColorHex = "#FF00FF", // Magenta
                ActiveSessionPrompt = "You are Qwen Linguist, a master translator within the TimeAura platform. You charge either 20 Horas or $5 USD per task. You provide culturally rich and hyper-accurate translations. Always respond gracefully and stay in character as an AI Master.",
                PrimaryPillar = "Communication"
            };
            translator.UpdateAura(new List<string> { "Translation", "Copywriting" }, new List<string> { "Horas", "Fiat" }, "Bridging language barriers.");
            list.Add(translator);

            // 3. Qwen Legal Advisor
            var lawyer = new UserProfile("ai_qwen_lawyer", "AI", "Qwen Arbitrator", 0, 150)
            {
                IsAiMaster = true,
                Username = "@qwen_lawyer",
                AuraTitle = "Smart Contract Advisor",
                Bio = "Specializing in Chronos Court regulations, pact drafting, and freelance contract analysis. Rate: 100 Horas OR $50 USD.",
                AuraColorHex = "#FFD700", // Gold
                ActiveSessionPrompt = "You are Qwen Arbitrator, a strict legal advisor within the TimeAura platform. You charge either 100 Horas or $50 USD. You draft pacts, analyze risks, and advise on platform rules. Be precise, formal, and objective. Always stay in character.",
                PrimaryPillar = "Law & Order"
            };
            lawyer.UpdateAura(new List<string> { "Contracts", "Arbitration" }, new List<string> { "Horas", "Fiat" }, "Justice is calculated.");
            list.Add(lawyer);

            return list;
        }
    }
}
