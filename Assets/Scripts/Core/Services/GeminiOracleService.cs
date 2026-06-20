using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Localization;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer;

namespace TimeAura.Core.Services
{
    public class GeminiOracleService : IAuraOracleService, IManager
    {
        private readonly IOracleService _gemini;
        private readonly AuthManager _authManager;
        private readonly LocalizationManager _localization;
        private readonly AuraPillarSO[] _pillars;
        
        private OraclePersonaSO _currentPersona;
        
        [Inject]
        public GeminiOracleService(IOracleService gemini, AuthManager authManager, LocalizationManager localization, AuraPillarSO[] pillars)
        {
            _gemini = gemini;
            _authManager = authManager;
            _localization = localization;
            _pillars = pillars;
        }

        public event System.Action<OracleStatusUpdate> OnStatusUpdate;

        public void Whisper(string message, OracleState state = OracleState.Active)
        {
            OnStatusUpdate?.Invoke(new OracleStatusUpdate { Message = message, State = state });
        }

        public bool IsInitialized { get; private set; }

        public UniTask InitializeAsync(System.Threading.CancellationToken ct)
        {
            RefreshPersona();
            IsInitialized = true;
            return UniTask.CompletedTask;
        }

        private void RefreshPersona()
        {
            OracleTone tone = _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Mystic;
            // Capitalize first letter for asset path: mystic -> Mystic
            string toneStr = tone.ToString();
            string capitalizedTone = char.ToUpper(toneStr[0]) + toneStr.Substring(1).ToLower();
            string path = $"Settings/Personas/OraclePersona_{capitalizedTone}";
            
            _currentPersona = Resources.Load<OraclePersonaSO>(path);
            
            if (_currentPersona == null)
            {
                Debug.LogWarning($"[OracleService] ⚠️ Persona asset NOT FOUND at path: {path}. Falling back to default.");
                // Try to load ANY persona if specific one fails
                _currentPersona = Resources.Load<OraclePersonaSO>("Settings/Personas/OraclePersona_Mystic");
            }
            else
            {
                Debug.Log($"[OracleService] 🎭 Persona loaded: {capitalizedTone}");
            }
        }

        private string GetFinalBasePrompt()
        {
            if (_currentPersona == null) RefreshPersona();
            
            string lang = _localization?.CurrentLanguage.ToString() ?? "English";
            string personaPrompt = _currentPersona != null ? _currentPersona.SystemPrompt : "You are the Oracle of Time Aura.";
            string contextWithLanguage = OracleContextManager.GetSystemPrompt(lang);
            
            return $"{personaPrompt}\n\n{contextWithLanguage}";
        }

        public UniTask ShutdownAsync()
        {
            IsInitialized = false;
            return UniTask.CompletedTask;
        }

        public async UniTask<AuraOracleVerdict> PredictAuraAsync(List<string> gifts)
        {
            Whisper("Decoding your soul spectrum...", OracleState.Processing);
            if (gifts == null || gifts.Count == 0)
            {
                return new AuraOracleVerdict { Title = AuraTerms.TITLE_EXPLORER, ColorHex = "#999999", Reason = "Default status for new Seeker." };
            }

            string tags = string.Join(", ", gifts);
            string titlesList = string.Join(", ", GetAvailableTitles());
            
            string prompt = $"{GetFinalBasePrompt()}\n\n" +
                            $"Analyze these Master's gifts: {tags}. " +
                            $"Choose the MOST fitting title from this list: [{titlesList}]. " +
                            $"Also, suggest a hex color (e.g., #FFD700) that represents this aura spectrum. " +
                            "Return format: TITLE_KEY|HEX_COLOR|SHORT_WHISPER. " +
                            "Return ONLY this string.";

            string response = await _gemini.RequestOracle(prompt, $"{AuraTerms.TITLE_SEEKER}|#FFD700|The path is yet unwritten.");
            
            try
            {
                string[] parts = response.Split('|');
                string title = parts[0].Trim();
                string whisper = parts.Length > 2 ? parts[2].Trim() : "Your spectrum is unique.";
                
                Whisper(whisper, OracleState.Active);
                
                return new AuraOracleVerdict
                {
                    Title = title,
                    ColorHex = parts.Length > 1 ? parts[1].Trim() : "#FFD700",
                    Reason = whisper
                };
            }
            catch
            {
                Whisper("The vision is hazy.", OracleState.Alert);
                return new AuraOracleVerdict { Title = AuraTerms.TITLE_SEEKER, ColorHex = "#FFD700" };
            }
        }

        public async UniTask<string> GetProphecyAsync(string chatContext)
        {
            Whisper("Consulting the threads of fate...", OracleState.Processing);
            
            var prompts = TimeAura.Core.Data.SO.OracleCorePromptsSO.GetInstance();
            string template = prompts != null ? prompts.ChamberProphecyPromptTemplate : "";
            
            string prompt = "";
            if (!string.IsNullOrEmpty(template))
            {
                try
                {
                    prompt = $"{GetFinalBasePrompt()}\n\n{string.Format(template, chatContext)}";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GeminiOracleService] Prophecy prompt formatting failed: {ex.Message}");
                    prompt = $"{GetFinalBasePrompt()}\n\nAnalyze this Harmony exchange context: '{chatContext}'. Provide a concise prophecy (max 15 words) about the fair value in Horas. Example: '👁️ Prophecy: The flow suggests 2.0 Horas for this creative convergence.'";
                }
            }
            else
            {
                prompt = $"{GetFinalBasePrompt()}\n\n" +
                         $"Analyze this Harmony exchange context: '{chatContext}'. " +
                         "Provide a concise prophecy (max 15 words) about the fair value in Horas. " +
                         "Example: '👁️ Prophecy: The flow suggests 2.0 Horas for this creative convergence.'";
            }

            var result = await _gemini.RequestOracle(prompt, "👁️ Prophecy: A fair exchange is 1.5 - 2.5 Horas.");
            Whisper(result, OracleState.Active);
            return result;
        }

        public async UniTask<string> ExplainSymmetryAsync(UserProfile currentUser, UserProfile targetUser)
        {
            Whisper("Finding the point of resonance...", OracleState.Processing);
            string myTags = string.Join(", ", currentUser.AuraGifts);
            string targetTags = string.Join(", ", targetUser.AuraGifts);
            
            string prompt = $"{GetFinalBasePrompt()}\n\n" +
                            $"Explain the SYMMETRY between two Masters. " +
                            $"Master A (You): Gifts [{myTags}], Seeking [{string.Join(", ", currentUser.AuraSeeks)}]. " +
                            $"Master B (Match): Gifts [{targetTags}]. " +
                            "Explain why they are a good match in a mystical, poetic way (max 20 words). " +
                            "Example: '👁️ The stars align: Your logic completes their creative fire.'";

            var result = await _gemini.RequestOracle(prompt, "👁️ A resonance is detected in the patterns of time.");
            Whisper(result, OracleState.Active);
            return result;
        }

        public async UniTask<OracleSuggestion> SuggestResonanceAsync(List<string> currentGifts, List<string> currentSeeks, string context = "", OracleTone tone = OracleTone.Mystic, string activePillarId = "")
        {
            Whisper("Gazing into your intent...", OracleState.Processing);

            string pillarContext = "";
            if (!string.IsNullOrEmpty(activePillarId))
            {
                var pillar = _pillars.FirstOrDefault(p => p.Id == activePillarId);
                if (pillar != null && pillar.tags != null)
                {
                    pillarContext = $"The Master is currently focused on the '{pillar.Id}' pillar. " +
                                    $"PRIORITIZE tags from this pillar: [{string.Join(", ", pillar.tags)}]. ";
                }
            }

            string allTags = "";
            if (_pillars != null)
            {
                var tagList = new List<string>();
                foreach (var p in _pillars) if (p.tags != null) tagList.AddRange(p.tags);
                allTags = string.Join(", ", tagList.Distinct());
            }

            string prompt = $"{GetFinalBasePrompt()}\n\n" +
                            $"Analyze the Master's meditation: \"{context}\". " +
                            $"{pillarContext}" +
                            $"Based on their thoughts, suggest 1 to 3 MOST RELEVANT tags from this exact list: [{allTags}]. " +
                            "Also provide a short whisper (max 15 words) consistent with your persona. " +
                            "Format: TAG1,TAG2|WHISPER. " +
                            "If no tags from the list match, you may suggest a new one, but prioritize the list.";

            string response = await _gemini.RequestOracle(prompt, "Seeking|The Oracle's vision is clouded.");
            
            try
            {
                string[] parts = response.Split('|');
                string tagStr = parts[0].Trim();
                string whisper = parts.Length > 1 ? parts[1].Trim() : "A balance is needed.";
                
                var suggestedTags = tagStr.Split(',').Select(t => t.Trim()).ToList();
                
                Whisper(whisper, OracleState.Active);
                
                return new OracleSuggestion { SuggestedTags = suggestedTags, Whisper = whisper };
            }
            catch
            {
                Whisper("The path is blocked.", OracleState.Alert);
                return new OracleSuggestion { SuggestedTags = new List<string> { "Seeking" }, Whisper = "Continue the ritual." };
            }
        }

        public async UniTask<string> GetPhilosophicalGuidanceAsync(string userMessage, string context = "Nexus")
        {
            Whisper("Listening to the cosmic echo...", OracleState.Processing);
            
            string prompt = $"{GetFinalBasePrompt()}\n\n" +
                            $"User Message: \"{userMessage}\". " +
                            "Provide a concise, meaningful answer (max 30 words).";

            string response = await _gemini.RequestOracle(prompt, "The flows of time are too complex to decipher right now.");
            Whisper(response, OracleState.Active);
            return response;
        }

        public async UniTask<AuraOracleVerdict> PredictEvolutionaryTitleAsync(UserProfile profile)
        {
            Whisper("Weaving your evolutionary title...", OracleState.Processing);
            if (profile == null)
            {
                return new AuraOracleVerdict { Title = "Initiate", ColorHex = "#FFFFFF", Reason = "The flow of time is silent." };
            }

            var prompts = TimeAura.Core.Data.SO.OracleCorePromptsSO.GetInstance();
            string template = prompts != null ? prompts.EvolutionaryTitleTemplate : "";

            string gifts = profile.AuraGifts != null && profile.AuraGifts.Count > 0 ? string.Join(", ", profile.AuraGifts) : "None";
            string seeks = profile.AuraSeeks != null && profile.AuraSeeks.Count > 0 ? string.Join(", ", profile.AuraSeeks) : "None";
            string legacyLog = profile.Legacy != null && profile.Legacy.Count > 0 ? string.Join(" | ", profile.Legacy) : "No legacy recorded yet";
            string pillar = profile.PrimaryPillar ?? "Unknown";

            string prompt = "";
            if (!string.IsNullOrEmpty(template))
            {
                prompt = string.Format(template, profile.DisplayName, gifts, seeks, legacyLog, pillar);
            }
            else
            {
                prompt = $"Synthesize a humorous Ukrainian title for Master {profile.DisplayName}. Gifts: {gifts}. Seeks: {seeks}. Legacy: {legacyLog}. Respond ONLY as JSON with title, colorHex, and commentary.";
            }

            string response = await _gemini.RequestOracleWithSystem(
                "You are the dynamic Identity Weaver. Respond ONLY with valid JSON.",
                prompt,
                "{\"title\": \"Лицар Ефірних Конфліктів\", \"colorHex\": \"#D4AF37\", \"commentary\": \"Хронос посміхається твоїй праці.\"}"
            );

            try
            {
                string title = "Лицар Ефірних Конфліктів";
                string colorHex = "#D4AF37";
                string commentary = "Хронос посміхається вашому спадку.";

                if (response.Contains("\"title\""))
                {
                    int index = response.IndexOf("\"title\"");
                    string sub = response.Substring(index);
                    int colonIndex = sub.IndexOf(":");
                    int quoteStart = sub.IndexOf("\"", colonIndex);
                    int quoteEnd = sub.IndexOf("\"", quoteStart + 1);
                    title = sub.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }

                if (response.Contains("\"colorHex\""))
                {
                    int index = response.IndexOf("\"colorHex\"");
                    string sub = response.Substring(index);
                    int colonIndex = sub.IndexOf(":");
                    int quoteStart = sub.IndexOf("\"", colonIndex);
                    int quoteEnd = sub.IndexOf("\"", quoteStart + 1);
                    colorHex = sub.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }

                if (response.Contains("\"commentary\""))
                {
                    int index = response.IndexOf("\"commentary\"");
                    string sub = response.Substring(index);
                    int colonIndex = sub.IndexOf(":");
                    int quoteStart = sub.IndexOf("\"", colonIndex);
                    int quoteEnd = sub.IndexOf("\"", quoteStart + 1);
                    commentary = sub.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }

                Whisper(commentary, OracleState.Active);

                return new AuraOracleVerdict
                {
                    Title = title,
                    ColorHex = colorHex,
                    Reason = commentary
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeminiOracleService] ⚠️ Evolutionary title parse failed: {ex.Message}. Falling back.");
                Whisper("The path of identity is hazy.", OracleState.Active);
                return new AuraOracleVerdict
                {
                    Title = "Монах-Кодер Ефіру",
                    ColorHex = "#8A2BE2",
                    Reason = "Ви отримали містичний титул в обхід кодексу."
                };
            }
        }

        private List<string> GetAvailableTitles()
        {
            return new List<string>
            {
                AuraTerms.TITLE_MENTOR, AuraTerms.TITLE_EXPERT, AuraTerms.TITLE_ADVISOR,
                AuraTerms.TITLE_CREATOR, AuraTerms.TITLE_INNOVATOR, AuraTerms.TITLE_AESTHETE,
                AuraTerms.TITLE_PRACTITIONER, AuraTerms.TITLE_SUPPORTER, AuraTerms.TITLE_CRAFTSMAN,
                AuraTerms.TITLE_EXPLORER, AuraTerms.TITLE_SEEKER
            };
        }
    }
}
