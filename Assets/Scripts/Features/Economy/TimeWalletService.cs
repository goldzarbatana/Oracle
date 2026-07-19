using System;
using TimeAura.Features.Data;
using TimeAura.Core.Services;
using UnityEngine;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// TimeWalletService - Dedicated service for the Time Barter (Horas/Minutes) economy.
    /// "Time spent, time shared. Recorded in minutes, measured in Horas."
    /// </summary>
    public sealed class TimeWalletService
    {
        private readonly ISecureBackendService _backend;

        [VContainer.Inject]
        public TimeWalletService(ISecureBackendService backend)
        {
            _backend = backend;
        }

        public const int MinutesPerHora = 60;

        public float MinutesToHoras(long minutes)
        {
            return (float)minutes / MinutesPerHora;
        }

        public long HorasToMinutes(float horas)
        {
            return Mathf.RoundToInt(horas * MinutesPerHora);
        }

        public long GetMinutes(UserProfile profile)
        {
            return profile != null ? profile.TimeBalanceMinutes : 0;
        }

        public async void AddMinutes(UserProfile profile, long minutes)
        {
            if (profile != null)
            {
                bool valid = _backend != null ? await _backend.ValidateIAPPurchase("minute_pack", "receipt") : true;
                if (valid) profile.AddMinutes(minutes);
            }
        }

        public async void SpendMinutes(UserProfile profile, long minutes)
        {
            if (profile != null)
            {
                bool valid = _backend != null ? await _backend.ValidateEscrowTransaction(profile.UserId, (int)minutes, "minutes") : true;
                if (valid) profile.SpendMinutes(minutes);
            }
        }

        public string FormatMinutes(long minutes)
        {
            return EconomyFormatter.FormatHoras(minutes);
        }
    }
}
