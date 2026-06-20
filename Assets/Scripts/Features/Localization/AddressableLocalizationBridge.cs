using UnityEngine;

namespace TimeAura.Features.Localization
{
    /// <summary>
    /// Helper component for Unity Addressables Localization integration.
    /// Bridges between legacy CSV-based localization and Unity's Localization Package.
    /// 
    /// INSTALLATION INSTRUCTIONS:
    /// 1. Install Unity Localization Package: Window > Package Manager > Unity Registry > Localization
    /// 2. Enable this script by uncommenting the #define below
    /// 3. Create String Tables via Window > Asset Management > Localization Tables
    /// 4. Map CSV keys to Addressables String Table entries
    /// 
    /// "The temple adapts to every tongue, whether ancient or modern."
    /// </summary>
    public class AddressableLocalizationBridge : MonoBehaviour
    {
        // Uncomment when Unity Localization Package is installed:
        // #define UNITY_LOCALIZATION_ENABLED

#if UNITY_LOCALIZATION_ENABLED
        // Add these using directives when package is installed:
        // using UnityEngine.Localization;
        // using UnityEngine.Localization.Settings;
        // using UnityEngine.Localization.Tables;

        [Header("Addressables Integration")]
        [SerializeField] private bool useAddressablesLocalization = false;
        [SerializeField] private string stringTableCollection = "AuraTerms";

        private LocalizationManager legacyManager;

        private void Awake()
        {
            legacyManager = FindObjectOfType<LocalizationManager>();

            if (useAddressablesLocalization)
            {
                Debug.Log("[AddressableLocalizationBridge] 🌍 Addressables Localization enabled.");
                InitializeAddressablesLocalization();
            }
            else
            {
                Debug.Log("[AddressableLocalizationBridge] Using legacy CSV-based localization.");
            }
        }

        private void InitializeAddressablesLocalization()
        {
            // Initialize Unity Localization
            // LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(SystemLanguage.English);

            // Subscribe to locale changes
            // LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
        {
            // Sync with legacy LocalizationManager
            if (legacyManager != null)
            {
                // Map Unity locale to SystemLanguage
                var systemLang = MapUnityLocaleToSystemLanguage(newLocale);
                legacyManager.SetLanguage(systemLang);
                Debug.Log($"[AddressableLocalizationBridge] Locale changed to {newLocale.Identifier.Code} -> {systemLang}");
            }
        }

        private SystemLanguage MapUnityLocaleToSystemLanguage(UnityEngine.Localization.Locale locale)
        {
            // Map Unity locale codes to SystemLanguage enum
            return locale.Identifier.Code switch
            {
                "en" => SystemLanguage.English,
                "de" => SystemLanguage.German,
                "fr" => SystemLanguage.French,
                "uk" => SystemLanguage.Ukrainian,
                "pl" => SystemLanguage.Polish,
                "es" => SystemLanguage.Spanish,
                "it" => SystemLanguage.Italian,
                "ru" => SystemLanguage.Russian,
                "tr" => SystemLanguage.Turkish,
                _ => SystemLanguage.English
            };
        }

        /// <summary>
        /// Get localized string from Addressables String Table.
        /// Falls back to legacy CSV if key not found.
        /// </summary>
        public string GetLocalizedString(string key)
        {
            if (!useAddressablesLocalization)
            {
                return legacyManager?.Get(key, key) ?? key;
            }

            // Get from Unity Localization String Table
            // var stringTable = LocalizationSettings.StringDatabase.GetTable(stringTableCollection);
            // if (stringTable != null)
            // {
            //     var entry = stringTable.GetEntry(key);
            //     if (entry != null)
            //     {
            //         return entry.GetLocalizedString();
            //     }
            // }

            // Fallback to legacy CSV
            return legacyManager?.Get(key, key) ?? key;
        }

        private void OnDestroy()
        {
            // LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }
#else
        private void Awake()
        {
            Debug.LogWarning("[AddressableLocalizationBridge] Unity Localization Package not installed. Using legacy CSV-based localization only.");
        }
#endif
    }
}
