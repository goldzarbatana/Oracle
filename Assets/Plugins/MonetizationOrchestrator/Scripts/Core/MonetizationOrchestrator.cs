using UnityEngine;

namespace UniversalMonetization
{
    /// <summary>
    /// Runtime orchestrator component that coordinates monetization managers (Ads and IAP).
    /// Supports both Singleton pattern and Dependency Injection.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class MonetizationOrchestrator : MonoBehaviour, IMonetizationOrchestrator
    {
        private static MonetizationOrchestrator _instance;
        public static IMonetizationOrchestrator Instance => _instance;

        [Header("Components")]
        [SerializeField] private AdManager adManager;
        [SerializeField] private IAPManager iapManager;

        public IAdManagerService Ads => adManager;
        public IIAPManagerService Iap => iapManager;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                if (adManager == null) adManager = GetComponent<AdManager>();
                if (iapManager == null) iapManager = GetComponent<IAPManager>();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
