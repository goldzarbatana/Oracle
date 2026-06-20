using UnityEditor;
using UnityEngine;
using System.IO;

namespace TimeAura.Editor
{
    public static class AuraDevelopmentTools
    {
        [MenuItem("Aura/Development/Reset Project to First Launch")]
        public static void ResetProjectToFirstLaunch()
        {
            Debug.Log("[AuraDevTools] 🕯️ Starting Project Purification Ritual (Reset to First Launch)...");

            // 1. Delete PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[AuraDevTools] 🧹 PlayerPrefs deleted.");

            // 2. Clear Caching
            if (Caching.ClearCache())
            {
                Debug.Log("[AuraDevTools] 🌪️ Unity Cache cleared.");
            }
            else
            {
                Debug.Log("[AuraDevTools] ⚠️ Unity Cache was already empty or could not be cleared.");
            }

            // 3. Clear persistentDataPath
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                var directoryInfo = new DirectoryInfo(path);
                int deletedFiles = 0;
                int deletedDirs = 0;

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    try
                    {
                        file.Delete();
                        deletedFiles++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[AuraDevTools] Could not delete file {file.Name}: {ex.Message}");
                    }
                }

                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                        deletedDirs++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[AuraDevTools] Could not delete directory {dir.Name}: {ex.Message}");
                    }
                }

                Debug.Log($"[AuraDevTools] 🌊 PersistentDataPath purged. Deleted {deletedFiles} files and {deletedDirs} directories.");
            }
            else
            {
                Debug.Log("[AuraDevTools] ℹ️ PersistentDataPath directory does not exist.");
            }

            // 4. Force Asset Database Refresh
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Ritual Complete", 
                "The project has been purified. All local database records, user profiles, PlayerPrefs, and addressable caches have been wiped. Next launch will be treated as the very first run.", 
                "Amen");

            Debug.Log("[AuraDevTools] ✨ Project purification complete. All local legacy history is dissolved into the void.");
        }
    }
}
