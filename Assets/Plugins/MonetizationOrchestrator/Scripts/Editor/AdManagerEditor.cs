#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UniversalMonetization.Editor
{
    [CustomEditor(typeof(AdManager))]
    public class AdManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty androidAppKeyProp;
        private SerializedProperty iosAppKeyProp;
        private SerializedProperty androidRewardedIdProp;
        private SerializedProperty androidInterstitialIdProp;
        private SerializedProperty androidBannerIdProp;
        
        private SerializedProperty iosRewardedIdProp;
        private SerializedProperty iosInterstitialIdProp;
        private SerializedProperty iosBannerIdProp;
        private SerializedProperty enableBannersProp;
        
        private SerializedProperty minSecondsBetweenInterstitialsProp;
        private SerializedProperty rewardedToInterstitialCooldownProp;
        private SerializedProperty sceneWarmupDelayProp;
        private SerializedProperty minSecondsBetweenLoadRequestsProp;
        
        private SerializedProperty gameOverAdFrequencyProp;
        private SerializedProperty levelCompleteAdFrequencyProp;
        
        private SerializedProperty bannerAtTopProp;
        
        private SerializedProperty useMockProviderInEditorProp;
        private SerializedProperty enableTestSuiteProp;
        private SerializedProperty verboseAdLogsProp;

        private void OnEnable()
        {
            androidAppKeyProp = serializedObject.FindProperty("androidAppKey");
            iosAppKeyProp = serializedObject.FindProperty("iosAppKey");
            
            androidRewardedIdProp = serializedObject.FindProperty("androidRewardedId");
            androidInterstitialIdProp = serializedObject.FindProperty("androidInterstitialId");
            androidBannerIdProp = serializedObject.FindProperty("androidBannerId");

            iosRewardedIdProp = serializedObject.FindProperty("iosRewardedId");
            iosInterstitialIdProp = serializedObject.FindProperty("iosInterstitialId");
            iosBannerIdProp = serializedObject.FindProperty("iosBannerId");
            enableBannersProp = serializedObject.FindProperty("enableBanners");
            
            minSecondsBetweenInterstitialsProp = serializedObject.FindProperty("minSecondsBetweenInterstitials");
            rewardedToInterstitialCooldownProp = serializedObject.FindProperty("rewardedToInterstitialCooldown");
            sceneWarmupDelayProp = serializedObject.FindProperty("sceneWarmupDelay");
            minSecondsBetweenLoadRequestsProp = serializedObject.FindProperty("minSecondsBetweenLoadRequests");
            
            gameOverAdFrequencyProp = serializedObject.FindProperty("gameOverAdFrequency");
            levelCompleteAdFrequencyProp = serializedObject.FindProperty("levelCompleteAdFrequency");
            
            bannerAtTopProp = serializedObject.FindProperty("bannerAtTop");
            
            useMockProviderInEditorProp = serializedObject.FindProperty("useMockProviderInEditor");
            enableTestSuiteProp = serializedObject.FindProperty("enableTestSuite");
            verboseAdLogsProp = serializedObject.FindProperty("verboseAdLogs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCustomHeader();

            EditorGUILayout.Space(5);
            DrawPlatformCredentials();
            
            EditorGUILayout.Space(5);
            DrawPlacements();

            EditorGUILayout.Space(5);
            DrawPacingSettings();

            EditorGUILayout.Space(5);
            DrawAesthetics();

            EditorGUILayout.Space(5);
            DrawDebugSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Monetization Orchestrator", headerStyle);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("⚙️ Open SDK Integrations Setup", GUILayout.Height(30)))
            {
                MonetizationSDKWindow.ShowWindow();
            }
        }

        private void DrawPlatformCredentials()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📱 Platform Credentials", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(androidAppKeyProp, new GUIContent("Android App Key"));
            EditorGUILayout.PropertyField(iosAppKeyProp, new GUIContent("iOS App Key"));
            EditorGUILayout.EndVertical();
        }

        private void DrawPlacements()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("🎯 Android Ad Unit IDs", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(androidRewardedIdProp, new GUIContent("Rewarded"));
            EditorGUILayout.PropertyField(androidInterstitialIdProp, new GUIContent("Interstitial"));
            if (enableBannersProp.boolValue)
            {
                EditorGUILayout.PropertyField(androidBannerIdProp, new GUIContent("Banner"));
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("🎯 iOS Ad Unit IDs", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(iosRewardedIdProp, new GUIContent("Rewarded"));
            EditorGUILayout.PropertyField(iosInterstitialIdProp, new GUIContent("Interstitial"));
            if (enableBannersProp.boolValue)
            {
                EditorGUILayout.PropertyField(iosBannerIdProp, new GUIContent("Banner"));
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(enableBannersProp, new GUIContent("Enable Banners (Global)"));
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPacingSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⏳ Pacing & Timers", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Time-Based Cooldowns", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(minSecondsBetweenInterstitialsProp, new GUIContent("Min Sec Between Interstitials"));
            EditorGUILayout.PropertyField(rewardedToInterstitialCooldownProp, new GUIContent("Rewarded -> Interstitial Cooldown"));
            EditorGUILayout.PropertyField(sceneWarmupDelayProp, new GUIContent("Scene Warmup Delay"));
            EditorGUILayout.PropertyField(minSecondsBetweenLoadRequestsProp, new GUIContent("Load Request Delay (Throttling)"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Event-Based Frequencies", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(gameOverAdFrequencyProp, new GUIContent("Game Over Ad Frequency", "Shows interstitial every X Game Overs"));
            EditorGUILayout.PropertyField(levelCompleteAdFrequencyProp, new GUIContent("Level Complete Ad Frequency", "Shows interstitial every X Levels"));
            EditorGUILayout.EndVertical();
        }

        private void DrawAesthetics()
        {
            if (!enableBannersProp.boolValue) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎨 Aesthetics", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(bannerAtTopProp, new GUIContent("Show Banner at Top"));
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛠️ Debug & Testing", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(useMockProviderInEditorProp, new GUIContent("Use Mock Provider in Editor"));
            if (useMockProviderInEditorProp.boolValue)
            {
                EditorGUILayout.HelpBox("Mock Provider is ON. Ads will be simulated using local UI Canvas instead of real networks when playing in the Editor.", MessageType.Info);
            }
            
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(enableTestSuiteProp, new GUIContent("Enable Test Suite (Mediation Debugger)"));
            if (enableTestSuiteProp.boolValue)
            {
                EditorGUILayout.HelpBox("Test Suite is ENABLED. Ensure you turn this OFF before publishing to production to avoid exposing the debugger to users.", MessageType.Warning);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(verboseAdLogsProp, new GUIContent("Verbose Ad Logs"));
            
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
