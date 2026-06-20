using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Services
{
    public sealed class FirebaseBackendService : ISecureBackendService
    {
        public async UniTask<bool> ValidateEscrowTransaction(string userId, int amount, string currency)
        {
            Debug.Log($"[FirebaseBackend] Validating escrow transaction for {userId}: {amount} {currency}...");
            await UniTask.Delay(300); // Simulate network latency
            // Stub: Always approve for now
            return true;
        }

        public async UniTask<bool> ValidateIAPPurchase(string productId, string receipt)
        {
            Debug.Log($"[FirebaseBackend] Validating IAP receipt for {productId}...");
            await UniTask.Delay(500);
            return true;
        }
    }
}
