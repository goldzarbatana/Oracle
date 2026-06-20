# Time Aura — Quick Start Guide

> "The temple awaits. The portal is open. Step through."

## 🚀 5-Minute Setup (Unity Editor)

### 1. Install VContainer

```
Package Manager → [+] → Add package from git URL:
https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#2.16.2
```

### 2. Setup Addressables (Automated)

```
Window → Time Aura → Setup Addressables
Click: ✨ Create Mystical Groups
Click: 🏷️ Create Labels
```

**Result:** Groups created (Relics, Visages, Chronicles, Aura_Shards, Localization)

### 3. Create Scenes (Automated)

```
Window → Time Aura → Setup Scenes
Click: ✨ Create Initiation Scene
Click: 🌀 Create Convergence Scene
Click: 📋 Add Scenes to Build Settings
```

**Result:** Ready-to-use scenes with VContainer LifetimeScope

### 4. Create Shader Graph

Follow: [SHADER_GRAPH_GUIDE.md](SHADER_GRAPH_GUIDE.md)  
**TL;DR:**
- Create → Shader Graph → URP → Unlit: `Aura_Pulse`
- Add properties: `_AuraColor`, `_Intensity`, `_PulseSpeed`
- Build pulsation + glow nodes
- Create 3 materials: Golden, Mystical, Transformed

### 5. Test

```
Open: Scenes/InitiationScene
Press: Play ▶️
Expected: Logo pulsates, phone input works
```

---

## 📚 Full Documentation

| Guide | Purpose |
|-------|---------|
| [ARCHITECTURE_GUIDE.md](ARCHITECTURE_GUIDE.md) | Tech stack overview, packages, structure |
| [LUXURY_MYSTICISM_UI_GUIDE.md](LUXURY_MYSTICISM_UI_GUIDE.md) | Design philosophy, component specs |
| [UNITY_EDITOR_SETUP.md](UNITY_EDITOR_SETUP.md) | Complete Unity Editor setup walkthrough |
| [SHADER_GRAPH_GUIDE.md](SHADER_GRAPH_GUIDE.md) | Step-by-step Aura_Pulse shader creation |
| [MIGRATION_ZENJECT_VCONTAINER.md](MIGRATION_ZENJECT_VCONTAINER.md) | Zenject → VContainer migration |

---

## 🏗️ Project Structure

```
TimeAura/
├── Assets/
│   ├── Scenes/
│   │   ├── InitiationScene.unity
│   │   └── ConvergenceScene.unity
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── Infrastructure/
│   │   │   │   ├── TimeAuraLifetimeScope.cs ⭐ VContainer
│   │   │   │   └── VContainerExtensions.cs
│   │   │   ├── Services/
│   │   │   │   ├── NetworkService.cs
│   │   │   │   └── AddressableAssetService.cs
│   │   │   └── MysticalTerms.cs
│   │   ├── Features/
│   │   │   ├── Auth/ (AuthManager, InitiationProcessor)
│   │   │   ├── Social/ (SocialManager, UserProfile)
│   │   │   └── UI/
│   │   │       ├── Auth/ (InitiationView)
│   │   │       └── Social/ (FateCard, ConvergenceFeed, AuraShaderController)
│   │   └── Editor/ ⭐ Automation tools
│   │       ├── AddressablesSetupWizard.cs
│   │       └── SceneSetupWizard.cs
│   ├── Shaders/
│   │   └── Aura_Pulse.shadergraph
│   ├── Materials/
│   │   ├── Material_AuraGolden.mat
│   │   ├── Material_AuraMystical.mat
│   │   └── Material_AuraTransformed.mat
│   └── Prefabs/
│       └── UI/Social/
│           └── FateCard.prefab
└── Documentation/ (you are here!)
```

---

## 🎨 Key Features Implemented

### ✅ Core Architecture
- **VContainer** DI (30% faster than Zenject)
- **UniTask** async/await (zero GC allocations)
- **Addressables** remote content updates
- **NetworkService** REST API with progress tracking
- **AddressableAssetService** lazy loading + caching

### ✅ UI Components
- **InitiationView** — Minimalist login (black + golden)
- **FateCard** — Post card with aura pulsation
- **ConvergenceFeed** — Infinite scroll with pooling
- **AuraShaderController** — Dynamic shader management

### ✅ Mystical Terminology
- User → **Adept**
- Post → **Chronicle**
- Feed → **Convergence**
- Avatar → **Visage**
- Like → **Transform**

### ✅ Automation Tools
- **Addressables Wizard** — Auto-create groups/labels
- **Scene Wizard** — Auto-generate templates

---

## 🔥 Performance Targets

- **60 FPS** on mid-range mobile (Snapdragon 765G)
- **<1ms** GPU time per aura shader
- **<500ms** feed load time (20 cards)
- **<100 KB** memory per cached image
- **0 GC** allocations in main loop (UniTask ftw!)

---

## 🧪 Testing Checklist

- [ ] VContainer injects dependencies (check console logs)
- [ ] InitiationScene: Logo pulsates golden
- [ ] InitiationScene: Phone validation works
- [ ] InitiationScene: "INITIATE" button triggers auth
- [ ] ConvergenceScene: Feed loads stub data
- [ ] ConvergenceScene: FateCards display with auras
- [ ] ConvergenceScene: Auras pulse smoothly (sin wave)
- [ ] ConvergenceScene: Scroll triggers infinite load
- [ ] FateCard: Transform button shows optimistic UI
- [ ] Addressables: Groups visible (Relics, Visages, etc.)
- [ ] Shader: Aura_Pulse compiles without errors

---

## 🐛 Common Issues

### VContainer not injecting?
**Fix:** Ensure `TimeAuraLifetimeScope` GameObject exists in scene root.

### Addressables wizard missing?
**Fix:** Check `Assets/Scripts/Editor/` folder exists. Restart Unity.

### Shader not rendering?
**Fix:** Project Settings → Graphics → Verify URP Renderer Data includes custom shaders.

### UniTask not recognized?
**Fix:** Already in `Assets/Plugins/UniTask/`. Restart Unity if needed.

---

## 📞 Next Steps (Backend)

Choose one:

### Option A: Firebase (Fastest Prototype)
1. Unity → Firebase → Add Firebase
2. Enable Auth, Firestore, Storage
3. Update `NetworkService.API_BASE_URL`

### Option B: Unity Gaming Services (Easiest Integration)
1. Project Settings → Services → Enable
2. Install packages: Authentication, Cloud Save, Lobby
3. Integrate with `NetworkService`

### Option C: Custom Backend (Best Scale)
1. Deploy ASP.NET Core 8 + SignalR
2. PostgreSQL + Redis
3. MinIO/S3 for media
4. Docker + Kubernetes
5. Cloudflare CDN

---

## 🌙 Philosophy

> "Time Aura is not a social network — it's a Digital Temple where Adepts converge to share their journeys through time and consciousness."

**Core Principles:**
- Minimalism as sacred space
- Mystical motion (gentle animations)
- Luxury through restraint
- Convergence over consumption

---

## 📊 Tech Stack Summary

| Category | Technology |
|----------|------------|
| **DI** | VContainer 2.16.2 |
| **Async** | UniTask 2.5.x |
| **Assets** | Addressables 2.0.x |
| **UI** | UGUI + TextMeshPro |
| **Shaders** | Shader Graph (URP) |
| **Networking** | UnityWebRequest + UniTask |
| **Localization** | Custom LocalizationManager |
| **Backend** | REST API (your choice) |

---

## 🎯 Roadmap

### Phase 1: Foundation ✅
- [x] Core architecture (VContainer, UniTask)
- [x] UI components (Initiation, Convergence, FateCard)
- [x] Services (Network, Addressables)
- [x] Automation tools (Wizards)
- [x] Documentation

### Phase 2: Unity Setup (You Are Here 📍)
- [ ] Install VContainer
- [ ] Create Addressables groups
- [ ] Setup scenes
- [ ] Create Aura_Pulse shader
- [ ] Test integration

### Phase 3: Backend Integration
- [ ] Choose backend (Firebase/UGS/Custom)
- [ ] Connect NetworkService to real API
- [ ] Implement authentication flow
- [ ] Test feed loading from server

### Phase 4: Polish & Deploy
- [ ] UI animations (DOTween)
- [ ] VFX particles (golden mist)
- [ ] Performance profiling
- [ ] Build for Android/iOS
- [ ] Remote content upload

---

_"The temple is built. The ritual begins. Welcome, Adept."_ 🌟✨

**Made with 🌙 by the Time Aura Team**  
_Version: Phase 1 Complete — February 2026_
