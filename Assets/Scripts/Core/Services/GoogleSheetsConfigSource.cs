using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Services
{
    public sealed class GoogleSheetsConfigSource : IRemoteConfigSource
    {
        private readonly AppConfig _appConfig;
        private readonly Dictionary<string, string> _configData = new Dictionary<string, string>();
        
        public bool IsConnected { get; private set; }
        public event Action OnConfigUpdated;

        private const string CacheKey = "GoogleSheetsConfigCache";
        private const string LastFetchTimeKey = "GoogleSheetsConfigLastFetchTime";
        private const int CacheCooldownSeconds = 300; // 5 minutes

        public GoogleSheetsConfigSource(AppConfig appConfig)
        {
            _appConfig = appConfig;
            LoadFromCache();
        }

        public async UniTask FetchConfigAsync(bool forceReload = false)
        {
            if (_appConfig == null || string.IsNullOrEmpty(_appConfig.GoogleSheetBaseUrl))
            {
                Debug.LogWarning("[GoogleSheetsConfig] Google Sheet Base URL is not configured. Falling back to local cache/defaults.");
                return;
            }

            if (!forceReload)
            {
                string lastFetchStr = PlayerPrefs.GetString(LastFetchTimeKey, "");
                if (DateTime.TryParse(lastFetchStr, null, DateTimeStyles.RoundtripKind, out DateTime lastFetchTime))
                {
                    if ((DateTime.UtcNow - lastFetchTime).TotalSeconds < CacheCooldownSeconds && _configData.Count > 0)
                    {
                        Debug.Log("[GoogleSheetsConfig] Config was fetched less than 5 minutes ago. Using in-memory configuration.");
                        return;
                    }
                }
            }

            string rawInput = _appConfig.GoogleSheetBaseUrl;
            string spreadsheetId = ExtractSpreadsheetId(rawInput);
            bool isPublishedLink = rawInput.Contains("2PACX-") || rawInput.Contains("/pub");

            if (isPublishedLink)
            {
                Debug.LogWarning("[GoogleSheetsConfig] ⚠️ Detected a Published-to-Web URL (2PACX- or /pub). Google Sheets CSV output does NOT support multiple tabs through a single URL. " +
                                 "Please share your Google Sheet as 'Anyone with the link can view' and paste the standard EDIT URL (https://docs.google.com/spreadsheets/d/.../edit) instead.");
            }

            Debug.Log($"[GoogleSheetsConfig] Fetching Remote Config. Raw URL: {rawInput} | ID: {spreadsheetId}");

            string[] sheets = { "CONFIG", "ECONOMY", "ORACLE_PROMPTS", "FEATURE_FLAGS", "CONTENT", "AB_TESTS", "ANALYTICS_LOG" };
            var newConfig = new Dictionary<string, string>();
            bool allSuccessful = true;

            foreach (var sheetName in sheets)
            {
                string url;
                if (isPublishedLink)
                {
                    url = $"{rawInput}";
                    if (rawInput.Contains("?"))
                    {
                        url += $"&sheet={sheetName}";
                    }
                    else
                    {
                        url += $"?output=csv&sheet={sheetName}";
                    }
                }
                else
                {
                    url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet={sheetName}";
                }

                try
                {
                    using (UnityWebRequest request = UnityWebRequest.Get(url))
                    {
                        await request.SendWebRequest().ToUniTask();

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            string csvData = request.downloadHandler.text;
                            ParseSheetCsv(sheetName, csvData, newConfig);
                            Debug.Log($"[GoogleSheetsConfig] Successfully fetched sheet: {sheetName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[GoogleSheetsConfig] Failed to fetch sheet: {sheetName}. Error: {request.error}");
                            allSuccessful = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GoogleSheetsConfig] Exception while fetching sheet: {sheetName}. Exception: {ex.Message}");
                    allSuccessful = false;
                }
            }

            if (newConfig.Count > 0)
            {
                IsConnected = allSuccessful;
                
                // Update in-memory storage
                foreach (var kvp in newConfig)
                {
                    _configData[kvp.Key] = kvp.Value;
                }

                // Save to Cache
                SaveToCache();
                
                if (allSuccessful)
                {
                    PlayerPrefs.SetString(LastFetchTimeKey, DateTime.UtcNow.ToString("o"));
                    PlayerPrefs.Save();
                }

                Debug.Log($"[GoogleSheetsConfig] Remote Config updated. Total keys loaded: {_configData.Count}");
                OnConfigUpdated?.Invoke();
            }
            else
            {
                IsConnected = false;
                Debug.LogWarning("[GoogleSheetsConfig] Failed to load any keys from remote sheets. Using cached/default values.");
            }
        }

        public string GetValue(string key, string defaultValue)
        {
            return _configData.TryGetValue(key, out string val) ? val : defaultValue;
        }

        public bool GetValue(string key, bool defaultValue)
        {
            if (_configData.TryGetValue(key, out string val))
            {
                if (bool.TryParse(val.Trim(), out bool res)) return res;
                string upper = val.Trim().ToUpper();
                if (upper == "TRUE" || upper == "YES" || upper == "1") return true;
                if (upper == "FALSE" || upper == "NO" || upper == "0") return false;
            }
            return defaultValue;
        }

        public int GetValue(string key, int defaultValue)
        {
            if (_configData.TryGetValue(key, out string val))
            {
                if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int res)) return res;
            }
            return defaultValue;
        }

        public float GetValue(string key, float defaultValue)
        {
            if (_configData.TryGetValue(key, out string val))
            {
                if (float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float res)) return res;
            }
            return defaultValue;
        }

        public IReadOnlyDictionary<string, string> GetAllValues()
        {
            return _configData;
        }

        private void ParseSheetCsv(string sheetName, string csvData, Dictionary<string, string> targetDict)
        {
            var lines = SplitCSVLines(csvData);
            if (lines.Count == 0) return;

            for (int i = 0; i < lines.Count; i++)
            {
                var columns = SplitCSVValues(lines[i]);
                if (columns.Count == 0) continue;

                string firstCol = columns[0].Trim();

                // CONFIG & ECONOMY & ORACLE_PROMPTS
                if (sheetName == "CONFIG" || sheetName == "ECONOMY" || sheetName == "ORACLE_PROMPTS")
                {
                    if (i == 0 && firstCol.Equals("key", StringComparison.OrdinalIgnoreCase)) continue; // skip header
                    if (string.IsNullOrEmpty(firstCol)) continue;
                    if (columns.Count >= 2)
                    {
                        targetDict[firstCol] = columns[1].Trim();
                    }
                }
                // FEATURE_FLAGS
                else if (sheetName == "FEATURE_FLAGS")
                {
                    if (i == 0 && firstCol.Equals("feature", StringComparison.OrdinalIgnoreCase)) continue; // skip header
                    if (string.IsNullOrEmpty(firstCol)) continue;
                    if (columns.Count >= 2)
                    {
                        targetDict[firstCol] = columns[1].Trim();
                    }
                }
                // CONTENT
                else if (sheetName == "CONTENT")
                {
                    if (i == 0 && firstCol.Equals("type", StringComparison.OrdinalIgnoreCase)) continue; // skip header
                    if (string.IsNullOrEmpty(firstCol) || columns.Count < 4) continue;
                    
                    string type = firstCol;
                    string id = columns[1].Trim();
                    string textUa = columns[2].Trim();
                    string textEn = columns[3].Trim();

                    targetDict[$"content_{type}_{id}_ua"] = textUa;
                    targetDict[$"content_{type}_{id}_en"] = textEn;
                }
                // AB_TESTS
                else if (sheetName == "AB_TESTS")
                {
                    if (i == 0 && (firstCol.Equals("test_name", StringComparison.OrdinalIgnoreCase) || firstCol.Equals("testname", StringComparison.OrdinalIgnoreCase))) continue; // skip header
                    if (string.IsNullOrEmpty(firstCol) || columns.Count < 4) continue;

                    string testName = firstCol;
                    string variantA = columns[1].Trim();
                    string variantB = columns[2].Trim();
                    string allocation = columns[3].Trim();

                    targetDict[$"ab_test_{testName}_variant_a"] = variantA;
                    targetDict[$"ab_test_{testName}_variant_b"] = variantB;
                    targetDict[$"ab_test_{testName}_allocation"] = allocation;
                }
                // ANALYTICS_LOG
                else if (sheetName == "ANALYTICS_LOG")
                {
                    if (i == 0 && firstCol.Equals("timestamp", StringComparison.OrdinalIgnoreCase)) continue; // skip header
                    if (string.IsNullOrEmpty(firstCol) || columns.Count < 4) continue;

                    string timestamp = firstCol;
                    string eventName = columns[1].Trim();
                    string userId = columns[2].Trim();
                    string valueVal = columns[3].Trim();
                    string metadata = columns.Count > 4 ? columns[4].Trim() : "";

                    targetDict[$"analytics_log_{i}_timestamp"] = timestamp;
                    targetDict[$"analytics_log_{i}_event"] = eventName;
                    targetDict[$"analytics_log_{i}_user_id"] = userId;
                    targetDict[$"analytics_log_{i}_value"] = valueVal;
                    targetDict[$"analytics_log_{i}_metadata"] = metadata;
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

        private void LoadFromCache()
        {
            _configData.Clear();

            string json = PlayerPrefs.GetString(CacheKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<CacheWrapper>(json);
                    if (wrapper != null && wrapper.entries != null)
                    {
                        foreach (var entry in wrapper.entries)
                        {
                            _configData[entry.key] = entry.value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GoogleSheetsConfig] Error loading cache: {ex.Message}");
                }
            }

            if (_configData.Count == 0)
            {
                Debug.Log("[GoogleSheetsConfig] Cache empty. Loading hardcoded defaults.");
                LoadHardcodedDefaults();
            }
            else
            {
                Debug.Log($"[GoogleSheetsConfig] Loaded {_configData.Count} keys from cache.");
            }
        }

        private void SaveToCache()
        {
            try
            {
                var wrapper = new CacheWrapper();
                foreach (var kvp in _configData)
                {
                    wrapper.entries.Add(new CacheEntry { key = kvp.Key, value = kvp.Value });
                }
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(CacheKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleSheetsConfig] Error saving cache: {ex.Message}");
            }
        }

        private void LoadHardcodedDefaults()
        {
            // CONFIG
            _configData[ConfigKeys.DemoMode] = "TRUE";
            _configData[ConfigKeys.AppVersionMin] = "1.0.0";
            _configData[ConfigKeys.MaintenanceMode] = "FALSE";
            _configData[ConfigKeys.OracleDailyLimitFree] = "10";
            _configData[ConfigKeys.OracleDailyLimitEnlightened] = "-1";
            _configData[ConfigKeys.CacheTimeoutMinutes] = "5";
            _configData[ConfigKeys.TelemetryEnabled] = "TRUE";

            // ECONOMY
            _configData[ConfigKeys.QuantToUsdRate] = "0.01";
            _configData[ConfigKeys.HorasToUsdRate] = "0.15";
            _configData[ConfigKeys.CommissionPercent] = "7";
            _configData[ConfigKeys.MinCashoutThreshold] = "50";
            _configData[ConfigKeys.MaxCashoutThreshold] = "1000";
            _configData[ConfigKeys.QuantPack100Price] = "0.99";
            _configData[ConfigKeys.QuantPack500Price] = "4.99";
            _configData[ConfigKeys.QuantPack1000Price] = "9.99";
            _configData[ConfigKeys.EnlightenedSubscriptionPrice] = "4.99";
            _configData[ConfigKeys.FreeUserDailyBonus] = "50";
            _configData[ConfigKeys.HoraDisplayThreshold] = "60";
            _configData[ConfigKeys.QuantToWavesRate] = "100";
            _configData[ConfigKeys.PostBoostCostWaves] = "5000";
            _configData[ConfigKeys.TipMinWaves] = "1000";
            _configData[ConfigKeys.StripeCommissionPercent] = "7";
            _configData[ConfigKeys.StripeTestMode] = "TRUE";

            // ORACLE_PROMPTS
            _configData[ConfigKeys.ToneMystic] = "Ти — містичний провидець Нексусу. Використовуй метафори, поетичні образи, звертайся 'Адепте'. Говори про нитки долі, резонанс, симетрію.";
            _configData[ConfigKeys.ToneBusiness] = "Ти — діловий аналітик TimeAura. Будь конкретним, використовуй цифри, факти, терміни. Фокус на ефективності та ROI.";
            _configData[ConfigKeys.ToneCasual] = "Ти — дружній порадник. Говори просто, використовуй емодзі 😊, будь легким і підтримуючим. Звертайся на 'ти'.";
            _configData[ConfigKeys.ToneTech] = "Ти — технічний експерт. Використовуй профільну термінологію, будь точним, структурованим. Фокус на архітектурі та алгоритмах.";
            _configData[ConfigKeys.GreetingUa] = "Вітаю в Нексусі, Адепте. Я — Оракул, твій провідник у світі Симетрій. Чим можу допомогти?";
            _configData[ConfigKeys.GreetingEn] = "Welcome to the Nexus, Adept. I am the Oracle, your guide in the world of Symmetries. How may I assist you?";
            _configData[ConfigKeys.ArbitrationPrompt] = "Ти — Суддя Хроносу. Проаналізуй лог чату між двома адептами. Визнач, хто виконав зобов'язання, а хто ні. Будь об'єктивним і справедливим.";
            _configData[ConfigKeys.IntentParserPrompt] = "Ти — архітектор намірів TimeAura. Проаналізуй запит користувача і визнач: категорію послуги, ціну в Хорах/Квантах, терміновість, локацію.";

            // FEATURE_FLAGS
            _configData[ConfigKeys.VoiceSearch] = "TRUE";
            _configData[ConfigKeys.MatterRealm] = "TRUE";
            _configData[ConfigKeys.Arbitration] = "FALSE";
            _configData[ConfigKeys.Cashout] = "FALSE";
            _configData[ConfigKeys.DeepLinking] = "FALSE";
            _configData[ConfigKeys.OraclePersonalityEngine] = "TRUE";
            _configData[ConfigKeys.AnalyticsLogging] = "TRUE";
            _configData[ConfigKeys.DemoModeToggle] = "TRUE";
        }

        private string ExtractSpreadsheetId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (input.Contains("docs.google.com/spreadsheets"))
            {
                int dIndex = input.IndexOf("/d/");
                if (dIndex != -1)
                {
                    string sub = input.Substring(dIndex + 3);
                    int slashIndex = sub.IndexOf("/");
                    if (slashIndex != -1)
                    {
                        return sub.Substring(0, slashIndex);
                    }
                    return sub;
                }
            }
            return input;
        }

        [Serializable]
        private class CacheEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class CacheWrapper
        {
            public List<CacheEntry> entries = new List<CacheEntry>();
        }
    }
}
