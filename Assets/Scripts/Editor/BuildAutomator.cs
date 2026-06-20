using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Rendering;
using System.Collections.Generic;

namespace TimeAura.Editor.Build
{
    /// <summary>
    /// Build Automator - Your one-click path to Google Play.
    /// Configures Android settings, optimizes size, and triggers builds.
    /// </summary>
    public class BuildAutomator : EditorWindow
    {
        [MenuItem("TimeAura/Build/Build Panel")]
        public static void ShowWindow() => GetWindow<BuildAutomator>("Build Master");

        private void OnGUI()
        {
            GUILayout.Label("GOOGLE PLAY OPTIMIZATION", EditorStyles.boldLabel);
            
            if (GUILayout.Button("1. CONFIGURE FOR GOOGLE PLAY", GUILayout.Height(40)))
            {
                OptimizeForGooglePlay();
            }

            GUILayout.Space(10);
            GUILayout.Label("DIAGNOSTICS", EditorStyles.boldLabel);
            if (GUILayout.Button("AUDIT POTENTIAL DUPLICATES"))
            {
                AuditDuplicates();
            }

            GUILayout.Space(10);
            GUILayout.Label("BUILD ACTIONS", EditorStyles.boldLabel);

            if (GUILayout.Button("BUILD ANDROID APP BUNDLE (AAB)", GUILayout.Height(50)))
            {
                PerformBuild(true);
            }
        }

        public static void AuditDuplicates()
        {
            Debug.Log("<color=#FFD700><b>[BUILD MASTER]</b></color> 🔍 Auditing Project for Duplicates...");
            
            // Logic: Find assets that are in Resources but also referenced elsewhere
            string[] resourcesPaths = System.IO.Directory.GetFiles(Application.dataPath, "*.*", System.IO.SearchOption.AllDirectories);
            int resourcesCount = 0;
            foreach (var path in resourcesPaths)
            {
                string normalizedPath = path.Replace("\\", "/");
                if (normalizedPath.Contains("/Resources/"))
                {
                    string relativePath = "Assets" + normalizedPath.Replace(Application.dataPath.Replace("\\", "/"), "");
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
                    if (asset != null && !relativePath.EndsWith(".meta"))
                    {
                        resourcesCount++;
                        Debug.Log($"[RESOURCES] 📦 {relativePath}");
                    }
                }
            }
            Debug.Log($"Found {resourcesCount} assets in Resources. Note: Assets in Resources are ALWAYS included in build.");
            Debug.Log("Tip: Move UI UXML/USS files OUT of Resources to avoid duplication if they are referenced by scenes directly.");
        }

        public static void OptimizeForGooglePlay()
        {
            // 1. Switch to Android
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // 2. Set App Bundle (AAB) requirement for Play Store
            EditorUserBuildSettings.buildAppBundle = true;
            
            // 3. Scripting Backend to IL2CPP (Required for 64-bit)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            
            // 4. Target Architectures: ARMv7 + ARM64 (Required)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            
            // 5. Optimization: Strip Unused Code
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High); // High for Play Store
            
            // 6. Texture Compression: ASTC (Modern standard)
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // 7. Disable unnecessary shader variants in Graphics Settings
            // Note: This is handled by ShaderVariantStripper below

            Debug.Log("<color=#FFD700><b>[BUILD MASTER]</b></color> ✦ Project optimized for Google Play!");
        }

        private static void PerformBuild(bool isAAB)
        {
            string extension = isAAB ? "aab" : "apk";
            string path = EditorUtility.SaveFilePanel("Save Build", "", "TimeAura_Release", extension);
            
            if (string.IsNullOrEmpty(path)) return;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"<color=#00FF00><b>[BUILD SUCCESS]</b></color> ✦ Size: {summary.totalSize / 1024 / 1024} MB");
            else
                Debug.LogError("[BUILD FAILED] Check console for details.");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            return scenes.ToArray();
        }
    }

    /// <summary>
    /// Shader Stripper - Stops the "Infinite Compilation" madness.
    /// Strips variants that aren't used in the project.
    /// </summary>
    public class ShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Task: Kill all variants we don't need for a fast build
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var keywordSet = data[i].shaderKeywordSet;

                // 1. Strip Fog (unless you use it)
                if (keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("FOG_LINEAR")) ||
                    keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("FOG_EXP")) ||
                    keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("FOG_EXP2")))
                {
                    data.RemoveAt(i);
                    continue;
                }

                // 2. Strip Lightmaps (if using Realtime or Simple lighting)
                if (keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("LIGHTMAP_ON")) ||
                    keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("DIRLIGHTMAP_COMBINED")))
                {
                    data.RemoveAt(i);
                    continue;
                }

                // 3. Strip Shadows variants if you don't need all of them
                if (keywordSet.IsEnabled(new UnityEngine.Rendering.ShaderKeyword("SHADOWS_SOFT")))
                {
                    // data.RemoveAt(i); // Uncomment if you don't use soft shadows
                }
            }
        }
    }
}
