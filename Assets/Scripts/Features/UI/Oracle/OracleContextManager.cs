namespace TimeAura.Core.Services
{
    public enum UIContext
    {
        Nexus,
        Aura,
        Radar,
        Vault,
        Sanctuary,
        Support,
        Harmony
    }

    /// <summary>
    /// OracleContextManager - Manages the current UI context to provide adaptive AI behavior.
    /// </summary>
    public static class OracleContextManager
    {
        public static UIContext CurrentContext { get; private set; } = UIContext.Nexus;

        public static void SetContext(UIContext context)
        {
            CurrentContext = context;
            UnityEngine.Debug.Log($"[OracleContext] 🧠 Context switched to: {context}");
        }

        public static string GetContextDescription()
        {
            return CurrentContext switch
            {
                UIContext.Aura => "The Master is refining their Aura Pillars and resonance. Help them find their place in the spectrum.",
                UIContext.Radar => "The Master is scanning the Nexus for Symmetries (matches). Analyze the potential social bonds.",
                UIContext.Vault => "The Master is in their Vault, managing their Legacy, profile, and Horas balance.",
                UIContext.Sanctuary => "The Master is in the Oracle Sanctuary, seeking deep wisdom or philosophical guidance.",
                UIContext.Support => "The Master is considering supporting the Nexus creators. Express gratitude and honor.",
                UIContext.Harmony => "A Harmony ritual (chat and exchange) is taking place between two Masters.",
                _ => "The Master is at the Nexus hub, deciding which ritual to initiate next."
            };
        }

        public static string GetSystemPrompt(string languageCode)
        {
            string context = GetContextDescription();
            return $"{context}\n\nIMPORTANT: You must provide your response strictly in the following language: {languageCode}. Use local cultural nuances appropriate for this language.";
        }
    }
}
