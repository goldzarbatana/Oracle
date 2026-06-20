using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro
{
    // Simple way to bootstrap the difficulty system
    public class Pro_DifficultyBootstrap : MonoBehaviour
    {
        [SerializeField] private Pro_DifficultyProfile profile;
        [SerializeField, Range(0, 100)] private float percent = -1f; // <0 -> use profile default

        private void Start()
        {
            if (Pro_DifficultyManager.Instance == null)
            {
                var go = new GameObject("Pro_DifficultyManager");
                go.AddComponent<Pro_DifficultyManager>();
            }
            var mgr = Pro_DifficultyManager.Instance;
            var p = profile;
            if (p == null)
            {
                Debug.LogWarning("[Pro DDA] Profile is null on Pro_DifficultyBootstrap. Using default multipliers.");
            }
            mgr.Initialize(p, percent);
        }
    }
}
