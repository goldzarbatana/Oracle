using UnityEngine;

namespace UniversalMonetization
{
    /// <summary>
    /// Mini self-rotation utility to spin the loading spinner in the scene.
    /// </summary>
    public class SimpleAdLoadingSpinner : MonoBehaviour
    {
        private void Update()
        {
            // Rotate 180 degrees per second (using unscaledDeltaTime since timeScale might be 0 during ads)
            transform.Rotate(0, 0, -180f * Time.unscaledDeltaTime);
        }
    }
}
