# 🌍 Time Aura Localization System Guide

## Overview
Time Aura використовує власну систему локалізації на базі CSV файлів з підтримкою 9 мов.

## Підтримувані мови
- 🇬🇧 English (en)
- 🇫🇷 French (fr)
- 🇩🇪 German (de)
- 🇮🇹 Italian (it)
- 🇵🇱 Polish (pl)
- 🇷🇺 Russian (ru)
- 🇪🇸 Spanish (es)
- 🇺🇦 Ukrainian (uk)
- 🇹🇷 **Turkish (tr)** - нова мова!

*Примітка: Latin більше не підтримується за замовчуванням, але можна додати через кастомну мапу мов.*

---

## 📁 Структура файлів

### 1. **AuraTerms.cs** - Ключі локалізації
**Розташування:** `Assets/Scripts/Core/Localization/AuraTerms.cs`

Містить константи для локалізаційних ключів:

```csharp
// Чотири основні терміни
AuraTerms.USER_ENTITY    // "Master" (Майстер/Meister/Maître...)
AuraTerms.CURRENCY       // "Vector" (Вектор/Vektor...)
AuraTerms.REPUTATION     // "Status" (Статус...)
AuraTerms.SOCIAL_FEED    // "Convergence" (Конвергенція...)
```

### 2. **AuraTerms.csv** - Таблиця перекладів
**Розташування:** `Assets/Resources/Localization/AuraTerms.csv`

CSV файл з перекладами для всіх мов. **Новий формат з кодами мов:**
```csv
Key,English(en),French(fr),German(de),Italian(it),Polish(pl),Russian(ru),Spanish(es),Ukrainian(uk),Turkish(tr)
term.user_entity,"Master","Maître","Meister","Maestro","Mistrz","Мастер","Maestro","Майстер","Usta"
term.currency,"Vector","Vecteur","Vektor","Vettore","Wektor","Вектор","Vector","Вектор","Vektör"
...
```

**Особливості формату:**
- Заголовки містять коди мов у дужках: `English(en)`, `French(fr)` тощо
- Всі значення обгорнуті у подвійні лапки для правильної обробки ком та спецсимволів
- Підтримується форматування з параметрами: `"{0} Vectors"`, `"Level {0}"`

### 3. **LocalizationManager.cs** - Менеджер локалізації
**Розташування:** `Assets/Scripts/Features/Localization/LocalizationManager.cs`

Завантажує CSV при ініціалізації і надає методи для отримання локалізованих рядків.

---

## 🚀 Використання в коді

### Базове використання

```csharp
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;

public class ExampleUI : MonoBehaviour
{
    [Inject] private LocalizationManager localizationManager;
    
    void Start()
    {
        // Отримати простий термін
        string currency = localizationManager.Get(AuraTerms.CURRENCY, "Vector");
        // Результат залежить від мови: "Vector" | "Vektor" | "Вектор"...
        
        // Використати helper методи з AuraTerms
        string userEntity = AuraTerms.GetUserEntity(localizationManager);
        // Результат: "Master" | "Meister" | "Майстер"...
    }
}
```

### Форматування з параметрами

```csharp
// CSV ключ: ui.vector_display,{0} Vectors,{0} Vektoren,{0} Vecteurs,...
int count = 150;
string text = localizationManager.GetFormatted("ui.vector_display", count);
// Результат: "150 Vectors" | "150 Vektoren" | "150 Векторів"...

// CSV ключ: ui.vector_per_hour,Vector: ${0} /hr,Vektor: ${0} /Std,...
int rate = 250;
string rateText = localizationManager.GetFormatted("ui.vector_per_hour", rate);
// Результат: "Vector: $250 /hr" | "Vektor: $250 /Std" | "Вектор: $250 /год"
```

### Зміна мови в рантаймі

```csharp
// Змінити мову на німецьку
localizationManager.SetLanguage(SystemLanguage.German);

// Змінити мову на українську
localizationManager.SetLanguage(SystemLanguage.Ukrainian);

// Після зміни потрібно оновити UI вручну або викликати івент
```

---

## ➕ Додавання нових термінів

### Крок 1: Додати ключ в `AuraTerms.cs`
```csharp
public const string NEW_TERM = "term.new_term";
```

### Крок 2: Додати переклади в CSV
Відкрийте `Assets/Resources/Localization/AuraTerms.csv` і додайте новий рядок:
```csv
term.new_term,EnglishValue,GermanValue,FrenchValue,UkrainianValue,PolishValue,SpanishValue,ItalianValue,RussianValue,LatinValue
```

### Крок 3: Перезавантажити гру
LocalizationManager завантажить CSV автоматично при ініціалізації.

---

## 🎨 Приклади інтеграції з UI

### InitiationView (Екран входу)
```csharp
SetStatus(localizationManager?.Get("msg.temple_awaits", "The temple awaits your presence.") 
    ?? "The temple awaits your presence.");
```

### FateCard (Картка користувача)
```csharp
int vectorValue = 250;
vectorText.text = localizationManager?.GetFormatted("ui.vector_per_hour", vectorValue) 
    ?? $"Vector: ${vectorValue} /hr";
```

### ActiveSessionView (Активна сесія)
```csharp
vectorsText.text = _localizationManager?.GetFormatted("ui.vector_display", vectorsExchanged) 
    ?? $"{vectorsExchanged} Vectors";
```

---

## 🔧 Налаштування і тестування

### Тестування різних мов в Editor
```csharp
// У будь-якому MonoBehaviour або ScriptableObject
#if UNITY_EDITOR
void OnValidate()
{
    // Змінити мову для тестування
    Application.systemLanguage = SystemLanguage.German;
}
#endif
```

### Fallback логіка
LocalizationManager автоматично використовує англійську мову як fallback, якщо поточна мова не знайдена в CSV:
```csharp
public string Get(string key, string fallback = "")
{
    // 1. Спробувати отримати поточну мову
    // 2. Якщо не знайдено - fallback на English
    // 3. Якщо і English немає - повернути параметр fallback
}
```

---

## 📝 CSV формат

### Заголовки (Header row)
```
Key,English,German,French,Ukrainian,Polish,Spanish,Italian,Russian,Latin
```

### Приклад рядка даних
```
term.currency,Vector,Vektor,Vecteur,Вектор,Wektor,Vector,Vettore,Вектор,Vector
```

**Важливо:**
- Перша колонка - унікальний ключ
- Решта колонок - переклади для кожної мови
- Порядок мов має відповідати заголовку
- Немає підтримки кирилиці в ключах (тільки латинські літери, цифри і крапки/підкреслення)

---

## 🌟 Найкращі практики

### ✅ DO (Робити)
- Використовувати константи з `AuraTerms` замість hardcoded рядків
- Надавати fallback значення у виклику `Get()` / `GetFormatted()`
- Групувати ключі за префіксами: `term.`, `msg.`, `ui.`
- Перевіряти `localizationManager` на null перед викликом
- Додавати helper методи в `AuraTerms` для часто використовуваних термінів

### ❌ DON'T (Не робити)
- Не використовувати hardcoded рядки в UI скриптах
- Не забувати додавати переклади для ВСІХ мов в CSV
- Не змінювати порядок колонок в CSV після першого релізу
- Не використовувати спеціальні символи (коми, нові рядки) без escaping в CSV

---

## 🐛 Troubleshooting

### Проблема: "CSV file not found"
**Рішення:** Перевірте що файл знаходиться в `Assets/Resources/Localization/AuraTerms.csv`

### Проблема: "Localization entries = 0"
**Рішення:** 
1. Перевірте формат CSV (UTF-8 без BOM)
2. Переконайтеся що перший рядок - заголовки
3. Перевірте що немає порожніх рядків

### Проблема: Переклад не змінюється після редагування CSV
**Рішення:** Перезапустіть Play Mode - CSV завантажується тільки при ініціалізації

### Проблема: Показується fallback замість перекладу
**Рішення:**
1. Перевірте що ключ написаний правильно (case-sensitive)
2. Перевірте що мова присутня в CSV
3. Перевірте консоль на повідомлення про завантаження CSV

---

## 🎯 Roadmap

### Phase 1 ✅ (Completed)
- ✅ Створено `AuraTerms.cs` з ключами
- ✅ Створено CSV таблицю з 9 мовами (включно з Turkish!)
- ✅ Інтегровано `LocalizationManager` з CSV + підтримка заголовків з кодами мов
- ✅ Improved CSV parser з підтримкою quoted fields
- ✅ Замінено hardcoded рядки в InitiationView, FateCard, ActiveSessionView, ResonanceSelectionView
- ✅ Створено `AddressableLocalizationBridge` для гібридної інтеграції

### Phase 2 ✅ (Ready for Integration)
- ✅ Підготовлено інтеграцію з Unity Localization Package
- ✅ Створено документацію: [ADDRESSABLES_LOCALIZATION_GUIDE.md](ADDRESSABLES_LOCALIZATION_GUIDE.md)
- ⏳ Runtime мовний селектор у Settings UI
- ⏳ Динамічне оновлення UI при зміні мови
- ⏳ Pluralization support (1 Vector vs 2 Vectors)
- ⏳ Gender/case support для слов'янських мов

---

## 🌐 Unity Addressables Integration

Time Aura підтримує **гібридний режим** локалізації:
- **CSV-based (legacy)** — працює з коробки, швидка розробка
- **Addressables** — оптимізовано для продакшену, remote updates, memory efficient

### Швидкий старт з Addressables:

1. Встановіть Unity Localization Package
2. Розкоментуйте `#define UNITY_LOCALIZATION_ENABLED` у `AddressableLocalizationBridge.cs`
3. Запустіть **Tools > Localization > Import CSV to Addressables** (створіть Editor скрипт з гайду)
4. Додайте `AddressableLocalizationBridge` компонент до сцени

Детальна інструкція: [ADDRESSABLES_LOCALIZATION_GUIDE.md](ADDRESSABLES_LOCALIZATION_GUIDE.md)

---

## 📚 Посилання на файли

- [AuraTerms.cs](../Scripts/Core/Localization/AuraTerms.cs) - Ключі локалізації
- [LocalizationManager.cs](../Scripts/Features/Localization/LocalizationManager.cs) - Менеджер локалізації
- [AddressableLocalizationBridge.cs](../Scripts/Features/Localization/AddressableLocalizationBridge.cs) - Гібридна інтеграція
- [AuraTerms.csv](../Resources/Localization/AuraTerms.csv) - CSV таблиця перекладів
- [ADDRESSABLES_LOCALIZATION_GUIDE.md](ADDRESSABLES_LOCALIZATION_GUIDE.md) - Повний гайд з Addressables інтеграції

---

**"In every language, the temple speaks its truth."** 🌍✨
