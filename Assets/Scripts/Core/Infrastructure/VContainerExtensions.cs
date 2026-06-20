using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TimeAura.Core.Infrastructure
{
    /// <summary>
    /// VContainer extensions for Time Aura patterns.
    /// </summary>
    public static class VContainerExtensions
    {
        /// <summary>
        /// Get CancellationToken that cancels when GameObject is destroyed.
        /// Equivalent to Cysharp's GetCancellationTokenOnDestroy().
        /// </summary>
        public static CancellationToken GetDestroyCancellationToken(this Component component)
        {
            if (component == null || component.gameObject == null)
            {
                return CancellationToken.None;
            }

            // Create a linked token source tied to object destruction
            var cts = new CancellationTokenSource();

            // Register destruction callback
            if (component is MonoBehaviour mb)
            {
                mb.GetCancellationTokenOnDestroy().Register(() => cts.Cancel());
            }

            return cts.Token;
        }

        /// <summary>
        /// Extension method to get CancellationToken from MonoBehaviour lifecycle.
        /// </summary>
        public static CancellationToken GetCancellationTokenOnDestroy(this MonoBehaviour mb)
        {
            return mb.GetCancellationTokenOnDestroy();
        }
    }

    /// <summary>
    /// Helper to create linked cancellation token with destroy tracking.
    /// </summary>
    public static class CancellationTokenHelper
    {
        /// <summary>
        /// Create CancellationToken that cancels when either token cancels or object is destroyed.
        /// </summary>
        public static CancellationToken CreateLinked(CancellationToken token, Component component)
        {
            if (component == null)
            {
                return token;
            }

            var destroyToken = component.GetDestroyCancellationToken();
            return CancellationTokenSource.CreateLinkedTokenSource(token, destroyToken).Token;
        }
    }
}
