# Time Aura - Phase 1: Foundation Setup Guide

## What Was Implemented

### 1. Core Architecture (Luxury Tech Terminology)
- **AspectType** enum: Lumen, Forma, Action, Essence
- **UserProfile** model with:
  - `Vectors` (float) - Time currency
  - `Status` (int) - Trust/reputation
  - `Aspects` (Dictionary) - User's skill categories
  - `Initiation` flag - First-time setup completion
- **AppConfig** with luxury palette and initial settings (300 Vectors)

### 2. Managers & Processors
- **InitiationProcessor** - First-time user onboarding and Aspect selection
- **UIManager** - Screen stack management
- **MatchmakingManager** - Convergence (finding partners for Transformation)
- **AuthManager** - Updated to use new Vector terminology
- **GlobalManager** - Centralized component container for Inspector configuration

### 3. UI Toolkit Components
- **LuxuryTech.uss** - Glassmorphism styles with gold (#D4AF37) and dark (#0A0A0A) palette
- **FateCard.uxml** - Card template for Convergence stream
- **FateCardController** - C# controller for card logic and interactions

### 4. Zenject Integration
- All managers registered in `TimeAuraInstaller`
- Dependency injection for clean separation of concerns
- `GlobalManager` prefab approach for Inspector-based configuration

## Quick Start in Unity

### Step 1: Create AppConfig
1. In Unity: `Assets > Create > TimeAura > App Config`
2. Save as `Assets/Resources/AppConfig.asset`
3. Configure:
   - Initial Vectors: 300
   - Transformation Min Vectors: 60
   - Aspect Colors (default luxury palette already set)

### Step 2: Setup GlobalManager GameObject
1. Create empty GameObject: `GameObject > Create Empty`
2. Name it `GlobalManager`
3. Add component: `GlobalManager` (Assets/Scripts/Core/GlobalManager.cs)
4. In Inspector, all manager components will be auto-added on Play or via context menu "Ensure Managers Exist"
5. Configure managers in Inspector as needed (optional - defaults work for MVP)

### Step 3: Configure ProjectContext
1. In scene: `GameObject > Zenject > Project Context`
2. Add Installer: `TimeAuraInstaller`
3. In Inspector:
   - Assign `GlobalManager` instance to the installer's `globalManager` field
   - Assign `AppConfig` asset (or leave empty - will load from Resources)

### Step 4: Test Run
1. Press Play
2. Check Console for:
   - `[GlobalManager] All inspector bindings present.`
   - `[InitiationProcessor] Initiation completed...`
   - `[BootstrapValidator] All critical DI bindings are present.` (if added)

## Folder Structure
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── AspectType.cs
│   │   ├── AppConfig.cs
│   │   ├── EventBus.cs
│   │   ├── GameManager.cs
│   │   ├── GlobalManager.cs
│   │   ├── BootstrapValidator.cs
│   │   └── Zenject/
│   │       └── TimeAuraInstaller.cs
│   └── Features/
│       ├── Auth/
│       │   ├── AuthManager.cs
│       │   ├── InitiationProcessor.cs
│       │   └── UserProfile.cs
│       ├── Data/
│       │   ├── IDataService.cs
│       │   └── FirebaseDataService.cs
│       ├── UI/
│       │   ├── UIManager.cs
│       │   └── FateCardController.cs
│       ├── Matching/
│       │   └── MatchmakingManager.cs
│       ├── Aura/
│       │   └── AuraEffectManager.cs
│       ├── Security/
│       │   ├── SecurityHub.cs
│       │   ├── ISmsGateway.cs
│       │   └── TwilioSmsGateway.cs
│       └── Localization/
│           └── LocalizationManager.cs
└── UI/
    ├── Styles/
    │   └── LuxuryTech.uss
    └── Templates/
        └── FateCard.uxml
```

## Next Steps (Phase 2)

1. **Implement Convergence Screen**
   - Create main UI with Vector counter
   - Implement card swipe mechanics (DOTween animations)
   - Connect FateCardController to MatchmakingManager

2. **Firebase Integration**
   - Replace stub implementations in FirebaseDataService
   - Implement real phone auth flow in AuthManager
   - Setup Firestore collections: users, transformations

3. **Shader Graph Aura Effect**
   - Create animated aura shader
   - Bind to AuraEffectManager
   - Dynamic color based on user's primary Aspect

4. **UniTask Integration**
   - Replace Task with UniTask for Firebase calls
   - Add cancellation token support throughout

## Glossary Reference

| Standard Term | Time Aura Term | Type |
|--------------|----------------|------|
| Token/Currency | Vector | float |
| Rating/Trust | Status | int |
| Registration | Initiation | process |
| Exchange/Session | Transformation | session |
| Categories | Aspects (Lumen/Forma/Action/Essence) | enum |

## Support

All core systems use fail-fast validation (throw if dependencies missing). Check Console for detailed error messages during development.

For UI Toolkit styles, see `Assets/UI/Styles/LuxuryTech.uss` for luxury glassmorphism design patterns.
