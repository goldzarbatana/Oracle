using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Services
{
    public interface IRemoteConfigSource
    {
        UniTask FetchConfigAsync(bool forceReload = false);
        string GetValue(string key, string defaultValue);
        bool GetValue(string key, bool defaultValue);
        int GetValue(string key, int defaultValue);
        float GetValue(string key, float defaultValue);
        IReadOnlyDictionary<string, string> GetAllValues();
        bool IsConnected { get; }
        event Action OnConfigUpdated;
    }
}
