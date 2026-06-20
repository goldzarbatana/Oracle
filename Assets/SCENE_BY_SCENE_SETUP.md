# TimeAura — Повний посібник по сценах (Що де створити)

> Усі кроки реалізуються прямо в Unity Editor.  
> Меню `TimeAura →` виконують більшість кроків автоматично.

---

## ЗМІСТ
1. [InitiationScene](#1-initiationscene)
2. [ConvergenceScene (Nexus)](#2-convergencescene-nexus)
3. [MainMenu Scene](#3-mainmenu-scene)
4. [Спільні правила (усі сцени)](#4-спільні-правила)
5. [Troubleshooting](#5-troubleshooting)

---

## 1. InitiationScene

**Файл:** `Assets/Scenes/InitiationScene.unity`  
**Призначення:** Перший екран. Телефон → OTP → перехід до Nexus.

### Ієрархія (що має бути)
```
InitiationScene
├── --== MANAGERS ==--          ← Prefab (Assets/Prefabs/Core/Managers.prefab)
│   ├── [Component] GameManager
│   ├── [Component] AuthManager
│   ├── [Component] LocalizationManager
│   ├── [Component] UIManager
│   ├── [Component] AuraEffectManager
│   ├── [Component] FirebaseDataService
│   └── [Component] TwilioSmsGateway
│
├── LifetimeScope               ← VContainer bootstrap
│   └── [Component] TimeAuraLifetimeScope
│       ● appConfig   → (drag AppConfig asset)
│       ● managersPrefab → (drag Managers.prefab)
│
├── Main Camera
├── EventSystem
│
└── Canvas  (UI canvas)
    ├── [Component] Canvas
    │   ● Render Mode: Screen Space – Overlay
    ├── [Component] CanvasScaler
    │   ● UI Scale Mode: Scale With Screen Size
    │   ● Reference: 1080 × 1920
    ├── [Component] GraphicRaycaster
    │
    └── InitiationView          ← Core screen
        ├── [Component] InitiationView  (script)
        ├── [Component] CanvasGroup     ← alpha = 1  ⚠️ НЕ 0!
        │
        ├── Background  (Image, чорний)
        ├── Logo        (Image, золотий)
        ├── PhoneLabel  (TextMeshProUGUI)
        ├── PhoneInput  (TMP_InputField)
        ├── StatusText  (TextMeshProUGUI)
        ├── InitiateButton
        │   └── ButtonText  (TextMeshProUGUI)  "INITIATE"
        └── ButtonGlow  (Image, прихований)
```

### Inspector — InitiationView
| Поле | Що призначити |
|---|---|
| `canvasGroup` | CanvasGroup на цьому ж GO |
| `phoneLabel` | TextMeshProUGUI — "PhoneLabel" |
| `phoneInputField` | TMP_InputField — "PhoneInput" |
| `statusText` | TextMeshProUGUI — "StatusText" |
| `initiateButton` | Button — "InitiateButton" |
| `initiateButtonText` | TextMeshProUGUI дочірній до Button |
| `initiateButtonGlow` | Image — "ButtonGlow" |
| `fadeInDuration` | 1.5 |

### Inspector — TimeAuraLifetimeScope
| Поле | Що призначити |
|---|---|
| `appConfig` | `Assets/Resources/TimeAuraAppConfig.asset` |
| `managersPrefab` | `Assets/Prefabs/Core/Managers.prefab` |

### Автоматизація ✅
```
TimeAura → Setup → Create Managers Prefab
TimeAura → Setup → Inject Managers in Active Scene
```

---

## 2. ConvergenceScene (Nexus)

**Файл:** `Assets/Scenes/ConvergenceScene.unity`  
**Призначення:** Головний хаб: Radar (знайти), Vault (баланс), Feed (стрічка).

### Ієрархія
```
ConvergenceScene
├── --== MANAGERS ==--          ← той самий Prefab (DontDestroyOnLoad захистить від дублювання)
│
├── Main Camera
│   └── [Component] Camera
│       ● Clear Flags: Solid Color
│       ● Background: #0D0D0D  (майже чорний)
│
├── EventSystem
│   ├── [Component] EventSystem
│   └── [Component] StandaloneInputModule  (або InputSystemUIInputModule)
│
└── NexusUI                     ← ГОЛОВНИЙ об'єкт UI
    ├── [Component] UIDocument
    │   ● Source Asset: Assets/UI/Nexus/NexusScene.uxml  ⚠️ ОБОВ'ЯЗКОВО!
    │   ● Sort Order: 0
    │   ● Panel Settings: (залиши дефолт або PanelSettings.asset)
    │
    └── [Component] NexusController  (script)
        ● uiDocument: (drag UIDocument з цього ж GO)
```

### Автоматизація ✅
```
TimeAura → Setup → Setup ConvergenceScene (Nexus UI)
```
Цей пункт створить усі об'єкти та підключить UXML автоматично.

### PanelSettings (ВАЖЛИВО якщо чорний екран)
Якщо UI не видно після Play:
1. Project → Create → UI Toolkit → Panel Settings
2. Назви `NexusPanelSettings`
3. `Scale Mode`: Scale With Screen Size, `Reference Resolution`: 1080×1920
4. Drag → UIDocument → `Panel Settings` поле

### Структура UXML (для розуміння — не чіпати руками)
```
ScreenRoot
├── WelcomeRite    ← приховується відразу (код)
├── PanelContent
│   ├── RadarPanel  ← показується відразу (RADAR ACTIVE — 3 MASTERS NEARBY)
│   ├── VaultPanel  ← прихований (VAULT ACTIVE — 10 HORAS)
│   ├── FeedPanel   ← прихований (FEED ACTIVE — NO MESSAGES)
│   └── ChatPanel   ← прихований
├── TransferModal   ← прихований
├── SymmetryCard    ← прихований
└── BottomNav
    ├── NavRadar  [RADAR]
    ├── NavFeed   [FEED]
    └── NavVault  [VAULT]
```

### Очікувані логи при Play
```
[UI_DEBUG] Navigation Buttons Created. Listeners Attached.
[UI_DEBUG] Panels: RadarPanel=True VaultPanel=True FeedPanel=True NavBar=True
```

---

## 3. MainMenu Scene

**Файл:** `Assets/Scenes/MainMenu.unity`  
**Призначення:** Головне меню (splash/logo), кнопка Start.

### Ієрархія
```
MainMenu
├── --== MANAGERS ==--   ← Prefab (мінімум LocalizationManager)
├── Main Camera
├── EventSystem
└── Canvas
    └── MainMenuView
        ├── [Component] будь-який MonoBehaviour с кнопкою Start
        └── StartButton → при кліку: SceneManager.LoadScene("InitiationScene")
```

> MainMenu — опціональна сцена. Якщо Start Scene встановлено на InitiationScene — MainMenu можна пропустити.

---

## 4. Спільні правила (усі сцени)

### Правило 1 — Managers завжди root
`--== MANAGERS ==--` повинен бути **кореневим** GameObject (не дочірнім).  
Якщо він дочірній → `DontDestroyOnLoad` не спрацює → managers зникають при зміні сцени.

### Правило 2 — CanvasGroup.alpha = 1 у Edit Mode
Усі панелі/views повинні мати `alpha = 1` у сцені.  
Код сам ставить `alpha = 0` перед анімацією в `Show()`.  
Якщо в сцені стоїть `alpha = 0` → чорний екран при будь-якій помилці bootstrap.

### Правило 3 — UIDocument потребує PanelSettings
Без `PanelSettings` UIDocument не рендерить нічого на деяких конфігураціях.  
Якщо Nexus UI не видно — перевір поле `Panel Settings` на UIDocument.

### Правило 4 — EventSystem обов'язковий
Без EventSystem натискання кнопок (UXML та UGUI) не спрацьовують.

### Правило 5 — Build Settings
Усі сцени мають бути у `File → Build Settings → Scenes In Build`:
```
0: Assets/Scenes/InitiationScene.unity   (Start Scene)
1: Assets/Scenes/ConvergenceScene.unity
2: Assets/Scenes/MainMenu.unity
```

---

## 5. Troubleshooting

| Симптом | Причина | Рішення |
|---|---|---|
| Чорний екран у InitiationScene | `CanvasGroup.alpha = 0` в prefab/сцені | Встанови alpha = 1 в Inspector |
| `VContainerException: NexusController not in scene` | NexusController відсутній у сцені | Запусти `TimeAura → Setup → Setup ConvergenceScene` |
| UI Toolkit нічого не показує | UIDocument не має Source Asset або PanelSettings | Призначи NexusScene.uxml і PanelSettings |
| `[UI_DEBUG] UXML elements missing` | Source Asset не той або неправильний UXML | Drag `Assets/UI/Nexus/NexusScene.uxml` → UIDocument → Source Asset |
| Managers зникають після зміни сцени | `--== MANAGERS ==--` є дочірнім | Винеси на верхній рівень ієрархії сцени |
| `appConfig is null` warning | AppConfig не призначений | Drag `Assets/Resources/TimeAuraAppConfig.asset` → LifetimeScope → appConfig |
| Addressables `InvalidKeyException` | Сцена не в Addressables або build не зроблено | `TimeAura → Addressables → Force Build Now` |

---

## Контрольний список перед тестуванням

- [ ] `Assets/Prefabs/Core/Managers.prefab` існує (`TimeAura → Setup → Create Managers Prefab`)
- [ ] `--== MANAGERS ==--` є у InitiationScene як root GO
- [ ] `TimeAuraLifetimeScope` має `appConfig` і `managersPrefab` призначені
- [ ] `InitiationView.CanvasGroup.alpha = 1` в сцені
- [ ] `ConvergenceScene` має `NexusUI` з `UIDocument` (Source: `NexusScene.uxml`) і `NexusController`
- [ ] `EventSystem` є у кожній сцені
- [ ] Усі 3 сцени у Build Settings
- [ ] Addressables build зроблено (`TimeAura → Addressables → Force Build Now`)

---

*Оновлено: 1 березня 2026*
