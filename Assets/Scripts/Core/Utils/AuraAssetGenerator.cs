using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Core.Utils
{
    /// <summary>
    /// Utility to generate procedural assets for the Aura environment.
    /// "From the void of data, we weave the golden light."
    /// </summary>
    public static class AuraAssetGenerator
    {
        /// <summary>
        /// Creates a procedural 2D Fuzzy Glow texture (Gaussian-like).
        /// </summary>
        public static Texture2D CreateFuzzyGlow(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    // Gaussian curve for soft falloff
                    float normalizedDist = dist / maxDist;
                    float alpha = Mathf.Exp(-normalizedDist * normalizedDist * 4f); // 4f controls falloff steepness

                    if (normalizedDist > 1f) alpha = 0;

                    // Golden aura color: #FFD700
                    Color color = new Color(1f, 0.84f, 0f, alpha);
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Creates a basic particle-like texture for simple effects.
        /// </summary>
        public static Texture2D CreateAuraSpark(int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1.0f - Mathf.Pow(dist / maxDist, 0.5f);
                    if (alpha < 0) alpha = 0;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
