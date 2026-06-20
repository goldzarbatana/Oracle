using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Data;
using TimeAura.Core.Data.SO;
using UnityEngine;
using VContainer;
using ZarbatanaSystems.BalanceOrchestrator;
using ZarbatanaSystems.BalanceOrchestrator.Pro;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// HorasEconomyService - Manages the sacred flow of Horas and the Escrow system.
    /// "Energy cannot be created or destroyed, only locked until the ritual is complete."
    /// </summary>
    public sealed class HorasEconomyService : IManager
    {
        private readonly IDataService _dataService;
        private readonly ResonanceEconomySO _economyConfig;
        private readonly AuraValueCalculator _calculator;

        [Inject]
        public HorasEconomyService(IDataService dataService, ResonanceEconomySO economyConfig, AuraValueCalculator calculator)
        {
            _dataService = dataService;
            _economyConfig = economyConfig;
            _calculator = calculator;
        }
        
        public bool IsInitialized { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[Economy] ⚖️ Balancing the scales of time...");
            ZarbatanaSystems.BalanceOrchestrator.Pro.Pro_DDA_Manager.OnDataUpdated += OnBalanceUpdated;
            IsInitialized = true;
            await UniTask.Yield();
        }

        public async UniTask ShutdownAsync()
        {
            ZarbatanaSystems.BalanceOrchestrator.Pro.Pro_DDA_Manager.OnDataUpdated -= OnBalanceUpdated;
            IsInitialized = false;
            await UniTask.Yield();
        }

        private void OnBalanceUpdated()
        {
            Debug.Log("[Economy] 📊 Received live market updates from Balance Orchestrator. The scales have shifted.");
        }

        /// <summary>
        /// Step 2.1: Lock Funds in Escrow (Заморозка коштів)
        /// Accepts an optional tag to compute dynamic non-linear Horas value.
        /// </summary>
        public async UniTask<bool> LockFundsAsync(UserProfile user, ContractRealm realm, long minutes, long fiatAmountCents, string sessionId, string tag = "", UserProfile partner = null)
        {
            if (realm == ContractRealm.Ether)
            {
                // Resolve fallback tag if empty
                if (string.IsNullOrEmpty(tag))
                {
                    if (user.AuraSeeks != null && user.AuraSeeks.Count > 0)
                        tag = user.AuraSeeks[0];
                    else if (user.AuraGifts != null && user.AuraGifts.Count > 0)
                        tag = user.AuraGifts[0];
                    else
                        tag = "general";
                }

                // Compute dynamic non-linear Horas (and convert to minutes value)
                int dynamicHoras = _calculator.CalculateValue((int)minutes, tag, user, partner);
                int dynamicMinutes = dynamicHoras * 60;

                if (user.TimeBalanceMinutes < dynamicMinutes)
                {
                    Debug.LogWarning($"[Economy] ❌ Insufficient Dynamic Minutes: {user.TimeBalanceMinutes} < {dynamicMinutes} ({dynamicHoras} Horas for '{tag}')");
                    return false;
                }
                user.LockMinutesInEscrow(dynamicMinutes);
                Debug.Log($"[Economy] 🔒 {dynamicMinutes} Minutes ({dynamicHoras} Horas) locked in Escrow for session {sessionId}. Tag: '{tag}'. Remaining: {user.TimeBalanceMinutes}");
            }
            else
            {
                if (user.FiatBalance < fiatAmountCents)
                {
                    Debug.Log($"[Economy] 💳 Simulating fiat charge of {fiatAmountCents} Cents.");
                }
                user.LockFiatInEscrow(fiatAmountCents);
                Debug.Log($"[Economy] 🔒 {fiatAmountCents} Cents locked in Escrow for session {sessionId}. Remaining: {user.FiatBalance}");
            }
            
            await _dataService.SaveUserProfileAsync(user, default);
            return true;
        }

        /// <summary>
        /// Step 3.1: Finalize transfer (Передача заморожених коштів виконавцю)
        /// </summary>
        public async UniTask ReleaseFundsToReceiverAsync(UserProfile initiator, UserProfile receiver, ContractRealm realm, long minutes, long fiatCents, string sessionId, string tag = "")
        {
            if (realm == ContractRealm.Ether)
            {
                // Resolve fallback tag if empty
                if (string.IsNullOrEmpty(tag))
                {
                    if (initiator.AuraSeeks != null && initiator.AuraSeeks.Count > 0)
                        tag = initiator.AuraSeeks[0];
                    else if (receiver.AuraGifts != null && receiver.AuraGifts.Count > 0)
                        tag = receiver.AuraGifts[0];
                    else
                        tag = "general";
                }

                // Compute dynamic non-linear Horas to release the correct amount
                int dynamicHoras = _calculator.CalculateValue((int)minutes, tag, initiator, receiver);
                int dynamicMinutes = dynamicHoras * 60;

                initiator.ReleaseMinutesEscrow(dynamicMinutes);
                receiver.AddMinutes(dynamicMinutes);
                Debug.Log($"[Economy] 🔓 {dynamicMinutes} Minutes ({dynamicHoras} Horas) released to {receiver.Nickname} for session {sessionId}. Tag: '{tag}'");
            }
            else
            {
                initiator.ReleaseFiatEscrow(fiatCents);
                receiver.AddFiat(fiatCents);
                Debug.Log($"[Economy] 🔓 {fiatCents} Cents released to {receiver.Nickname} for session {sessionId}");
            }
            
            await _dataService.SaveUserProfileAsync(initiator, default);
            await _dataService.SaveUserProfileAsync(receiver, default);
        }

        /// <summary>
        /// Step 2.2: Return Funds to Initiator (Повернення при скасуванні або рішенні Суду)
        /// </summary>
        public async UniTask RefundFundsAsync(UserProfile initiator, ContractRealm realm, long minutes, long fiatCents, string sessionId, string tag = "")
        {
            if (realm == ContractRealm.Ether)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    if (initiator.AuraSeeks != null && initiator.AuraSeeks.Count > 0)
                        tag = initiator.AuraSeeks[0];
                    else
                        tag = "general";
                }

                int dynamicHoras = _calculator.CalculateValue((int)minutes, tag, initiator, null);
                int dynamicMinutes = dynamicHoras * 60;

                initiator.RevertMinutesEscrow(dynamicMinutes);
                Debug.Log($"[Economy] ↩️ {dynamicMinutes} Minutes ({dynamicHoras} Horas) refunded to {initiator.Nickname} for session {sessionId}");
            }
            else
            {
                initiator.RevertFiatEscrow(fiatCents);
                Debug.Log($"[Economy] ↩️ {fiatCents} Cents refunded to {initiator.Nickname} for session {sessionId}");
            }
            
            await _dataService.SaveUserProfileAsync(initiator, default);
        }
    }
}
