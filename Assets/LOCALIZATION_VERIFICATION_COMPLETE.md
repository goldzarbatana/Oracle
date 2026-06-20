# TimeAura Localization & Addressables — Verification Complete ✅

## Статус перевірки (28 лютого 2026)

### ✅ Що працює
- **Addressables Groups**: `TimeAura_Scenes` і `TimeAura_Localization` створені
- **Сцени додані**: `InitiationScene.unity`, `ConvergenceScene.unity`, `MainMenu.unity` — усі в групі з адресами
- **CSV додано**: `Assets/Resources/Localization/AuraTerms.csv` присутній у `TimeAura_Localization`
- **Localization Settings**: створено (підтверджено користувачем)
- **9 мов**: English, French, German, Italian, Polish, Russian, Spanish, Ukrainian, Turkish

### 🔧 Що виправлено
1. **Auto-build скрипт оновлено**:
   - Тепер автоматично побудує Addressables content при наступному запуску Editor (якщо ще не зроблено).
   - Це усуне `InvalidKeyException` для `ConvergenceScene.unity`.

2. **Додано меню Force Build**:
   - `TimeAura → Addressables → Force Build Now` — запускає build вручну прямо зараз.

---

## Швидкі кроки для завершення налаштування

### Крок 1: Побудувати Addressables content (ЗАРАЗ)
Виконай **одну** з опцій:

**Опція A (рекомендується — швидко):**
- Unity menu → `TimeAura` → `Addressables` → `Force Build Now`
- Чекай лог: "[TimeAuraAddressablesSetup] ✅ Addressables content built successfully!"

**Опція B (стандартна):**
- Window → Asset Management → Addressables → Groups
- Build → New Build → Default Build Script

**Опція C (авто при перезапуску):**
- Закрий і відкрий Unity Editor — скрипт автоматично зробить build.

---

### Крок 2: Перевірити локалізацію у Play Mode

Після build натисни **Play** і перевір Console:

**Очікувані логи:**
```
[TimeAuraLifetimeScope] FirebaseDataService found — registered as IDataService.
[TimeAuraLifetimeScope] LocalizationManager not found in scene — creating runtime LocalizationManager component.
[LocalizationManager] Detected 9 languages in CSV: English, French, German, ...
[LocalizationManager] Successfully loaded X rows with Y unique keys.
[TimeAuraBootstrapper] 🌟 The Digital Temple awakens...
[InitiationView] Show() called
```

**Більше НЕ повинно бути:**
- ❌ `InvalidKeyException: No Location found for Key=Assets/Scenes/ConvergenceScene.unity`

---

### Крок 3: Виправити FormatException (якщо залишається)

Якщо бачиш багато:
```
FormatException: Input string was not in a correct format.
...PropertyVariants.TrackedProperties.TrackedProperty...
```

**Причина**: У сцені є Property Variant з некоректним значенням (наприклад, числове поле містить текст "n" або порожній рядок).

**Як виправити:**
1. Window → Localization → Scene references (або GameObject Property Variants у Hierarchy)
2. Знайди компоненти `LocalizedProperty` або `Game Object Localizer` на об'єктах сцени
3. Перевір Property Variants для числових полів (float/int) — видали або виправ некоректні значення
4. Збережи сцену

**Швидкий спосіб:** Якщо Property Variants не використовуються:
- Відкрий сцену InitiationScene
- Знайди об'єкти з компонентом "Game Object Localizer" або схожим
- Видали компонент або виправ значення варіантів

---

## Підсумок: Що робити зараз

1. **Натисни** у Unity: `TimeAura → Addressables → Force Build Now` _(це найшвидше)_
2. **Перезапусти Play Mode** і перевір, що `ConvergenceScene` більше не падає з `InvalidKeyException`
3. **Перевір UI** — тексти повинні відображатися локалізовано (англійською за замовчуванням або системною мовою)
4. **(Опціонально)** Якщо FormatException лишається — знайди Property Variants у сцені й виправ числові поля

---

## Готово! 🎉

Після build усе має працювати:
- Локалізація завантажується з CSV ✅
- Сцени завантажуються через Addressables ✅
- UI показується й працює коректно ✅

---

### Додаткові команди (меню TimeAura)

- `TimeAura/Addressables/Setup (Groups + Entries)` — створює групи без build
- `TimeAura/Addressables/Setup + Build` — створює групи та одразу робить build
- `TimeAura/Addressables/Force Build Now` — лише build (швидко)
- `TimeAura/Addressables/Reset Setup Flags` — скидає прапорці, щоб авто-setup перезапустився

---

**Створено:** 28.02.2026  
**Статус:** Готово до використання після Force Build
