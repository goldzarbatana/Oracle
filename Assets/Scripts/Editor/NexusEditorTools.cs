using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

namespace TimeAura.Editor
{
    public class NexusEditorTools
    {
        [MenuItem("Aura/UI/Force Refresh Nexus UI")]
        public static void ForceRefreshUI()
        {
            string uxmlPath = "Assets/UI/Nexus/NexusScene.uxml";
            string ussPath = "Assets/UI/Nexus/NexusScene.uss";

            Debug.Log("[NexusTools] 🔄 Initiating UI Force Refresh Ritual...");

            // 1. Force Re-import of assets
            if (File.Exists(uxmlPath)) AssetDatabase.ImportAsset(uxmlPath, ImportAssetOptions.ForceUpdate);
            if (File.Exists(ussPath)) AssetDatabase.ImportAsset(ussPath, ImportAssetOptions.ForceUpdate);

            // 2. Global Asset Refresh
            AssetDatabase.Refresh();

            // 3. Find active UIDocument in scene and force reload
            var uiDocs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doc in uiDocs)
            {
                if (doc.visualTreeAsset != null && doc.visualTreeAsset.name == "NexusScene")
                {
                    Debug.Log($"[NexusTools] ⚡ Forcing reload on {doc.name}");
                    var asset = doc.visualTreeAsset;
                    doc.visualTreeAsset = null;
                    doc.visualTreeAsset = asset;
                }
            }

            Debug.Log("[NexusTools] ✅ Ritual Complete. UI should be resurrected.");
        }

        [MenuItem("Aura/UI/Clear UI Toolkit Cache")]
        public static void ClearCache()
        {
            // Unity 6 has some internal caching, sometimes clearing the library helps
            // but for now, simple asset re-import is safer.
            Debug.Log("[NexusTools] 🧹 Cache purge requested (simulated via Refresh)");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
