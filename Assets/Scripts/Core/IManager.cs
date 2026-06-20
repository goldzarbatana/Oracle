using System.Threading;
using Cysharp.Threading.Tasks;

namespace TimeAura.Core
{
    /// <summary>
    /// Base interface for all managers using modern UniTask async patterns.
    /// Managers handle high-level systems (Network, Assets, Social, etc.)
    /// </summary>
    public interface IManager : IService
    {
        bool IsInitialized { get; }
        /// <summary>
        /// Initialize manager asynchronously with cancellation support.
        /// </summary>
        UniTask InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Shutdown manager and cleanup resources.
        /// </summary>
        UniTask ShutdownAsync();
    }
}
