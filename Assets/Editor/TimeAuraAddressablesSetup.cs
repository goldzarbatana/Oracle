#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace TimeAura.Editor
{
    public static class TimeAuraAddressablesSetup
    {
        private const string SetupKey = "TimeAura.AddressablesSetupDone";
        private const string BuildKey = "TimeAura.AddressablesBuildDone";

        private const string ScenesGroupName = "TimeAura_Scenes";
        private const string LocalizationGroupName = "TimeAura_Localization";

        private const string ConvergenceScenePath = "Assets/Scenes/ConvergenceScene.unity";
        private const string AuraTermsCsvPath = "Assets/Resources/Localization/AuraTerms.csv";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            // Setup groups and entries if not done
            if (!EditorPrefs.GetBool(SetupKey, false))
            {
                try
                {
                    SetupAddressablesInternal(build: false);
                    EditorPrefs.SetBool(SetupKey, true);
                    Debug.Log("[TimeAuraAddressablesSetup] ✅ Groups and entries configured.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TimeAuraAddressablesSetup] Auto setup failed: {ex.Message}");
                    return;
                }
            }

            // Build content if not done (deferred to avoid blocking Editor startup)
            if (!EditorPrefs.GetBool(BuildKey, false))
            {
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        Debug.Log("[TimeAuraAddressablesSetup] Building Addressables content...");
                        AddressableAssetSettings.BuildPlayerContent();
                        EditorPrefs.SetBool(BuildKey, true);
                        Debug.Log("[TimeAuraAddressablesSetup] ✅ Addressables content built successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[TimeAuraAddressablesSetup] Build failed: {ex.Message}");
                    }
                };
            }
        }

        [MenuItem("TimeAura/Addressables/Setup (Groups + Entries)")]
        private static void SetupOnly()
        {
            SetupAddressablesInternal(build: false);
            EditorPrefs.SetBool(SetupKey, true);
            Debug.Log("[TimeAuraAddressablesSetup] Setup completed. Build Addressables to update catalogs.");
        }

        [MenuItem("TimeAura/Addressables/Setup + Build")]
        private static void SetupAndBuild()
        {
            SetupAddressablesInternal(build: true);
            EditorPrefs.SetBool(SetupKey, true);
            EditorPrefs.SetBool(BuildKey, true);
            Debug.Log("[TimeAuraAddressablesSetup] Setup + Build completed.");
        }

        [MenuItem("TimeAura/Addressables/Reset Setup Flags")]
        private static void ResetFlags()
        {
            EditorPrefs.DeleteKey(SetupKey);
            EditorPrefs.DeleteKey(BuildKey);
            Debug.Log("[TimeAuraAddressablesSetup] Setup flags cleared. Auto-setup will run on next Editor load.");
        }

        [MenuItem("TimeAura/Addressables/Force Build Now")]
        private static void ForceBuildNow()
        {
            try
            {
                Debug.Log("[TimeAuraAddressablesSetup] 🔨 Force building Addressables content...");
                AddressableAssetSettings.BuildPlayerContent();
                EditorPrefs.SetBool(BuildKey, true);
                Debug.Log("[TimeAuraAddressablesSetup] ✅ Addressables content built successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimeAuraAddressablesSetup] ❌ Build failed: {ex.Message}");
            }
        }

        private static void SetupAddressablesInternal(bool build)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                throw new InvalidOperationException("Addressables settings could not be created or loaded.");
            }

            var scenesGroup = EnsureGroup(settings, ScenesGroupName);
            var localizationGroup = EnsureGroup(settings, LocalizationGroupName);

            EnsureAddressableEntry(settings, scenesGroup, ConvergenceScenePath, label: "scene");
            EnsureAddressableEntry(settings, localizationGroup, AuraTermsCsvPath, label: "localization");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (build)
            {
                AddressableAssetSettings.BuildPlayerContent();
            }
        }

        private static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            var schemas = new System.Collections.Generic.List<AddressableAssetGroupSchema>();
            var bundledSchema = settings.DefaultGroup.Schemas[0] is BundledAssetGroupSchema
                ? settings.DefaultGroup.Schemas[0]
                : settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>();
            if (bundledSchema != null)
            {
                schemas.Add(bundledSchema);
            }

            var contentUpdateSchema = settings.DefaultGroup.GetSchema<ContentUpdateGroupSchema>();
            if (contentUpdateSchema != null)
            {
                schemas.Add(contentUpdateSchema);
            }

            group = settings.CreateGroup(groupName, false, false, false, schemas);
            return group;
        }

        private static void EnsureAddressableEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string label)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[TimeAuraAddressablesSetup] Asset not found at path: {assetPath}");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
            }
            else if (entry.parentGroup != group)
            {
                settings.MoveEntry(entry, group, false, false);
            }

            entry.address = assetPath;

            if (!string.IsNullOrWhiteSpace(label))
            {
                settings.AddLabel(label, true);
                entry.SetLabel(label, true, true);
            }
        }
    }
}
#endif
