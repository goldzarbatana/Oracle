#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UniversalMonetization.Editor
{
    [CustomEditor(typeof(IAPManager))]
    public class IAPManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty shopOffersProp;
        private SerializedProperty enableMockModeInEditorProp;

        private void OnEnable()
        {
            shopOffersProp = serializedObject.FindProperty("registeredOffers");
            enableMockModeInEditorProp = serializedObject.FindProperty("enableMockModeInEditor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCustomHeader();

            EditorGUILayout.Space(5);
            DrawIAPStatus();

            EditorGUILayout.Space(5);
            DrawOffers();

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
            EditorGUILayout.LabelField("In-App Purchasing Manager", headerStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawIAPStatus()
        {
#if MONETIZATION_IAP
            EditorGUILayout.HelpBox("Unity IAP is integrated and active. Make sure your Product IDs match your Google Play / App Store Connect consoles.", MessageType.Info);
#else
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Unity IAP is NOT integrated. Purchases will only run in Mock Mode.", MessageType.Warning);
            if (GUILayout.Button("⚙️ Open SDK Integrations Setup", GUILayout.Height(30)))
            {
                MonetizationSDKWindow.ShowWindow();
            }
            EditorGUILayout.EndVertical();
#endif
        }

        private void DrawOffers()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛍️ Shop Offers", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(shopOffersProp, new GUIContent("Configured Offers"), true);
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🛠️ Debug & Testing", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(enableMockModeInEditorProp, new GUIContent("Enable Mock Mode in Editor"));
            if (enableMockModeInEditorProp.boolValue)
            {
                EditorGUILayout.HelpBox("Mock Mode is ON. Purchases in the Editor will be simulated locally and will not connect to Apple/Google servers.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Mock Mode is OFF. Editor purchases will attempt to connect to the real store (if supported) or fail if testing on PC.", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
