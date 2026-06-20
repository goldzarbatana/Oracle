using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeAura.Editor
{
    /// <summary>
    /// Utility to create the '--== MANAGERS ==--' prefab and inject it into all project scenes.
    /// Menu: TimeAura → Setup → Create Managers Prefab
    ///       TimeAura → Setup → Inject Managers Prefab in All Scenes
    ///       TimeAura → Setup → Inject Managers Prefab in Active Scene
    /// </summary>
    public static class ManagersPrefabCreator
    {
        private const string PrefabFolder = "Assets/Prefabs/Core";
        private const string PrefabPath = PrefabFolder + "/Managers.prefab";
        private const string ManagersGOName = "--== MANAGERS ==--";

        // All manager/service component type names that live on the prefab.
        // Add/remove entries here to match your project.
        private static readonly string[] ManagerTypeNames =
        {
            "TimeAura.Core.GameManager",
            "TimeAura.Features.Auth.AuthManager",
            "TimeAura.Features.Localization.LocalizationManager",
            "TimeAura.Features.UI.UIManager",
            "TimeAura.Features.Aura.AuraEffectManager",
            "TimeAura.Features.Auth.InitiationProcessor",
            "TimeAura.Features.Social.SocialManager",
            "TimeAura.Features.Transformation.TransformationManager",
            "TimeAura.Features.Matching.MatchingManager",
            "TimeAura.Features.Security.SecurityHub",
            "TimeAura.Features.Data.FirebaseDataService",
            "TimeAura.Features.Security.TwilioSmsGateway",
        };

        // ─────────────────────────────────────
        //  MENU ITEMS
        // ─────────────────────────────────────

        [MenuItem("TimeAura/Setup/Create Managers Prefab", priority = 0)]
        public static void CreateManagersPrefab()
        {
            CreateOrUpdatePrefab();
        }

        [MenuItem("TimeAura/Setup/Inject Managers in Active Scene", priority = 10)]
        public static void InjectInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            InjectInScene(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ManagersPrefabCreator] Saved scene: {scene.name}");
        }

        [MenuItem("TimeAura/Setup/Inject Managers in All Build Scenes", priority = 11)]
        public static void InjectInAllBuildScenes()
        {
            // Save open scene first
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var buildScenes = EditorBuildSettings.scenes;
            foreach (var buildScene in buildScenes)
            {
                if (!buildScene.enabled) continue;
                var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                InjectInScene(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[ManagersPrefabCreator] ✅ Processed: {scene.name}");
            }

            Debug.Log($"[ManagersPrefabCreator] 🎉 Done! Processed {buildScenes.Length} scene(s).");
        }

        [MenuItem("TimeAura/Setup/Show Managers Setup Status", priority = 20)]
        public static void ShowStatus()
        {
            bool prefabExists = File.Exists(PrefabPath);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Managers Prefab Status ===");
            sb.AppendLine($"Prefab: {(prefabExists ? "✅ EXISTS" : "❌ MISSING")} at {PrefabPath}");

            if (prefabExists)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                sb.AppendLine("\nComponents on prefab:");
                foreach (var typeName in ManagerTypeNames)
                {
                    var type = FindType(typeName);
                    if (type == null) { sb.AppendLine($"  ⚠️  {typeName} — type not found"); continue; }
                    var comp = prefab.GetComponent(type);
                    sb.AppendLine($"  {(comp != null ? "✅" : "❌")} {type.Name}");
                }
            }

            sb.AppendLine("\nBuild Scenes:");
            foreach (var bs in EditorBuildSettings.scenes)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(bs.path);
                sb.AppendLine($"  {(bs.enabled ? "✅" : "—")} {bs.path}");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Managers Prefab Status", sb.ToString(), "OK");
        }

        // ─────────────────────────────────────
        //  CORE LOGIC
        // ─────────────────────────────────────

        /// <summary>Create or update Assets/Prefabs/Core/Managers.prefab.</summary>
        public static GameObject CreateOrUpdatePrefab()
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Core");

            // Create a temporary runtime GO to build the prefab from
            var go = new GameObject(ManagersGOName);

            int added = 0, skipped = 0;
            foreach (var typeName in ManagerTypeNames)
            {
                var type = FindType(typeName);
                if (type == null)
                {
                    Debug.LogWarning($"[ManagersPrefabCreator] Type not found — skipping: {typeName}");
                    skipped++;
                    continue;
                }

                if (go.GetComponent(type) == null)
                {
                    go.AddComponent(type);
                    added++;
                }
            }

            // Save as prefab (overwrite if exists)
            bool overwrite = File.Exists(PrefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out bool success);
            GameObject.DestroyImmediate(go);

            if (success)
            {
                Debug.Log($"[ManagersPrefabCreator] ✅ Prefab {(overwrite ? "updated" : "created")}: {PrefabPath}  ({added} components, {skipped} skipped)");
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError($"[ManagersPrefabCreator] ❌ Failed to save prefab at {PrefabPath}");
            }

            return prefab;
        }

        /// <summary>
        /// Inject the Managers prefab into a scene. If an instance already exists (by name),
        /// skip. Otherwise instantiate the prefab, make it root, and keep a scene reference.
        /// Also auto-assigns the prefab to TimeAuraLifetimeScope.managersPrefab if found.
        /// </summary>
        public static void InjectInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"[ManagersPrefabCreator] Scene not loaded: {scene.name}");
                return;
            }

            // Check if already present
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == ManagersGOName)
                {
                    Debug.Log($"[ManagersPrefabCreator] '{ManagersGOName}' already present in scene '{scene.name}' — skipping.");
                    TryAssignPrefabToLifetimeScope(scene);
                    return;
                }
            }

            // Load prefab (create if missing)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.Log("[ManagersPrefabCreator] Prefab not found — creating first...");
                prefab = CreateOrUpdatePrefab();
            }

            if (prefab == null)
            {
                Debug.LogError("[ManagersPrefabCreator] Cannot inject — prefab creation failed.");
                return;
            }

            // Instantiate as prefab link
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(null); // ensure root
            instance.name = ManagersGOName;

            Debug.Log($"[ManagersPrefabCreator] ✅ Injected '{ManagersGOName}' into scene '{scene.name}'.");
            TryAssignPrefabToLifetimeScope(scene);
        }

        // ─────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────

        /// <summary>Try to assign the Managers prefab to TimeAuraLifetimeScope.managersPrefab via reflection.</summary>
        private static void TryAssignPrefabToLifetimeScope(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                // Find VContainer LifetimeScope subclass
                var scope = root.GetComponentInChildren<VContainer.Unity.LifetimeScope>(true);
                if (scope == null) continue;

                var field = scope.GetType().GetField("managersPrefab",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null) continue;

                if (field.GetValue(scope) == null)
                {
                    field.SetValue(scope, prefab);
                    EditorUtility.SetDirty(scope);
                    Debug.Log($"[ManagersPrefabCreator] ✅ Assigned Managers prefab → {scope.gameObject.name}.managersPrefab");
                }
                else
                {
                    Debug.Log($"[ManagersPrefabCreator] LifetimeScope '{scope.gameObject.name}' already has managersPrefab assigned.");
                }
                break;
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
