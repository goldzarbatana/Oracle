using Cysharp.Threading.Tasks;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// Contract for the Fiat payment layer (Stripe integration).
    /// </summary>
    public interface IStripeService
    {
        UniTask<string> CreateEscrowAsync(string clientId, string freelancerId, long amountCents);
        UniTask<bool> ReleaseEscrowAsync(string escrowId);
        UniTask<bool> RefundEscrowAsync(string escrowId);
    }
}
