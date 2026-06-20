using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZarbatanaSystems.BalanceOrchestrator.Pro.Utils;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Core
{
    public static class Pro_GoogleSheetsDownloader
    {
        private const string EXPORT_URL_FORMAT = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";

        /// <summary>
        /// Downloads and parses CSV data from a Google Sheet.
        /// </summary>
        public static IEnumerator DownloadDataRoutine(string dataSource, string gid, Action<List<Dictionary<string, string>>> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(dataSource))
            {
                onError?.Invoke("Data Source is empty.");
                yield break;
            }

            if (string.IsNullOrEmpty(gid)) gid = "0";

            string url = (dataSource.StartsWith("http://") || dataSource.StartsWith("https://"))
                ? dataSource
                : string.Format(EXPORT_URL_FORMAT, dataSource, gid);
            Debug.Log($"[Pro DDA] Downloading from: {url}");

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError?.Invoke($"Failed to download: {www.error}");
                }
                else
                {
                    string rawText = www.downloadHandler.text;
                    try
                    {
                        string trimmed = rawText.TrimStart();
                        bool isJson = url.Contains(".json") || url.Contains("firebaseio.com") || trimmed.StartsWith("{") || trimmed.StartsWith("[");

                        if (isJson)
                        {
                            string keyCol = "LevelId";
                            if (Pro_DDA_Manager.Instance != null)
                            {
                                keyCol = Pro_DDA_Manager.Instance.KeyColumnName;
                            }
                            var parsedData = Pro_DDAJsonParser.Parse(rawText, keyCol);
                            onSuccess?.Invoke(new List<Dictionary<string, string>>(parsedData.Values));
                        }
                        else
                        {
                            var parsedData = Pro_DDACSVParser.Parse(rawText);
                            onSuccess?.Invoke(parsedData);
                        }
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse data: {e.Message}");
                    }
                }
            }
        }
    }
}
