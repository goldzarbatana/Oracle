using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Utils
{
    /// <summary>
    /// A lightweight, self-contained JSON parser designed to convert balance sheets in JSON format
    /// (e.g. Firebase Realtime Database or CDNs) into DDA-compatible dictionary structures.
    /// Eliminates external dependencies on third-party JSON libraries.
    /// </summary>
    public static class Pro_DDAJsonParser
    {
        /// <summary>
        /// Parses a JSON string containing balance rows into a nested dictionary structure.
        /// Supports JSON arrays of objects and nested objects.
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> Parse(string json, string keyColumnName)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            if (string.IsNullOrEmpty(json)) return result;

            json = json.Trim();

            // Simple tokenization using regex to extract object blocks {...}
            // Matches flat objects inside JSON.
            var objectPattern = @"\{[^{}]*\}";
            var matches = Regex.Matches(json, objectPattern);

            foreach (Match match in matches)
            {
                var rowObj = ParseSingleObject(match.Value);
                if (rowObj.TryGetValue(keyColumnName, out string keyVal) && !string.IsNullOrEmpty(keyVal))
                {
                    result[keyVal] = rowObj;
                }
            }

            // Fallback: If no objects were matched using the key column directly,
            // assume the JSON is a dictionary where the keys are the IDs:
            // e.g. { "Level_1": { "HpMultiplier": "1.0", ... } }
            if (result.Count == 0)
            {
                var dictPattern = @"""([^""]+)""\s*:\s*(\{[^{}]*\})";
                var dictMatches = Regex.Matches(json, dictPattern);
                foreach (Match match in dictMatches)
                {
                    string keyVal = match.Groups[1].Value;
                    string objContent = match.Groups[2].Value;
                    var rowObj = ParseSingleObject(objContent);
                    // Insert the key into the dictionary to ensure it has the KeyColumnName field
                    rowObj[keyColumnName] = keyVal;
                    result[keyVal] = rowObj;
                }
            }

            return result;
        }

        private static Dictionary<string, string> ParseSingleObject(string jsonObject)
        {
            var rowData = new Dictionary<string, string>();

            // Matches "Key" : "Value" or "Key" : 123 or "Key" : true/false
            var kvPattern = @"""([^""]+)""\s*:\s*(?:""([^""]*)""|(-?\d+(?:\.\d+)?)|(true|false|null))";
            var matches = Regex.Matches(jsonObject, kvPattern);

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string val = "";

                if (match.Groups[2].Success)
                {
                    val = match.Groups[2].Value;
                }
                else if (match.Groups[3].Success)
                {
                    val = match.Groups[3].Value;
                }
                else if (match.Groups[4].Success)
                {
                    val = match.Groups[4].Value;
                }

                rowData[key] = val;
            }

            return rowData;
        }
    }
}
