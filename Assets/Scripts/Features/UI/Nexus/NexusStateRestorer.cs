using System.Threading.Tasks;
using TimeAura.Features.Harmony;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Evaluates the user's current context and determines the most appropriate 
    /// UI panel to display upon entering the Nexus.
    /// This prevents monolithic routing logic inside NexusController.
    /// </summary>
    public class NexusStateRestorer
    {
        private readonly HarmonyManager _harmony;

        public NexusStateRestorer(HarmonyManager harmony)
        {
            _harmony = harmony;
        }

        /// <summary>
        /// Determines the entry point panel ID.
        /// Priority:
        /// 1. Active Harmony Session (Chamber)
        /// 2. Discovery / Radar (Default)
        /// </summary>
        public string DetermineEntryPoint()
        {
            // Priority 1: Do we have an active or pending Harmony session?
            if (_harmony != null && _harmony.HasActiveSession)
            {
                return "harmony";
            }
            
            // Note for Variant B: If we wanted to spoof the demo, we could check PlayerPrefs here.
            // if (UnityEngine.PlayerPrefs.HasKey("ActiveDemoSession")) return "harmony";

            // Priority 2: Notifications / Unread Messages (To be implemented)
            // if (hasUnreadMessages) return "sanctuary";

            // Default: Send them to the Discovery Radar to find new matches
            return "radar";
        }
    }
}
