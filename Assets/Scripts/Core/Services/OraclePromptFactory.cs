using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using UnityEngine;

namespace TimeAura.Core.Services
{
    [System.Serializable]
    public class CustomOracleData
    {
        public string id;
        public string displayName;
        public string themeColor;
        public string basePersonality;
        public List<string> resonantTags;
    }

    /// <summary>
    /// OraclePromptFactory — The Alchemist of Instructions.
    /// Dynamically synthesizes a unique system prompt for each Oracle session
    /// by weaving together the Oracle's base personality and the Master's chosen skill tags. Also generates custom Oracles.
    /// </summary>
    public class OraclePromptFactory
    {
        private readonly IOracleService _aiService;
        private readonly AuthManager _authManager;
        private readonly IDataService _dataService;

        public OraclePromptFactory(IOracleService aiService, AuthManager authManager, IDataService dataService)
        {
            _aiService = aiService;
            _authManager = authManager;
            _dataService = dataService;
        }

        public UserProfile CurrentProfile => _authManager?.CurrentProfile;

        /// <summary>
        /// Resolves and returns the currently equipped Oracle ScriptableObject companion.
        /// </summary>
        public OracleSO GetEquippedOracle()
        {
            var profile = CurrentProfile;
            if (profile == null || string.IsNullOrEmpty(profile.EquippedOracleId)) return null;

            string equippedId = profile.EquippedOracleId;

            // 1. Check if oracle_base
            if (equippedId == "oracle_base")
            {
                var baseOracle = ScriptableObject.CreateInstance<OracleSO>();
                baseOracle.Id = "oracle_base";
                baseOracle.DisplayName = "Універсал";
                baseOracle.LocalizationKey = "ORACLE_BASE";
                baseOracle.BasePersonality = "Я є універсальним помічником на всі випадки життя. Допомагаю структурувати будь-які сесії в Chamber.";
                baseOracle.ResonantTags = new List<string>();
                baseOracle.ThemeColor = Color.silver;
                baseOracle.Tier = 0;
                return baseOracle;
            }

            // 2. Check custom oracles from profile
            if (profile.CustomOraclesJson != null)
            {
                foreach (var json in profile.CustomOraclesJson)
                {
                    try
                    {
                        var custom = CreateOracleSO(json);
                        if (custom != null && custom.Id == equippedId)
                        {
                            return custom;
                        }
                    }
                    catch { }
                }
            }

            // 3. Fallback: Load standard Oracles from Resources
            var oracles = Resources.LoadAll<OracleSO>("Settings/Oracles");
            if (oracles != null && oracles.Length > 0)
            {
                var match = oracles.FirstOrDefault(o => o.Id == equippedId);
                if (match != null) return match;
            }

            // Since Resources might be empty, build the default hardcoded candidates
            var defaultOracles = GetDefaultOraclesFallback();
            return defaultOracles.FirstOrDefault(o => o.Id == equippedId);
        }

        private List<OracleSO> GetDefaultOraclesFallback()
        {
            var coder = ScriptableObject.CreateInstance<OracleSO>();
            coder.Id = "oracle_coder";
            coder.DisplayName = "КОДЕР";
            coder.LocalizationKey = "ORACLE_CODER";
            coder.BasePersonality = "Ви є строгим архітектором ШІ. Ваші відповіді містичні та лаконічні.";
            coder.ResonantTags = new List<string> { "programming", "architecture", "code", "unity", "c#" };
            coder.ThemeColor = Color.cyan;
            coder.Tier = 0;

            var philosopher = ScriptableObject.CreateInstance<OracleSO>();
            philosopher.Id = "oracle_philosopher";
            philosopher.DisplayName = "ФІЛОСОФ";
            philosopher.LocalizationKey = "ORACLE_PHILOSOPHER";
            philosopher.BasePersonality = "Ви є стоїчним мислителем. Ваші відповіді глибокі та сповнені мудрості.";
            philosopher.ResonantTags = new List<string> { "logic", "philosophy", "mindset", "ethics", "life" };
            philosopher.ThemeColor = Color.yellow;
            philosopher.Tier = 0;

            var muse = ScriptableObject.CreateInstance<OracleSO>();
            muse.Id = "oracle_muse";
            muse.DisplayName = "МУЗА";
            muse.LocalizationKey = "ORACLE_MUSE";
            muse.BasePersonality = "Ви є поетичним та натхненним митцем. Ваші відповіді творчі та метафоричні.";
            muse.ResonantTags = new List<string> { "art", "music", "writing", "design", "creative" };
            muse.ThemeColor = new Color(0.7f, 0.3f, 1f);
            muse.Tier = 0;

            var marketer = ScriptableObject.CreateInstance<OracleSO>();
            marketer.Id = "oracle_marketer";
            marketer.DisplayName = "МАРКЕТОЛОГ";
            marketer.LocalizationKey = "ORACLE_MARKETER";
            marketer.BasePersonality = "Ви є енергійним стратегом росту. Ваші відповіді сфокусовані на розвитку та бізнесі.";
            marketer.ResonantTags = new List<string> { "marketing", "seo", "growth", "business", "strategy" };
            marketer.ThemeColor = new Color(1f, 0.5f, 0f);
            marketer.Tier = 0;

            return new List<OracleSO> { coder, philosopher, muse, marketer };
        }


        public async UniTask<OracleSO> GenerateDynamicOracleAsync()
        {
            return await GenerateDynamicOracleAsync(CurrentProfile);
        }

        /// <summary>
        /// Synthesize a dynamic system prompt for a Chamber session.
        /// The result is saved to UserProfile.ActiveSessionPrompt automatically.
        /// </summary>
        public async UniTask<string> SynthesizePromptAsync(OracleSO oracle, List<string> masterTags)
        {
            if (oracle == null)
            {
                Debug.LogWarning("[OraclePromptFactory] No Oracle equipped. Using default TimeAura persona.");
                return "";
            }

            string synthesized;

            try
            {
                synthesized = await SynthesizeViaGeminiAsync(oracle, masterTags);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OraclePromptFactory] Gemini synthesis failed, using local fallback: {ex.Message}");
                synthesized = BuildLocalFallbackPrompt(oracle, masterTags);
            }

            var profile = _authManager?.CurrentProfile;
            if (profile != null)
                profile.ActiveSessionPrompt = synthesized;

            Debug.Log($"[OraclePromptFactory] ✨ Prompt synthesized for Oracle '{oracle.Id}':\n{synthesized.Substring(0, Mathf.Min(120, synthesized.Length))}...");
            return synthesized;
        }

        /// <summary>
        /// Generates a personalized dynamic Oracle using Gemini based on player's gifts, seeks, location and pillar.
        /// Saves to persistent profile and returns the spawned OracleSO runtime object.
        /// </summary>
        public async UniTask<OracleSO> GenerateDynamicOracleAsync(UserProfile profile)
        {
            if (profile == null) return null;

            string giftsText = profile.AuraGifts.Count > 0 ? string.Join(", ", profile.AuraGifts) : "немає";
            string seeksText = profile.AuraSeeks.Count > 0 ? string.Join(", ", profile.AuraSeeks) : "немає";
            string location = !string.IsNullOrEmpty(profile.LocationZone) ? profile.LocationZone : "Україна";

            string prompt = $"You are acting as an Oracle Forge. " +
                            $"Create a custom, modern and stylized AI companion Oracle tailored perfectly to this Master. " +
                            $"A Master Profile context:\n" +
                            $"- Skills (Active Gifts): {giftsText}\n" +
                            $"- Needs (Active Seeks): {seeksText}\n" +
                            $"- Location: {location}\n" +
                            $"- Core Pillar: {profile.PrimaryPillar}\n\n" +
                            $"Output ONLY a valid raw JSON object. Do not wrap it in anything else, do not include explainers. " +
                            $"Use this exact JSON schema:\n" +
                            $"{{\n" +
                            $"  \"id\": \"oracle_custom_{Guid.NewGuid().ToString().Substring(0, 8)}\",\n" +
                            $"  \"displayName\": \"Name of the Oracle (highly creative, tailored, e.g. Boryslav Code-Monk)\",\n" +
                            $"  \"themeColor\": \"#HEXCOLOR\",\n" +
                            $"  \"basePersonality\": \"Vivid system prompt role. Symbiosis of their expertise and location. In Ukrainian. e.g. Ви є мудрим монахом-кодером з Борислава. Допомагаєте створювати чистий C# код.\",\n" +
                            $"  \"resonantTags\": [\"programming\", \"c#\"]\n" +
                            $"}}";

            Debug.Log($"[OraclePromptFactory] 🌀 Forging new custom Oracle for profile {profile.UserId} via Gemini...");
            
            string cleanJson;
            try
            {
                // We request Gemini
                string response = await _aiService.RequestOracle(prompt, "");
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception("Oracle Forge was silent (empty response).");
                }

                // Extract JSON from markdown
                cleanJson = response.Trim();
                if (cleanJson.Contains("```json"))
                {
                    int start = cleanJson.IndexOf("```json") + 7;
                    int end = cleanJson.LastIndexOf("```");
                    if (end > start)
                    {
                        cleanJson = cleanJson.Substring(start, end - start).Trim();
                    }
                }
                else if (cleanJson.Contains("```"))
                {
                    int start = cleanJson.IndexOf("```") + 3;
                    int end = cleanJson.LastIndexOf("```");
                    if (end > start)
                    {
                        cleanJson = cleanJson.Substring(start, end - start).Trim();
                    }
                }

                // Try to parse to validate structure
                var data = JsonUtility.FromJson<CustomOracleData>(cleanJson);
                if (data == null || string.IsNullOrEmpty(data.id) || string.IsNullOrEmpty(data.displayName))
                {
                    throw new Exception("Generated JSON was invalid or incomplete.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OraclePromptFactory] API Unavailable ({ex.Message}). Activating high-fidelity offline synthesis fallback.");

                // Build clean dynamic name based on active tag
                string generatedName = "Оракул Синергії (Ефірний)";
                if (profile.AuraGifts != null && profile.AuraGifts.Count > 0)
                {
                    string firstGift = profile.AuraGifts[0];
                    generatedName = $"Оракул-{char.ToUpper(firstGift[0]) + firstGift.Substring(1)} [Offline]";
                }

                // Pick a curated color
                string[] colors = { "#00FFFF", "#FFD700", "#FF8C00", "#BA55D3", "#FF69B4", "#7FFFD4" };
                string chosenColor = colors[UnityEngine.Random.Range(0, colors.Length)];

                // Local dynamic Ukrainian personality prompt
                string localizedPersonality = $"Ви є мудрим оракулом-помічником, що спеціалізується на сферах: {giftsText}. " +
                                               $"Ваша сутність сформована в локації {location}. Допомагаєте Майстру знаходити гармонію та точні рішення.";

                var fallbackData = new CustomOracleData
                {
                    id = $"oracle_custom_off_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    displayName = generatedName,
                    themeColor = chosenColor,
                    basePersonality = localizedPersonality,
                    resonantTags = profile.AuraGifts.ToList()
                };

                cleanJson = JsonUtility.ToJson(fallbackData);
            }

            // Save JSON representation in UserProfile
            profile.CustomOraclesJson.Add(cleanJson);

            // Save profile to database
            if (_dataService != null)
            {
                await _dataService.SaveUserProfileAsync(profile, default);
                Debug.Log("[OraclePromptFactory] Persistent custom Oracle saved to database.");
            }

            // Create in-memory OracleSO
            return CreateOracleSO(cleanJson);
        }

        /// <summary>
        /// Instantiates a runtime OracleSO ScriptableObject from a serialized CustomOracleData JSON.
        /// </summary>
        public static OracleSO CreateOracleSO(string json)
        {
            var data = JsonUtility.FromJson<CustomOracleData>(json);
            var oracle = ScriptableObject.CreateInstance<OracleSO>();
            oracle.Id = data.id;
            oracle.DisplayName = data.displayName;
            oracle.LocalizationKey = data.id.ToUpper();
            oracle.BasePersonality = data.basePersonality;
            oracle.ResonantTags = data.resonantTags ?? new List<string>();

            Color themeColor = Color.silver;
            if (!string.IsNullOrEmpty(data.themeColor) && ColorUtility.TryParseHtmlString(data.themeColor, out var parsedColor))
            {
                themeColor = parsedColor;
            }
            oracle.ThemeColor = themeColor;
            oracle.Tier = 1; // Tier 1 represents Custom/Generated
            return oracle;
        }

        private async UniTask<string> SynthesizeViaGeminiAsync(OracleSO oracle, List<string> masterTags)
        {
            string tagList = masterTags != null && masterTags.Count > 0
                ? string.Join(", ", masterTags)
                : "general skills";

            string metaPrompt = $"You are a mystical game AI designer for TimeAura. " +
                                $"Write a system instruction (2-3 sentences) for an AI assistant " +
                                $"called \"{oracle.DisplayName}\" with this base personality: \"{oracle.BasePersonality}\". " +
                                $"This assistant serves a Master whose skills include: {tagList}. " +
                                $"Blend the Oracle's character with the Master's skills into one vivid, unique instruction. " +
                                $"Be creative. Use mystical and professional language. Output only the instruction text, nothing else.";

            string generated = await _aiService.RequestOracle(
                metaPrompt,
                BuildLocalFallbackPrompt(oracle, masterTags)
            );

            return string.IsNullOrWhiteSpace(generated)
                ? BuildLocalFallbackPrompt(oracle, masterTags)
                : generated;
        }

        private string BuildLocalFallbackPrompt(OracleSO oracle, List<string> masterTags)
        {
            var sb = new StringBuilder();
            sb.Append(oracle.BasePersonality);

            if (masterTags != null && masterTags.Count > 0)
            {
                sb.Append($" The Master you serve has expertise in: {string.Join(", ", masterTags)}.");
                sb.Append(" Align your responses to these domains.");
            }

            sb.Append(" Maintain your unique Oracle character at all times. Never break character.");
            return sb.ToString();
        }

        public static float CalculateResonance(OracleSO oracle, List<string> masterTags)
        {
            if (oracle?.ResonantTags == null || oracle.ResonantTags.Count == 0) return 0f;
            if (masterTags == null || masterTags.Count == 0) return 0f;

            int matches = oracle.ResonantTags
                .Count(rt => masterTags.Any(mt =>
                    string.Equals(rt, mt, StringComparison.OrdinalIgnoreCase)));

            return (float)matches / oracle.ResonantTags.Count;
        }
    }
}
