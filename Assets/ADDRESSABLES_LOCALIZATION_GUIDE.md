# 🌐 Unity Addressables Localization Integration Guide

## Overview
Цей гайд описує як інтегрувати Time Aura локалізацію з Unity Localization Package та Addressables.

---

## 📦 Встановлення Unity Localization Package

### Крок 1: Встановити пакет
1. Відкрийте **Window > Package Manager**
2. Переключіть на **Unity Registry**
3. Знайдіть **Localization**
4. Натисніть **Install**

### Крок 2: Налаштувати проєкт
1. Відкрийте **Window > Asset Management > Localization Tables**
2. Створіть нову **String Table Collection**: `AuraTerms`
3. Додайте підтримувані мови (Locales):
   - English (en)
   - French (fr)
   - German (de)
   - Italian (it)
   - Polish (pl)
   - Russian (ru)
   - Spanish (es)
   - Ukrainian (uk)
   - Turkish (tr)

---

## 🔄 Міграція з CSV на Addressables

### Автоматична міграція (рекомендовано)

Створіть Editor скрипт для імпорту CSV в Unity String Tables:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
using System.IO;

public class LocalizationImporter : EditorWindow
{
    [MenuItem("Tools/Localization/Import CSV to Addressables")]
    static void ImportCSV()
    {
        var csvPath = "Assets/Resources/Localization/AuraTerms.csv";
        var csvText = File.ReadAllText(csvPath);
        
        // Parse CSV
        var lines = csvText.Split('\n');
        var headers = lines[0].Split(',');
        
        // Get or create String Table Collection
        var collection = LocalizationEditorSettings.GetStringTableCollection("AuraTerms");
        
        // Import each row
        for (int i = 1; i < lines.Length; i++)
        {
            var cells = lines[i].Split(',');
            if (cells.Length < 2) continue;
            
            var key = cells[0].Trim();
            
            // Add translations for each language
            for (int lang = 1; lang < headers.Length; lang++)
            {
                var locale = GetLocaleFromHeader(headers[lang]);
                var value = cells[lang].Trim().Trim('"');
                
                var table = collection.GetTable(locale);
                if (table != null)
                {
                    table.AddEntry(key, value);
                }
            }
        }
        
        EditorUtility.SetDirty(collection);
        Debug.Log("✨ CSV imported to Addressables String Tables!");
    }
    
    static string GetLocaleFromHeader(string header)
    {
        // Extract locale code from "English(en)" format
        if (header.Contains("("))
        {
            var start = header.IndexOf('(') + 1;
            var end = header.IndexOf(')');
            return header.Substring(start, end - start);
        }
        return "en";
    }
}
#endif
```

---

## 🔌 Використання AddressableLocalizationBridge

### Крок 1: Активувати інтеграцію

У файлі `AddressableLocalizationBridge.cs` розкоментуйте:

```csharp
#define UNITY_LOCALIZATION_ENABLED
```

### Крок 2: Додати компонент у сцену

1. Виберіть `--== MANAGERS ==--` GameObject
2. Add Component > **Addressable Localization Bridge**
3. Вкажіть параметри:
   - **Use Addressables Localization**: `true`
   - **String Table Collection**: `AuraTerms`

### Крок 3: Використання у коді

```csharp
using TimeAura.Features.Localization;

public class ExampleUI : MonoBehaviour
{
    private AddressableLocalizationBridge bridge;
    
    void Start()
    {
        bridge = FindObjectOfType<AddressableLocalizationBridge>();
        
        // Get localized string (автоматичний fallback на CSV)
        string text = bridge.GetLocalizedString("msg.temple_awaits");
    }
}
```

---

## 🎯 Гібридний режим (CSV + Addressables)

### Переваги гібридного підходу:
- ✅ **Швидкий старт**: CSV працює без додаткових пакетів
- ✅ **Плавна міграція**: можна поступово переносити ключі в Addressables
- ✅ **Fallback**: якщо ключ відсутній в Addressables, використовується CSV
- ✅ **Editor підтримка**: CSV легко редагувати в будь-якому текстовому редакторі

### Робочий процес:

1. **Розробка**: використовувати CSV (швидше, легше)
2. **Білд**: міграція в Addressables (оптимізовано для runtime)
3. **Hotfix**: оновлення через Addressables без перезбірки

---

## 🔧 Налаштування Addressables

### Створення груп для локалізації

1. **Window > Asset Management > Addressables > Groups**
2. Створіть групу `Localization_[LanguageCode]` для кожної мови:
   - `Localization_en`
   - `Localization_fr`
   - `Localization_de`
   - тощо

3. Налаштуйте **Remote Build Path** для кожної групи (опціонально для remote updates)

### Приклад Build Script для автоматизації:

```csharp
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

public static class LocalizationBuildPipeline
{
    [MenuItem("Tools/Localization/Build Addressables for All Locales")]
    static void BuildAllLocales()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        
        // Build for each locale
        foreach (var locale in new[] { "en", "fr", "de", "it", "pl", "ru", "es", "uk", "tr" })
        {
            BuildLocaleGroup(settings, locale);
        }
        
        Debug.Log("✨ All localization Addressables built!");
    }
    
    static void BuildLocaleGroup(AddressableAssetSettings settings, string locale)
    {
        var groupName = $"Localization_{locale}";
        var group = settings.FindGroup(groupName);
        
        if (group == null)
        {
            Debug.LogWarning($"Group not found: {groupName}");
            return;
        }
        
        // Build this group only
        AddressableAssetSettings.BuildPlayerContent();
        Debug.Log($"Built localization for {locale}");
    }
}
#endif
```

---

## 📊 Порівняння підходів

| Характеристика | CSV (Legacy) | Addressables | Гібрид |
|----------------|--------------|-------------|--------|
| Швидкість розробки | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| Runtime performance | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| Memory usage | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| Remote updates | ❌ | ✅ | ✅ |
| Pluralization | ❌ | ✅ | ✅ |
| Context/Notes | ❌ | ✅ | ✅ |
| Easy to edit | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |

---

## 🚀 Best Practices

### 1. Використовувати Async Loading
```csharp
var handle = Addressables.LoadAssetAsync<StringTable>("AuraTerms_en");
await handle.Task;
var table = handle.Result;
```

### 2. Preload часто використовувані таблиці
```csharp
void Start()
{
    // Preload UI terms
    Addressables.LoadAssetAsync<StringTable>("AuraTerms_UI");
}
```

### 3. Unload непотрібні мови
```csharp
void OnLanguageChanged(SystemLanguage newLang)
{
    // Unload old language
    Addressables.Release(currentTable);
    
    // Load new language
    currentTable = await Addressables.LoadAssetAsync<StringTable>($"AuraTerms_{GetLocaleCode(newLang)}");
}
```

### 4. Використовувати Smart Strings для динамічного контенту
```csharp
// У String Table:
// "ui.level_info" = "Level {level}, XP: {xp}/{maxXp}"

var smartString = new LocalizedString("AuraTerms", "ui.level_info");
smartString.Arguments = new object[] { 
    new { level = 5, xp = 1200, maxXp = 2000 } 
};
var result = smartString.GetLocalizedString(); // "Level 5, XP: 1200/2000"
```

---

## 🐛 Troubleshooting

### Проблема: "Locale not found"
**Рішення:** Переконайтесь що Locale створений у **Window > Asset Management > Localization Tables > Locales**

### Проблема: "String Table Collection is null"
**Рішення:** 
1. Перевірте що ім'я колекції правильне
2. Rebuilt Addressables: **Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script**

### Проблема: CSV import не працює
**Рішення:**
1. Перевірте формат CSV (UTF-8 encoding)
2. Перевірте що всі лапки закриті правильно
3. Подивіться Console на помилки парсингу

---

## 📚 Додаткові ресурси

- [Unity Localization Package Documentation](https://docs.unity3d.com/Packages/com.unity.localization@latest)
- [Addressables Tutorial](https://learn.unity.com/tutorial/addressables-tutorial)
- [Time Aura Localization Guide](LOCALIZATION_GUIDE.md)

---

**"Whether through ancient scrolls or modern bytes, every language finds its home in the temple."** 🌍✨
