using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;

namespace TimeAura.Features.Aura
{
    public sealed class AuraEffectManager : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }

        [Range(0f, 1f)]
        [SerializeField] private float auraIntensity = 0.5f;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            ApplyAura(auraIntensity);
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public void ApplyAura(float intensity)
        {
            auraIntensity = Mathf.Clamp01(intensity);
            // TODO: Bind to Shader Graph / VFX Graph parameters.
        }
    }
}
