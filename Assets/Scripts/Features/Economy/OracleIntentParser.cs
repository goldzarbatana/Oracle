using System;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using UnityEngine;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// Parsers user's natural language or simulated voice intents into structured service request parameters.
    /// Manages daily rate limits for AI request generation.
    /// "Through the digital veil, the Oracle translates desire into action."
    /// </summary>
    public sealed class OracleIntentParser
    {
        private readonly IOracleService _geminiAI;
        private readonly AuthManager _authManager;

        private const int FreeDailyLimit = 10;
        private const string DailyCountKeyPrefix = "OracleIntent_DailyCount_";
        private const string DailyDateKeyPrefix = "OracleIntent_DailyDate_";

        private const string SystemInstruction = 
            "You are the Oracle Intent Parser for the TimeAura platform. " +
            "Your job is to parse the user's natural language request (wish) into a structured service request post. " +
            "You MUST return ONLY a raw JSON object and nothing else. Do not wrap in markdown block, do not output any explanation text. " +
            "Allowed categories: Code, Teaching, Craft, Art, Nature, All. If not specified or matches nothing else, use All. " +
            "Allowed realms: Ether (for Time/Horas based exchanges), Matter (for real money/Quants based premium services). " +
            "If the user mentions Horas (or horas, hours, хори, годин, год), use Ether. If the user mentions Quants (or quants, waves, dollars, грн, $, кванти, квати), use Matter. If not specified, default to Ether. " +
            "Extract or estimate the price as a decimal number (e.g. 1.5, 5.0, 10). If not specified, default to 0. " +
            "Rate your confidence of parsing from 0.0 to 1.0. If the input is clear and specifies content, category, price, and realm, confidence is high (e.g., 0.9). If it is garbage or extremely vague, confidence is low (e.g., 0.2). " +
            "Strictly output this JSON schema:\n" +
            "{\n" +
            "  \"content\": \"<clear, polished description of the task or need>\",\n" +
            "  \"category\": \"<Code|Teaching|Craft|Art|Nature|All>\",\n" +
            "  \"realm\": \"<Ether|Matter>\",\n" +
            "  \"price\": <number>,\n" +
            "  \"confidence\": <number between 0.0 and 1.0>\n" +
            "}";

        public OracleIntentParser(IOracleService geminiAI, AuthManager authManager)
        {
            _geminiAI = geminiAI;
            _authManager = authManager;
        }

        /// <summary>
        /// Check if the daily query limit has been reached for the current user.
        /// </summary>
        public bool IsDailyLimitReached
        {
            get
            {
                if (_authManager?.CurrentProfile != null && _authManager.CurrentProfile.IsAscensionSubscribed)
                {
                    return false; // Unlimited for Enlightened/Ascended users
                }

                string today = GetDateKey();
                string savedDate = PlayerPrefs.GetString(GetDailyDateKey(), "");
                if (savedDate != today)
                {
                    return false; // Different day, counter reset at runtime
                }

                int count = PlayerPrefs.GetInt(GetDailyCountKey(), 0);
                return count >= FreeDailyLimit;
            }
        }

        /// <summary>
        /// Get requests used today by the user.
        /// </summary>
        public int DailyRequestsUsed
        {
            get
            {
                string today = GetDateKey();
                string savedDate = PlayerPrefs.GetString(GetDailyDateKey(), "");
                if (savedDate != today) return 0;
                return PlayerPrefs.GetInt(GetDailyCountKey(), 0);
            }
        }

        /// <summary>
        /// Manually force-upgrade the current user's profile to Enlightened (Ascension subscribed) to bypass limits.
        /// </summary>
        public void EnableEnlightenedLimits()
        {
            if (_authManager?.CurrentProfile != null)
            {
                _authManager.CurrentProfile.IsAscensionSubscribed = true;
                Debug.Log("[OracleIntentParser] 🌌 Enlightened subscription enabled. Unlimited AI requests granted.");
            }
            else
            {
                Debug.LogWarning("[OracleIntentParser] Cannot enable Enlightened status: No active user profile.");
            }
        }

        /// <summary>
        /// Asynchronously parses a natural language intent via Gemini.
        /// </summary>
        public async UniTask<OracleIntentResult> ParseIntentAsync(string userText)
        {
            if (IsDailyLimitReached)
            {
                Debug.LogWarning("[OracleIntentParser] Daily AI request limit reached. Parsing blocked.");
                return new OracleIntentResult
                {
                    content = userText,
                    category = "All",
                    realm = "Ether",
                    price = 0f,
                    confidence = 0f
                };
            }

            IncrementDailyRequests();

            string prompt = $"Parse this intent: \"{userText}\"";
            string rawResponse = await _geminiAI.RequestOracleWithSystem(SystemInstruction, prompt, "");
            
            string cleanedJson = CleanJson(rawResponse);
            
            try
            {
                if (!string.IsNullOrEmpty(cleanedJson))
                {
                    var result = JsonUtility.FromJson<OracleIntentResult>(cleanedJson);
                    if (result != null)
                    {
                        // Normalize parsed realm values to prevent typos
                        if (string.IsNullOrEmpty(result.realm))
                        {
                            result.realm = "Ether";
                        }
                        else if (result.realm.Equals("Material", StringComparison.OrdinalIgnoreCase) || result.realm.Equals("Matter", StringComparison.OrdinalIgnoreCase))
                        {
                            result.realm = "Matter";
                        }
                        else
                        {
                            result.realm = "Ether";
                        }
                        
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OracleIntentParser] Failed to parse JSON response from Oracle: {cleanedJson}. Error: {ex.Message}");
            }

            // Fallback for failed parsing (extremely low confidence)
            return new OracleIntentResult
            {
                content = userText,
                category = "All",
                realm = "Ether",
                price = 0f,
                confidence = 0.1f
            };
        }

        /// <summary>
        /// Asynchronously parses a voice recording (base64 PCM WAV) via Gemini.
        /// </summary>
        public async UniTask<OracleIntentResult> ParseIntentWithAudioAsync(string audioBase64)
        {
            if (IsDailyLimitReached)
            {
                Debug.LogWarning("[OracleIntentParser] Daily AI request limit reached. Parsing blocked.");
                return new OracleIntentResult
                {
                    content = "🎙️ [Голосовий ліміт вичерпано]",
                    category = "All",
                    realm = "Ether",
                    price = 0f,
                    confidence = 0f
                };
            }

            IncrementDailyRequests();

            string rawResponse = await _geminiAI.RequestOracleWithAudio(
                audioBase64,
                prompt: null,
                systemInstruction: SystemInstruction,
                fallback: "{\"content\":\"🎙️ (Голосовий запит без бекенду)\",\"category\":\"All\",\"realm\":\"Ether\",\"price\":0,\"confidence\":0.1}"
            );

            string cleanedJson = CleanJson(rawResponse);
            
            try
            {
                if (!string.IsNullOrEmpty(cleanedJson))
                {
                    var result = JsonUtility.FromJson<OracleIntentResult>(cleanedJson);
                    if (result != null)
                    {
                        if (string.IsNullOrEmpty(result.realm))
                        {
                            result.realm = "Ether";
                        }
                        else if (result.realm.Equals("Material", StringComparison.OrdinalIgnoreCase) || result.realm.Equals("Matter", StringComparison.OrdinalIgnoreCase))
                        {
                            result.realm = "Matter";
                        }
                        else
                        {
                            result.realm = "Ether";
                        }
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OracleIntentParser] Failed to parse JSON response from Oracle audio: {cleanedJson}. Error: {ex.Message}");
            }

            return new OracleIntentResult
            {
                content = "🎙️ [Помилка розпізнавання голосу]",
                category = "All",
                realm = "Ether",
                price = 0f,
                confidence = 0.1f
            };
        }

        private void IncrementDailyRequests()
        {
            string today = GetDateKey();
            string savedDate = PlayerPrefs.GetString(GetDailyDateKey(), "");
            int count = 0;

            if (savedDate == today)
            {
                count = PlayerPrefs.GetInt(GetDailyCountKey(), 0);
            }

            count++;
            PlayerPrefs.SetString(GetDailyDateKey(), today);
            PlayerPrefs.SetInt(GetDailyCountKey(), count);
            PlayerPrefs.Save();
            
            Debug.Log($"[OracleIntentParser] Incrementing daily AI count: {count}/{FreeDailyLimit} used today.");
        }

        private string GetDateKey() => DateTime.UtcNow.ToString("yyyyMMdd");

        private string GetUserSuffix()
        {
            return _authManager?.CurrentProfile?.UserId ?? "guest";
        }

        private string GetDailyCountKey() => DailyCountKeyPrefix + GetUserSuffix();
        private string GetDailyDateKey() => DailyDateKeyPrefix + GetUserSuffix();

        private string CleanJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Trim();
            
            // Strip markdown block markers
            if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(7);
            }
            else if (text.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(3);
            }
            
            if (text.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(0, text.Length - 3);
            }
            
            return text.Trim();
        }
    }

    [Serializable]
    public class OracleIntentResult
    {
        public string content;
        public string category;
        public string realm;
        public float price;
        public float confidence;
    }
}
