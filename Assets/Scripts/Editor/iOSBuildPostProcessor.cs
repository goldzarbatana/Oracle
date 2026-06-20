using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace TimeAura.Editor.Build
{
    /// <summary>
    /// iOS Build Post Processor - Automatically injects required location permissions into the generated Xcode plist.
    /// Eliminates manual plist editing inside Xcode after each Unity build.
    /// </summary>
    public static class iOSBuildPostProcessor
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

#if UNITY_IOS
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var rootDict = plist.root;

            // 1. Geolocation Permission Description
            rootDict.SetString("NSLocationWhenInUseUsageDescription", 
                "TimeAura потребує доступу до вашої геолокації для визначення ефірного царства та пошуку Майстрів поблизу.");

            rootDict.SetString("NSLocationAlwaysAndWhenInUseUsageDescription", 
                "TimeAura потребує постійного доступу до геолокації для автономного резонансу та синхронізації зіркових карт.");

            // 2. Camera Permission (for Visage avatar capture)
            rootDict.SetString("NSCameraUsageDescription", 
                "TimeAura потребує доступу до камери для зйомки вашого Visage-аватара.");

            // Write updates back to plist
            plist.WriteToFile(plistPath);
            UnityEngine.Debug.Log("<color=#FFD700><b>[BUILD MASTER]</b></color> 🍏 iOS Info.plist successfully configured with dynamic geolocation and visage permissions!");
#endif
        }
    }
}
