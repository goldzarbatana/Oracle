using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Sacred Compass - Handles geolocation detection for finding nearby masters.
    /// "To find others, one must first know where they stand."
    /// </summary>
    public class LocationService : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }
        public bool IsEnabled => Input.location.isEnabledByUser;
        public LocationData CurrentLocation { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[LocationService] 📍 Calibrating the Sacred Compass...");
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public async UniTask<LocationData> RequestLocationAsync()
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[LocationService] ‼️ Location services are disabled by the user.");
                return new LocationData { Zone = "Unknown Realm" };
            }

            Input.location.Start(10f, 10f);

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                await UniTask.Delay(1000);
                maxWait--;
            }

            if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[LocationService] ‼️ Failed to acquire location within timeout.");
                Input.location.Stop();
                return new LocationData { Zone = "Ether" };
            }

            var loc = Input.location.lastData;
            var data = new LocationData
            {
                Latitude = loc.latitude,
                Longitude = loc.longitude,
                Zone = "Earth Realm", // We could geocode this later
                Timestamp = DateTime.UtcNow
            };

            CurrentLocation = data;
            Debug.Log($"[LocationService] 📍 Position acquired: {data.Latitude}, {data.Longitude}");
            
            Input.location.Stop();
            return data;
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            Input.location.Stop();
            await UniTask.Yield();
        }

        public static float GetDistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {
            float R = 6371e3f; // Earth radius in meters
            float phi1 = (float)lat1 * Mathf.Deg2Rad;
            float phi2 = (float)lat2 * Mathf.Deg2Rad;
            float deltaPhi = (float)(lat2 - lat1) * Mathf.Deg2Rad;
            float deltaLambda = (float)(lon2 - lon1) * Mathf.Deg2Rad;

            float a = Mathf.Sin(deltaPhi / 2) * Mathf.Sin(deltaPhi / 2) +
                      Mathf.Cos(phi1) * Mathf.Cos(phi2) *
                      Mathf.Sin(deltaLambda / 2) * Mathf.Sin(deltaLambda / 2);
            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

            return R * c;
        }

        public static string FormatDistance(float meters)
        {
            if (meters < 1000) return $"{Mathf.RoundToInt(meters)}m";
            return $"{(meters / 1000f):F1}km";
        }
    }

    [Serializable]
    public struct LocationData
    {
        public double Latitude;
        public double Longitude;
        public string Zone;
        public DateTime Timestamp;
    }
}
