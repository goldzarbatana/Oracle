using System;
using TimeAura.Features.Data;
using TimeAura.Core.Services;
using UnityEngine;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// QuantumWalletService - Manages the conversion, validation and formatting 
    /// of the Matter (Quants/Waves) economy.
    /// "Stored in waves, visible in constellations."
    /// </summary>
    public sealed class QuantumWalletService
    {
        private readonly ISecureBackendService _backend;

        [VContainer.Inject]
        public QuantumWalletService(ISecureBackendService backend)
        {
            _backend = backend;
        }

        // ── Constants ───────────────────────────────────────────
        public const int WavesPerQuant = 100;

        // ── Matter Conversions (Quants <-> Waves) ─────────────────
        public float WavesToQuants(long waves)
        {
            return (float)waves / WavesPerQuant;
        }

        public long QuantsToWaves(float quants)
        {
            return Mathf.RoundToInt(quants * WavesPerQuant);
        }

        // ── Balance Operations ──────────────────────────────────
        public long GetWaves(UserProfile profile)
        {
            return profile != null ? profile.WavesBalance : 0;
        }

        public async void AddWaves(UserProfile profile, long waves)
        {
            if (profile != null)
            {
                bool valid = _backend != null ? await _backend.ValidateIAPPurchase("wave_pack", "receipt") : true;
                if (valid) profile.AddWaves(waves);
            }
        }

        public async void SpendWaves(UserProfile profile, long waves)
        {
            if (profile != null)
            {
                bool valid = _backend != null ? await _backend.ValidateEscrowTransaction(profile.UserId, (int)waves, "waves") : true;
                if (valid) profile.SpendWaves(waves);
            }
        }

        // ── UI Formatting Helpers ────────────────────────────────
        public string FormatQuants(long waves)
        {
            return EconomyFormatter.FormatQuants(waves);
        }
    }
}
