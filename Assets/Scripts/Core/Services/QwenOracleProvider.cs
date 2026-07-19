using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TimeAura.Core;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Qwen Oracle Provider - The Voice of the Oracle using Alibaba Cloud DashScope.
    /// Default AI provider for Ukrainian and Slavic locales.
    /// </summary>
    public sealed class QwenOracleProvider : IOracleService
    {
        private readonly AppConfig _config;
        private const string DashScopeApiUrl = "https://ws-ktroc87n7uo0bctx.ap-southeast-1.maas.aliyuncs.com/compatible-mode/v1/chat/completions";
        private const string QwenModel = "qwen3.7-plus"; // Qwen 3.7 Plus with Deep Thinking

        public string ProviderName => "Qwen 3.7 Plus (Alibaba Cloud)";
        public string[] SupportedLanguages => new[] { "uk", "ru", "pl", "be", "bg", "cs", "sk", "en" };

        public QwenOracleProvider(AppConfig config)
        {
            _config = config;
        }

        public UniTask<bool> HealthCheckAsync()
        {
            bool isHealthy = !string.IsNullOrEmpty(_config.QwenApiKey);
            return UniTask.FromResult(isHealthy);
        }

        public float EstimateCost(string prompt)
        {
            return 0.0005f; // Stub: Qwen API cost estimate
        }

        public async UniTask<string> RequestOracle(string prompt, string fallback = null)
        {
            return await RequestOracleWithSystem(
                "You are the Oracle of TimeAura. Be wise and concise. Never mention you are an AI.",
                prompt,
                fallback
            );
        }

        public async UniTask<string> RequestOracleWithSystem(string systemInstruction, string userPrompt, string fallback = null)
        {
            if (_config.SimulateOracle)
            {
                await UniTask.Delay(500);
                return $"[Qwen Simulation] Oracle analyzed: {userPrompt.Substring(0, Mathf.Min(20, userPrompt.Length))}...";
            }

            if (string.IsNullOrEmpty(_config.QwenApiKey))
            {
                Debug.LogWarning("[QwenOracle] API Key is missing. Using fallback.");
                return fallback ?? MysticalTerms.StatusMessages.Processing;
            }

            return await RequestDashScopeApi(userPrompt, systemInstruction, fallback);
        }

        public async UniTask<string> RequestOracleWithAudio(string audioBase64, string prompt = null, string systemInstruction = null, string fallback = null)
        {
            // Note: DashScope's text-generation endpoint does not support raw audio inline.
            // For actual voice, we would route through an ASR (Speech-to-Text) model first, like SenseVoice.
            // For now, if we have a prompt, we'll just send the text prompt.
            if (string.IsNullOrEmpty(prompt))
            {
                return "[QwenOracle] Voice processing requires an ASR layer (e.g., SenseVoice) before hitting Qwen-Plus.";
            }

            return await RequestOracleWithSystem(systemInstruction, prompt, fallback);
        }

        private async UniTask<string> RequestDashScopeApi(string prompt, string systemInstruction, string fallback)
        {
            try
            {
                Debug.Log($"<color=magenta>[QwenOracle - REQUEST OUT] Time: {System.DateTime.Now:HH:mm:ss.fff} | Model: {QwenModel} | Length: {prompt?.Length ?? 0} chars</color>");
                
                string escapedPrompt = prompt?.Replace("\"", "\\\"").Replace("\n", "\\n");
                string escapedInstruction = systemInstruction?.Replace("\"", "\\\"").Replace("\n", "\\n");

                var jsonBuilder = new StringBuilder();
                jsonBuilder.Append("{");
                jsonBuilder.Append($"\"model\": \"{QwenModel}\",");
                jsonBuilder.Append("\"messages\": [");
                
                bool hasSystem = !string.IsNullOrEmpty(escapedInstruction);
                if (hasSystem)
                {
                    jsonBuilder.Append($"{{\"role\": \"system\", \"content\": \"{escapedInstruction}\"}}");
                }

                if (!string.IsNullOrEmpty(escapedPrompt))
                {
                    if (hasSystem) jsonBuilder.Append(",");
                    jsonBuilder.Append($"{{\"role\": \"user\", \"content\": \"{escapedPrompt}\"}}");
                }
                
                jsonBuilder.Append("],");
                jsonBuilder.Append("\"extra_body\": {\"enable_thinking\": true}");
                jsonBuilder.Append("}");

                string jsonBody = jsonBuilder.ToString();

                using var request = new UnityWebRequest(DashScopeApiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {_config.QwenApiKey}");

                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"<color=red>[QwenOracle - ERROR IN] Time: {System.DateTime.Now:HH:mm:ss.fff} | Error: {request.error}</color>\n{request.downloadHandler.text}");
                    return fallback ?? MysticalTerms.StatusMessages.Error;
                }

                string responseJson = request.downloadHandler.text;
                string extracted = ExtractText(responseJson);

                if (!string.IsNullOrEmpty(extracted))
                {
                    Debug.Log($"<color=magenta>[QwenOracle - RESPONSE IN] Time: {System.DateTime.Now:HH:mm:ss.fff} | Status: Success | Length: {extracted.Length} chars</color>\n✨ Content: {extracted.Substring(0, Mathf.Min(80, extracted.Length))}...");
                    return extracted;
                }

                Debug.LogWarning("[QwenOracle] Received empty response from DashScope.");
                return fallback ?? MysticalTerms.StatusMessages.Error;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QwenOracle] Connection faltered: {ex.Message}");
                return fallback ?? MysticalTerms.StatusMessages.NetworkError;
            }
        }

        private string ExtractText(string json)
        {
            try
            {
                // OpenAI-compatible format: {"choices": [{"message": {"role": "assistant", "content": "...", "reasoning_content": "..."}}]}
                int contentKeyIndex = -1;
                int searchStart = 0;
                
                while (true)
                {
                    contentKeyIndex = json.IndexOf("\"content\"", searchStart);
                    if (contentKeyIndex == -1) return null;
                    
                    // Check if it's part of "reasoning_content"
                    if (contentKeyIndex > 0 && json[contentKeyIndex - 1] == '_')
                    {
                        searchStart = contentKeyIndex + 9;
                        continue;
                    }
                    break;
                }

                int colonIndex = json.IndexOf(":", contentKeyIndex);
                if (colonIndex == -1) return null;
                
                int quoteIndex = json.IndexOf("\"", colonIndex);
                if (quoteIndex == -1) return null;
                
                int start = quoteIndex + 1;
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
}
