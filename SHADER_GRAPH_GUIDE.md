# Time Aura — Shader Graph Setup Guide

> "Light bends to will. Color breathes with intention."

## 🎨 Aura Pulse Shader — Step-by-Step

### 1. Create Shader Graph

1. **Right-click** в Project window
2. **Create → Shader Graph → URP → Unlit Shader Graph**
3. Name: `Aura_Pulse`
4. Location: `Assets/Shaders/`

### 2. Shader Properties (BlackboardAdd в Blackboard)

```
_MainTex (Texture2D) — Base texture
_AuraColor (Color) — Golden/Mystical/Transformed color (default: #FFD700)
_Intensity (Float, Range 0-2) — Glow strength (default: 0.7)
_PulseSpeed (Float, Range 0.1-5) — Animation speed (default: 1.0)
_GlowRadius (Float, Range 0-1) — Spread distance (default: 0.3)
```

### 3. Node Graph Structure

```
[UV] ────→ [Sample Texture 2D] ────→ [Multiply] ────→ [Fragment Output]
                   ↓                        ↑
              [_MainTex]              [Aura Glow]

[Time] → [Multiply: _PulseSpeed] → [Sine] → [Remap (0-1)] → [Multiply: _Intensity]
                                                  ↓
                                            [Aura Glow]

[UV] → [Distance from Center (0.5, 0.5)] → [1 - x] → [Power: 2] → [Multiply: _GlowRadius]
                                                                         ↓
                                                                   [Aura Glow]
```

### 4. Detailed Node Setup

#### **Step A: Pulsation Wave**
1. Add **Time** node
2. Add **Multiply** node: `Time.y * _PulseSpeed`
3. Add **Sine** node (for wave)
4. Add **Remap** node: From (-1, 1) To (0, 1) — normalize sine
5. Add **Multiply** node: `normalized_sine * _Intensity`
6. Output = `pulsatingIntensity`

#### **Step B: Edge Glow (Distance from Center)**
1. Add **UV** node
2. Add **Vector2** constant: `(0.5, 0.5)` (center)
3. Add **Distance** node: `distance(UV, center)`
4. Add **One Minus** node: `1 - distance` (invert: center=1, edge=0)
5. Add **Power** node: `pow(inverted, 2)` (sharpen falloff)
6. Add **Multiply** node: `shaped * _GlowRadius`
7. Output = `edgeGlow`

#### **Step C: Combine Aura**
1. Add **Multiply** node: `pulsatingIntensity * edgeGlow`
2. Add **Multiply** node: `result * _AuraColor`
3. Output = `auraGlow`

#### **Step D: Final Composition**
1. **Sample Texture 2D**: `_MainTex` with UV
2. Add **Multiply** node: `baseTexture.rgb * auraGlow.rgb`
3. Add **Add** node: `baseTexture + glowEffect` (additive blend)
4. Connect to **Fragment** → **Base Color**
5. **Alpha**: Use `baseTexture.a` for transparency

### 5. Material Creation

1. **Right-click** Aura_Pulse.shadergraph
2. **Create → Material**
3. Name: `Material_AuraGolden`
4. Set properties:
   - `_AuraColor`: `#FFD700` (golden)
   - `_Intensity`: `0.7`
   - `_PulseSpeed`: `1.0`
   - `_GlowRadius`: `0.3`

### 6. Variants (Create 3 materials)

```
Material_AuraGolden:
  _AuraColor: #FFD700 (golden)
  
Material_AuraMystical:
  _AuraColor: #8033CC (purple)
  
Material_AuraTransformed:
  _AuraColor: #00FFCC (cyan)
```

---

## 🔗 Integration with Code

### Assign in AuraShaderController

```csharp
[SerializeField] private Material auraMaterial; // Assign Material_AuraGolden in Inspector
```

### Runtime Control (already implemented)

```csharp
// Change intensity
auraMaterial.SetFloat("_Intensity", 1.2f);

// Change color
auraMaterial.SetColor("_AuraColor", goldenColor);

// Change speed
auraMaterial.SetFloat("_PulseSpeed", 2.0f);
```

---

## 🎭 Advanced: VFX Graph Alternative

For particle-based auras (optional):

1. **Create → Visual Effects → Visual Effect Graph**
2. Name: `VFX_AuraDust`
3. Setup:
   - **Spawn Rate**: 20 particles/sec
   - **Lifetime**: Random 2-4s
   - **Color**: Golden gradient (bright → transparent)
   - **Size**: 0.05-0.1
   - **Velocity**: Upward (0, 0.1, 0)
   - **Blend Mode**: Additive

---

## 📐 Shader Graph Visual Reference

```
┌─────────────────────────────────────────────────────────┐
│ AURA PULSE SHADER GRAPH                                 │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  [Time] ──→ [*PulseSpeed] ──→ [Sin] ──→ [Remap]       │
│                                          ↓               │
│                                    [*Intensity]          │
│                                          ↓               │
│  [UV] ──→ [Dist from (0.5,0.5)] ──→ [1-x] ──→ [^2]    │
│                                          ↓               │
│                                    [*GlowRadius]         │
│                                          ↓               │
│                                    [Multiply] ←─────┐    │
│                                          ↓          │    │
│                                    [*AuraColor] ────┤    │
│                                          ↓          │    │
│  [MainTex] ──→ [Sample] ──→ [Multiply] ←──────────┘    │
│                                ↓                         │
│                          [Add (blend)]                   │
│                                ↓                         │
│                        [Fragment Output]                 │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🧪 Testing Checklist

- [ ] Shader compiles without errors
- [ ] Material shows golden glow
- [ ] Pulsation animates smoothly (sin wave)
- [ ] Edge glow visible (brighter at center)
- [ ] _Intensity slider works (0-2 range)
- [ ] _PulseSpeed affects animation rate
- [ ] _AuraColor changes shader tint
- [ ] Alpha transparency preserved
- [ ] Works on UI Image components
- [ ] Works on world-space quads
- [ ] Performance: <1ms GPU time per material

---

## 🔮 Production Tips

1. **Shader Variants**: Disable unused variants in Project Settings → Graphics
2. **Batching**: Use same material instance for multiple objects when possible
3. **Mobile**: Consider simplified shader (remove Power node) for low-end devices
4. **Addressables**: Place shader + materials in `Aura_Shards` group

---

**Next Steps:**
1. Open Unity Editor
2. Follow this guide to create shader
3. Test with AuraShaderController component
4. Assign to FateCard prefab

_"In every shader, a universe of light awaits."_ 🌙✨
