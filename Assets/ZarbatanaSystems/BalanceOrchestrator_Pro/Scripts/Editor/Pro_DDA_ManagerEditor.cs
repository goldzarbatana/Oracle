using UnityEditor;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools
{
    [CustomEditor(typeof(Pro_DDA_Manager))]
    public class Pro_DDA_ManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            
            // Custom Header Banner Style
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = new Color(0.1f, 0.6f, 0.9f); // Sleek modern blue
            
            EditorGUILayout.LabelField("Balance Orchestrator (PRO) Settings", headerStyle);
            EditorGUILayout.LabelField("Configure your Cloud Data Source or Local fallbacks.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            // Data Source section
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField("Data Source Config", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty dataSourceProp = serializedObject.FindProperty("DataSource");
            SerializedProperty gidProp = serializedObject.FindProperty("Gid");
            SerializedProperty keyColumnProp = serializedObject.FindProperty("KeyColumnName");
            SerializedProperty defaultCSVProp = serializedObject.FindProperty("DefaultBalanceCSV");
            SerializedProperty autoSyncProp = serializedObject.FindProperty("AutoSyncOnPlay");

            if (dataSourceProp != null)
            {
                EditorGUILayout.PropertyField(dataSourceProp, new GUIContent("Data Source (Sheet ID or URL)", dataSourceProp.tooltip));
            }
            if (gidProp != null)
            {
                EditorGUILayout.PropertyField(gidProp, new GUIContent("Sheet GID (tab)", gidProp.tooltip));
            }
            if (keyColumnProp != null)
            {
                EditorGUILayout.PropertyField(keyColumnProp, new GUIContent("Key Column Name", keyColumnProp.tooltip));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Offline Fallback section
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField("Offline Fallback", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            if (defaultCSVProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(defaultCSVProp, new GUIContent("Default Balance CSV", defaultCSVProp.tooltip));
                if (EditorGUI.EndChangeCheck() && defaultCSVProp.objectReferenceValue != null)
                {
                    string path = AssetDatabase.GetAssetPath(defaultCSVProp.objectReferenceValue);
                    if (!string.IsNullOrEmpty(path) && !path.ToLower().EndsWith(".csv") && !path.ToLower().EndsWith(".json"))
                    {
                        Debug.LogWarning("[Pro DDA] Invalid file type! Please assign only .csv or .json files.");
                        defaultCSVProp.objectReferenceValue = null;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Editor Sync section
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField("Editor Sync Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            if (autoSyncProp != null)
            {
                EditorGUILayout.PropertyField(autoSyncProp, new GUIContent("Auto Sync On Play", autoSyncProp.tooltip));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            
            // Sync Button right in the Inspector!
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f); // Green tint
            if (GUILayout.Button("🔄 Sync Data from Web / Cloud", GUILayout.Height(35)))
            {
                Pro_DDA_DashboardWindow.ShowWindow();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
