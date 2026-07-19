using UnityEditor;
using UnityEngine;
using System.IO;

namespace TimeAura.Core.Editor
{
    public static class AppConfigVerification
    {
        [MenuItem("TimeAura/Debug/Verify AppConfig Overrides")]
        public static void VerifyOverrides()
        {
            Debug.Log("<b>[AppConfig Verification]</b> Starting override verification...");

            // 1. Find the AppConfig asset
            var appConfig = Resources.Load<AppConfig>("TimeAuraAppConfig");
            if (appConfig == null)
            {
                Debug.LogError("[AppConfig Verification] ❌ Could not find TimeAuraAppConfig in Resources!");
                return;
            }

            // 2. Prepare test inputs
            string testLocalConfigPath = Path.Combine(Application.dataPath, "../TimeAuraAppConfig.local.json");
            bool hadLocalConfig = File.Exists(testLocalConfigPath);
            string originalLocalConfig = hadLocalConfig ? File.ReadAllText(testLocalConfigPath) : null;

            // Save original environment variables
            string origGeminiEnv = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            string origQwenEnv = System.Environment.GetEnvironmentVariable("QWEN_API_KEY");

            try
            {
                // Test Case 1: Environment Variables
                System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", "env_gemini_test_key");
                System.Environment.SetEnvironmentVariable("QWEN_API_KEY", "env_qwen_test_key");
                
                // Temp remove local config file for clean test
                if (hadLocalConfig)
                {
                    File.Delete(testLocalConfigPath);
                }

                // Load overrides
                var testObj = ScriptableObject.CreateInstance<AppConfig>();
                testObj.LoadLocalOverrides();

                if (testObj.GeminiApiKey == "env_gemini_test_key" && testObj.QwenApiKey == "env_qwen_test_key")
                {
                    Debug.Log("<b>[AppConfig Verification]</b> Case 1: Environment Variables -> <b><color=green>PASSED</color></b>");
                }
                else
                {
                    Debug.LogError($"[AppConfig Verification] Case 1: Environment Variables -> ❌ FAILED! Gemini: {testObj.GeminiApiKey}, Qwen: {testObj.QwenApiKey}");
                }

                // Test Case 2: Local JSON overrides
                string testJson = @"{
                    ""geminiApiKey"": ""json_gemini_test_key"",
                    ""qwenApiKey"": ""json_qwen_test_key"",
                    ""twilioAccountSid"": ""json_twilio_sid"",
                    ""twilioAuthToken"": ""json_twilio_token"",
                    ""twilioFromNumber"": ""json_twilio_from"",
                    ""oracleCloudFunctionUrl"": ""https://json-cloud-function.url""
                }";
                File.WriteAllText(testLocalConfigPath, testJson);

                testObj = ScriptableObject.CreateInstance<AppConfig>();
                testObj.LoadLocalOverrides();

                bool jsonPassed = testObj.GeminiApiKey == "json_gemini_test_key" &&
                                  testObj.QwenApiKey == "json_qwen_test_key" &&
                                  testObj.TwilioAccountSid == "json_twilio_sid" &&
                                  testObj.TwilioAuthToken == "json_twilio_token" &&
                                  testObj.TwilioFromNumber == "json_twilio_from" &&
                                  testObj.OracleCloudFunctionUrl == "https://json-cloud-function.url";

                if (jsonPassed)
                {
                    Debug.Log("<b>[AppConfig Verification]</b> Case 2: Local JSON Overrides -> <b><color=green>PASSED</color></b>");
                }
                else
                {
                    Debug.LogError($"[AppConfig Verification] Case 2: Local JSON Overrides -> ❌ FAILED!");
                }
            }
            finally
            {
                // Cleanup
                System.Environment.SetEnvironmentVariable("GEMINI_API_KEY", origGeminiEnv);
                System.Environment.SetEnvironmentVariable("QWEN_API_KEY", origQwenEnv);

                if (hadLocalConfig)
                {
                    File.WriteAllText(testLocalConfigPath, originalLocalConfig);
                }
                else if (File.Exists(testLocalConfigPath))
                {
                    File.Delete(testLocalConfigPath);
                }
            }

            // Test Case 3: Live configuration override check (without mutating serialized assets)
            appConfig.LoadLocalOverrides();
            Debug.Log($"<b>[AppConfig Verification]</b> Current Active Configuration loaded successfully.");
            Debug.Log($"   - Gemini API Key: {(string.IsNullOrEmpty(appConfig.GeminiApiKey) ? "<empty/default>" : "[SET]")}");
            Debug.Log($"   - Qwen API Key: {(string.IsNullOrEmpty(appConfig.QwenApiKey) ? "<empty/default>" : "[SET]")}");
            Debug.Log($"   - Cloud Function URL: {appConfig.OracleCloudFunctionUrl}");
            Debug.Log("<b>[AppConfig Verification]</b> Override verification complete!");
        }
    }
}
