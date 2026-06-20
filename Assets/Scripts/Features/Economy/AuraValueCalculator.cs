using System;
using System.Linq;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Services;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer;
using LocationService = TimeAura.Core.Services.LocationService;
using ZarbatanaSystems.BalanceOrchestrator;

namespace TimeAura.Features.Economy
{
    public sealed class AuraValueCalculator
    {
        private readonly AuraPillarSO[] _pillars;
        private readonly LocationService _locationService;

        [Inject]
        public AuraValueCalculator(AuraPillarSO[] pillars, LocationService locationService = null)
        {
            _pillars = pillars;
            _locationService = locationService;
        }

        /// <summary>
        /// Calculates the non-linear value of Horas for a given duration and tag.
        /// </summary>
        public int CalculateValue(int minutes, string tag)
        {
            if (minutes <= 0) return 0;

            // 1. Get Pillar Weight
            float pillarWeight = GetPillarWeight(tag);

            // 2. Get Dynamic Market Multiplier
            float dynamicMultiplier = GetDynamicMarketMultiplier(tag);

            // --- Zarbatana DDA Integration ---
            ApplyZarbatanaDDA(tag, ref pillarWeight, ref dynamicMultiplier);

            // 3. Compute non-linear value (converted to hours where 60 minutes = 1 base hour)
            float baseHours = minutes / 60f;
            float totalHoras = (baseHours * pillarWeight) * dynamicMultiplier;

            // Ensure at least 1 Horas as the baseline minimum
            int finalHoras = Mathf.Max(1, Mathf.RoundToInt(totalHoras));
            
            Debug.Log($"[AuraValueCalculator] Calculated: {minutes} mins for tag '{tag}'. Weight: {pillarWeight:F1}, Mult: {dynamicMultiplier:F1} -> {finalHoras} Horas.");
            
            return finalHoras;
        }

        private float GetPillarWeight(string tag)
        {
            if (string.IsNullOrEmpty(tag) || _pillars == null) return 1.0f;

            // Find the pillar that contains this tag
            var matchingPillar = _pillars.FirstOrDefault(p => 
                p != null && 
                p.isActive && 
                p.tags != null && 
                p.tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));

            if (matchingPillar != null)
            {
                // Ensure weight is at least 1
                return matchingPillar.weight > 0 ? matchingPillar.weight : 1.0f;
            }

            return 1.0f;
        }

        private float GetDynamicMarketMultiplier(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 1.0f;

            // Simple premium market simulation based on location and tag rarity
            // In a production build, this would consult the Cloud Oracle's real-time scarcity indices.
            float baseMultiplier = 1.0f;

            if (_locationService != null && _locationService.CurrentLocation.Zone != null)
            {
                string zone = _locationService.CurrentLocation.Zone.ToLower();
                // If in a active coordinate zone, vary the demand based on geodiffusion
                if (zone.Contains("earth") || zone.Contains("realm"))
                {
                    double lat = _locationService.CurrentLocation.Latitude;
                    double lon = _locationService.CurrentLocation.Longitude;
                    float hashFactor = (float)(Math.Abs(Math.Sin(lat) * Math.Cos(lon))); // Stable pseudo-random 0..1
                    baseMultiplier += hashFactor * 1.5f; // Add up to +1.5x based on exact location coordinates
                }
            }

            // Tag-based rarity multipliers (scarcity simulation)
            string lowerTag = tag.ToLower();
            if (lowerTag.Contains("code") || lowerTag.Contains("unity") || lowerTag.Contains("c#") || lowerTag.Contains("program"))
            {
                baseMultiplier *= 1.8f; // High demand for developers
            }
            else if (lowerTag.Contains("art") || lowerTag.Contains("design") || lowerTag.Contains("concept"))
            {
                baseMultiplier *= 1.4f; // Moderate-high demand for creators
            }
            else if (lowerTag.Contains("gardening") || lowerTag.Contains("clean"))
            {
                baseMultiplier *= 0.9f; // Abundant local supply
            }

            // Clamp between 0.8 and 3.5 as per the implementation plan design
            return Mathf.Clamp(baseMultiplier, 0.8f, 3.5f);
        }

        private static readonly System.Collections.Generic.Dictionary<string, float> RegionalPppRates = new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "switzerland", 50f },
            { "switzerland/zurich", 50f },
            { "switzerland/geneva", 50f },
            { "united states", 40f },
            { "us", 40f },
            { "germany", 35f },
            { "france", 35f },
            { "united kingdom", 35f },
            { "uk", 35f },
            { "poland", 20f },
            { "ukraine", 15f },
            { "india", 10f },
            { "general", 20f }
        };

        private float GetPppRate(string zone)
        {
            if (string.IsNullOrEmpty(zone)) return 20f; // Default baseline

            string lowerZone = zone.ToLower();
            if (lowerZone.Contains("switzerland") || lowerZone.Contains("ch")) return 50f;
            if (lowerZone.Contains("united states") || lowerZone.Contains("us") || lowerZone.Contains("america")) return 40f;
            if (lowerZone.Contains("germany") || lowerZone.Contains("de") || lowerZone.Contains("france") || lowerZone.Contains("uk") || lowerZone.Contains("united kingdom")) return 35f;
            if (lowerZone.Contains("poland") || lowerZone.Contains("pl")) return 20f;
            if (lowerZone.Contains("ukraine") || lowerZone.Contains("ua")) return 15f;
            if (lowerZone.Contains("india") || lowerZone.Contains("in")) return 10f;

            return 20f; // Default baseline
        }

        /// <summary>
        /// Calculates the non-linear value of Horas for a given duration and tag,
        /// applying Zonal PPP with Cosmic Dilation Clamping between initiator and receiver.
        /// </summary>
        public int CalculateValue(int minutes, string tag, UserProfile initiator, UserProfile receiver)
        {
            if (minutes <= 0) return 0;

            // 1. Get Pillar Weight & Dynamic Market Multiplier
            float pillarWeight = GetPillarWeight(tag);
            float dynamicMultiplier = GetDynamicMarketMultiplier(tag);

            // --- Zarbatana DDA Integration ---
            ApplyZarbatanaDDA(tag, ref pillarWeight, ref dynamicMultiplier);

            float baseHours = minutes / 60f;
            float baseHoras = baseHours * pillarWeight * dynamicMultiplier;

            // 2. PPP calculation with Dilation Clamping (Zonal PPP)
            float pppInitiator = GetPppRate(initiator?.LocationZone);
            float pppReceiver = GetPppRate(receiver?.LocationZone);

            float pppRatio = 1.0f;
            if (pppInitiator > 0 && pppReceiver > 0)
            {
                pppRatio = pppInitiator / pppReceiver;
            }

            // Cosmic Dilation Clamping: clamp maximum disparity between 0.5x and 2.0x
            float clampedRatio = Mathf.Clamp(pppRatio, 0.5f, 2.0f);

            // 3. Compute final Horas
            float totalHoras = baseHoras * clampedRatio;

            // Ensure at least 1 Horas as the baseline minimum
            int finalHoras = Mathf.Max(1, Mathf.RoundToInt(totalHoras));

            Debug.Log($"[AuraValueCalculator] Calculated with PPP: {minutes} mins for tag '{tag}'. " +
                      $"Zonal PPP: {initiator?.LocationZone ?? "None"} ({pppInitiator}) -> {receiver?.LocationZone ?? "None"} ({pppReceiver}). " +
                      $"Ratio: {pppRatio:F2} (Clamped: {clampedRatio:F2}). Base: {baseHoras:F2} -> Final: {finalHoras} Horas.");

            return finalHoras;
        }

        private void ApplyZarbatanaDDA(string tag, ref float pillarWeight, ref float dynamicMultiplier)
        {
            if (ZarbatanaSystems.BalanceOrchestrator.Pro.Pro_DDA_Manager.Instance != null && !string.IsNullOrEmpty(tag))
            {
                var rowData = ZarbatanaSystems.BalanceOrchestrator.Pro.Pro_DDA_Manager.Instance.GetRowData(tag);
                if (rowData != null)
                {
                    if (rowData.TryGetValue("HorasReward", out string rawHoras))
                    {
                        if (float.TryParse(rawHoras, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedReward))
                        {
                            pillarWeight *= parsedReward;
                        }
                    }
                    if (rowData.TryGetValue("BonusMultiplier", out string rawBonus))
                    {
                        if (float.TryParse(rawBonus, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedBonus))
                        {
                            dynamicMultiplier *= parsedBonus;
                        }
                    }
                }
            }
        }
    }
}
