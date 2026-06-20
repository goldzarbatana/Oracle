using System;
using System.Collections.Generic;
using UnityEngine;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Interfaces;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Core;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;
using System.Linq;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    public class Pro_DDA_Manager : MonoBehaviour
    {
        public static Pro_DDA_Manager Instance { get; private set; }

        /// <summary>
        /// Event fired when DDA data has been successfully fetched and parsed.
        /// Useful for ScriptableObjects to subscribe and update their values.
        /// </summary>
        public static event Action OnDataUpdated;

        [Header("Data Source (Sheet ID or URL)")]
        [Tooltip("Paste your Google Sheet ID, Firebase RTDB URL (.json), or any direct CSV/JSON CDN link. The system auto-detects the format.")]
        [UnityEngine.Serialization.FormerlySerializedAs("SheetId")]
        public string DataSource = "1tqITiO_iAecnLoY7WWL3Am29VgoVpjk1GxlugVwzv6U";
        
        [Tooltip("The gid found in the URL for the specific tab.")]
        public string Gid = "0";

        [Tooltip("The name of the column that acts as the unique Key for rows.")]
        public string KeyColumnName = "LevelId";

        [Header("Offline Fallback")]
        [Tooltip("Local CSV asset to load if the player is offline and no cache exists yet.")]
        public TextAsset DefaultBalanceCSV;

        [Header("Editor Sync Options")]
        [Tooltip("If true, the editor will automatically sync balance from Google Sheets when entering Play Mode.")]
        public bool AutoSyncOnPlay = false;

        [Header("Data Cache")]
        [SerializeField, HideInInspector]
        private string cachedCsvData;

        // Runtime dictionary: Key -> ColumnData -> Value
        private Dictionary<string, Dictionary<string, string>> ddaData = new Dictionary<string, Dictionary<string, string>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Try to auto-load fallback from Resources if not manually assigned
                if (DefaultBalanceCSV == null)
                {
                    DefaultBalanceCSV = Resources.Load<TextAsset>("DefaultBalance");
                }

                InitializeFromCache();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeFromCache()
        {
            // 1. Try memory or PlayerPrefs cache first
            if (string.IsNullOrEmpty(cachedCsvData))
            {
                cachedCsvData = PlayerPrefs.GetString("ZarbatanaSystems_Pro_DDA_Cache", "");
            }

            // 2. Fallback to local TextAsset if cache is empty
            if (string.IsNullOrEmpty(cachedCsvData) && DefaultBalanceCSV != null)
            {
                cachedCsvData = DefaultBalanceCSV.text;
                Debug.Log("[Pro DDA] Local cache empty. Initialized from local DefaultBalance CSV TextAsset.");
            }

            // 3. Parse if we have data
            if (!string.IsNullOrEmpty(cachedCsvData))
            {
                ParseData(cachedCsvData);
            }
        }

        public void FetchDataFromSheet(Action<bool, string> onComplete = null)
        {
            StartCoroutine(Pro_GoogleSheetsDownloader.DownloadDataRoutine(DataSource, Gid,
                (data) => {
                    // Reconstruct CSV for caching
                    if (data.Count > 0)
                    {
                        var headers = data[0].Keys.ToList();
                        string csv = string.Join(",", headers) + "\n";
                        foreach (var row in data)
                        {
                            csv += string.Join(",", headers.Select(h => EscapeCSV(row[h]))) + "\n";
                        }
                        cachedCsvData = csv;
                        PlayerPrefs.SetString("ZarbatanaSystems_Pro_DDA_Cache", csv);
                        PlayerPrefs.Save();
                        
                        ProcessDataList(data);
                        onComplete?.Invoke(true, "Success");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Sheet is empty.");
                    }
                },
                (error) => {
                    Debug.LogError($"[Pro DDA] Fetch failed: {error}");
                    onComplete?.Invoke(false, error);
                }
            ));
        }

        private string EscapeCSV(string val)
        {
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
        }

        public void ParseData(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return;

            string trimmed = rawData.TrimStart();
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                // Parse as JSON!
                ddaData = Pro_DDAJsonParser.Parse(rawData, KeyColumnName);
            }
            else
            {
                // Parse as CSV!
                var data = Pro_DDACSVParser.Parse(rawData);
                ddaData.Clear();
                foreach (var row in data)
                {
                    if (row.TryGetValue(KeyColumnName, out string key) && !string.IsNullOrEmpty(key))
                    {
                        ddaData[key] = row;
                    }
                }
            }

            ApplyToAllActiveBalanceables();
            OnDataUpdated?.Invoke();
        }

        private void ProcessDataList(List<Dictionary<string, string>> list)
        {
            ddaData.Clear();
            foreach (var row in list)
            {
                if (row.TryGetValue(KeyColumnName, out string key) && !string.IsNullOrEmpty(key))
                {
                    ddaData[key] = row;
                }
            }
            ApplyToAllActiveBalanceables();
            OnDataUpdated?.Invoke(); // Notify ScriptableObjects and other non-MonoBehaviour listeners
        }

        public void ApplyToAllActiveBalanceables()
        {
#if UNITY_2023_1_OR_NEWER
            var receivers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<Pro_IDDABalanceable>();
#else
            var receivers = FindObjectsOfType<MonoBehaviour>().OfType<Pro_IDDABalanceable>();
#endif
            foreach (var receiver in receivers)
            {
                ApplyTo(receiver);
            }
        }

        public void ApplyTo(Pro_IDDABalanceable receiver)
        {
            string key = receiver.GetDDAKey();
            if (ddaData.TryGetValue(key, out var rowData))
            {
                receiver.ApplyDDAUpdate(rowData);
            }
            else
            {
                Debug.LogWarning($"[Pro DDA] Row Key '{key}' not found in Google Sheet.");
            }
        }

        public Dictionary<string, string> GetRowData(string key)
        {
            if (ddaData.TryGetValue(key, out var rowData))
            {
                return rowData;
            }
            return null;
        }
        
        public string GetCachedCsvData() => cachedCsvData;
        public void SetCachedCsvData(string csv) => cachedCsvData = csv;

        // Statistics for Editor Dashboard
        public int GetRowCount() => ddaData.Count;
        public int GetColumnCount() => ddaData.Count > 0 ? ddaData.Values.First().Count : 0;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticData()
        {
            Instance = null;
            OnDataUpdated = null;
        }
#endif
    }
}
