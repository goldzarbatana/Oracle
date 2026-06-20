using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Services
{
    public interface ISecureBackendService
    {
        UniTask<bool> ValidateEscrowTransaction(string userId, int amount, string currency);
        UniTask<bool> ValidateIAPPurchase(string productId, string receipt);
    }
}
