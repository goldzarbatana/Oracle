using System.Collections.Generic;
using UnityEngine;

namespace TimeAura.Features.Aura
{
    public class AuraVFXController : MonoBehaviour
    {
        public ParticleSystem FlashEffect;
        public ParticleSystem AmbientEffect;

        public void PlayFlash(Color color)
        {
            if (FlashEffect != null)
            {
                var main = FlashEffect.main;
                main.startColor = color;
                FlashEffect.Play();
            }
            else
            {
                Debug.Log($"[AuraVFXController] ✧ Pillar Flash: {color}");
            }
        }

        public void UpdateAuraColors(List<Color> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                if (AmbientEffect != null) AmbientEffect.Stop();
                return;
            }

            // Simple mixing: average the colors
            Color mixedColor = Color.black;
            foreach (var c in colors) mixedColor += c;
            mixedColor /= colors.Count;
            mixedColor.a = 0.4f; // Keep ambient soft

            if (AmbientEffect != null)
            {
                var main = AmbientEffect.main;
                main.startColor = mixedColor;
                if (!AmbientEffect.isPlaying) AmbientEffect.Play();
            }

            if (FlashEffect != null)
            {
                var main = FlashEffect.main;
                main.startColor = mixedColor;
            }
        }
    }
}
