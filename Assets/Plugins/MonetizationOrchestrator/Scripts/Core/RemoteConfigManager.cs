using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncTimerTask = Cysharp.Threading.Tasks.UniTask;
#else
using AsyncTimerTask = System.Threading.Tasks.Task;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Optional module to fetch pacing parameters (or other configs) from a published Google Sheet CSV.
    /// To use: publish your Google Sheet to the web as CSV, and paste the URL here.
    /// </summary>
    public class RemoteConfigManager : MonoBehaviour
    {
        [Header("Google Sheets Configuration")]
        [Tooltip("The URL to the published CSV of your Google Sheet. Format: Key,Value")]
        [TextArea(2, 5)]
        [SerializeField] private string googleSheetCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vSdUDeV1qOd9sH0-CdJ0jIvAbOqXl_qypNgmEhXszmP-YlwnJkYpJAK5ezVCJCRAFqKlZzqUGR1lbe9/pub?gid=0&single=true&output=csv";

        [Header("Settings")]
        [SerializeField] private bool fetchOnStart = true;
        [SerializeField] private bool applyToAdManagerAutomatically = true;
        [SerializeField] private bool debugLogs = true;

        public Dictionary<string, string> ConfigData { get; private set; } = new Dictionary<string, string>();

        public event Action<Dictionary<string, string>> OnConfigReady;
        public event Action<string> OnConfigFailed;

        private void Start()
        {
            if (fetchOnStart)
            {
                _ = FetchConfigAsync();
            }
        }

        public async AsyncTimerTask FetchConfigAsync()
        {
            if (string.IsNullOrEmpty(googleSheetCsvUrl) || googleSheetCsvUrl == "https://docs.google.com/spreadsheets/d/e/2PACX-1v.../pub?output=csv")
            {
                LogWarning("Google Sheet CSV URL is empty or using the default placeholder.");
                return;
            }

            Log($"Fetching remote config from: {googleSheetCsvUrl}");

            using (UnityWebRequest request = UnityWebRequest.Get(googleSheetCsvUrl))
            {
                var asyncOp = request.SendWebRequest();

                while (!asyncOp.isDone)
                {
#if MONETIZATION_UNITASK
                    await Cysharp.Threading.Tasks.UniTask.Yield();
#else
                    await Task.Yield();
#endif
                }

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    LogError($"Failed to fetch config: {request.error}");
                    OnConfigFailed?.Invoke(request.error);
                }
                else
                {
                    string csvData = request.downloadHandler.text;
                    ParseCsvToDictionary(csvData);
                    Log($"Successfully loaded {ConfigData.Count} config keys.");
                    
                    if (applyToAdManagerAutomatically)
                    {
                        ApplyToAdManager();
                    }

                    OnConfigReady?.Invoke(ConfigData);
                }
            }
        }

        private void ParseCsvToDictionary(string csvData)
        {
            ConfigData.Clear();
            var lines = SplitCSVLines(csvData);

            foreach (string line in lines)
            {
                var columns = SplitCSVValues(line);

                if (columns.Count >= 2)
                {
                    string key = columns[0].Trim();
                    string value = columns[1].Trim();

                    // Skip header row if it exists
                    if (key.ToLower() == "key" || string.IsNullOrEmpty(key)) continue;

                    ConfigData[key] = value;
                }
            }
        }

        private List<string> SplitCSVLines(string csvText)
        {
            var lines = new List<string>();
            var currentLine = "";
            bool insideQuotes = false;

            foreach (char c in csvText)
            {
                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                    currentLine += c;
                }
                else if (c == '\n' && !insideQuotes)
                {
                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        lines.Add(currentLine.TrimEnd('\r'));
                    }
                    currentLine = "";
                }
                else
                {
                    currentLine += c;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        private List<string> SplitCSVValues(string line)
        {
            var values = new List<string>();
            var currentValue = "";
            bool insideQuotes = false;

            int commaCount = 0, semicolonCount = 0, tabCount = 0;
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { i++; continue; }
                    q = !q;
                    continue;
                }
                if (!q)
                {
                    if (ch == ',') commaCount++;
                    else if (ch == ';') semicolonCount++;
                    else if (ch == '\t') tabCount++;
                }
            }

            char sep = ',';
            if (tabCount > 0 && tabCount > commaCount && tabCount > semicolonCount) sep = '\t';
            else if (commaCount > 0) sep = ',';
            else if (semicolonCount > 0) sep = ';';

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue += '"';
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == sep && !insideQuotes)
                {
                    values.Add(currentValue.Trim());
                    currentValue = string.Empty;
                }
                else
                {
                    currentValue += c;
                }
            }

            values.Add(currentValue.Trim());
            return values;
        }

        private IAdManagerService _adManager;

        /// <summary>
        /// Optional: Inject the AdManager service dynamically (useful for Zenject/VContainer).
        /// </summary>
        public void Initialize(IAdManagerService adManager)
        {
            _adManager = adManager;
        }

        private void ApplyToAdManager()
        {
            IAdManagerService manager = _adManager;
            if (manager == null)
            {
                if (MonetizationOrchestrator.Instance != null)
                {
                    manager = MonetizationOrchestrator.Instance.Ads;
                }
                else
                {
                    manager = AdManager.Instance;
                }
            }

            if (manager == null) return;

            // Use exact variable names or common fallbacks from the provided demo table
            float? minInter = GetFloatConfig("minSecondsBetweenInterstitials") ?? GetFloatConfig("interstitial_min_interval");
            float? rewCooldown = GetFloatConfig("rewardedToInterstitialCooldown") ?? GetFloatConfig("rewarded_cooldown_seconds");
            float? warmup = GetFloatConfig("sceneWarmupDelay") ?? GetFloatConfig("initial_ad_delay");
            int? gameOverFreq = GetIntConfig("gameOverAdFrequency") ?? GetIntConfig("interstitial_game_over_freq");
            int? levelFreq = GetIntConfig("levelCompleteAdFrequency") ?? GetIntConfig("interstitial_level_complete_freq");

            if (manager is AdManager concreteAdManager)
            {
                concreteAdManager.ApplyRemoteConfig(minInter, rewCooldown, warmup, gameOverFreq, levelFreq);
            }
        }

        public string GetStringConfig(string key, string defaultValue = null)
        {
            return ConfigData.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public float? GetFloatConfig(string key, float? defaultValue = null)
        {
            if (ConfigData.TryGetValue(key, out string val))
            {
                if (float.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public int? GetIntConfig(string key, int? defaultValue = null)
        {
            if (ConfigData.TryGetValue(key, out string val))
            {
                if (int.TryParse(val, out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public bool? GetBoolConfig(string key, bool? defaultValue = null)
        {
            if (ConfigData.TryGetValue(key, out string val))
            {
                if (bool.TryParse(val, out bool result))
                {
                    return result;
                }
                if (int.TryParse(val, out int intResult))
                {
                    return intResult > 0;
                }
            }
            return defaultValue;
        }

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"<color=#38ff8e>[RemoteConfig]</color> {msg}");
        }

        private void LogWarning(string msg)
        {
            if (debugLogs) Debug.LogWarning($"<color=yellow>[RemoteConfig] ⚠️</color> {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[RemoteConfig] ❌ {msg}");
        }
    }
}
