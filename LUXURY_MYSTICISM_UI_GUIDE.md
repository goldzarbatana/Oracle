# Time Aura — Luxury Mysticism UI Guide

> "Through stillness, we find ourselves. Through connection, we transform."

## 🌟 Design Philosophy

**Time Aura** is not a social network — it's a **Digital Temple** where Adepts converge to share their journeys through time and consciousness. Every UI element reflects this mystical luxury.

### Core Principles

1. **Minimalism as Sacred Space** — Black void with golden accents
2. **Mystical Motion** — Gentle pulsations, never aggressive animations
3. **Luxury Through Restraint** — Premium feel through spacing, typography, and subtle effects
4. **Convergence Over Consumption** — Quality connections, not endless scroll addiction

---

## 🎨 Color Palette (The Sacred Spectrum)

### Primary Colors

```csharp
// Deep Void — The canvas of infinite possibilities
Color deepVoid = new Color(0.05f, 0.05f, 0.05f, 1f); // #0D0D0D

// Golden Aura — Enlightenment, luxury, premium
Color goldenAura = new Color(1f, 0.84f, 0f, 1f); // #FFD700

// Mystical Purple — Magic, transformation, higher consciousness
Color mysticalPurple = new Color(0.5f, 0.2f, 0.8f, 1f); // #8033CC

// Transformation Cyan — Active energy flow, connection
Color transformCyan = new Color(0f, 1f, 0.8f, 1f); // #00FFCC
```

### Semantic Colors

```csharp
// Status states
Color convergenceActive = new Color(0f, 1f, 0.8f, 0.8f); // Active transformation
Color awarenessGlow = new Color(1f, 0.92f, 0.5f, 0.3f); // Subtle awareness glow
Color shadowMist = new Color(0f, 0f, 0f, 0.5f); // Background depth
```

---

## 🖼️ Component Library

### 1. InitiationView (Login/Register)

**Purpose:** The gateway for new Adepts

**Visual Structure:**
- Black background (`deepVoid`)
- Centered golden Time Aura logo (rotating gently at 5°/sec)
- Single input field: "Your sacred number" (phone)
- Button: "INITIATE" with golden glow on hover/loading

**Animation Flow:**
1. Screen fades in over 1.5s
2. Logo pulsates subtly (0.7 → 1.0 alpha, 2s cycle)
3. On "INITIATE" click:
   - Golden aura activates around button (Optimistic UI)
   - Text changes to "OPENING THE PORTAL..."
   - Success → fade to ConvergenceFeed
   - Failure → aura fades, show error, restore button

**Code Reference:** [InitiationView.cs](Assets/Scripts/Features/UI/Auth/InitiationView.cs)

---

### 2. FateCard (Post Card)

**Purpose:** A mystical representation of an Adept's shared moment

**Visual Structure:**
```
┌─────────────────────────────────────┐
│  👤 Avatar (Visage)  │  Username     │
│     with golden      │  "Status"     │
│     border           │  Vector: $X/hr│
├─────────────────────────────────────┤
│  Post content text                   │
│  [Optional image if present]         │
├─────────────────────────────────────┤
│  🌟 123K Transforms  💬 4.5K Conn.   │
│  [START TRANSFORMATION] [CONNECT]    │
└─────────────────────────────────────┘
```

**Key Features:**
- **Avatar Border:** Golden ring with pulsating aura shader
- **Aura Glow:** Particle effect around avatar, intensity based on scroll position
- **Status Text:** User's mystical status (bio), italicized in golden color
- **Vector Display:** Time value ($/hour) — shows adept's convergence rate
- **Optimistic UI:** "Start Transformation" button glows immediately on click

**Animation Details:**
- Aura pulsation: `sin(time * 1.0) * 0.3 amplitude`
- On scroll center: Max intensity (1.5x)
- On scroll edge: Min intensity (0.5x)
- On "Transform" click: Golden burst (3x speed for 0.5s)

**Code Reference:** [FateCard.cs](Assets/Scripts/Features/UI/Social/FateCard.cs)

---

### 3. ConvergenceFeed (Main Feed)

**Purpose:** The central stream where all Adepts' fates converge

**Visual Structure:**
- **Header:** Status text ("123 fates revealed...") in golden
- **Refresh Button:** Golden circular orb (top-left or pull-down)
- **ScrollRect:** Vertical infinite scroll
- **Loading Orb:** Animated golden sphere with particles

**Scroll Behavior:**
- **Infinite Scroll:** Preload at 75% scroll position
- **Card Visibility Tracking:** Cards near viewport center get stronger auras
- **Object Pooling:** Reuse 30+ FateCard instances for performance

**Performance:**
- Target: 60 FPS with 1000+ cards loaded
- Pooling: Max 30 active cards rendered at once
- Addressables: Lazy-load images/avatars asynchronously

**Code Reference:** [ConvergenceFeed.cs](Assets/Scripts/Features/UI/Social/ConvergenceFeed.cs)

---

### 4. AuraShaderController

**Purpose:** Dynamic shader management for mystical pulsation effects

**Shader Properties (Shader Graph):**
```hlsl
_Intensity (float, 0-2): Glow strength
_AuraColor (Color): Base aura color
_PulseSpeed (float): Animation speed
_GlowRadius (float): Spread distance
```

**Themes:**
- **Golden Aura:** Default state (#FFD700)
- **Mystical Aura:** Special state (#8033CC, purple)
- **Transformed Aura:** Active transformation (#00FFCC, cyan)

**Dynamic Behavior:**
- Continuous pulsation: `sin(time * speed) * amplitude`
- Scroll-based intensity: Cards near center = brighter
- Burst effect: Spike on user interaction (transform, like)

**Code Reference:** [AuraShaderController.cs](Assets/Scripts/Features/UI/Social/AuraShaderController.cs)

---

## 🎭 Animation Principles

### 1. Mystical Motion (Easing)
- Use smooth curves: `Ease.OutQuad`, `Ease.InOutSine`
- Avoid linear motion — nothing in nature moves linearly
- Preferred library: **DOTween Pro** (already in project)

### 2. Timing Harmony
- **Short interactions:** 0.2-0.3s (button press, toggle)
- **Medium transitions:** 0.5-0.8s (card appear, scroll snap)
- **Long rituals:** 1.5-2.0s (screen fade, loading states)

### 3. Golden Ratio in Spacing
- Use Fibonacci sequence for margins: 8px, 13px, 21px, 34px
- Card padding: 21px
- Inter-card spacing: 13px
- Screen margins: 34px

---

## 🌀 Shader Effects Guide

### Required Shader Graph Effects

#### 1. **Aura Pulse Shader**
- **Input:** Base Texture, Aura Color, Intensity, Time
- **Output:** Glowing border with pulsation
- **Effect:** Sine wave based on `_Time.y * _PulseSpeed`

#### 2. **Golden Mist Particles**
- **System:** Particle System with `Soft Additive` blend
- **Color:** Golden gradient (bright → transparent)
- **Movement:** Slow upward drift (0.1 units/sec)
- **Spawn rate:** 10-20 particles/sec

#### 3. **Vignette Darkness**
- **Shader:** Post-processing vignette
- **Settings:** Center = 0.7, Smoothness = 0.5, Intensity = 0.35
- **Purpose:** Focus attention on center content

---

## 📱 Responsive Layout (Multi-Device)

### Screen Breakpoints

```csharp
// Detect screen category
public enum ScreenCategory
{
    Phone,     // < 600 logical width
    Tablet,    // 600-1024
    Desktop    // > 1024
}

// Adjust UI dynamically
public void AdaptToScreen(ScreenCategory category)
{
    switch (category)
    {
        case ScreenCategory.Phone:
            cardWidth = Screen.width - 32f; // Full width minus margins
            fontSize = 14f;
            break;
        case ScreenCategory.Tablet:
            cardWidth = 400f; // Fixed width
            fontSize = 16f;
            break;
        case ScreenCategory.Desktop:
            cardWidth = 500f;
            fontSize = 18f;
            break;
    }
}
```

---

## 🎯 Optimistic UI Patterns

### Pattern 1: Like/Transform Button

```csharp
// 1. Update UI immediately (optimistic state)
post.isLiked = true;
post.likesCount++;
UpdateUI();
ShowGoldenGlow();

// 2. Send request to server
var success = await SocialManager.ToggleLikeAsync(postId, true);

// 3. Rollback on failure
if (!success)
{
    post.isLiked = false;
    post.likesCount--;
    UpdateUI();
    HideGoldenGlow();
    ShowError("Connection lost");
}
```

### Pattern 2: Post Creation

```csharp
// 1. Show post immediately with "Converging..." overlay
var tempPost = CreateTempPost(content, image);
feedView.AddPostOptimistically(tempPost);

// 2. Upload in background
var uploadedPost = await SocialManager.CreatePostAsync(content, image);

// 3. Replace temp with real post (with ID)
feedView.ReplacePost(tempPost, uploadedPost);
```

---

## 🔮 Mystical Terminology Guide

Use these terms consistently in UI text:

| Standard Term | Time Aura Term | Context |
|---------------|----------------|---------|
| User | **Adept** | A member of the community |
| Avatar | **Visage** | Profile picture |
| Post | **Chronicle** | Shared moment/story |
| Feed | **Convergence** | Main timeline |
| Like | **Transform** | Resonance with content |
| Comment | **Connection** | Engaging with chronicle |
| Login | **Initiation** | Entering the temple |
| Profile | **Aura** | User's essence/identity |
| Time Value | **Vector** | $/hour rate |
| Status | **State** | Current consciousness level |

---

## 🛠️ Unity Implementation Checklist

### Scene Setup

1. **InitiationScene** (Login)
   - [ ] Canvas with black background
   - [ ] Golden logo (transparent PNG, 512x512)
   - [ ] Phone input field (TextMeshPro)
   - [ ] "INITIATE" button with glow Image component
   - [ ] Particle system (golden mist)

2. **ConvergenceScene** (Main Feed)
   - [ ] ScrollRect with ConvergenceFeed component
   - [ ] FateCard prefab in Resources or Addressables
   - [ ] Loading orb prefab (animated golden sphere)
   - [ ] Status text (top bar)

### Addressables Setup

Create these groups in Unity Editor:
- **`Relics`** (UI icons, buttons)
- **`Visages`** (default avatars)
- **`Chronicles`** (post templates)
- **`Aura_Shards`** (shader effects, particles)

### Shader Graph Creation

1. Create new Shader Graph: **Aura_Pulse**
   - Unlit shader (for UI or world space)
   - Properties: `_Intensity`, `_AuraColor`, `_PulseSpeed`
   - Sample base texture
   - Add glow based on UV distance from edge
   - Multiply by pulsating sine wave

2. Create material from shader
3. Assign to `AuraShaderController.auraMaterial`

---

## 🧘 Philosophical Notes for Developers

> "Every pixel is a meditation. Every animation is a breath."

- **Avoid notification spam** — respect the Adept's peace
- **No dark patterns** — honor free will and conscious choice
- **Quality over virality** — depth, not addiction
- **Privacy as sacred** — data minimization, local-first when possible

**Time Aura is not about stealing attention — it's about gifting presence.**

---

## 📚 Further Reading

- [Unity Addressables Best Practices](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [UniTask Performance Guide](https://github.com/Cysharp/UniTask)
- [Shader Graph for UI Effects](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest)
- [Object Pooling in Unity 2026](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity7.html)

---

**Built with 🌙 by the Time Aura Team**  
*Version: Phase 1 — The Initiation (February 2026)*
