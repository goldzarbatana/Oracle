using UnityEditor;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools
{
    /// <summary>
    /// Editor hook that automatically runs a Google Sheet data fetch on entering Play Mode
    /// if the AutoSyncOnPlay toggle is enabled on the active scene manager.
    /// </summary>
    [InitializeOnLoad]
    public static class Pro_DDA_AutoSyncOnPlay
    {
        static Pro_DDA_AutoSyncOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
#if UNITY_2023_1_OR_NEWER
                var manager = Object.FindAnyObjectByType<Pro_DDA_Manager>();
#else
                var manager = Object.FindObjectOfType<Pro_DDA_Manager>();
#endif
                if (manager != null && manager.AutoSyncOnPlay)
                {
                    Debug.Log("[Pro DDA] Auto-Sync on Play triggered. Fetching data from Google Sheets...");
                    manager.FetchDataFromSheet((success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log("[Pro DDA] Play Mode Auto-Sync succeeded and applied to all active listeners.");
                        }
                        else
                        {
                            Debug.LogWarning($"[Pro DDA] Play Mode Auto-Sync failed: {message}. Using cached or fallback data.");
                        }
                    });
                }
            }
        }
    }
}
