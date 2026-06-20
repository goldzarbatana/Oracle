# Phase 3: The Flow - Implementation Guide

## 🌊 Overview
Phase 3 реалізує серце Time Aura — **активний обмін Vectors між Адептами** через систему **Трансформації** та **Резонансу**.

---

## ✨ Implemented Features

### 1. **Transformation System**
Керує активними сесіями обміну часом між двома користувачами.

#### Core Components:
- **TransformationManager** (`Features/Transformation/TransformationManager.cs`)
  - Ініціює та завершує Transformation сесії
  - Відстежує прогрес в реальному часі
  - Інтегрується з Firebase для збереження даних

- **TransformationSession Model** (`Features/Transformation/Models/TransformationSession.cs`)
  - Статус сесії (Active, Completed, Cancelled)
  - Таймер та прогрес
  - Кількість обмінюваних Vectors

#### Usage Example:
```csharp
// Start a transformation
var session = await transformationManager.StartTransformationAsync(
    recipientUserId: "user_123",
    vectorsToExchange: 50
);

// Complete with resonance
await transformationManager.CompleteTransformationAsync(
    ResonanceLevel.Synchronized
);
```

---

### 2. **Resonance System**
Система оцінювання синхронності обміну.

#### Resonance Levels:
1. **Dissonant** (Дисонанс) - 0.5x multiplier
2. **Neutral** (Нейтральний) - 1.0x multiplier
3. **Harmonious** (Гармонійний) - 1.2x multiplier
4. **Synchronized** (Синхронізований) - 1.5x multiplier
5. **Transcendent** (Трансцендентний) - 2.0x multiplier

#### Auto-calculation:
```csharp
var resonance = ResonanceSystem.CalculateResonance(session);
// Considers: duration, vectors exchanged, time of day
```

#### Rewards:
- **Vectors Received** = base * resonance multiplier
- **Experience Points** = (vectors / 2) * multiplier
- **Status Promotion** based on average resonance

---

### 3. **Audio Service**
Orchestrates mystical sounds throughout the app.

#### Key Methods:
```csharp
audioService.PlayInitiationSound();          // Crystal chime on initiation
audioService.PlayTransformationAmbience();   // Low-frequency hum during session
audioService.PlayResonanceChime(level);      // Pitch varies by resonance
audioService.PlayButtonClick();              // UI feedback
```

#### Sound Design Vision:
- **Initiation**: Crystalline bell (high-pitched, pure)
- **Transformation**: Deep 40-60Hz hum (grounding, meditative)
- **Resonance**: Harmonic overtones based on level
- **Background**: Ambient drone with subtle modulation

---

### 4. **Active Session UI**
Visual representation of live Transformation.

#### Features (`Features/UI/Transformation/ActiveSessionView.cs`):
- **Live Timer**: Real-time countdown/countup
- **Progress Bar**: Visual progress indicator
- **Golden Connection Line**: LineRenderer connecting two auras (animated pulse)
- **Vector Flow Particles**: ParticleSystem showing energy transfer
- **End Ritual Button**: Completes transformation
- **Cancel Button**: Aborts session

#### VFX:
- Golden connection line pulses with `0.05 + sin(time * 2) * 0.05` width
- Color intensity animates: `baseColor * (1 + sin(time * 1.5) * 0.3)`

---

### 5. **Resonance Selection View**
UI for choosing resonance level after transformation.

#### Features (`Features/UI/Transformation/ResonanceSelectionView.cs`):
- 5 buttons, each representing a resonance level
- Color-coded by `ResonanceSystem.GetResonanceColor()`
- Auto-suggests level based on session metrics
- Plays particle effect on selection

---

### 6. **Privacy & Private Mode**
Allows users to hide from public discovery.

#### Privacy Settings (`Features/Social/PrivacySettings.cs`):
- **Private Mode**: Hides from Convergence feed
- **Visibility Levels**:
  - `Public`: Visible to all
  - `GuardiansOnly`: Visible only to trusted friends
  - `Private`: Hidden from everyone
- **Guardians**: List of trusted user IDs

#### Usage:
```csharp
await socialManager.EnablePrivateModeAsync();
// User is now hidden from public view, visible only to Guardians

await socialManager.DisablePrivateModeAsync();
// User is now public again
```

---

### 7. **Firebase Cloud Functions**
Secure server-side operations to prevent client manipulation.

#### Functions (`Firebase/functions/index.js`):

##### `updateVectors` (Callable HTTPS)
Safely updates user vectors after transformation completes.
- Validates session integrity
- Uses Firestore transactions for atomicity
- Applies resonance multipliers server-side
- Awards XP based on performance

##### `createTransformation` (Callable HTTPS)
Creates new transformation session.
- Checks vector balance
- Creates session document
- Sends push notification to recipient

##### `updateResonance` (Firestore Trigger)
Auto-updates user resonance stats.
- Calculates average resonance
- Checks for status promotion (Neophyte → Adept → Master)
- Updates user profile

#### Deployment:
```bash
cd Firebase/functions
npm install
npm run deploy
```

---

## 🎨 Visual Hierarchy: "Кільця Резонансу"

### Status Rings (proposed visual design):

#### Neophyte
- **1 thin golden ring** around avatar
- Slow rotation (10s per revolution)
- Faint glow (bloom intensity: 0.3)

#### Adept
- **2 rings** counter-rotating
- Inner ring: clockwise (8s)
- Outer ring: counter-clockwise (12s)
- Moderate glow (bloom: 0.6)

#### Master
- **Sacred geometry** (Metatron's Cube or Mandala)
- Complex animation with `sin/cos` based pulsing
- Shifts color based on Lumen/Forma alignment
- Strong glow (bloom: 1.0) with HDR emission

---

## 🔧 Integration Checklist

### Unity Setup:
1. ✅ Add `TransformationManager` component to scene
2. ✅ Add `AudioService` component to scene
3. ✅ Register both in `TimeAuraLifetimeScope`
4. ⚠️ Create UI prefabs:
   - `ActiveSessionView.prefab`
   - `ResonanceSelectionView.prefab`
5. ⚠️ Assign audio clips in `AudioService` Inspector
6. ⚠️ Setup LineRenderer for golden connection
7. ⚠️ Create ParticleSystem for vector flow

### Firebase Setup:
1. ⚠️ Deploy Cloud Functions (`npm run deploy`)
2. ⚠️ Update Firestore security rules
3. ⚠️ Setup FCM for push notifications
4. ⚠️ Test functions locally with emulator

### Testing:
1. ⚠️ Test transformation flow end-to-end
2. ⚠️ Verify vector calculations on server
3. ⚠️ Test resonance multipliers
4. ⚠️ Test private mode visibility
5. ⚠️ Test audio transitions

---

## 🚀 Next Steps (Phase 4+)

1. **Aura Visualization Enhancements**
   - Implement status rings with Shader Graph
   - Sacred geometry for Masters
   - Real-time color shifts based on alignment

2. **Matchmaking Refinements**
   - Consider privacy settings in matching algorithm
   - Add "Guardians-only" matching pool

3. **Social Features**
   - Guardian management UI
   - Group transformations (3+ users)
   - Transformation history timeline

4. **Analytics**
   - Track average session duration
   - Resonance distribution heatmaps
   - User progression funnel

---

## 📝 Notes

- All TODO comments in code mark integration points with Firebase/Addressables
- VFX placeholder values should be tuned with real assets
- Audio clips need to be created/sourced and assigned
- UI layouts are code-based; create visual designs in Unity Editor

---

**"Through transformation, we transcend time. Through resonance, we echo across souls."**
