using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Matching
{
    public sealed class MatchingManager : IManager
    {
        public bool IsInitialized { get; private set; }
        private readonly IDataService dataService;

        [Inject]
        public MatchingManager(IDataService dataService)
        {
            this.dataService = dataService;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (dataService == null) throw new System.InvalidOperationException("IDataService not injected into MatchingManager. Ensure TimeAuraInstaller binds IDataService.");
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public async UniTask FindMatchAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[MatchingManager] Matching requested (stub). AI matching can plug in here.");
            await UniTask.Yield(cancellationToken);
        }
    }
}
