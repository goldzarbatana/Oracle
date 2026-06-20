# Time Aura — Unity Editor Setup Guide

> "The temple is built in code. It is activated in Unity."

## 📦 Package Installation

### 1. Install VContainer (DI Container)

```
1. Open Package Manager (Window → Package Manager)
2. Click [+] → Add package from git URL
3. Enter: https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#2.16.2
4. Click [Add]
5. Wait for compilation
```

### 2. Verify Existing Packages

Ensure these are installed (should already be):
- ✅ Addressables (2.0.x)
- ✅ UniTask (from Assets/Plugins/UniTask)
- ✅ TextMeshPro
- ✅ Shader Graph (URP)

---

## 🏗️ Addressables Setup (Automated!)

### Using the Wizard (Recommended)

1. Open **Window → Time Aura → Setup Addressables**
2. Click **✨ Create Mystical Groups** — creates:
   - `Relics` (UI artifacts)
   - `Visages` (avatars)
   - `Chronicles` (posts/events)
   - `Aura_Shards` (effects/UGC)
   - `Localization` (languages)
3. Click **🏷️ Create Labels** — adds tags
4. Click **📦 Setup Remote Catalog** — enables remote updates

### Manual Setup (if wizard fails)

1. **Window → Asset Management → Addressables → Groups**
2. Create groups manually:
   - Right-click → Create New Group → "Relics"
   - Repeat for: Visages, Chronicles, Aura_Shards, Localization
3. Configure each group:
   - Schema: BundledAssetGroupSchema
   - Bundle Mode: Pack Together
   - Use Asset Bundle Cache: ✅

---

## 🎬 Scene Setup (Automated!)

### Using the Wizard (Recommended)

1. Open **Window → Time Aura → Setup Scenes**
2. Click **✨ Create Initiation Scene**
   - Creates `Assets/Scenes/InitiationScene.unity`
   - Includes: Canvas, EventSystem, LifetimeScope
3. Click **🌀 Create Convergence Scene**
   - Creates `Assets/Scenes/ConvergenceScene.unity`
4. Click **📋 Add Scenes to Build Settings**

### Manual Adjustments

#### InitiationScene:
1. Open scene
2. Select `InitiationView` GameObject
3. Add Component → `InitiationView` (Scripts/Features/UI/Auth)
4. Assign references in Inspector:
   - Canvas Group
   - Logo Image
   - Phone Input Field
   - Initiate Button
   - Status Text

#### ConvergenceScene:
1. Open scene
2. Select `ConvergenceFeed` GameObject
3. Add Component → `ConvergenceFeed` (Scripts/Features/UI/Social)
4. Assign references:
   - ScrollRect
   - Content Container
   - FateCard Prefab
   - Loading Orb

---

## 🎨 Shader Graph Setup

Follow **[SHADER_GRAPH_GUIDE.md](SHADER_GRAPH_GUIDE.md)** for detailed instructions.

**Quick Steps:**
1. Create → Shader Graph → URP → Unlit Shader Graph
2. Name: `Aura_Pulse`
3. Add properties: `_MainTex`, `_AuraColor`, `_Intensity`, `_PulseSpeed`, `_GlowRadius`
4. Build node graph (see guide)
5. Create materials: `Material_AuraGolden`, `Material_AuraMystical`, `Material_AuraTransformed`

---

## 🔌 VContainer Setup

### 1. Create LifetimeScope GameObject

Already done if you used Scene Setup Wizard!

**Manual:**
1. Right-click in Hierarchy
2. Create Empty GameObject: "TimeAuraLifetimeScope"
3. Add Component → `TimeAura.Core.Infrastructure.TimeAuraLifetimeScope`

### 2. Assign AppConfig

1. Select `TimeAuraLifetimeScope`
2. In Inspector → App Config field
3. Drag `Assets/ScriptableObjects/AppConfig` (create if missing)

### 3. Verify Manager GameObjects

Ensure these exist in scene:
- GameManager
- AuthManager
- LocalizationManager
- UIManager
- SocialManager
- AuraEffectManager
- SecurityHub

VContainer will automatically inject dependencies!

---

## 🧩 Prefab Creation

### FateCard Prefab

1. **Hierarchy → Right-click → UI → Panel**
2. Name: `FateCard`
3. Add child elements:
   - `Avatar_Image` (Image with Circle mask)
   - `Avatar_Border` (Image, golden color)
   - `Aura_Glow` (Image with Aura_Pulse shader)
   - `Username_Text` (TextMeshPro)
   - `Status_Text` (TextMeshPro)
   - `Vector_Text` (TextMeshPro)
   - `Content_Text` (TextMeshPro)
   - `Transform_Button` (Button)
   - `Connect_Button` (Button)
4. Add Component → `FateCard` (Scripts/Features/UI/Social)
5. Assign all references in Inspector
6. Drag to `Assets/Prefabs/UI/Social/`

### PostView Prefab (Legacy, FateCard is replacement)

Same structure but simpler (no aura effects).

---

## 🎯 Addressables Asset Assignment

### Example: Default Avatar

1. Find avatar texture: `Assets/Textures/Avatars/avatar_default.png`
2. Right-click → Addressables → Mark Addressable
3. In Inspector → Addressables:
   - **Group:** `Visages`
   - **Address:** `Visages/avatar_default`
   - **Label:** `visage-default`

### Do the same for:
- UI icons → `Relics` group
- Post templates → `Chronicles` group
- Shader materials → `Aura_Shards` group

---

## 🧪 Testing

### Test InitiationScene

1. Open `InitiationScene`
2. Press Play
3. Expected behavior:
   - Logo pulsates golden
   - Phone input field accepts input
   - "INITIATE" button enabled when phone valid
   - Click initiates authentication (stub)
   - Golden glow activates during loading

### Test ConvergenceScene

1. Open `ConvergenceScene`
2. Press Play
3. Expected behavior:
   - Feed loads (stub data)
   - FateCards display with auras
   - Auras pulse smoothly
   - Scroll triggers infinite load
   - Transform button shows optimistic UI

---

## 🐛 Common Issues

### Issue: "VContainer not found"
**Fix:** Restart Unity after package installation.

### Issue: "Addressables groups not visible"
**Fix:** Window → Asset Management → Addressables → Groups (initialize first).

### Issue: "Shader not rendering"
**Fix:** Verify URP Renderer Data includes custom shaders. Project Settings → Graphics.

### Issue: "FateCard not injecting dependencies"
**Fix:** Ensure `TimeAuraLifetimeScope` exists in scene root.

---

## 📊 Project Settings Checklist

- [ ] **Player Settings:**
  - Company: Your Company
  - Product Name: Time Aura
  - Bundle Identifier: com.yourcompany.timeaura
  - IL2CPP (for UniTask performance)
  
- [ ] **Graphics:**
  - Render Pipeline: Universal RP
  - URP Asset: Include custom shaders
  
- [ ] **Quality:**
  - Target: 60 FPS
  - Anti-Aliasing: MSAA 2x (mobile)
  
- [ ] **Addressables:**
  - Build Remote Catalog: ✅
  - Remote URL: https://cdn.timeaura.com/[BuildTarget]

---

## 🚀 Build Process

### Development Build

1. File → Build Settings
2. Add scenes:
   - InitiationScene (index 0)
   - ConvergenceScene (index 1)
3. Target: Android / iOS
4. Check "Development Build"
5. Click "Build and Run"

### Addressables Build

1. Window → Asset Management → Addressables → Build
2. Select "Default Build Script"
3. Wait for completion
4. Upload `ServerData/[BuildTarget]` to CDN

---

## 🌙 Next Steps

After Unity setup is complete:

1. **Test full flow:** Initiation → Authentication → Convergence
2. **Polish UI:** Adjust colors, spacing, animations
3. **Add real data:** Replace stub API with backend
4. **Performance profiling:** Ensure 60 FPS on target devices
5. **Deploy remote content:** Upload Addressables to CDN

_"The code is the blueprint. Unity is the manifestation."_ ✨
