using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TimeAura.Editor
{
    /// <summary>
    /// One-click setup for ConvergenceScene (Nexus UI with UIDocument).
    /// Menu: TimeAura → Setup → Setup ConvergenceScene
    /// </summary>
    public static class ConvergenceSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/ConvergenceScene.unity";
        private const string UxmlPath = "Assets/UI/Nexus/NexusScene.uxml";
        private const string SceneFolder = "Assets/Scenes";

        [MenuItem("TimeAura/Setup/Setup ConvergenceScene (Nexus UI)", priority = 5)]
        public static void SetupConvergenceScene()
        {
            // Ensure Scenes folder
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Save current scene
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Open or create scene
            Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log($"[ConvergenceSceneSetup] Opened existing scene: {ScenePath}");
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                Debug.Log($"[ConvergenceSceneSetup] Created new scene: {ScenePath}");
            }

            SetupSceneObjects(scene);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ConvergenceSceneSetup] ✅ ConvergenceScene setup complete! Press Play to test.");
        }

        private static void SetupSceneObjects(Scene scene)
        {
            // ── 1. Camera ─────────────────────────────────────────────────────
            EnsureCamera(scene);

            // ── 2. EventSystem ────────────────────────────────────────────────
            EnsureEventSystem(scene);

            // ── 3. UIDocument GameObject ──────────────────────────────────────
            EnsureUIDocument(scene);

            // ── 4. NexusController on the same GO ─────────────────────────────
            EnsureNexusController(scene);

            Debug.Log("[ConvergenceSceneSetup] Scene objects configured:");
            Debug.Log("  ✅ Main Camera");
            Debug.Log("  ✅ EventSystem");
            Debug.Log("  ✅ NexusUI (UIDocument + NexusController)");
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void EnsureCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<Camera>() != null) return;

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            cam.orthographic = false;
            SceneManager.MoveGameObjectToScene(camGO, scene);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<EventSystem>() != null) return;

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            SceneManager.MoveGameObjectToScene(esGO, scene);
        }

        private static void EnsureUIDocument(Scene scene)
        {
            // Find existing
            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<UIDocument>(true);
                if (existing != null)
                {
                    AssignUxml(existing);
                    Debug.Log($"[ConvergenceSceneSetup] UIDocument already exists on '{root.name}' — UXML assigned.");
                    return;
                }
            }

            // Create new
            var go = new GameObject("NexusUI");
            SceneManager.MoveGameObjectToScene(go, scene);
            var doc = go.AddComponent<UIDocument>();
            AssignUxml(doc);
            Debug.Log("[ConvergenceSceneSetup] Created 'NexusUI' with UIDocument.");
        }

        private static void AssignUxml(UIDocument doc)
        {
            if (doc.visualTreeAsset != null) return; // already assigned
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxml != null)
            {
                doc.visualTreeAsset = uxml;
                EditorUtility.SetDirty(doc);
                Debug.Log($"[ConvergenceSceneSetup] Assigned {UxmlPath} to UIDocument.");
            }
            else
            {
                Debug.LogWarning($"[ConvergenceSceneSetup] UXML not found at {UxmlPath}. Assign it manually.");
            }
        }

        private static void EnsureNexusController(Scene scene)
        {
            // Find the UIDocument GO and add NexusController to it
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "NexusUI") continue;
                var nexus = root.GetComponent("TimeAura.Features.UI.Nexus.NexusController");
                if (nexus == null)
                {
                    // Use reflection to add
                    var type = FindType("TimeAura.Features.UI.Nexus.NexusController");
                    if (type != null)
                    {
                        root.AddComponent(type);
                        EditorUtility.SetDirty(root);
                        Debug.Log("[ConvergenceSceneSetup] Added NexusController to NexusUI.");
                    }
                    else
                    {
                        Debug.LogWarning("[ConvergenceSceneSetup] Type NexusController not found — add it manually.");
                    }
                }
                return;
            }
        }

        private static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
