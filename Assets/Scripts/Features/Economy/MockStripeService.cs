using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// Temporary simulation of Stripe payments, logging operations to console.
    /// </summary>
    public sealed class MockStripeService : IStripeService
    {
        public async UniTask<string> CreateEscrowAsync(string clientId, string freelancerId, long amountCents)
        {
            Debug.Log($"[StripeMock] CreateEscrowAsync: Client={clientId}, Freelancer={freelancerId}, Amount={amountCents} cents (${amountCents/100.0:F2})");
            await UniTask.Delay(500); // Simulate network lag
            string mockEscrowId = $"esc_{System.Guid.NewGuid():N}";
            Debug.Log($"[StripeMock] Escrow created with ID: {mockEscrowId}");
            return mockEscrowId;
        }

        public async UniTask<bool> ReleaseEscrowAsync(string escrowId)
        {
            Debug.Log($"[StripeMock] ReleaseEscrowAsync: EscrowId={escrowId}");
            await UniTask.Delay(300);
            Debug.Log($"[StripeMock] Escrow {escrowId} released successfully.");
            return true;
        }

        public async UniTask<bool> RefundEscrowAsync(string escrowId)
        {
            Debug.Log($"[StripeMock] RefundEscrowAsync: EscrowId={escrowId}");
            await UniTask.Delay(300);
            Debug.Log($"[StripeMock] Escrow {escrowId} refunded successfully.");
            return true;
        }
    }
}
