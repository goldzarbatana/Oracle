using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Utils
{
    public static class Pro_DDACSVParser
    {
        private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        private static readonly char[] TRIM_CHARS = { '\"' };

        /// <summary>
        /// Parses a CSV string exported from Google Sheets into a list of dictionaries.
        /// Each dictionary represents a row, keyed by the column headers.
        /// </summary>
        public static List<Dictionary<string, string>> Parse(string csvText)
        {
            var list = new List<Dictionary<string, string>>();

            if (string.IsNullOrEmpty(csvText))
            {
                Debug.LogWarning("[Pro DDA] CSV text is empty.");
                return list;
            }

            var lines = Regex.Split(csvText, LINE_SPLIT_RE);

            if (lines.Length <= 1)
            {
                Debug.LogWarning("[Pro DDA] CSV text contains no data rows.");
                return list;
            }

            var header = Regex.Split(lines[0], SPLIT_RE);
            for (var i = 0; i < header.Length; i++)
            {
                header[i] = header[i].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
            }

            for (var i = 1; i < lines.Length; i++)
            {
                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || string.IsNullOrEmpty(values[0])) continue;

                var entry = new Dictionary<string, string>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                    value = value.Replace("\"\"", "\""); // Unescape double quotes
                    entry[header[j]] = value;
                }
                list.Add(entry);
            }

            return list;
        }
    }
}
