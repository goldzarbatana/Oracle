using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;
using VContainer;
using TimeAura.Core.Data.SO;

namespace TimeAura.Features.Localization
{
    /// <summary>
    /// Manages localization with modern UniTask async patterns.
    /// Loads translations from CSV files in Resources/Localization folder.
    /// "In every tongue, the temple speaks its truth."
    /// </summary>
    public sealed class LocalizationManager : MonoBehaviour, IManager
    {
        [Inject]
        private AppConfig appConfig;

        [Header("Localization Settings")]
        [SerializeField] private string csvFileName = "AuraTerms";

        private readonly Dictionary<string, Dictionary<SystemLanguage, string>> table = new();

        private static readonly Dictionary<string, SystemLanguage> LanguageMap = new()
        {
            { "English", SystemLanguage.English },
            { "German", SystemLanguage.German },
            { "French", SystemLanguage.French },
            { "Ukrainian", SystemLanguage.Ukrainian },
            { "Polish", SystemLanguage.Polish },
            { "Spanish", SystemLanguage.Spanish },
            { "Italian", SystemLanguage.Italian },
            { "Russian", SystemLanguage.Russian },
            { "Turkish", SystemLanguage.Turkish },
            { "Hindi", SystemLanguage.Hindi }
        };

        public SystemLanguage CurrentLanguage { get; private set; } = SystemLanguage.Unknown;
        public OracleTone CurrentTone { get; private set; } = OracleTone.Mystic;
        public bool IsInitialized { get; private set; }

        private const string PrefKeyLang = "TimeAura_SelectedLanguage";

        private void Awake()
        {
            int savedLang = PlayerPrefs.GetInt(PrefKeyLang, -1);
            if (savedLang != -1)
            {
                CurrentLanguage = (SystemLanguage)savedLang;
            }
            else
            {
                CurrentLanguage = Application.systemLanguage;
            }
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            var csvPath = $"Localization/{csvFileName}";
            var csvAsset = Resources.Load<TextAsset>(csvPath);

            if (csvAsset != null)
            {
                Debug.Log($"[LocalizationManager] 📂 CSV loaded from Resources: {csvPath} (Size: {csvAsset.text.Length} chars)");
                LoadFromCSV(csvAsset.text);
            }
            else
            {
                Debug.LogError($"[LocalizationManager] ❌ CSV NOT FOUND at Resources/{csvPath}");
            }

            IsInitialized = true;
            
            if (table.Count > 0 && !IsLanguageSupported(CurrentLanguage))
            {
                Debug.Log($"[LocalizationManager] ℹ️ {CurrentLanguage} not in CSV. Defaulting to English.");
                SetLanguage(SystemLanguage.English);
            }
            else
            {
                SetLanguage(CurrentLanguage);
            }

            await UniTask.Yield(cancellationToken);
        }

        private bool IsLanguageSupported(SystemLanguage lang)
        {
            if (table.Count == 0) return false;
            return table.Values.Any(dict => dict.ContainsKey(lang));
        }

        public async UniTask ShutdownAsync()
        {
            table.Clear();
            IsInitialized = false;
            await UniTask.Yield();
        }

        public void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;
            PlayerPrefs.SetInt(PrefKeyLang, (int)language);
            PlayerPrefs.Save();
            
            Debug.Log($"[LocalizationManager] 🌐 Switched to: {language}");
            Core.EventBus.Publish(new Core.LanguageChangedEvent(language));
        }

        public void SetTone(OracleTone tone)
        {
            CurrentTone = tone;
            Debug.Log($"[LocalizationManager] 🎭 Persona Tone set to: {CurrentTone}");
            // Trigger a UI refresh by publishing the language event again
            Core.EventBus.Publish(new Core.LanguageChangedEvent(CurrentLanguage));
        }

        public void Register(string key, SystemLanguage language, string value)
        {
            if (!table.TryGetValue(key, out var localized))
            {
                localized = new Dictionary<SystemLanguage, string>();
                table[key] = localized;
            }
            localized[language] = value;
        }

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            string cleanKey = key.Trim();

            // 1. Try Tone-Specific Override (e.g., "term.login.business", "term.login.mystic")
            string toneStr = CurrentTone.ToString().ToLower();
            string toneKey = $"{cleanKey}.{toneStr}";
            if (table.TryGetValue(toneKey, out var toneDict))
            {
                if (toneDict.TryGetValue(CurrentLanguage, out var toneValue)) return toneValue;
                if (CurrentLanguage != SystemLanguage.English && toneDict.TryGetValue(SystemLanguage.English, out var engToneValue)) return engToneValue;
            }

            // 2. Try Base Key
            if (table.TryGetValue(cleanKey, out var localized))
            {
                if (localized.TryGetValue(CurrentLanguage, out var value))
                {
                    return value;
                }

                if (CurrentLanguage != SystemLanguage.English && localized.TryGetValue(SystemLanguage.English, out var englishValue))
                {
                    return englishValue;
                }
            }

            if (Application.isPlaying && string.IsNullOrEmpty(fallback))
                Debug.LogWarning($"[LocalizationManager] ⚠️ Missing key: '{cleanKey}' for {CurrentLanguage}");
                
            return !string.IsNullOrEmpty(fallback) ? fallback : cleanKey;
        }

        public string GetFormatted(string key, params object[] args)
        {
            var template = Get(key, key);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        /// <summary>
        /// Gets a localized string with persona-specific override support.
        /// Tries "key.tone" first, then falls back to "key".
        /// </summary>
        public string GetPersonaString(string key, OracleTone tone, string fallback = "")
        {
            string toneStr = tone.ToString().ToLower();
            string personaKey = $"{key}.{toneStr}";
            
            // Try persona-specific key
            if (table.ContainsKey(personaKey))
            {
                return Get(personaKey, fallback);
            }
            
            // Fallback to default key
            return Get(key, fallback);
        }

        /// <summary>
        /// Formats time balance according to the chosen persona (Mystic vs. Others).
        /// 1 Horas = 60 minutes. 1 Atom = 1 minute.
        /// </summary>
        public string FormatTimeBalance(long totalMinutes, OracleTone tone)
        {
            long hours = totalMinutes / 60;
            long mins = Math.Abs(totalMinutes % 60);

            bool isUa = CurrentLanguage == SystemLanguage.Ukrainian;

            if (tone == OracleTone.Mystic)
            {
                // Mystic format
                string horasWord = isUa ? (hours == 1 || hours == -1 ? "Хорас" : "Хораси") : "Horas";
                string atomsWord = isUa ? (mins == 1 ? "Атом" : "Атомів") : (mins == 1 ? "Atom" : "Atoms");

                if (mins == 0) return $"{hours} {horasWord}";
                return $"{hours} {horasWord}, {mins} {atomsWord}";
            }
            else
            {
                // Standard format
                string hStr = isUa ? "год." : "h";
                string mStr = isUa ? "хв." : "m";

                if (mins == 0) return $"{hours} {hStr}";
                return $"{hours} {hStr} {mins} {mStr}";
            }
        }

        private void LoadFromCSV(string csvText)
        {
            if (string.IsNullOrEmpty(csvText)) return;

            // Remove Byte Order Mark
            if (csvText.StartsWith("\uFEFF")) csvText = csvText.Substring(1);

            var lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log($"[LocalizationManager] 📄 Lines detected: {lines.Length}");
            
            if (lines.Length < 2) return;

            var headers = ParseCSVLine(lines[0]);
            var languageColumns = new List<(int index, SystemLanguage language)>();

            for (int i = 1; i < headers.Count; i++)
            {
                var rawHeader = headers[i].Trim().Replace("\"", "");
                var langName = Regex.Replace(rawHeader, @"\s*\(.+\)\s*$", "");
                if (LanguageMap.TryGetValue(langName, out var systemLang))
                {
                    languageColumns.Add((i, systemLang));
                }
            }

            Debug.Log($"[LocalizationManager] 🌍 Languages detected in CSV: {string.Join(", ", languageColumns.Select(lc => lc.language))}");

            int loadedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var cells = ParseCSVLine(lines[i]);
                if (cells.Count < 2) continue;

                var key = cells[0].Trim().Replace("\"", "");
                if (string.IsNullOrEmpty(key)) continue;

                foreach (var (colIndex, language) in languageColumns)
                {
                    if (colIndex < cells.Count)
                    {
                        var value = cells[colIndex].Trim();
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                            value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");

                        if (!string.IsNullOrEmpty(value))
                            Register(key, language, value);
                    }
                }
                loadedCount++;
            }
            
            Debug.Log($"[LocalizationManager] ✅ Successfully populated table with {loadedCount} keys.");
        }

        private List<string> ParseCSVLine(string line)
        {
            var result = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else currentField.Append(c);
            }
            result.Add(currentField.ToString());
            return result;
        }
    }
}
