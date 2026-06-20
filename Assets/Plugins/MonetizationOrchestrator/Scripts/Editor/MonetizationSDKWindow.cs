#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniversalMonetization.Editor
{
    public class MonetizationSDKWindow : EditorWindow
    {
        [Serializable]
        private class VersionDefineData
        {
            public string name;
            public string expression;
            public string define;
        }

        [Serializable]
        private class AsmdefData
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public VersionDefineData[] versionDefines;
            public bool noEngineReferences;
        }

        private bool _hasIAPPackage;
        private bool _hasUniTaskPackage;

        private bool _enableIAP;
        private bool _enableUniTask;

        private string _asmdefPath;
        private AsmdefData _currentAsmdef;

        [MenuItem("Tools/Monetization Orchestrator/SDK Integrations")]
        public static void ShowWindow()
        {
            GetWindow<MonetizationSDKWindow>("SDK Setup").Show();
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            // Detect if packages are installed in the project
            _hasIAPPackage = TypeExists("UnityEngine.Purchasing.IStoreController");
            _hasUniTaskPackage = TypeExists("Cysharp.Threading.Tasks.UniTask");

            // Locate the main asmdef
            string[] guids = AssetDatabase.FindAssets("UniversalMonetization t:AssemblyDefinitionAsset");
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(p) == "UniversalMonetization.asmdef")
                {
                    _asmdefPath = p;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(_asmdefPath))
            {
                string content = File.ReadAllText(_asmdefPath);
                _currentAsmdef = JsonUtility.FromJson<AsmdefData>(content);

                var refs = _currentAsmdef.references != null ? new List<string>(_currentAsmdef.references) : new List<string>();
                _enableIAP = refs.Contains("UnityEngine.Purchasing");
                _enableUniTask = refs.Contains("UniTask");
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Monetization Orchestrator SDKs", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(_asmdefPath))
            {
                EditorGUILayout.HelpBox("Could not find UniversalMonetization.asmdef in the project.", MessageType.Error);
                if (GUILayout.Button("Refresh")) RefreshStatus();
                return;
            }

            EditorGUILayout.HelpBox(
                "Toggle these integrations ONLY if you have the required packages installed. " +
                "Applying changes will modify the UniversalMonetization assembly definition and trigger a recompile.", 
                MessageType.Info);

            EditorGUILayout.Space();

            // --- IAP ---
            EditorGUI.BeginDisabledGroup(!_hasIAPPackage);
            _enableIAP = EditorGUILayout.ToggleLeft(" Enable Unity IAP Integration", _enableIAP);
            if (!_hasIAPPackage)
            {
                EditorGUILayout.HelpBox("Unity IAP (com.unity.purchasing) is not installed in this project.", MessageType.Warning);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // --- UniTask ---
            EditorGUI.BeginDisabledGroup(!_hasUniTaskPackage);
            _enableUniTask = EditorGUILayout.ToggleLeft(" Enable UniTask Integration", _enableUniTask);
            if (!_hasUniTaskPackage)
            {
                EditorGUILayout.HelpBox("UniTask (com.cysharp.unitask) is not installed in this project.", MessageType.Warning);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Integrations", GUILayout.Height(30)))
            {
                ApplyChanges();
            }
        }

        private void ApplyChanges()
        {
            if (_currentAsmdef == null) return;

            var refs = new List<string>();
            var defines = new List<VersionDefineData>();

            if (_enableIAP)
            {
                refs.Add("UnityEngine.Purchasing");
                refs.Add("UnityEngine.Purchasing.Stores");
                defines.Add(new VersionDefineData { name = "com.unity.purchasing", expression = "0.0.1", define = "MONETIZATION_IAP" });
            }

            if (_enableUniTask)
            {
                refs.Add("UniTask");
                defines.Add(new VersionDefineData { name = "com.cysharp.unitask", expression = "0.0.1", define = "MONETIZATION_UNITASK" });
            }

            _currentAsmdef.references = refs.ToArray();
            _currentAsmdef.versionDefines = defines.ToArray();

            string newContent = JsonUtility.ToJson(_currentAsmdef, true);
            File.WriteAllText(_asmdefPath, newContent);
            
            Debug.Log("[MonetizationOrchestrator] Applied SDK integrations to UniversalMonetization.asmdef. Recompiling...");
            
            AssetDatabase.ImportAsset(_asmdefPath);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            RefreshStatus();
            ShowNotification(new GUIContent("Integrations Applied!"));
        }

        private static bool TypeExists(string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(className, false))
                .Any(t => t != null);
        }
    }
}
#endif
