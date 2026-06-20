using UnityEditor;
using UnityEngine;
using System.Diagnostics;

namespace TimeAura.EditorScripts
{
    public class FirebaseDeployEditor : EditorWindow
    {
        [MenuItem("TimeAura/🚀 Deploy Firebase Functions")]
        public static void DeployFirebase()
        {
            UnityEngine.Debug.Log("[Firebase Deploy] Starting deployment process...");
            
            string firebaseDir = System.IO.Path.Combine(Application.dataPath, "../FirebaseServer");
            firebaseDir = System.IO.Path.GetFullPath(firebaseDir);
            
            if (!System.IO.Directory.Exists(firebaseDir))
            {
                UnityEngine.Debug.LogError($"[Firebase Deploy] Directory not found: {firebaseDir}");
                return;
            }

            // Create process info for npm and firebase
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c cd /d \"{firebaseDir}\" & npm install & firebase deploy --only functions");
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = true;

            try
            {
                Process process = Process.Start(processInfo);
                UnityEngine.Debug.Log("[Firebase Deploy] Deployment terminal opened. Check the new window for progress.");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[Firebase Deploy] Failed to launch deploy process: {ex.Message}");
            }
        }
    }
}
