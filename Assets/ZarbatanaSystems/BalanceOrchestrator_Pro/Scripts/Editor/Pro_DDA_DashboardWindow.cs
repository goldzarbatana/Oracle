using UnityEngine;
using UnityEditor;
using ZarbatanaSystems.BalanceOrchestrator.Pro;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools
{
    public class Pro_DDA_DashboardWindow : EditorWindow
    {
        private Pro_DDA_Manager manager;
        private string testMessage = "Not connected.";
        private bool isSuccess = false;
        private bool isFetching = false;

        [MenuItem("Tools/Balance Orchestrator/Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<Pro_DDA_DashboardWindow>("Balance Orchestrator Dashboard");
        }

        private void OnGUI()
        {
            GUILayout.Label("Balance Orchestrator (PRO)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (manager == null)
            {
#if UNITY_2023_1_OR_NEWER
                manager = FindFirstObjectByType<Pro_DDA_Manager>();
#else
                manager = FindObjectOfType<Pro_DDA_Manager>();
#endif
                if (manager == null)
                {
                    EditorGUILayout.HelpBox("No Pro_DDA_Manager found in the active scene. Please add one to a GameObject to sync.", MessageType.Warning);
                    return;
                }
            }

            EditorGUI.BeginChangeCheck();
            manager.DataSource = EditorGUILayout.TextField("Data Source (Sheet ID or URL)", manager.DataSource);
            manager.Gid = EditorGUILayout.TextField("Sheet GID", manager.Gid);
            manager.KeyColumnName = EditorGUILayout.TextField("Key Column Name", manager.KeyColumnName);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            // Copy Demo Sheet button
            if (GUILayout.Button("Copy Demo Sheet (Google Sheets Template)", GUILayout.Height(30)))
            {
                Application.OpenURL("https://docs.google.com/spreadsheets/d/1tqITiO_iAecnLoY7WWL3Am29VgoVpjk1GxlugVwzv6U/copy");
            }
            EditorGUILayout.Space();

            GUI.enabled = !isFetching;
            if (GUILayout.Button("Sync Data with Web / Cloud", GUILayout.Height(35)))
            {
                if (Application.isPlaying)
                {
                    isFetching = true;
                    testMessage = "Fetching data in Play Mode...";
                    manager.FetchDataFromSheet((success, msg) => {
                        isSuccess = success;
                        testMessage = success ? "Data successfully synchronized!" : $"Error: {msg}";
                        isFetching = false;
                        Repaint();
                    });
                }
                else
                {
                    // Safe Web Request in Editor Mode
                    isFetching = true;
                    testMessage = "Fetching data in Editor Mode...";
                    
                    string url = (manager.DataSource.StartsWith("http://") || manager.DataSource.StartsWith("https://"))
                        ? manager.DataSource
                        : $"https://docs.google.com/spreadsheets/d/{manager.DataSource}/export?format=csv&gid={manager.Gid}";

                    var www = UnityEngine.Networking.UnityWebRequest.Get(url);
                    www.SendWebRequest();

                    EditorApplication.CallbackFunction updateCallback = null;
                    updateCallback = () =>
                    {
                        if (!www.isDone) return;

                        EditorApplication.update -= updateCallback;
                        isFetching = false;

                        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                        {
                            isSuccess = false;
                            testMessage = $"Error: {www.error}";
                        }
                        else
                        {
                            string rawText = www.downloadHandler.text;
                            try
                            {
                                string trimmed = rawText.TrimStart();
                                bool isJson = url.Contains(".json") || url.Contains("firebaseio.com") || trimmed.StartsWith("{") || trimmed.StartsWith("[");
                                
                                if (isJson)
                                {
                                    var parsedData = ZarbatanaSystems.BalanceOrchestrator.Pro.Utils.Pro_DDAJsonParser.Parse(rawText, manager.KeyColumnName);
                                    if (parsedData.Count > 0)
                                    {
                                        isSuccess = true;
                                        testMessage = "Data successfully synchronized!";
                                        manager.ParseData(rawText);
                                    }
                                    else
                                    {
                                        isSuccess = false;
                                        testMessage = "Error: Sheet is empty or Key Column Name is incorrect.";
                                    }
                                }
                                else
                                {
                                    var parsedData = ZarbatanaSystems.BalanceOrchestrator.Pro.Utils.Pro_DDACSVParser.Parse(rawText);
                                    if (parsedData.Count > 0)
                                    {
                                        isSuccess = true;
                                        testMessage = "Data successfully synchronized!";
                                        manager.ParseData(rawText);
                                    }
                                    else
                                    {
                                        isSuccess = false;
                                        testMessage = "Error: Sheet is empty.";
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                isSuccess = false;
                                testMessage = $"Error Parsing: {ex.Message}";
                            }
                        }
                        
                        www.Dispose();
                        Repaint();
                    };

                    EditorApplication.update += updateCallback;
                }
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            
            // Connection Status Indicator
            GUIStyle statusStyle = new GUIStyle(EditorStyles.helpBox);
            statusStyle.normal.textColor = isFetching ? Color.yellow : (isSuccess ? new Color(0.1f, 0.6f, 0.1f) : Color.red);
            statusStyle.alignment = TextAnchor.MiddleCenter;
            statusStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(testMessage, statusStyle, GUILayout.Height(30));

            // Statistics
            if (isSuccess && manager.GetRowCount() > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Data Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Rows Loaded: {manager.GetRowCount()}");
                EditorGUILayout.LabelField($"Columns Detected: {manager.GetColumnCount()}");
                
                EditorGUILayout.Space();
                if (GUILayout.Button("Clear Offline Cache"))
                {
                    manager.SetCachedCsvData("");
                    PlayerPrefs.DeleteKey("ZarbatanaSystems_Pro_DDA_Cache");
                    testMessage = "Offline cache cleared.";
                    isSuccess = false;
                }
            }
        }
    }
}
