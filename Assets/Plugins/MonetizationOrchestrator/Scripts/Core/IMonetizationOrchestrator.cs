namespace UniversalMonetization
{
    /// <summary>
    /// Unified facade interface for the monetization features.
    /// Exposes both Ads and In-App Purchase services for clean Dependency Injection.
    /// </summary>
    public interface IMonetizationOrchestrator
    {
        IAdManagerService Ads { get; }
        IIAPManagerService Iap { get; }
    }
}
