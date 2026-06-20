#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace TimeAura.Editor
{
    /// <summary>
    /// Addressables Setup Wizard - автоматично створює містичні групи.
    /// Window → Time Aura → Setup Addressables
    /// </summary>
    public class AddressablesSetupWizard : EditorWindow
    {
        private static readonly string[] GroupNames = { "Relics", "Visages", "Chronicles", "Aura_Shards", "Localization" };
        private static readonly string[] Labels =
        {
            "relic-icon", "relic-button",
            "visage-default", "visage-golden",
            "chronicle-template", "chronicle-card",
            "aura-golden", "aura-mystical", "aura-transformed",
            "tongue-en", "tongue-uk"
        };

        [MenuItem("Window/Time Aura/Setup Addressables")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddressablesSetupWizard>("Addressables Setup");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("🌙 Time Aura - Addressables Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Цей майстер створить містичні групи Addressables:\n" +
                "• Relics (UI артефакти)\n" +
                "• Visages (аватари адептів)\n" +
                "• Chronicles (пости та події)\n" +
                "• Aura_Shards (ефекти та UGC)\n" +
                "• Localization (мови)",
                MessageType.Info
            );

            GUILayout.Space(20);

            if (GUILayout.Button("✨ Create Mystical Groups", GUILayout.Height(40)))
            {
                CreateAddressablesGroups();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🏷️ Create Labels", GUILayout.Height(30)))
            {
                CreateLabels();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("📦 Setup Remote Catalog", GUILayout.Height(30)))
            {
                SetupRemoteCatalog();
            }
        }

        private void CreateAddressablesGroups()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables not initialized. Go to Window → Asset Management → Addressables → Groups");
                return;
            }

            foreach (var groupName in GroupNames)
            {
                // Check if group already exists
                var existingGroup = settings.FindGroup(groupName);
                if (existingGroup != null)
                {
                    Debug.Log($"[AddressablesSetup] Group '{groupName}' already exists. Skipping.");
                    continue;
                }

                // Create group
                var group = settings.CreateGroup(groupName, false, false, true, null);
                Debug.Log($"[AddressablesSetup] ✅ Created group: {groupName}");

                // Configure group settings
                var schema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                if (schema != null)
                {
                    schema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                    schema.UseAssetBundleCache = true;
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[AddressablesSetup] 🌟 All mystical groups created!");
            EditorUtility.DisplayDialog("Success", "Addressables groups created successfully!", "OK");
        }

        private void CreateLabels()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables not initialized.");
                return;
            }

            foreach (var label in Labels)
            {
                if (!settings.GetLabels().Contains(label))
                {
                    settings.AddLabel(label);
                    Debug.Log($"[AddressablesSetup] ✅ Created label: {label}");
                }
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[AddressablesSetup] 🏷️ All labels created!");
            EditorUtility.DisplayDialog("Success", "Labels created successfully!", "OK");
        }

        private void SetupRemoteCatalog()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressables not initialized.");
                return;
            }

            // Enable remote catalog
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, "RemoteBuildPath");
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, "RemoteLoadPath");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[AddressablesSetup] 📦 Remote catalog enabled!");
            EditorUtility.DisplayDialog(
                "Remote Catalog Setup",
                "Remote catalog enabled!\n\n" +
                "Next steps:\n" +
                "1. Configure remote URL in Addressables settings\n" +
                "2. Build → New Build → Default Build Script",
                "OK"
            );
        }
    }
}
#endif
