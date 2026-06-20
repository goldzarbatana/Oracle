using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Oracle's Map - Connects to OpenStreetMap (Nominatim) to find coordinates for names.
    /// Uses JsonUtility for maximum compatibility.
    /// </summary>
    public class GeocodingService : IManager
    {
        public bool IsInitialized { get; private set; }
        public GeocodingService() { }
        private const string SEARCH_URL = "https://nominatim.openstreetmap.org/search?q={0}&format=json&addressdetails=1&limit=5";
        private const string REVERSE_URL = "https://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}";
        private const string USER_AGENT = "TimeAura/1.0 (Unity; contact@timeaura.dev)";

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[GeocodingService] 🗺️ Map of Existence initialized.");
            IsInitialized = true;
            await UniTask.Yield();
        }

        public async UniTask<List<LocationResult>> SearchLocationAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3) return new List<LocationResult>();

            try
            {
                string url = string.Format(SEARCH_URL, Uri.EscapeDataString(query));
                
                using var request = UnityWebRequest.Get(url);
                request.SetRequestHeader("User-Agent", USER_AGENT);

                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[GeocodingService] ‼️ Network Error: {request.error}");
                    return new List<LocationResult>();
                }

                string json = request.downloadHandler.text;
                
                // Wrap JSON array to make it compatible with JsonUtility
                string wrappedJson = "{ \"items\": " + json + " }";
                var wrapper = JsonUtility.FromJson<NominatimWrapper>(wrappedJson);
                
                var locationResults = new List<LocationResult>();

                if (wrapper != null && wrapper.items != null)
                {
                    foreach (var res in wrapper.items)
                    {
                        locationResults.Add(new LocationResult
                        {
                            DisplayName = res.display_name,
                            Latitude = double.Parse(res.lat, System.Globalization.CultureInfo.InvariantCulture),
                            Longitude = double.Parse(res.lon, System.Globalization.CultureInfo.InvariantCulture),
                            Type = res.type,
                            Importance = res.importance
                        });
                    }
                }

                return locationResults;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeocodingService] ‼️ Failed to consult the Map: {ex.Message}");
                return new List<LocationResult>();
            }
        }

        public async UniTask<string> ReverseGeocodeAsync(double lat, double lon)
        {
            try
            {
                string url = string.Format(REVERSE_URL, lat.ToString(System.Globalization.CultureInfo.InvariantCulture), lon.ToString(System.Globalization.CultureInfo.InvariantCulture));
                
                using var request = UnityWebRequest.Get(url);
                request.SetRequestHeader("User-Agent", USER_AGENT);

                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[GeocodingService] ‼️ Reverse Geocode Error: {request.error}");
                    return null;
                }

                string json = request.downloadHandler.text;
                var res = JsonUtility.FromJson<NominatimReverseResult>(json);
                
                if (res != null && res.address != null)
                {
                    string city = res.address.city ?? res.address.town ?? res.address.village ?? res.address.county;
                    string country = res.address.country;
                    if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
                        return $"{city}, {country}";
                    else if (!string.IsNullOrEmpty(city))
                        return city;
                    else if (!string.IsNullOrEmpty(country))
                        return country;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeocodingService] ‼️ Failed Reverse Geocoding: {ex.Message}");
                return null;
            }
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }
    }

    [Serializable]
    public struct LocationResult
    {
        public string DisplayName;
        public double Latitude;
        public double Longitude;
        public string Type;
        public float Importance;
    }

    #region Internal Nominatim Schema
    [Serializable]
    internal class NominatimWrapper
    {
        public NominatimResult[] items;
    }

    [Serializable]
    internal class NominatimResult
    {
        public string display_name;
        public string lat;
        public string lon;
        public string type;
        public float importance;
    }

    [Serializable]
    internal class NominatimReverseResult
    {
        public NominatimAddress address;
    }

    [Serializable]
    internal class NominatimAddress
    {
        public string city;
        public string town;
        public string village;
        public string county;
        public string country;
    }
    #endregion
}
