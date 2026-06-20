using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TimeAura.Core;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Gemini Oracle Provider - The Voice of the Oracle using Google Gemini.
    /// Connects the Temple to the Great Intelligence (Gemini 3.5 Flash).
    /// </summary>
    public class GeminiOracleProvider : IOracleService
    {
        protected readonly AppConfig _config;

        public string ProviderName => "Gemini (Google)";
        public string[] SupportedLanguages => new[] { "en", "es", "fr", "de" };

        public GeminiOracleProvider(AppConfig config)
        {
            _config = config;
        }

        public UniTask<bool> HealthCheckAsync()
        {
            bool isHealthy = !string.IsNullOrEmpty(_config.OracleCloudFunctionUrl);
            return UniTask.FromResult(isHealthy);
        }

        public float EstimateCost(string prompt)
        {
            return 0.001f; // Dummy implementation
        }

        public async UniTask<string> RequestOracle(string prompt, string fallback = null)
        {
            return await RequestOracleWithSystem(
                "You are the Oracle of TimeAura, a wise digital architect and guide. Your language is clear, professional, yet deeply insightful and encouraging. Use terms: Aura, Symmetry, Vectors, Chronos as professional game mechanics. Be concise, constructive, and helpful. Never break character. Never mention you are an AI.",
                prompt,
                fallback
            );
        }

        public async UniTask<string> RequestOracleWithSystem(string systemInstruction, string userPrompt, string fallback = null)
        {
            if (_config.SimulateOracle)
            {
                await UniTask.Delay(500);
                return $"[Simulation] Oracle responds to: {userPrompt.Substring(0, Mathf.Min(20, userPrompt.Length))}...";
            }

            if (string.IsNullOrEmpty(_config.OracleCloudFunctionUrl))
            {
                Debug.LogWarning("[Oracle] Cloud Function URL is missing. Using mystical fallback.");
                return fallback ?? MysticalTerms.StatusMessages.Processing;
            }

            return await RequestOracleViaDirectApi(_config.OracleCloudFunctionUrl, userPrompt, systemInstruction, null, fallback);
        }

        public async UniTask<string> RequestOracleWithAudio(string audioBase64, string prompt = null, string systemInstruction = null, string fallback = null)
        {
            if (_config.SimulateOracle)
            {
                await UniTask.Delay(1000);
                return "[Request: (Тестовий голосовий запит)]\n[Simulation] Оракул почув твій голос через Золотий Мікрофон! Для реального розпізнавання вимкни 'Simulate Oracle' та вкажи Cloud Function URL.\n[Search: Seek=Lawn]";
            }

            if (string.IsNullOrEmpty(_config.OracleCloudFunctionUrl))
            {
                Debug.LogWarning("[Oracle] Cloud Function URL is missing. Using mystical fallback.");
                return fallback ?? MysticalTerms.StatusMessages.Processing;
            }

            return await RequestOracleViaDirectApi(_config.OracleCloudFunctionUrl, prompt, systemInstruction, audioBase64, fallback);
        }

        private async UniTask<string> RequestOracleViaDirectApi(string apiKeyOrUrl, string prompt, string systemInstruction, string audioBase64, string fallback)
        {
            try
            {
                // If it's a raw API key, build the direct URL. Otherwise, use it as a URL.
                string url = apiKeyOrUrl.StartsWith("http") 
                    ? apiKeyOrUrl 
                    : $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKeyOrUrl}";

                Debug.Log($"<color=magenta>[GeminiOracle - REQUEST OUT] Time: {System.DateTime.Now:HH:mm:ss.fff} | Length: {prompt?.Length ?? 0} chars | HasAudio: {!string.IsNullOrEmpty(audioBase64)}</color>");
                
                string escapedPrompt = prompt?.Replace("\"", "\\\"").Replace("\n", "\\n");
                string escapedInstruction = systemInstruction?.Replace("\"", "\\\"").Replace("\n", "\\n");

                var jsonBuilder = new StringBuilder();
                jsonBuilder.Append("{");
                
                if (!string.IsNullOrEmpty(escapedInstruction))
                {
                    jsonBuilder.Append($"\"system_instruction\": {{\"parts\": [{{\"text\": \"{escapedInstruction}\"}}]}},");
                }

                jsonBuilder.Append("\"contents\": [{\"parts\": [");
                
                bool hasParts = false;

                if (!string.IsNullOrEmpty(escapedPrompt))
                {
                    jsonBuilder.Append($"{{\"text\": \"{escapedPrompt}\"}}");
                    hasParts = true;
                }

                if (!string.IsNullOrEmpty(audioBase64))
                {
                    if (hasParts) jsonBuilder.Append(",");
                    // Assuming standard format. Adjust mime_type if needed.
                    jsonBuilder.Append($"{{\"inline_data\": {{\"mime_type\": \"audio/wav\", \"data\": \"{audioBase64}\"}}}}");
                }

                jsonBuilder.Append("]}]}");
                string jsonBody = jsonBuilder.ToString();

                using var request = new UnityWebRequest(url, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"<color=red>[GeminiOracle - ERROR IN] Time: {System.DateTime.Now:HH:mm:ss.fff} | Error: {request.error}</color>\n{request.downloadHandler.text}");
                    return fallback ?? MysticalTerms.StatusMessages.Error;
                }

                string responseJson = request.downloadHandler.text;
                string extracted = ExtractText(responseJson);

                if (!string.IsNullOrEmpty(extracted))
                {
                    Debug.Log($"<color=magenta>[GeminiOracle - RESPONSE IN] Time: {System.DateTime.Now:HH:mm:ss.fff} | Status: Success | Length: {extracted.Length} chars</color>\n✨ Content: {extracted.Substring(0, Mathf.Min(80, extracted.Length))}...");
                    return extracted;
                }

                Debug.LogWarning("[OracleProvider] Received empty response from Gemini.");
                return fallback ?? MysticalTerms.StatusMessages.Error;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OracleProvider] Secure connection faltered: {ex.Message}");
                return fallback ?? MysticalTerms.StatusMessages.NetworkError;
            }
        }

        private string ExtractText(string json)
        {
            try
            {
                const string searchKey = "\"text\":\"";
                const string searchKeySpaced = "\"text\": \"";
                
                int textIndex = json.IndexOf(searchKey);
                int keyLen = searchKey.Length;

                if (textIndex == -1)
                {
                    textIndex = json.IndexOf(searchKeySpaced);
                    keyLen = searchKeySpaced.Length;
                }

                if (textIndex == -1) return null;
                
                int start = textIndex + keyLen;
                int end = json.IndexOf("\"", start);
                
                while (end > 0 && json[end - 1] == '\\')
                {
                    end = json.IndexOf("\"", end + 1);
                }

                if (end == -1) return null;

                string result = json.Substring(start, end - start);
                
                return result
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("\\r", "")
                    .Trim();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Type-alias wrapper for backward compatibility.
    /// Any class asking for GeminiAIService will receive GeminiOracleProvider behavior.
    /// </summary>
    [Obsolete("Use IOracleService and GeminiOracleProvider instead.")]
    public sealed class GeminiAIService : GeminiOracleProvider
    {
        public GeminiAIService(AppConfig config) : base(config) { }
    }
}
