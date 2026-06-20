# 🌟 Initiation Screen Setup Guide - "The Gateway"

## 📋 Prerequisites

Перевірте, що у вас встановлено:
- ✅ Unity 2022.3+ 
- ✅ TextMeshPro (Package Manager → Unity Registry)
- ✅ Input System Package
- ✅ Addressables Package
- ✅ UniTask (через Package Manager або Git URL)
- ✅ VContainer (через Git URL)

---

## 🎨 Step 1: Створення Initiation Scene

### 1.1 Create Scene
```
Assets → Create → Scene
Назва: InitiationScene.unity
Розташування: Assets/Scenes/InitiationScene.unity
```

### 1.2 Scene Lighting Setup (містичний чорний фон)
- **Environment:**
  - Window → Rendering → Lighting
  - Environment → Skybox Material: None
  - Environment → Background: Solid Color → RGB(0, 0, 0) чорний
  - Ambient Mode: Color → чорний

### 1.3 Camera Setup
- Main Camera:
  - Clear Flags: Solid Color
  - Background: RGB(0, 0, 0)
  - Projection: Orthographic (для 2D UI) або Perspective
  - Position: (0, 0, -10)

---

## 🏗️ Step 2: Створення Canvas та UI Structure

### 2.1 Create Canvas
```
Hierarchy → Right Click → UI → Canvas
Назва: InitiationCanvas
```

**Canvas Settings:**
- Render Mode: **Screen Space - Overlay**
- Canvas Scaler:
  - UI Scale Mode: **Scale With Screen Size**
  - Reference Resolution: **1920 x 1080**
  - Match: **0.5** (середнє між width/height)

### 2.2 Add EventSystem
Якщо немає EventSystem:
```
Hierarchy → Right Click → UI → Event System
```

**⚠️ ВАЖЛИВО: Fix Input System Conflict**
- Видаліть компонент `Standalone Input Module`
- Додайте компонент `Input System UI Input Module` (з пакету Input System)
- Або: Edit → Project Settings → Player → Active Input Handling → **Both**

---

## 🎭 Step 3: Створення Initiation Screen Prefab

### 3.1 Create Root GameObject
```
InitiationCanvas → Right Click → Create Empty
Назва: InitiationScreen
```

**Components to Add:**
1. Add Component → **Canvas Group** (для fade in/out)
2. Add Component → **Scripts/Features/UI/Auth/InitiationView** (ваш скрипт)

**InitiationScreen Transform:**
- Rect Transform:
  - Anchors: Stretch (всі 0/1)
  - Left/Right/Top/Bottom: 0
  - Pivot: (0.5, 0.5)

### 3.2 Background Vignette
```
InitiationScreen → Right Click → UI → Image
Назва: BackgroundVignette
```

**Settings:**
- Image: None (або dark vignette texture)
- Color: RGB(0, 0, 0) чорний / прозорий по краях
- Raycast Target: **unchecked**
- Rect Transform:
  - Anchors: Stretch
  - Offsets: 0

### 3.3 Golden Logo (центр екрану)
```
InitiationScreen → Right Click → UI → Image
Назва: LogoImage
```

**Settings:**
- Image: ваш лого (golden symbol/mandala)
- Color: RGB(255, 215, 0) золотий #FFD700
- Preserve Aspect: **checked**
- Rect Transform:
  - Anchors: Middle Center
  - Width: 300, Height: 300
  - Pos X: 0, Pos Y: 100

**⚠️ Важливо:** Створіть placeholder якщо немає logo:
- Assets → Create → Sprites → Circle
- Або використайте UI → Image (None)

### 3.4 Aura Mist Particles (містичний ефект)
```
InitiationScreen → Right Click → Effects → Particle System
Назва: AuraMist
```

**Particle System Settings:**
- **Main:**
  - Duration: 5
  - Looping: **checked**
  - Start Lifetime: 3-5
  - Start Speed: 0.5
  - Start Size: 0.5-2 (random)
  - Start Color: RGB(255, 215, 0, 50) золотий напівпрозорий
  - Max Particles: 50
- **Emission:**
  - Rate over Time: 10
- **Shape:**
  - Shape: Box
  - Scale: (10, 10, 0)
- **Renderer:**
  - Render Mode: Billboard
  - Material: Default-Particle (або створіть additive golden material)

**Position:**
- Transform: (0, 0, 0)
- За LogoImage у hierarchy (нижче)

---

## 📝 Step 4: Input Fields та Labels

### 4.1 Phone Label (над input field)
```
InitiationScreen → Right Click → UI → Text - TextMeshPro
Назва: PhoneLabel
```

**Settings:**
- Text: "ENTER YOUR SIGNAL"
- Font: Roboto або Orbitron (sci-fi)
- Font Size: 24
- Color: RGB(255, 215, 0) золотий
- Alignment: Center
- Rect Transform:
  - Anchors: Middle Center
  - Pos Y: -100
  - Width: 600, Height: 50

### 4.2 Phone Input Field
```
InitiationScreen → Right Click → UI → Input Field - TextMeshPro
Назва: PhoneInputField
```

**Settings:**
- Placeholder Text: "+1 (555) 123-4567"
- Placeholder Color: RGB(255, 215, 0, 80) золотий прозорий
- Text Color: RGB(255, 255, 255) білий
- Font Size: 28
- Content Type: **Standard** (або Phone Number)
- Character Limit: 20
- Rect Transform:
  - Anchors: Middle Center
  - Pos Y: -150
  - Width: 600, Height: 80

**Styling:**
- Background Image:
  - Source Image: UI Sprite (rounded rect)
  - Color: RGB(20, 20, 20, 200) темно-сірий
- Border: додайте outline золотого кольору (можна через Shader або Image border)

### 4.3 Status Text (feedback користувачу)
```
InitiationScreen → Right Click → UI → Text - TextMeshPro
Назва: StatusText
```

**Settings:**
- Text: "The temple awaits your presence."
- Font Size: 18
- Color: RGB(200, 170, 0) тьмяніше золото
- Alignment: Center
- Rect Transform:
  - Anchors: Middle Center
  - Pos Y: -250
  - Width: 700, Height: 60

---

## 🔘 Step 5: Initiate Button

### 5.1 Create Button
```
InitiationScreen → Right Click → UI → Button - TextMeshPro
Назва: InitiateButton
```

**Settings:**
- Button:
  - Interactable: **checked**
  - Transition: Color Tint
  - Normal Color: RGB(80, 80, 80)
  - Highlighted: RGB(255, 215, 0)
  - Pressed: RGB(200, 170, 0)
  - Disabled: RGB(40, 40, 40)
- Rect Transform:
  - Anchors: Middle Center
  - Pos Y: -350
  - Width: 400, Height: 80

### 5.2 Button Text
```
InitiateButton → Text (TMP) (child)
Назва: InitiateButtonText
```

**Settings:**
- Text: "INITIATE"
- Font Size: 32
- Font Style: Bold
- Color: RGB(255, 215, 0) золотий
- Alignment: Center/Middle

### 5.3 Button Glow Effect
```
InitiateButton → Right Click → UI → Image
Назва: InitiateButtonGlow
```

**Settings:**
- Image: Sprite з radial gradient (білий центр → прозорий)
- Color: RGB(255, 215, 0, 100) золотий напівпрозорий
- Raycast Target: **unchecked**
- Active: **unchecked** (вимкнено за замовчуванням)
- Rect Transform:
  - Anchors: Stretch
  - Offsets: (-50, -50, -50, -50) виходить за межі кнопки

---

## 🔗 Step 6: Assign References у InitiationView

**Виберіть GameObject `InitiationScreen` → Inspector → InitiationView (Script)**

**Assign:**
- **Visual Elements:**
  - Canvas Group: InitiationScreen (Canvas Group component)
  - Logo Image: LogoImage
  - Background Vignette: BackgroundVignette
  - Aura Mist: AuraMist (Particle System)

- **Input Fields:**
  - Phone Input Field: PhoneInputField (TMP_InputField)
  - Phone Label: PhoneLabel (TextMeshProUGUI)
  - Status Text: StatusText (TextMeshProUGUI)

- **Buttons:**
  - Initiate Button: InitiateButton (Button)
  - Initiate Button Text: InitiateButtonText (TextMeshProUGUI)
  - Initiate Button Glow: InitiateButtonGlow (Image)

- **Animation Settings:**
  - Fade In Duration: 1.5
  - Logo Rotation Speed: 5
  - Golden Color: RGB(255, 215, 0, 255)

**Injected Dependencies** (автоматично через VContainer):
- AuthManager ✅
- UIManager ✅
- AudioService ✅

---

## 💾 Step 7: Create Prefab

### 7.1 Save as Prefab
```
Hierarchy → InitiationScreen → Drag до Project
Розташування: Assets/Prefabs/UI/InitiationScreen.prefab
```

### 7.2 Scene Setup (optional)
Якщо ви хочете тестувати InitiationScreen у сцені InitiationScene:
- Залиште екземпляр у сцені
- Або: Instantiate prefab через код/UIManager

---

## 🎯 Step 8: TimeAuraLifetimeScope Setup

### 8.1 Create Global Managers GameObject
```
InitiationScene → Create Empty
Назва: --== MANAGERS ==--
```

Додайте як дочірні об'єкти:
```
--== MANAGERS ==--
  ├─ TimeAuraLifetimeScope (порожній GameObject)
  ├─ GameManager (порожній GameObject)
  ├─ AuthManager (порожній GameObject)
  ├─ UIManager (порожній GameObject)
  ├─ AudioService (порожній GameObject)
  └─ ... (інші менеджери за потреби)
```

### 8.2 Configure TimeAuraLifetimeScope
На GameObject `TimeAuraLifetimeScope`:
1. Add Component → **TimeAuraLifetimeScope** (Scripts/Core/Infrastructure/)
2. Inspector → App Config:
   - **Assign:** Assets/Resources/TimeAuraAppConfig.asset (створений автоматично)
   - Або: створіть власний AppConfig: Assets → Create → Time Aura → App Config

### 8.3 Add Manager Components
На кожному GameObject додайте відповідний компонент:
- `GameManager` → Add Component → GameManager
- `AuthManager` → Add Component → AuthManager
- `UIManager` → Add Component → UIManager
- `AudioService` → Add Component → AudioService

### 💾 8.4 Create Global Managers Prefab (ВАЖЛИВО)
Щоб менеджери були доступні у всіх сценах:
1. Перетягніть об'єкт `--== MANAGERS ==--` з Hierarchy у папку `Assets/Prefabs/Core/`.
2. Тепер це "Глобальний Префаб".
3. Код `TimeAuraLifetimeScope` автоматично робить цей об'єкт `DontDestroyOnLoad`, тому він буде існувати протягом усієї гри.

**⚠️ ПРИМІТКА:** Цей префаб має бути **лише в першій сцені** (InitiationScene). В інших сценах він не потрібен, VContainer сам буде передавати посилання на ці менеджери при переході між сценами.

**⚠️ ВАЖЛИВО для RegisterComponentInHierarchy:** Всі ці компоненти мають бути **в ієрархії префаба**, щоб `RegisterComponentInHierarchy<T>()` міг їх знайти.

---

## 🔊 Step 9: AudioService Setup

### 9.1 Assign Audio Clips
На GameObject `AudioService`:

**Audio Clips:**
- Initiation Sound: (placeholder — Assets/Audio/Initiation_Chime.wav)
- Transformation Ambience: (placeholder)
- Resonance Chime: (placeholder)
- Button Click: (placeholder)
- Aura Hum: (placeholder)

**⚠️ Якщо немає аудіо:**
- Залиште поля пустими — код має null-перевірки
- Або: використайте тимчасові sound effects з Unity Store (безкоштовні пакети)

### 9.2 AudioSource Setup
AudioService автоматично створить AudioSource'и під час Initialize. Якщо хочете налаштувати вручну:
- Music Source: loop = true, volume = 0.7
- SFX Source: loop = false, volume = 0.8
- Ambience Source: loop = true, volume = 0.5

---

## 📦 Step 10: Addressables Setup

### 10.1 Install Addressables
```
Window → Package Manager → Unity Registry → Addressables → Install
```

### 10.2 Initialize Addressables
```
Window → Asset Management → Addressables → Groups
```

Якщо з'явиться Create Settings — натисніть **Create Addressables Settings**.

### 10.3 Mark Scene as Addressable
```
Project → Assets/Scenes/ConvergenceScene.unity → Inspector
☑ Addressable (checkbox)
Address: Assets/Scenes/ConvergenceScene.unity
Group: Default Local Group
```

**⚠️ Примітка:** Якщо ConvergenceScene ще не створена — створіть тимчасову:
```
Assets → Create → Scene
Назва: ConvergenceScene.unity
```

### 10.4 Build Addressables
```
Window → Asset Management → Addressables → Groups
Меню → Build → New Build → Default Build Script
```

---

## ✅ Step 11: Testing Checklist

### 11.1 Scene Test
1. Відкрийте InitiationScene
2. Натисніть Play
3. Перевірте:
   - ✅ Fade-in анімація (1.5 секунди)
   - ✅ Logo обертається повільно
   - ✅ Particles з'являються (золоті крапки)
   - ✅ Input field активний
   - ✅ Кнопка неактивна до введення валідного номера

### 11.2 Input Validation Test
1. Введіть невалідний номер: "123" → кнопка неактивна
2. Введіть валідний: "+1234567890" → кнопка активна
3. Натисніть Initiate → перевірте:
   - ✅ Звучить initiation sound (якщо є audio)
   - ✅ Glow效果 пульсує під кнопкою
   - ✅ Текст змінюється на "CONVERGING..."
   - ✅ Status text оновлюється

### 11.3 AuthManager Integration Test
(Потребує реалізації AuthManager.AuthenticateAsync)
- Якщо AuthManager повертає успішний результат → переходить до Convergence
- Якщо ні → показує помилку

---

## 🐛 Troubleshooting

### Помилка: "VContainerException: TimeAuraLifetimeScope not found"
**Fix:** Упевніться, що GameObject з TimeAuraLifetimeScope є в сцені та активний.

### Помилка: "SecurityHub is not in this scene"
**Fix:** Це warning, не помилка. SecurityHub опціональний — ігноруйте або додайте компонент якщо потрібен.

### Помилка: "InvalidOperationException: Input System"
**Fix:** 
- EventSystem → Remove Standalone Input Module → Add Input System UI Input Module
- Або: Player Settings → Active Input Handling → Both

### Particles не видно
**Fix:** 
- Particle System → Renderer → Material → Default-Particle
- Збільшить Start Size (2-5)
- Перевірте Start Color alpha > 0

### Logo не обертається
**Fix:** 
- Перевірте, що logoImage assigned в Inspector
- Logo Rotation Speed > 0 (рекомендую 5)

### Addressables scene не завантажується
**Fix:**
- Window → Addressables → Groups → Build → New Build
- Перевірте, що сцена Addressable (checkbox в Inspector)
- Перевірте назву/шлях у InitiationView: "Assets/Scenes/ConvergenceScene.unity"

---

## 📚 Next Steps

1. **Create ConvergenceScene** — екран з feed/соціальною мережею
2. **Implement AuthManager.AuthenticateAsync** — інтеграція з Firebase Phone Auth
3. **Create Audio Assets** — записати/знайти містичні звуки
4. **Polish VFX** — додати більше particle effects, bloom, glow shaders
5. **Add Localization** — підтримка української/англійської мов

---

## 🎨 Design Guidelines

**Color Palette:**
- Primary: Golden #FFD700 (RGB 255, 215, 0)
- Background: Pure Black #000000
- Accent: Dark Gray #141414 (для input fields)
- Text: White #FFFFFF / Golden tint

**Typography:**
- Headers: Orbitron Bold / Montserrat Bold
- Body: Roboto Regular / Inter
- Buttons: Uppercase, Letter Spacing +2

**Animation:**
- Fade In/Out: 1-2 seconds
- Pulsing Glow: sin(time * 2) для smooth loop
- Logo Rotation: 5-10 degrees/second

**Sound Design:**
- Initiation: High-pitched crystal bell (1-2 seconds)
- Ambience: Low 40-60Hz drone (looping)
- Button Click: Soft tap (0.1 seconds)

---

## ✨ Conclusion

**"The Gateway stands ready. Through stillness, we find ourselves."**

Екран Ініціації готовий до першого контакту з користувачем. Тепер треба:
- Налаштувати Firebase Phone Auth
- Створити Convergence Scene (Feed UI)
- Додати звуки та полірувати анімації

**Status:** ✅ Ready for Alpha Testing
**Last Updated:** 2026-02-28
