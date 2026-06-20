using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using ZarbatanaSystems.BalanceOrchestrator.Pro;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Examples;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;
using System.Linq;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools
{
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class Pro_DDA_WelcomeWindow : EditorWindow
    {
        private static bool dontShowAgain = false;

        static Pro_DDA_WelcomeWindow()
        {
            EditorApplication.delayCall += ShowOnStartup;
        }

        private static void ShowOnStartup()
        {
            dontShowAgain = EditorPrefs.GetBool("Pro_DDA_WelcomeWindow_Hide", false);
            if (!dontShowAgain)
            {
                ShowWindow();
            }
        }

        [MenuItem("Tools/Balance Orchestrator/Welcome Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<Pro_DDA_WelcomeWindow>(true, "Balance Orchestrator Onboarding");
            window.minSize = new Vector2(440, 560);
            window.maxSize = new Vector2(440, 560);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            dontShowAgain = EditorPrefs.GetBool("Pro_DDA_WelcomeWindow_Hide", false);
        }

        private bool IsStep1Completed()
        {
            string systemPrefabPath = "Assets/BalanceOrchestrator_Pro_Presets/Pro_DDA_System.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(systemPrefabPath) != null;
        }

        private bool IsStep2Completed()
        {
            return EditorPrefs.GetBool("Pro_DDA_Synced", false) || PlayerPrefs.HasKey("ZarbatanaSystems_Pro_DDA_Cache");
        }

        private bool IsStep3Completed()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            bool isDemoSceneActive = activeScene.name == "Pro_DDA_DemoScene";
            return EditorPrefs.GetBool("Pro_DDA_DemoSceneOpened", false) || isDemoSceneActive;
        }

        private bool IsStep4Completed()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<Pro_DDA_Manager>() != null;
#else
            return Object.FindObjectOfType<Pro_DDA_Manager>() != null;
#endif
        }

        private void SyncGoogleSheetsInEditor()
        {
            string sheetId = "1tqITiO_iAecnLoY7WWL3Am29VgoVpjk1GxlugVwzv6U";
            string gid = "0";
            string keyColumn = "LevelId"; // Default

            string systemPrefabPath = "Assets/BalanceOrchestrator_Pro_Presets/Pro_DDA_System.prefab";
            var systemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(systemPrefabPath);
            if (systemPrefab != null)
            {
                var ddaManager = systemPrefab.GetComponent<Pro_DDA_Manager>();
                if (ddaManager != null)
                {
                    sheetId = ddaManager.DataSource;
                    gid = ddaManager.Gid;
                    keyColumn = ddaManager.KeyColumnName;
                }
            }

            string url = (sheetId.StartsWith("http://") || sheetId.StartsWith("https://"))
                ? sheetId
                : $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";

            Debug.Log($"[Pro DDA Editor Sync] Downloading from: {url}");
            
            var www = UnityEngine.Networking.UnityWebRequest.Get(url);
            www.SendWebRequest();

            EditorApplication.CallbackFunction updateCallback = null;
            updateCallback = () =>
            {
                if (!www.isDone)
                {
                    EditorUtility.DisplayProgressBar("Syncing Google Sheets", "Downloading balance data...", www.downloadProgress);
                    return;
                }

                EditorUtility.ClearProgressBar();
                EditorApplication.update -= updateCallback;

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError || www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                {
                    EditorUtility.DisplayDialog("Sync Failed", $"Failed to download sheet:\n{www.error}\n\nPlease check your internet connection or URL.", "OK");
                    www.Dispose();
                }
                else
                {
                    string rawText = www.downloadHandler.text;
                    try
                    {
                        string trimmed = rawText.TrimStart();
                        bool isJson = url.Contains(".json") || url.Contains("firebaseio.com") || trimmed.StartsWith("{") || trimmed.StartsWith("[");
                        
                        if (isJson)
                        {
                            var parsedData = Pro_DDAJsonParser.Parse(rawText, keyColumn);
                            if (parsedData.Count > 0)
                            {
                                var headers = parsedData.Values.First().Keys.ToList();
                                string csv = string.Join(",", headers) + "\n";
                                foreach (var row in parsedData.Values)
                                {
                                    csv += string.Join(",", headers.Select(h => EscapeCSV(row[h]))) + "\n";
                                }
                                PlayerPrefs.SetString("ZarbatanaSystems_Pro_DDA_Cache", csv);
                            }
                        }
                        else
                        {
                            var parsedData = Pro_DDACSVParser.Parse(rawText);
                            if (parsedData == null || parsedData.Count == 0)
                            {
                                throw new Exception("Parsed data is empty or invalid CSV structure.");
                            }
                            PlayerPrefs.SetString("ZarbatanaSystems_Pro_DDA_Cache", rawText);
                        }

                        PlayerPrefs.Save();
                        EditorPrefs.SetBool("Pro_DDA_Synced", true);

#if UNITY_2023_1_OR_NEWER
                        var activeManager = Object.FindAnyObjectByType<Pro_DDA_Manager>();
#else
                        var activeManager = Object.FindObjectOfType<Pro_DDA_Manager>();
#endif
                        if (activeManager != null)
                        {
                            activeManager.ParseData(rawText);
                        }

                        EditorUtility.DisplayDialog("Sync Successful", "Successfully synchronized and cached balance data from Google Sheets in Editor!", "Awesome");
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog("Sync Parse Error", $"Failed to parse the downloaded data:\n{ex.Message}", "OK");
                    }
                    finally
                    {
                        www.Dispose();
                    }
                }
            };

            EditorApplication.update += updateCallback;
        }

        private string EscapeCSV(string val)
        {
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
        }

        private void OnGUI()
        {
            // Check for LITE version presence
            bool liteDetected = AssetDatabase.IsValidFolder("Assets/ZarbatanaSystems/BalanceOrchestrator");
            if (liteDetected)
            {
                Color originalBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.85f, 0.2f); // Premium gold-yellow
                EditorGUILayout.BeginVertical("helpBox");
                
                GUIStyle warningTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                warningTextStyle.fontStyle = FontStyle.Bold;
                warningTextStyle.alignment = TextAnchor.MiddleCenter;
                
                EditorGUILayout.LabelField("⚠️ LITE version detected. Remove it to avoid duplicate menus?", warningTextStyle);
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Remove LITE Package", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Remove LITE?", "This will delete Assets/ZarbatanaSystems/BalanceOrchestrator/. Continue?", "Yes", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset("Assets/ZarbatanaSystems/BalanceOrchestrator");
                        AssetDatabase.Refresh();
                        Debug.Log("✅ Balance Orchestrator LITE removed successfully.");
                    }
                }
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = originalBg;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.Space(15);
            GUILayout.Label("Welcome to Zarbatana Balance Orchestrator PRO!", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            GUILayout.Label("Your advanced dynamic difficulty adjustment and balance manager.", EditorStyles.miniLabel);
            EditorGUILayout.Space(15);

            bool step1Done = IsStep1Completed();
            bool step2Done = IsStep2Completed();
            bool step3Done = IsStep3Completed();
            bool step4Done = IsStep4Completed();

            float progress = 0f;
            if (step1Done) progress += 0.25f;
            if (step2Done) progress += 0.25f;
            if (step3Done) progress += 0.25f;
            if (step4Done) progress += 0.25f;

            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, progress, $"Onboarding Progress: {progress * 100:0}%");
            EditorGUILayout.Space(15);

            GUILayout.Label("Onboarding Steps (Please complete sequentially)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // STEP 1
            string step1Label = step1Done ? "✔️ Step 1: Create Default Presets & Prefabs (Completed)" : "▶️ Step 1: Create Default Presets & Prefabs";
            if (GUILayout.Button(step1Label, GUILayout.Height(30)))
            {
                CreateDefaultPresetsAndPrefabs(showSuccessDialog: true);
            }
            EditorGUILayout.Space(5);

            // STEP 2
            GUI.enabled = step1Done;
            string step2Label = step2Done 
                ? "✔️ Step 2: Sync with Google Sheets (Completed)" 
                : (step1Done ? "▶️ Step 2: Sync with Google Sheets" : "🔒 Step 2: Sync with Google Sheets (Requires Step 1)");
            if (GUILayout.Button(step2Label, GUILayout.Height(30)))
            {
                SyncGoogleSheetsInEditor();
            }
            EditorGUILayout.Space(5);

            // STEP 3
            GUI.enabled = step1Done && step2Done;
            string step3Label = step3Done 
                ? "✔️ Step 3: Open & Play Demo Scene (Completed)" 
                : (step1Done && step2Done ? "▶️ Step 3: Open & Play Demo Scene" : "🔒 Step 3: Open & Play Demo Scene (Requires Step 2)");
            if (GUILayout.Button(step3Label, GUILayout.Height(30)))
            {
                OpenDemoScene();
            }
            EditorGUILayout.Space(5);

            // STEP 4
            GUI.enabled = step1Done && step2Done && step3Done;
            string step4Label = step4Done 
                ? "✔️ Step 4: Setup Active Scene (Completed)" 
                : (step1Done && step2Done && step3Done ? "▶️ Step 4: Setup Active Scene (Add Prefab to Scene)" : "🔒 Step 4: Setup Active Scene (Requires Step 3)");
            if (GUILayout.Button(step4Label, GUILayout.Height(30)))
            {
                SetupScene();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(15);

            GUILayout.Label("Dashboard & Documentation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("🛠️ Open Balance Orchestrator Dashboard", GUILayout.Height(30)))
            {
                Pro_DDA_DashboardWindow.ShowWindow();
            }
            EditorGUILayout.Space(3);

            if (GUILayout.Button("📚 View QuickStart Guide", GUILayout.Height(30)))
            {
                OpenQuickStartGuide();
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 Reset Progress", EditorStyles.miniButton, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Reset Onboarding", "Are you sure you want to reset your onboarding progress? This will reset the cached Google Sheets and Demo Scene state markers. It will NOT delete your created prefabs/scenes.", "Yes", "No"))
                {
                    EditorPrefs.DeleteKey("Pro_DDA_Synced");
                    EditorPrefs.DeleteKey("Pro_DDA_DemoSceneOpened");
                    PlayerPrefs.DeleteKey("ZarbatanaSystems_Pro_DDA_Cache");
                    PlayerPrefs.Save();
                    Debug.Log("[Pro DDA] Onboarding progress has been reset.");
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            dontShowAgain = EditorGUILayout.ToggleLeft("Don't show this again", dontShowAgain, GUILayout.Width(150));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("Pro_DDA_WelcomeWindow_Hide", dontShowAgain);
            }
            
            GUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void OpenDemoScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                GenerateDemoScene(openScene: true);
                EditorPrefs.SetBool("Pro_DDA_DemoSceneOpened", true);
            }
        }

        private void OpenQuickStartGuide()
        {
            var guids = AssetDatabase.FindAssets("DDA_Pro_QuickStart");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
            else
            {
                Debug.LogWarning("[Pro DDA] QuickStart guide not found.");
            }
        }

        private void SetupScene()
        {
#if UNITY_2023_1_OR_NEWER
            var manager = Object.FindAnyObjectByType<Pro_DDA_Manager>();
            var diffManager = Object.FindAnyObjectByType<Pro_DifficultyManager>();
#else
            var manager = Object.FindObjectOfType<Pro_DDA_Manager>();
            var diffManager = Object.FindObjectOfType<Pro_DifficultyManager>();
#endif

            if (manager != null && diffManager != null)
            {
                EditorUtility.DisplayDialog("Pro DDA Scene Setup", "Pro_DDA_Manager and Pro_DifficultyManager are already present in the active scene.", "OK");
                return;
            }

            // Find the prefabricated system asset to maintain prefab connection!
            GameObject systemPrefab = null;
            var prefabGuids = AssetDatabase.FindAssets("Pro_DDA_System t:Prefab");
            if (prefabGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                systemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // If the prefab doesn't exist yet, automatically trigger Step 1 (silent generation)
            bool autoGenerated = false;
            if (systemPrefab == null)
            {
                autoGenerated = true;
                CreateDefaultPresetsAndPrefabs(showSuccessDialog: false);

                // Search again after generating
                prefabGuids = AssetDatabase.FindAssets("Pro_DDA_System t:Prefab");
                if (prefabGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                    systemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            GameObject instantiatedGo = null;

            if (systemPrefab != null)
            {
                // Instantiate the prefab in the scene using PrefabUtility to maintain link!
                instantiatedGo = (GameObject)PrefabUtility.InstantiatePrefab(systemPrefab);
                Undo.RegisterCreatedObjectUndo(instantiatedGo, "Instantiate Pro DDA System Prefab");
                Debug.Log($"[Pro DDA] Instantiated Pro_DDA_System from prefab: {AssetDatabase.GetAssetPath(systemPrefab)}");
            }
            else
            {
                // Fallback: create raw gameobject if prefab isn't found/generated yet
                instantiatedGo = new GameObject("Pro_DDA_System");
                instantiatedGo.AddComponent<Pro_DDA_Manager>();
                instantiatedGo.AddComponent<Pro_DifficultyManager>();
                Undo.RegisterCreatedObjectUndo(instantiatedGo, "Create Pro DDA System");
                Debug.LogWarning("[Pro DDA] Pro_DDA_System prefab not found. Created raw GameObject in scene.");
            }

            // Assign default difficulty profile if it exists and isn't set
            var diffComp = instantiatedGo.GetComponent<Pro_DifficultyManager>();
            if (diffComp != null)
            {
                var serializedObject = new SerializedObject(diffComp);
                var prop = serializedObject.FindProperty("profile");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    var profileGuids = AssetDatabase.FindAssets("Default_DifficultyProfile");
                    if (profileGuids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
                        var profile = AssetDatabase.LoadAssetAtPath<Pro_DifficultyProfile>(path);
                        if (profile != null)
                        {
                            prop.objectReferenceValue = profile;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            Selection.activeGameObject = instantiatedGo;

            if (autoGenerated)
            {
                EditorUtility.DisplayDialog("Pro DDA Scene Setup", "Successfully generated default presets & prefabs in the background and set up the Pro DDA System in the active scene using the prefab link!", "Awesome");
            }
            else
            {
                EditorUtility.DisplayDialog("Pro DDA Scene Setup", "Successfully set up the Pro DDA System in the active scene using the prefab link!", "Awesome");
            }
        }

        private void CreateDefaultPresetsAndPrefabs(bool showSuccessDialog = true)
        {
            string folderPath = "Assets/BalanceOrchestrator_Pro_Presets";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "BalanceOrchestrator_Pro_Presets");
            }

            // 1. Create Economy Profile
            string economyPath = $"{folderPath}/Default_EconomyProfile.asset";
            var economyProfile = AssetDatabase.LoadAssetAtPath<Pro_EconomyProfile>(economyPath);
            if (economyProfile == null)
            {
                economyProfile = ScriptableObject.CreateInstance<Pro_EconomyProfile>();
                economyProfile.description = "Default Economy Profile with baseline rewards and costs.";
                economyProfile.baseEnergyPerCorrectAnimal = 10;
                economyProfile.baseEnergyDeductedForWrongAnimalHit = 2;
                economyProfile.baseCoinsForLevelCompletion = 50;
                economyProfile.baseBonusCoinsPerStar = 25;
                AssetDatabase.CreateAsset(economyProfile, economyPath);
            }

            // 2. Create Difficulty Profile
            string diffPath = $"{folderPath}/Default_DifficultyProfile.asset";
            var diffProfile = AssetDatabase.LoadAssetAtPath<Pro_DifficultyProfile>(diffPath);
            if (diffProfile == null)
            {
                diffProfile = ScriptableObject.CreateInstance<Pro_DifficultyProfile>();
                diffProfile.description = "Default Difficulty Profile with standard difficulty scaling curves.";
                diffProfile.defaultEconomyProfile = economyProfile;
                diffProfile.enemyHealthMul = AnimationCurve.Linear(0, 1f, 100, 1.5f);
                diffProfile.enemySpeedMul = AnimationCurve.Linear(0, 1f, 100, 1.3f);
                diffProfile.enemySpawnChanceMul = AnimationCurve.Linear(0, 1f, 100, 1.4f);
                diffProfile.waveDelayMul = AnimationCurve.Linear(0, 1.2f, 100, 0.6f);
                diffProfile.rewardMul = AnimationCurve.Linear(0, 1f, 100, 1.25f);
                diffProfile.abilityIntensityMul = AnimationCurve.Linear(0, 1f, 100, 1.2f);
                diffProfile.defaultDifficultyPercentage = 30f;
                AssetDatabase.CreateAsset(diffProfile, diffPath);
            }

            // 3. Create Enemy Set
            string enemySetPath = $"{folderPath}/Default_EnemySet.asset";
            var enemySet = AssetDatabase.LoadAssetAtPath<Pro_EnemySet>(enemySetPath);
            if (enemySet == null)
            {
                enemySet = ScriptableObject.CreateInstance<Pro_EnemySet>();
                enemySet.displayName = "Default Enemy Set";
                enemySet.description = "Baseline enemy set configuration.";
                AssetDatabase.CreateAsset(enemySet, enemySetPath);
            }

            // 4. Create DDA System Prefab (combining Pro_DDA_Manager and Pro_DifficultyManager)
            string systemPrefabPath = $"{folderPath}/Pro_DDA_System.prefab";
            var systemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(systemPrefabPath);
            if (systemPrefab == null)
            {
                var tempGo = new GameObject("Pro_DDA_System");
                tempGo.AddComponent<Pro_DDA_Manager>();
                var diffManager = tempGo.AddComponent<Pro_DifficultyManager>();

                // Set the difficulty manager to refer to our newly created default profile
                var serializedObject = new SerializedObject(diffManager);
                var profileProperty = serializedObject.FindProperty("profile");
                if (profileProperty != null)
                {
                    profileProperty.objectReferenceValue = diffProfile;
                    serializedObject.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(tempGo, systemPrefabPath);
                Object.DestroyImmediate(tempGo);
            }

            // 5. Create Example Enemy Prefab (2D Sprite Circle)
            string enemyPrefabPath = $"{folderPath}/Pro_EnemyDDAExample.prefab";
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            if (enemyPrefab == null)
            {
                var tempGo = new GameObject("Pro_EnemyDDAExample");
                var spriteRenderer = tempGo.AddComponent<SpriteRenderer>();

                // Generate and save a physical circle sprite texture
                int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float center = size / 2f;
                float radius = size / 2f - 1f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        if (dist <= radius - 1f) texture.SetPixel(x, y, Color.white);
                        else if (dist <= radius) texture.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - (dist - (radius - 1f))));
                        else texture.SetPixel(x, y, Color.clear);
                    }
                }
                texture.Apply();

                byte[] bytes = texture.EncodeToPNG();
                string texturePath = $"{folderPath}/Pro_CircleSprite.png";
                System.IO.File.WriteAllBytes(texturePath, bytes);
                AssetDatabase.ImportAsset(texturePath);

                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = size;
                    AssetDatabase.ImportAsset(texturePath);
                }

                var savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                spriteRenderer.sprite = savedSprite;
                spriteRenderer.color = Color.white;

                var exampleComponent = tempGo.AddComponent<Pro_EnemyDDAExample>();
                exampleComponent.LevelID = "Level_1"; // Match the first LevelId in our template sheet

                PrefabUtility.SaveAsPrefabAsset(tempGo, enemyPrefabPath);
                Object.DestroyImmediate(tempGo);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showSuccessDialog)
            {
                EditorUtility.DisplayDialog("DDA Pro Setup", "Successfully created default presets and prefabs at:\nAssets/BalanceOrchestrator_Pro_Presets/", "Awesome");
            }

            // Highlight the folder/assets in the project window
            var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folderAsset != null)
            {
                Selection.activeObject = folderAsset;
                EditorUtility.FocusProjectWindow();
            }
        }

        [MenuItem("Tools/Balance Orchestrator/Internal/Generate Demo Scene")]
        public static void GenerateDemoSceneMenu()
        {
            GenerateDemoScene(openScene: false);
        }

        public static void GenerateDemoScene(bool openScene)
        {
            string sceneFolder = "Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Demo";
            if (!AssetDatabase.IsValidFolder("Assets/ZarbatanaSystems/BalanceOrchestrator_Pro"))
            {
                AssetDatabase.CreateFolder("Assets/ZarbatanaSystems", "BalanceOrchestrator_Pro");
            }
            if (!AssetDatabase.IsValidFolder(sceneFolder))
            {
                AssetDatabase.CreateFolder("Assets/ZarbatanaSystems/BalanceOrchestrator_Pro", "Demo");
            }

            // Delete old scene if it was generated in the Scenes folder previously
            string oldScenePath = "Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Scenes/Pro_DDA_DemoScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(oldScenePath) != null)
            {
                AssetDatabase.DeleteAsset(oldScenePath);
            }

            var currentScenePath = EditorSceneManager.GetActiveScene().path;

            // Create new empty scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Add Main Camera (Orthographic 2D Camera Setup)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Dark-grey background #333333
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.transform.rotation = Quaternion.identity;

#if USING_UNITY_UI
            // 4. Add EventSystem
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            var inputSystemUIModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUIModuleType != null)
            {
                eventSystemGo.AddComponent(inputSystemUIModuleType);
            }
            else
            {
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
#else
            Debug.LogWarning("[Pro DDA] Cannot add EventSystem because Unity UI package (com.unity.ugui) is missing!");
#endif

            // 5. Add Pro_DDA_System prefab if it exists
            string systemPrefabPath = "Assets/BalanceOrchestrator_Pro_Presets/Pro_DDA_System.prefab";
            var systemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(systemPrefabPath);
            if (systemPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(systemPrefab);
            }

            // 6. Add Demo Canvas prefab (generate it if missing)
            string canvasPrefabPath = "Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Demo/DemoCanvas.prefab";
            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
            if (canvasPrefab == null)
            {
#if USING_UNITY_UI
                Demo.CanvasPrefabGenerator.GenerateCanvasPrefab();
#else
                Debug.LogWarning("[Pro DDA] Cannot generate Demo Canvas because Unity UI package (com.unity.ugui) is missing!");
#endif
                canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
            }
            if (canvasPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(canvasPrefab);
            }

            // Save the scene
            string scenePath = $"{sceneFolder}/Pro_DDA_DemoScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[Pro DDA] Successfully generated Demo Scene at: {scenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Load original scene back
            if (!openScene && !string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath);
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticData()
        {
            dontShowAgain = false;
        }
#endif
    }
}
