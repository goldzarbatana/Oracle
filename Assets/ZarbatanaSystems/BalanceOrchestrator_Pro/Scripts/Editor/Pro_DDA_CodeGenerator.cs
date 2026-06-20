using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools
{
    public class Pro_DDA_CodeGenerator : EditorWindow
    {
        private string sheetUrlOrId = "1tqITiO_iAecnLoY7WWL3Am29VgoVpjk1GxlugVwzv6U";
        private string gid = "0";
        private string className = "LevelBalanceData";
        private string targetNamespace = "ZarbatanaSystems.BalanceOrchestrator.Pro.Generated";
        private string outputPath = "Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Scripts/Runtime/Generated";
        private string keyColumnName = "LevelId";

        private string statusMessage = "Ready.";
        private bool isWorking = false;
        private UnityWebRequest currentRequest;

        [MenuItem("Tools/Balance Orchestrator/Code Generator")]
        public static void ShowWindow()
        {
            GetWindow<Pro_DDA_CodeGenerator>("Balance Orchestrator Code Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Balance Orchestrator C# Code Generator (PRO)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            sheetUrlOrId = EditorGUILayout.TextField("Google Sheet URL or ID", sheetUrlOrId);
            gid = EditorGUILayout.TextField("Sheet GID", gid);
            className = EditorGUILayout.TextField("Generated Class Name", className);
            targetNamespace = EditorGUILayout.TextField("Target Namespace", targetNamespace);
            outputPath = EditorGUILayout.TextField("Output Directory", outputPath);
            keyColumnName = EditorGUILayout.TextField("Key Column Name", keyColumnName);

            EditorGUILayout.Space();

            GUI.enabled = !isWorking;
            if (GUILayout.Button("Generate C# Scripts", GUILayout.Height(35)))
            {
                StartGeneration();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            GUIStyle statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            statusStyle.normal.textColor = isWorking ? Color.yellow : Color.white;
            EditorGUILayout.LabelField(statusMessage, statusStyle, GUILayout.Height(30));
        }

        private void StartGeneration()
        {
            string sheetId = ExtractSheetId(sheetUrlOrId);
            if (string.IsNullOrEmpty(sheetId))
            {
                statusMessage = "Error: Invalid Sheet URL or ID.";
                return;
            }

            string urlGid = ExtractGid(sheetUrlOrId);
            string finalGid = string.IsNullOrEmpty(urlGid) ? gid : urlGid;

            isWorking = true;
            statusMessage = "Downloading Sheet CSV...";

            string exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={finalGid}";
            currentRequest = UnityWebRequest.Get(exportUrl);
            currentRequest.SendWebRequest();

            EditorApplication.update += MonitorDownload;
        }

        private void MonitorDownload()
        {
            if (currentRequest == null)
            {
                EditorApplication.update -= MonitorDownload;
                isWorking = false;
                return;
            }

            if (currentRequest.isDone)
            {
                EditorApplication.update -= MonitorDownload;
                isWorking = false;

                if (currentRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        GenerateFromCSV(currentRequest.downloadHandler.text, ExtractSheetId(sheetUrlOrId), gid);
                        statusMessage = "C# Scripts generated successfully! Refreshing database...";
                        AssetDatabase.Refresh();
                    }
                    catch (Exception ex)
                    {
                        statusMessage = $"Error during generation: {ex.Message}";
                        Debug.LogException(ex);
                    }
                }
                else
                {
                    statusMessage = $"Download failed: {currentRequest.error}";
                    Debug.LogError($"[Pro DDA Code Generator] Download failed: {currentRequest.error}");
                }

                currentRequest.Dispose();
                currentRequest = null;
            }
        }

        private void GenerateFromCSV(string csvText, string sheetId, string sheetGid)
        {
            var parsed = Pro_DDACSVParser.Parse(csvText);
            if (parsed == null || parsed.Count == 0)
            {
                throw new Exception("Parsed CSV contains no rows.");
            }

            // Parse headers in original order
            string firstLine = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[0];
            string[] headers = Regex.Split(firstLine, @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].TrimStart('"').TrimEnd('"').Replace("\\", "");
            }

            // Map each column to its inferred type
            var columnTypes = new Dictionary<string, string>();
            foreach (var h in headers)
            {
                columnTypes[h] = InferTypeForColumn(parsed, h);
            }

            // Create target folder
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Generate C# Data Class
            string classContent = GenerateDataClassContent(headers, columnTypes);
            string classFilePath = Path.Combine(outputPath, $"{className}.cs");
            File.WriteAllText(classFilePath, classContent, Encoding.UTF8);

            // Generate C# Container ScriptableObject
            string containerContent = GenerateContainerClassContent(headers, columnTypes, sheetId, sheetGid);
            string containerFilePath = Path.Combine(outputPath, $"{className}Container.cs");
            File.WriteAllText(containerFilePath, containerContent, Encoding.UTF8);

            Debug.Log($"[Pro DDA Code Generator] Generated {className}.cs and {className}Container.cs at {outputPath}");
        }

        private string InferTypeForColumn(List<Dictionary<string, string>> rows, string column)
        {
            foreach (var row in rows)
            {
                if (row.TryGetValue(column, out string val) && !string.IsNullOrEmpty(val))
                {
                    val = val.Trim();
                    
                    // Check Boolean
                    string lowerVal = val.ToLowerInvariant();
                    if (lowerVal == "true" || lowerVal == "false" || lowerVal == "yes" || lowerVal == "no")
                    {
                        return "bool";
                    }

                    // Check Integer
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        return "int";
                    }

                    // Check Float
                    if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    {
                        return "float";
                    }

                    break;
                }
            }
            return "string"; // Default fallback
        }

        private string SanitizeFieldName(string header)
        {
            if (string.IsNullOrEmpty(header)) return "EmptyColumn";
            string result = Regex.Replace(header, @"[^a-zA-Z0-9_]", "_");
            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            return result;
        }

        private string GenerateDataClassContent(string[] headers, Dictionary<string, string> columnTypes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Automatically generated data class matching the schema of Google Sheet.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            foreach (var h in headers)
            {
                string fieldName = SanitizeFieldName(h);
                string fieldType = columnTypes[h];
                sb.AppendLine($"        [Tooltip(\"Original Column: {h}\")]");
                sb.AppendLine($"        public {fieldType} {fieldName};");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateContainerClassContent(string[] headers, Dictionary<string, string> columnTypes, string sheetId, string sheetGid)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;");
            sb.AppendLine();
            sb.AppendLine($"namespace {targetNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Automatically generated container ScriptableObject for {className}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}Container\", menuName = \"BalanceOrchestrator/Generated/{className} Container\")]");
            sb.AppendLine($"    public class {className}Container : ScriptableObject");
            sb.AppendLine("    {");
            sb.AppendLine("        [Header(\"Google Sheet Configuration\")]");
            sb.AppendLine($"        public string SheetId = \"{sheetId}\";");
            sb.AppendLine($"        public string Gid = \"{sheetGid}\";");
            sb.AppendLine($"        public string KeyColumnName = \"{keyColumnName}\";");
            sb.AppendLine();
            sb.AppendLine($"        public List<{className}> Items = new List<{className}>();");
            sb.AppendLine();
            sb.AppendLine("        public void LoadFromCSV(string csvText)");
            sb.AppendLine("        {");
            sb.AppendLine("            Items.Clear();");
            sb.AppendLine("            var parsed = Pro_DDACSVParser.Parse(csvText);");
            sb.AppendLine("            foreach (var row in parsed)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var item = new {className}();");

            foreach (var h in headers)
            {
                string fieldName = SanitizeFieldName(h);
                string fieldType = columnTypes[h];
                string getter = "GetString";
                if (fieldType == "int") getter = "GetInt";
                else if (fieldType == "float") getter = "GetFloat";
                else if (fieldType == "bool") getter = "GetBool";

                sb.AppendLine($"                item.{fieldName} = row.{getter}(\"{h}\");");
            }

            sb.AppendLine("                Items.Add(item);");
            sb.AppendLine("            }");
            sb.AppendLine("#if UNITY_EDITOR");
            sb.AppendLine("            UnityEditor.EditorUtility.SetDirty(this);");
            sb.AppendLine("#endif");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#if UNITY_EDITOR");
            sb.AppendLine("namespace ZarbatanaSystems.BalanceOrchestrator.Pro.EditorTools");
            sb.AppendLine("{");
            sb.AppendLine($"    [UnityEditor.CustomEditor(typeof({targetNamespace}.{className}Container))]");
            sb.AppendLine("    public class GeneratedContainerEditor : UnityEditor.Editor");
            sb.AppendLine("    {");
            sb.AppendLine("        private bool _isSyncing = false;");
            sb.AppendLine();
            sb.AppendLine("        public override void OnInspectorGUI()");
            sb.AppendLine("        {");
            sb.AppendLine("            DrawDefaultInspector();");
            sb.AppendLine();
            sb.AppendLine($"            var container = ({targetNamespace}.{className}Container)target;");
            sb.AppendLine("            EditorGUILayout.Space();");
            sb.AppendLine();
            sb.AppendLine("            GUI.enabled = !_isSyncing;");
            sb.AppendLine("            if (GUILayout.Button(\"Sync from Google Sheets\", GUILayout.Height(30)))");
            sb.AppendLine("            {");
            sb.AppendLine("                _isSyncing = true;");
            sb.AppendLine("                string url = $\"https://docs.google.com/spreadsheets/d/{container.SheetId}/export?format=csv&gid={container.Gid}\";");
            sb.AppendLine("                var www = UnityEngine.Networking.UnityWebRequest.Get(url);");
            sb.AppendLine("                var op = www.SendWebRequest();");
            sb.AppendLine();
            sb.AppendLine("                UnityEditor.EditorApplication.CallbackFunction monitor = null;");
            sb.AppendLine("                monitor = () =>");
            sb.AppendLine("                {");
            sb.AppendLine("                    if (op.isDone)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        UnityEditor.EditorApplication.update -= monitor;");
            sb.AppendLine("                        _isSyncing = false;");
            sb.AppendLine("                        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)");
            sb.AppendLine("                        {");
            sb.AppendLine("                            container.LoadFromCSV(www.downloadHandler.text);");
            sb.AppendLine("                            Debug.Log($\"[Pro DDA] Synced ScriptableObject '{container.name}' from Google Sheets successfully!\");");
            sb.AppendLine("                        }");
            sb.AppendLine("                        else");
            sb.AppendLine("                        {");
            sb.AppendLine("                            Debug.LogError($\"[Pro DDA] Sync failed: {www.error}\");");
            sb.AppendLine("                        }");
            sb.AppendLine("                        www.Dispose();");
            sb.AppendLine("                    }");
            sb.AppendLine("                };");
            sb.AppendLine("                UnityEditor.EditorApplication.update += monitor;");
            sb.AppendLine("            }");
            sb.AppendLine("            GUI.enabled = true;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("#endif");

            return sb.ToString();
        }

        private string ExtractSheetId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (input.Contains("docs.google.com/spreadsheets"))
            {
                var match = Regex.Match(input, @"/d/([a-zA-Z0-9-_]+)");
                if (match.Success) return match.Groups[1].Value;
            }
            return input.Trim();
        }

        private string ExtractGid(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (input.Contains("gid="))
            {
                var match = Regex.Match(input, @"gid=([0-9]+)");
                if (match.Success) return match.Groups[1].Value;
            }
            return "";
        }
    }
}
