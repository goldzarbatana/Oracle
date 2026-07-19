using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TimeAura.Features.Localization;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Oracle Service Provider - The intelligent router that decides which AI backend to use.
    /// Implements the Proxy/Decorator pattern to seamlessly route IOracleService calls.
    /// </summary>
    public sealed class OracleServiceProvider : IOracleService
    {
        private readonly GeminiOracleProvider _geminiProvider;
        private readonly QwenOracleProvider _qwenProvider;
        private readonly LocalizationManager _localization;
        private readonly RemoteConfigService _remoteConfig;
        private readonly TelemetryService _telemetry;

        public string ProviderName => "Oracle Service Router";
        public string[] SupportedLanguages => _geminiProvider.SupportedLanguages.Concat(_qwenProvider.SupportedLanguages).ToArray();

        public OracleServiceProvider(GeminiOracleProvider geminiProvider, QwenOracleProvider qwenProvider, LocalizationManager localization, RemoteConfigService remoteConfig, TelemetryService telemetry)
        {
            _geminiProvider = geminiProvider;
            _qwenProvider = qwenProvider;
            _localization = localization;
            _remoteConfig = remoteConfig;
            _telemetry = telemetry;
        }

        private async UniTask<IOracleService> GetActiveProviderAsync()
        {
            // PARTNER DECISION: Gemini is completely disabled for now.
            // All requests are routed exclusively to Qwen.
            Debug.Log("[OracleProvider] Gemini is disabled. Routing request exclusively to Qwen.");
            
            // Wait for Qwen health check just to be consistent, but never fallback
            await _qwenProvider.HealthCheckAsync();
            
            return _qwenProvider;
        }

        public async UniTask<bool> HealthCheckAsync()
        {
            var provider = await GetActiveProviderAsync();
            return await provider.HealthCheckAsync();
        }

        public float EstimateCost(string prompt)
        {
            return 0.001f;
        }

        public async UniTask<string> RequestOracle(string prompt, string fallback = null)
        {
            _telemetry?.LogOracleRequestMade("standard");
            var provider = await GetActiveProviderAsync();
            return await provider.RequestOracle(prompt, fallback);
        }

        public async UniTask<string> RequestOracleWithSystem(string systemInstruction, string userPrompt, string fallback = null)
        {
            _telemetry?.LogOracleRequestMade("system_prompt");
            var provider = await GetActiveProviderAsync();
            return await provider.RequestOracleWithSystem(systemInstruction, userPrompt, fallback);
        }

        public async UniTask<string> RequestOracleWithAudio(string audioBase64, string prompt = null, string systemInstruction = null, string fallback = null)
        {
            _telemetry?.LogOracleRequestMade("audio");
            var provider = await GetActiveProviderAsync();
            return await provider.RequestOracleWithAudio(audioBase64, prompt, systemInstruction, fallback);
        }
    }
}
