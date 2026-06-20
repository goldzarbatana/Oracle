#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UniversalMonetization.Editor
{
    [InitializeOnLoad]
    public class MonetizationWelcomeWindow : EditorWindow
    {
        private const string WindowShownKey = "UniversalMonetization_WelcomeShown_v1";

        static MonetizationWelcomeWindow()
        {
            EditorApplication.delayCall += ShowWindowOnFirstLoad;
        }

        private static void ShowWindowOnFirstLoad()
        {
            if (!EditorPrefs.GetBool(WindowShownKey, false))
            {
                EditorPrefs.SetBool(WindowShownKey, true);
                ShowWindow();
            }
        }

        [MenuItem("Tools/Monetization Orchestrator/Welcome", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<MonetizationWelcomeWindow>(true, "Welcome to Monetization Orchestrator", true);
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(400, 350);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUILayout.Label("Monetization Orchestrator", headerStyle);
            
            GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.gray }
            };
            GUILayout.Label("The AAA Ad Mediation & IAP Setup for Unity", subHeaderStyle);
            
            EditorGUILayout.Space(20);

            // Description
            GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            GUILayout.Label("Thank you for using Monetization Orchestrator! This package provides a robust, zero-allocation architecture for AdMob, AppLovin, LevelPlay, UnityAds, and Unity IAP.", textStyle);

            EditorGUILayout.Space(20);

            // Action Buttons
            if (GUILayout.Button("📖 Read Documentation", GUILayout.Height(35)))
            {
                string readmePath = "Assets/Plugins/MonetizationOrchestrator/README.md";
                var readmeObj = AssetDatabase.LoadAssetAtPath<Object>(readmePath);
                if (readmeObj != null)
                {
                    Selection.activeObject = readmeObj;
                    EditorGUIUtility.PingObject(readmeObj);
                }
                else
                {
                    Debug.LogWarning("Could not find README.md at " + readmePath);
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("⚙️ Apply SDK Integrations", GUILayout.Height(35)))
            {
                MonetizationSDKWindow.ShowWindow();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🎮 Open Demo Scene", GUILayout.Height(35)))
            {
                string demoScenePath = "Assets/Plugins/MonetizationOrchestrator/Demo/MonetizationDemoScene.unity";
                var sceneObj = AssetDatabase.LoadAssetAtPath<SceneAsset>(demoScenePath);
                if (sceneObj != null)
                {
                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(demoScenePath);
                    }
                }
                else
                {
                    Debug.LogWarning("Could not find Demo Scene at " + demoScenePath);
                }
            }

            EditorGUILayout.Space(20);

            GUILayout.FlexibleSpace();

            // Footer
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }
    }
}
#endif
