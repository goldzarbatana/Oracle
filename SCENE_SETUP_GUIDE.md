# 🏛️ Time Aura Scene Setup Guide
**Version:** 1.0  
**Date:** 28 лютого 2026 р.

---

## 📋 Огляд

Цей документ описує стандартний підхід до налаштування сцен у проекті **Time Aura**, включаючи глобальні менеджери, VContainer DI, та правила персистентності об'єктів через `DontDestroyOnLoad`.

---

## 🎯 Архітектурні Принципи

### 1. **Global Managers Prefab**
- Всі глобальні менеджери (що мають зберігатися між сценами) розміщуються в єдиному **root GameObject** з назвою `--== MANAGERS ==--`.
- Цей об'єкт містить компонент `TimeAuraLifetimeScope`, який автоматично:
  - Від'єднується від батьківських об'єктів (`transform.SetParent(null)`).
  - Позначається як `DontDestroyOnLoad`.
  - Реєструє всі знайдені менеджери у VContainer.

### 2. **Scene Independence**
- Сцени можуть працювати **автономно** або **у ланцюжку**.
- Якщо `--== MANAGERS ==--` вже існує з попередньої сцени (DontDestroyOnLoad), новий екземпляр не створюється.
- `TimeAuraLifetimeScope` використовує `FindObjectOfType` для пошуку компонентів у всіх сценах, включаючи DontDestroyOnLoad.

### 3. **Interface-Based Registration**
- Менеджери реєструються як через власні типи (`AsSelf`), так і через інтерфейси (`AsImplementedInterfaces`).
- Це дозволяє inject'ити залежності через конкретні імплементації або абстрактні інтерфейси.

---

## 🗂️ Стандартна Структура Сцени

### Базова Ієрархія

```
Scene Root
│
├── --== MANAGERS ==-- (Root GameObject, DontDestroyOnLoad)
│   ├── TimeAuraLifetimeScope (Component)
│   ├── GameManager (Component, implements IManager)
│   ├── AuthManager (Component, implements IManager)
│   ├── UIManager (Component, implements IManager)
│   ├── FirebaseDataService (Component)
│   ├── TwilioSmsGateway (Component)
│   └── AudioService (Component, implements IManager)
│
├── --== UI ==--
│   ├── Canvas
│   │   └── InitiationView (або інші View компоненти)
│   └── EventSystem
│
└── --== ENVIRONMENT ==--
    └── Directional Light / Camera / тощо
```

---

## 🛠️ Покрокове Налаштування Нової Сцени

### Крок 1: Створити Root GameObject для Менеджерів
1. В ієрархії сцени створіть порожній GameObject.
2. Назвіть його `--== MANAGERS ==--`.
3. **Важливо:** Переконайтеся, що він знаходиться на **root-рівні** (без батьківських об'єктів).

### Крок 2: Додати TimeAuraLifetimeScope
1. До об'єкта `--== MANAGERS ==--` додайте компонент:
   ```
   Component -> Scripts -> Core -> Infrastructure -> TimeAuraLifetimeScope
   ```
2. В Inspector'і призначте поле **App Config**:
   - Якщо конфіг вже існує: `Assets/Resources/TimeAuraAppConfig.asset`.
   - Якщо ні — він створиться автоматично при першому запуску.

### Крок 3: Додати Обов'язкові Менеджери
Додайте наступні скрипти як **дочірні об'єкти** (або компоненти на тому ж GameObject):

#### **Core Managers** (обов'язкові для всіх сцен):
- `GameManager` (`Assets/Scripts/Core/GameManager.cs`)
- `AuthManager` (`Assets/Scripts/Features/Auth/AuthManager.cs`)
- `UIManager` (`Assets/Scripts/Features/UI/UIManager.cs`)

#### **Data & Services** (обов'язкові для online-функціональності):
- `FirebaseDataService` (`Assets/Scripts/Features/Data/FirebaseDataService.cs`)
- `TwilioSmsGateway` (`Assets/Scripts/Features/Security/TwilioSmsGateway.cs`)

#### **Optional Managers** (додайте за потребою):
| Менеджер | Призначення | Сцени |
|----------|-------------|-------|
| `LocalizationManager` | Мультімовність | Усі UI сцени |
| `AudioService` | Звуки та музика | Усі сцени з аудіо |
| `SocialManager` | Друзі, інвайти | Social Hub |
| `AuraEffectManager` | Візуальні ефекти аури | Astral, Convergence |
| `MatchingManager` | Система метчингу | Convergence |
| `SecurityHub` | Анти-чіт, валідація | Convergence, PvP |

### Крок 4: Налаштувати UI Canvas
1. Створіть `Canvas` (якщо немає) з компонентом `NovaCanvasScaler`.
2. Додайте `EventSystem` (Unity UI).
3. Створіть ваш View (наприклад, `InitiationView`, `ConvergenceView`) як дочірній об'єкт Canvas.

### Крок 5: Створити Prefab (Перша Сцена)
**Лише для першої/стартової сцени** (наприклад, Initiation):
1. Перетягніть `--== MANAGERS ==--` з Hierarchy у папку `Assets/Prefabs/Core/`.
2. Це створить префаб, який можна легко оновлювати.
3. В інших сценах цей префаб **НЕ потрібно дублювати** — він автоматично збережеться через `DontDestroyOnLoad`.

---

## 🔧 Інтерфейси та Типи Менеджерів

### IManager (Базовий інтерфейс)
Усі менеджери повинні імплементувати `IManager`:

```csharp
// Assets/Scripts/Core/IManager.cs
namespace TimeAura.Core
{
    /// <summary>
    /// Marker interface for all manager components.
    /// Used by TimeAuraLifetimeScope for auto-registration.
    /// </summary>
    public interface IManager
    {
        // Маркерний інтерфейс — не потребує методів
    }
}
```

### Приклад Менеджера

```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TimeAura.Features.MyFeature
{
    /// <summary>
    /// Manages XYZ functionality.
    /// </summary>
    public class MyFeatureManager : MonoBehaviour, IManager
    {
        [Inject] private IObjectResolver _resolver;
        [Inject] private GameManager _gameManager;

        private void Start()
        {
            Debug.Log("[MyFeatureManager] Initialized.");
        }

        public void DoSomething()
        {
            // Implementation
        }
    }
}
```

---

## 🔄 Логіка Переходів між Сценами

### Сценарій 1: Запуск з Initiation (Startup Scene)
1. Unity завантажує `Initiation` сцену.
2. `TimeAuraLifetimeScope.Awake()` викликається:
   - Об'єкт стає root (`transform.SetParent(null)`).
   - Застосовується `DontDestroyOnLoad(gameObject)`.
   - Реєструються всі менеджери.
3. `TimeAuraBootstrapper.Start()` запускає ініціалізацію:
   - Викликає `GameManager.InitializeAsync()`.
   - Пушить `InitiationView` на UI стек.
4. Користувач авторизується і переходить до Convergence:
   - `Addressables.LoadSceneAsync("ConvergenceScene")` або `SceneManager.LoadScene`.
   - **Менеджери залишаються** (DontDestroyOnLoad).

### Сценарій 2: Запуск з Convergence (Пряме тестування)
1. Unity завантажує `Convergence` сцену безпосередньо.
2. Якщо в ній є `--== MANAGERS ==--`:
   - `TimeAuraLifetimeScope` реєструє менеджерів.
   - Якщо їх немає — виводяться попередження (warnings), але додаток не падає завдяки null-object fallbacks.
3. Якщо немає `--== MANAGERS ==--` взагалі:
   - VContainer помилка — **сцена має містити принаймні DI контейнер**.

---

## ⚠️ Типові Помилки та Рішення

### Помилка: `DontDestroyOnLoad only works for root GameObjects`
**Причина:** Об'єкт `--== MANAGERS ==--` має батьківський GameObject.  
**Рішення:** Перемістіть його на root-рівень сцени.

### Помилка: `VContainerException: No such registration of type: GameManager`
**Причина:** `GameManager` відсутній у сцені.  
**Рішення:** Додайте компонент `GameManager` до `--== MANAGERS ==--`.

### Помилка: `VContainerException: SecurityHub is not in this scene DontDestroyOnLoad`
**Причина:** (Виправлено у v1.0) Застаріла версія `TimeAuraLifetimeScope` використовувала `RegisterComponentInHierarchy`.  
**Рішення:** Оновіть код до використання `RegisterComponent` з конкретним екземпляром (див. актуальний `TimeAuraLifetimeScope.cs`).

### Попередження: `[TimeAuraLifetimeScope] LocalizationManager is not in this scene — skipping registration`
**Причина:** Це нормально — менеджер опціональний.  
**Дія:** Додайте компонент, якщо він потрібен для цієї сцени.

---

## 📊 Чеклист для Нової Сцени

- [ ] Створено root GameObject `--== MANAGERS ==--`.
- [ ] Додано `TimeAuraLifetimeScope` компонент.
- [ ] Призначено `AppConfig` в Inspector.
- [ ] Додано `GameManager`, `AuthManager`, `UIManager`.
- [ ] Додано `FirebaseDataService` та `TwilioSmsGateway` (для online).
- [ ] Додано опціональні менеджери залежно від потреб сцени.
- [ ] Налаштовано UI Canvas з Nova UI компонентами.
- [ ] Перевірено відсутність батьківських об'єктів для `--== MANAGERS ==--`.
- [ ] (Перша сцена) Створено Prefab для `--== MANAGERS ==--`.
- [ ] Протестовано запуск сцени в Play Mode.

---

## 🎓 Best Practices

### 1. Мінімізація Дублювання
- Створіть **один Prefab** для `--== MANAGERS ==--`.
- Використовуйте його лише у **першій сцені** завантаження.
- В інших сценах покладайтеся на `DontDestroyOnLoad`.

### 2. Scene-Specific Managers
- Менеджери, специфічні для конкретної сцени (наприклад, `BossAI`, `MinigameController`), **НЕ** додавайте до `--== MANAGERS ==--`.
- Створіть окремі GameObjects у сцені і inject'те залежності через `[Inject]`.

### 3. Тестування Окремих Сцен
- Щоб тестувати сцену без попереднього завантаження Initiation:
  - Додайте до неї мінімальний набір менеджерів.
  - Або створіть **Test Bootstrap Scene** з базовими менеджерами.

### 4. Inspector Fields
- Уникайте `[SerializeField]` посилань на менеджерів у View компонентах.
- Використовуйте `[Inject]` для отримання залежностей через VContainer.

```csharp
// ❌ Погано
[SerializeField] private GameManager gameManager;

// ✅ Добре
[Inject] private GameManager _gameManager;
```

---

## 🚀 Наступні Кроки

1. **Створіть IManager інтерфейс** (якщо ще не існує):
   - Файл: `Assets/Scripts/Core/IManager.cs`
   - Реалізуйте його у всіх менеджерах.

2. **Оновіть існуючі сцени**:
   - Застосуйте цю структуру до `Initiation`, `Convergence`, `Astral`.

3. **Створіть Convergence сцену**:
   - Використайте цей гайд як референс.
   - Додайте специфічні менеджери для головного хабу.

4. **Налаштуйте Addressables**:
   - Позначте всі сцени як Addressable Assets.
   - Виконайте Content Build (`Window -> Asset Management -> Addressables -> Build -> New Build -> Default Build Script`).

---

## 📚 Додаткові Ресурси

- [ARCHITECTURE_GUIDE.md](ARCHITECTURE_GUIDE.md) — Загальна архітектура проекту
- [INITIATION_SCREEN_SETUP.md](INITIATION_SCREEN_SETUP.md) — Налаштування стартового екрану
- [MIGRATION_ZENJECT_VCONTAINER.md](MIGRATION_ZENJECT_VCONTAINER.md) — Міграція на VContainer
- [VContainer Documentation](https://vcontainer.hadashikick.jp/) — Офіційна документація

---

**Автор:** Time Aura Development Team  
**Останнє оновлення:** 28 лютого 2026 р.

> *"Through structure, we find clarity. Through clarity, we manifest greatness."*
