using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Utils
{
    /// <summary>
    /// Utility class that provides type-safe extension methods for Dictionary<string, string>.
    /// </summary>
    public static class Pro_DDADataConverter
    {
        public static string GetString(this Dictionary<string, string> data, string columnName, string fallback = "")
        {
            if (data.TryGetValue(columnName, out string value))
            {
                return value;
            }
            Debug.LogWarning($"[Pro DDA] Column '{columnName}' not found. Using fallback: {fallback}");
            return fallback;
        }

        public static float GetFloat(this Dictionary<string, string> data, string columnName, float fallback = 0f)
        {
            if (data.TryGetValue(columnName, out string value))
            {
                string cleanValue = value.Replace("%", "").Replace(",", ".").Trim();
                if (float.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
                Debug.LogWarning($"[Pro DDA] Failed to parse float from column '{columnName}' with value '{value}'. Using fallback: {fallback}");
                return fallback;
            }
            Debug.LogWarning($"[Pro DDA] Column '{columnName}' not found. Using fallback: {fallback}");
            return fallback;
        }

        public static int GetInt(this Dictionary<string, string> data, string columnName, int fallback = 0)
        {
            if (data.TryGetValue(columnName, out string value))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                {
                    return result;
                }
                Debug.LogWarning($"[Pro DDA] Failed to parse int from column '{columnName}' with value '{value}'. Using fallback: {fallback}");
                return fallback;
            }
            Debug.LogWarning($"[Pro DDA] Column '{columnName}' not found. Using fallback: {fallback}");
            return fallback;
        }

        public static bool GetBool(this Dictionary<string, string> data, string columnName, bool fallback = false)
        {
            if (data.TryGetValue(columnName, out string value))
            {
                value = value.Trim().ToLowerInvariant();
                if (value == "true" || value == "1" || value == "yes") return true;
                if (value == "false" || value == "0" || value == "no") return false;
                
                Debug.LogWarning($"[Pro DDA] Failed to parse bool from column '{columnName}' with value '{value}'. Using fallback: {fallback}");
                return fallback;
            }
            Debug.LogWarning($"[Pro DDA] Column '{columnName}' not found. Using fallback: {fallback}");
            return fallback;
        }
    }
}
