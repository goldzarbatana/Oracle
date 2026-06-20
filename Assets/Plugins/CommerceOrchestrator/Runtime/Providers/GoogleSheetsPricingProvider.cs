using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using CommerceOrchestrator.Core;

namespace CommerceOrchestrator.Providers
{
    public class GoogleSheetsPricingProvider : IStorePricingProvider
    {
        private readonly string _sheetUrl;
        private readonly StoreDatabase _fallbackDatabase;
        private readonly Dictionary<string, ProductConfig> _configs = new Dictionary<string, ProductConfig>();

        public event Action OnPricingUpdated;
        public bool IsConnected { get; private set; }

        private const string CacheKey = "CommerceOrchestrator_PricingCache";
        private const string LastFetchKey = "CommerceOrchestrator_PricingLastFetch";
        private const int CacheCooldownSeconds = 300;

        public GoogleSheetsPricingProvider(string sheetUrl, StoreDatabase fallbackDatabase)
        {
            _sheetUrl = sheetUrl;
            _fallbackDatabase = fallbackDatabase;
            LoadFromCacheOrFallback();
        }

        public async UniTask FetchProductsAsync(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(_sheetUrl))
            {
                Debug.LogWarning("[CommerceOrchestrator] Google Sheets URL is not configured. Using local database fallback.");
                LoadFromFallbackDirect();
                return;
            }

            if (!forceReload)
            {
                string lastFetchStr = PlayerPrefs.GetString(LastFetchKey, "");
                if (DateTime.TryParse(lastFetchStr, null, DateTimeStyles.RoundtripKind, out DateTime lastFetchTime))
                {
                    if ((DateTime.UtcNow - lastFetchTime).TotalSeconds < CacheCooldownSeconds && _configs.Count > 0)
                    {
                        Debug.Log("[CommerceOrchestrator] Using cached pricing configuration (fetched less than 5 min ago).");
                        return;
                    }
                }
            }

            string spreadsheetId = ExtractSpreadsheetId(_sheetUrl);
            string queryUrl = _sheetUrl;

            if (_sheetUrl.Contains("docs.google.com/spreadsheets") && !string.IsNullOrEmpty(spreadsheetId) && !_sheetUrl.Contains("/pub"))
            {
                queryUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&sheet=SHOP_ITEMS";
            }

            Debug.Log($"[CommerceOrchestrator] Fetching pricing from Google Sheet. URL: {queryUrl}");

            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(queryUrl))
                {
                    await request.SendWebRequest().ToUniTask();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string csvData = request.downloadHandler.text;
                        ParseCsvData(csvData);
                        IsConnected = true;
                        SaveToCache();
                        PlayerPrefs.SetString(LastFetchKey, DateTime.UtcNow.ToString("o"));
                        PlayerPrefs.Save();
                        OnPricingUpdated?.Invoke();
                        Debug.Log($"[CommerceOrchestrator] Pricing successfully updated from Google Sheet. Loaded {_configs.Count} items.");
                    }
                    else
                    {
                        Debug.LogWarning($"[CommerceOrchestrator] Failed to fetch sheet: {request.error}. Using cache.");
                        IsConnected = false;
                        LoadFromCacheOrFallback();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CommerceOrchestrator] Exception while fetching pricing: {ex.Message}. Using cache.");
                IsConnected = false;
                LoadFromCacheOrFallback();
            }
        }

        public ProductConfig GetProductConfig(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            _configs.TryGetValue(productId, out var config);
            return config;
        }

        public IReadOnlyDictionary<string, ProductConfig> GetAllProductConfigs()
        {
            return _configs;
        }

        private void ParseCsvData(string csvText)
        {
            _configs.Clear();
            var lines = SplitCSVLines(csvText);
            if (lines.Count == 0) return;

            for (int i = 0; i < lines.Count; i++)
            {
                var columns = SplitCSVValues(lines[i]);
                if (columns.Count == 0) continue;

                string productId = columns[0].Trim();
                if (i == 0 && productId.Equals("productid", StringComparison.OrdinalIgnoreCase)) continue; // header
                if (string.IsNullOrEmpty(productId)) continue;

                float price = 0f;
                if (columns.Count > 1)
                {
                    float.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                }

                string rewardStr = columns.Count > 2 ? columns[2].Trim() : "";
                
                float discount = 0f;
                if (columns.Count > 3)
                {
                    float.TryParse(columns[3], NumberStyles.Any, CultureInfo.InvariantCulture, out discount);
                }

                bool isSub = false;
                if (columns.Count > 4)
                {
                    string subStr = columns[4].Trim().ToUpper();
                    isSub = subStr == "TRUE" || subStr == "1" || subStr == "YES";
                }

                var config = new ProductConfig
                {
                    productId = productId,
                    priceUsd = price,
                    rewardString = rewardStr,
                    discountPercent = discount,
                    isSubscription = isSub,
                    parsedRewards = ParseRewardString(rewardStr)
                };

                _configs[productId] = config;
            }
        }

        private List<RewardEntry> ParseRewardString(string rewardStr)
        {
            var list = new List<RewardEntry>();
            if (string.IsNullOrEmpty(rewardStr)) return list;

            var parts = rewardStr.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^([\d,]+)\s+(.+)$");
                if (match.Success)
                {
                    string numStr = match.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(numStr, out int amount))
                    {
                        list.Add(new RewardEntry(match.Groups[2].Value.Trim(), amount));
                    }
                }
            }
            return list;
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
                    if (slashIndex != -1) return sub.Substring(0, slashIndex);
                    return sub;
                }
            }
            return input;
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

        private void LoadFromCacheOrFallback()
        {
            _configs.Clear();
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
                            _configs[entry.productId] = entry.config;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CommerceOrchestrator] Error loading cache: {ex.Message}");
                }
            }

            if (_configs.Count == 0)
            {
                LoadFromFallbackDirect();
            }
        }

        private void LoadFromFallbackDirect()
        {
            if (_fallbackDatabase == null || _fallbackDatabase.products == null) return;
            foreach (var product in _fallbackDatabase.products)
            {
                if (product == null || string.IsNullOrEmpty(product.productId)) continue;

                var config = new ProductConfig
                {
                    productId = product.productId,
                    priceUsd = product.fallbackPriceUsd,
                    rewardString = product.fallbackRewardString,
                    isSubscription = product.isSubscription,
                    discountPercent = 0,
                    parsedRewards = ParseRewardString(product.fallbackRewardString)
                };
                _configs[product.productId] = config;
            }
        }

        private void SaveToCache()
        {
            try
            {
                var wrapper = new CacheWrapper();
                foreach (var kvp in _configs)
                {
                    wrapper.entries.Add(new CacheEntry { productId = kvp.Key, config = kvp.Value });
                }
                string json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(CacheKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommerceOrchestrator] Error saving cache: {ex.Message}");
            }
        }

        [Serializable]
        private class CacheEntry
        {
            public string productId;
            public ProductConfig config;
        }

        [Serializable]
        private class CacheWrapper
        {
            public List<CacheEntry> entries = new List<CacheEntry>();
        }
    }
}
