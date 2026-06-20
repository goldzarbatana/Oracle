#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TimeAura.Editor
{
    /// <summary>
    /// Creates or updates the ConvergenceScene with required root GameObjects and (when available)
    /// attaches existing project components by full type name via reflection.
    /// Run via TimeAura -> Scenes -> Create/Update ConvergenceScene
    /// </summary>
    public static class ConvergenceSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/ConvergenceScene.unity";

        [MenuItem("TimeAura/Scenes/Create or Update ConvergenceScene")]
        public static void CreateOrUpdateScene()
        {
            // Create new scene if it doesn't exist, otherwise open existing
            Scene scene;
            if (!System.IO.File.Exists(ScenePath))
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Debug.Log("[ConvergenceSceneCreator] Created new ConvergenceScene.");
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("[ConvergenceSceneCreator] Opened existing ConvergenceScene.");
            }

            // Ensure main camera
            if (UnityEngine.Object.FindAnyObjectByType<Camera>() == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                Debug.Log("[ConvergenceSceneCreator] Added Main Camera.");
            }

            // Ensure EventSystem
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                #if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
                Debug.Log("[ConvergenceSceneCreator] Added EventSystem (InputSystem adapted).");
            }

            // Ensure Canvas
            if (UnityEngine.Object.FindAnyObjectByType<Canvas>() == null)
            {
                var canvasGO = new GameObject("UI Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[ConvergenceSceneCreator] Added UI Canvas.");
            }

            // Create root manager objects and attempt to attach real components
            CreateRootObjectWithComponent("TimeAuraLifetimeScope", "TimeAura.Core.Infrastructure.TimeAuraLifetimeScope");
            CreateRootObjectWithComponent("GlobalManager", "TimeAura.Core.GlobalManager");
            CreateRootObjectWithComponent("GameManager", "TimeAura.Features.Aura.GameManager");
            CreateRootObjectWithComponent("UIManager", "TimeAura.Features.UI.UIManager");
            CreateRootObjectWithComponent("LocalizationManager", "TimeAura.Features.Localization.LocalizationManager");
            CreateRootObjectWithComponent("AudioService", "TimeAura.Features.Audio.AudioService");
            CreateRootObjectWithComponent("FirebaseDataService", "TimeAura.Features.Data.FirebaseDataService");
            CreateRootObjectWithComponent("TwilioSmsGateway", "TimeAura.Features.Security.TwilioSmsGateway");
            CreateRootObjectWithComponent("AuraEffectManager", "TimeAura.Features.Aura.AuraEffectManager");
            CreateRootObjectWithComponent("TransformationManager", "TimeAura.Features.Transformation.TransformationManager");

            // Add placeholders for UI views
            CreateRootObjectWithComponent("ActiveSessionView", "TimeAura.Features.UI.Auth.ActiveSessionView");
            CreateRootObjectWithComponent("ResonanceSelectionView", "TimeAura.Features.UI.Transformation.ResonanceSelectionView");

            // Save scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[ConvergenceSceneCreator] ConvergenceScene saved at: " + ScenePath);

            // Focus Project window on scene
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset != null) EditorGUIUtility.PingObject(sceneAsset);
        }

        private static void CreateRootObjectWithComponent(string objectName, string componentFullName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Debug.Log($"[ConvergenceSceneCreator] Found existing GameObject '{objectName}', checking component '{componentFullName}'.");
                AttachComponentIfMissing(existing, componentFullName);
                return;
            }

            var go = new GameObject(objectName);
            AttachComponentIfMissing(go, componentFullName);
            // Keep as root
            go.transform.SetParent(null);
            Debug.Log($"[ConvergenceSceneCreator] Created GameObject '{objectName}' (component: {componentFullName}).");
        }

        private static void AttachComponentIfMissing(GameObject go, string componentFullName)
        {
            if (string.IsNullOrEmpty(componentFullName)) return;

            // Search loaded assemblies for the type
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type found = null;
            foreach (var asm in assemblies)
            {
                try
                {
                    var t = asm.GetType(componentFullName, throwOnError: false, ignoreCase: false);
                    if (t != null && typeof(Component).IsAssignableFrom(t))
                    {
                        found = t;
                        break;
                    }
                }
                catch { }
            }

            if (found != null)
            {
                if (go.GetComponent(found) == null)
                {
                    go.AddComponent(found);
                    Debug.Log($"[ConvergenceSceneCreator] Attached component '{componentFullName}' to '{go.name}'.");
                }
                else
                {
                    Debug.Log($"[ConvergenceSceneCreator] '{go.name}' already has component '{componentFullName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"[ConvergenceSceneCreator] Type '{componentFullName}' not found in loaded assemblies. Created GameObject '{go.name}' without that component.");
            }
        }
    }
}
#endif
