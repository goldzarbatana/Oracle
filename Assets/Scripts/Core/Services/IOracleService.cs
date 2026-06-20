using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Services
{
    public interface IOracleService : IService
    {
        string ProviderName { get; }
        string[] SupportedLanguages { get; }
        
        UniTask<bool> HealthCheckAsync();
        float EstimateCost(string prompt);
        
        UniTask<string> RequestOracle(string prompt, string fallback = null);
        UniTask<string> RequestOracleWithSystem(string systemInstruction, string userPrompt, string fallback = null);
        UniTask<string> RequestOracleWithAudio(string audioBase64, string prompt = null, string systemInstruction = null, string fallback = null);
    }
}
