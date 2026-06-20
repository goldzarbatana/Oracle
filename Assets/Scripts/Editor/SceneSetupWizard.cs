#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeAura.Editor
{
    /// <summary>
    /// Scene Setup Wizard - створює InitiationScene та ConvergenceScene.
    /// Window → Time Aura → Setup Scenes
    /// </summary>
    public class SceneSetupWizard : EditorWindow
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string InitiationSceneName = "InitiationScene";
        private const string ConvergenceSceneName = "ConvergenceScene";

        [MenuItem("Window/Time Aura/Setup Scenes")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupWizard>("Scene Setup");
            window.minSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            GUILayout.Label("🌙 Time Aura - Scene Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Створює дві сцени:\n" +
                "• InitiationScene (Login/Register)\n" +
                "• ConvergenceScene (Main Feed)",
                MessageType.Info
            );

            GUILayout.Space(20);

            if (GUILayout.Button("✨ Create Initiation Scene", GUILayout.Height(40)))
            {
                CreateInitiationScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🌀 Create Convergence Scene", GUILayout.Height(40)))
            {
                CreateConvergenceScene();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📋 Add Scenes to Build Settings", GUILayout.Height(30)))
            {
                AddScenesToBuildSettings();
            }
        }

        private void CreateInitiationScene()
        {
            CreateSceneWithTemplate(InitiationSceneName, SetupInitiationScene);
        }

        private void CreateConvergenceScene()
        {
            CreateSceneWithTemplate(ConvergenceSceneName, SetupConvergenceScene);
        }

        private void CreateSceneWithTemplate(string sceneName, System.Action<Scene> setupCallback)
        {
            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string scenePath = $"{ScenesFolder}/{sceneName}.unity";

            // Check if scene already exists
            if (File.Exists(scenePath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Scene Exists",
                    $"Scene '{sceneName}' already exists. Overwrite?",
                    "Yes", "No"
                );

                if (!overwrite)
                {
                    return;
                }
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            setupCallback?.Invoke(scene);

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SceneSetup] ✅ Created scene: {scenePath}");

            EditorUtility.DisplayDialog("Success", $"Scene '{sceneName}' created successfully!", "OK");
        }

        private void SetupInitiationScene(Scene scene)
        {
            // Create VContainer LifetimeScope
            var lifetimeScope = new GameObject("TimeAuraLifetimeScope");
            lifetimeScope.AddComponent<TimeAura.Core.Infrastructure.TimeAuraLifetimeScope>();

            // Create Canvas
            var canvas = CreateCanvas("InitiationCanvas");

            // Create InitiationView placeholder
            var initiationView = new GameObject("InitiationView");
            initiationView.transform.SetParent(canvas.transform);
            // User will attach InitiationView component manually

            // Create EventSystem
            CreateEventSystem();

            Debug.Log("[SceneSetup] InitiationScene template created. Add InitiationView component manually.");
        }

        private void SetupConvergenceScene(Scene scene)
        {
            // Create VContainer LifetimeScope
            var lifetimeScope = new GameObject("TimeAuraLifetimeScope");
            lifetimeScope.AddComponent<TimeAura.Core.Infrastructure.TimeAuraLifetimeScope>();

            // Create Canvas
            var canvas = CreateCanvas("ConvergenceCanvas");

            // Create ConvergenceFeed placeholder
            var feedView = new GameObject("ConvergenceFeed");
            feedView.transform.SetParent(canvas.transform);
            // User will attach ConvergenceFeed component manually

            // Create EventSystem
            CreateEventSystem();

            Debug.Log("[SceneSetup] ConvergenceScene template created. Add ConvergenceFeed component manually.");
        }

        private GameObject CreateCanvas(string name)
        {
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // Phone portrait

            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            return canvasGO;
        }

        private void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Use InputSystemUIInputModule if using the New Input System
                #if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
            }
        }

        private void AddScenesToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            string[] scenePaths =
            {
                $"{ScenesFolder}/{InitiationSceneName}.unity",
                $"{ScenesFolder}/{ConvergenceSceneName}.unity"
            };

            foreach (var path in scenePaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[SceneSetup] Scene not found: {path}");
                    continue;
                }

                // Check if already in build settings
                bool exists = scenes.Exists(s => s.path == path);
                if (!exists)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    Debug.Log($"[SceneSetup] Added to build: {path}");
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            EditorUtility.DisplayDialog("Success", "Scenes added to Build Settings!", "OK");
        }
    }
}
#endif
