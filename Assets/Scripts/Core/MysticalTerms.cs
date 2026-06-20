namespace TimeAura.Core
{
    /// <summary>
    /// Mystical terminology constants for Time Aura.
    /// Use these terms consistently across all UI to maintain "Luxury Mysticism" theme.
    /// "In words, we encode reality. Choose them with presence."
    /// </summary>
    public static class MysticalTerms
    {
        // Core Identity
        public const string User = "Adept";
        public const string Users = "Adepts";
        public const string Avatar = "Visage";
        public const string Profile = "Aura";

        // Content
        public const string Post = "Chronicle";
        public const string Posts = "Chronicles";
        public const string Feed = "Convergence";
        public const string Story = "Fate";

        // Interactions
        public const string Like = "Transform";
        public const string Liked = "Transformed";
        public const string Likes = "Transforms";
        public const string Comment = "Connection";
        public const string Comments = "Connections";
        public const string Share = "Resonate";
        public const string Follow = "Align";
        public const string Following = "Aligned With";
        public const string Followers = "Convergent Souls";

        // Authentication & Navigation
        public const string Login = "Initiation";
        public const string Logout = "Dissolution";
        public const string Register = "Awakening";
        public const string Welcome = "The Temple Opens";

        // States & Status
        public const string Status = "State";
        public const string Online = "Present";
        public const string Offline = "In Stillness";
        public const string Active = "Converging";
        public const string TimeValue = "Vector";

        // UI Actions
        public const string Loading = "Converging...";
        public const string Success = "Aligned";
        public const string Error = "The Aura Flickered";
        public const string Retry = "Realign";
        public const string Cancel = "Release";
        public const string Confirm = "Manifest";

        // Time References
        public const string Now = "This Moment";
        public const string Today = "This Cycle";
        public const string JustNow = "Just Now";

        // Asset Groups (Addressables)
        public const string RelicsGroup = "Relics"; // UI artifacts
        public const string VisagesGroup = "Visages"; // Avatars
        public const string ChroniclesGroup = "Chronicles"; // Posts/content
        public const string AuraShardsGroup = "Aura_Shards"; // Effects/UGC
        public const string LocalizationGroup = "Localization"; // Languages

        // Shader & Effects
        public const string GoldenAura = "Golden Aura";
        public const string MysticalAura = "Mystical Aura";
        public const string TransformedAura = "Transformed Aura";

        // Premium Features
        public const string Premium = "Enlightened";
        public const string Subscribe = "Ascend";
        public const string Subscription = "Ascension";

        // Economic Realms (Ether + Matter)
        public const string RealmEther = "Ether";
        public const string RealmMatter = "Matter";
        public const string CurrencyHoras = "Horas";
        public const string CurrencyQuants = "Quants";
        public const string UnitAtoms = "Atoms";
        public const string UnitWaves = "Waves";

        /// <summary>
        /// Format user count with mystical terminology.
        /// </summary>
        public static string FormatUserCount(int count)
        {
            if (count == 1) return $"{count} {User}";
            return $"{FormatNumber(count)} {Users}";
        }

        /// <summary>
        /// Format transform (like) count.
        /// </summary>
        public static string FormatTransformCount(int count)
        {
            if (count == 0) return "Untransformed";
            if (count == 1) return "1 Transform";
            return $"{FormatNumber(count)} {Likes}";
        }

        /// <summary>
        /// Format connection (comment) count.
        /// </summary>
        public static string FormatConnectionCount(int count)
        {
            if (count == 0) return "No Connections";
            if (count == 1) return "1 Connection";
            return $"{FormatNumber(count)} {Comments}";
        }

        /// <summary>
        /// Format large numbers (1K, 1M, etc.)
        /// </summary>
        private static string FormatNumber(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 1000000) return $"{count / 1000f:0.#}K";
            if (count < 1000000000) return $"{count / 1000000f:0.#}M";
            return $"{count / 1000000000f:0.#}B";
        }

        /// <summary>
        /// Get mystical status message for various states.
        /// </summary>
        public static class StatusMessages
        {
            public const string Connecting = "Opening the portal...";
            public const string Loading = "The convergence reveals itself...";
            public const string Uploading = "Weaving your chronicle into the tapestry...";
            public const string Processing = "The temple contemplates...";
            public const string Success = "The alignment is complete.";
            public const string Error = "A disturbance in the flow.";
            public const string NetworkError = "The connection falters.";
            public const string Timeout = "The portal closed before arrival.";
            public const string Unauthorized = "The temple does not recognize you.";
            public const string Empty = "The convergent silence... Nothing to show.";
            public const string EndOfFeed = "You've reached the edge of the known convergence.";
        }

        /// <summary>
        /// Time-based mystical greetings.
        /// </summary>
        public static class Greetings
        {
            public const string Morning = "The dawn illuminates your path, Adept.";
            public const string Afternoon = "The sun honors your presence, Adept.";
            public const string Evening = "The dusk embraces your arrival, Adept.";
            public const string Night = "The stars guide you, Adept.";

            /// <summary>
            /// Get greeting based on current hour (24h format).
            /// </summary>
            public static string GetTimeBasedGreeting()
            {
                var hour = System.DateTime.Now.Hour;

                if (hour >= 5 && hour < 12) return Morning;
                if (hour >= 12 && hour < 17) return Afternoon;
                if (hour >= 17 && hour < 21) return Evening;
                return Night;
            }
        }
    }
}
