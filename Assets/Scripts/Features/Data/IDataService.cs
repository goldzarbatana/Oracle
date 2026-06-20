using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Auth;

namespace TimeAura.Features.Data
{
    public interface IDataService : IService
    {
        UniTask<UserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken);
        UniTask<System.Collections.Generic.List<UserProfile>> GetAllProfilesAsync(CancellationToken cancellationToken);
        UniTask SaveUserProfileAsync(UserProfile profile, CancellationToken cancellationToken);

        // ── Harmony Messaging ───────────────────────────────────────
        UniTask SendHarmonyMessageAsync(string sessionId, Harmony.ChatMessage message);
        System.IDisposable ListenToHarmonyMessages(string sessionId, System.Action<System.Collections.Generic.List<Harmony.ChatMessage>> onMessagesChanged);
    }
}
