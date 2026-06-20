using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;
using VContainer;

namespace TimeAura.Core.Services
{
    [Serializable]
    public struct AudioEntry
    {
        public string Name;
        public AudioClip Clip;
    }

    public enum AudioChannel { Master, Music, SFX, Ambience }

    /// <summary>
    /// Sacred Sound Manager - handles volume channels, persistence, and mystical feedback.
    /// </summary>
    public class AudioService : MonoBehaviour, IManager
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;

        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;

        [Header("Audio Library")]
        [SerializeField] private List<AudioEntry> audioLibrary = new List<AudioEntry>();

        private readonly Dictionary<string, AudioClip> _loadedClips = new Dictionary<string, AudioClip>();
        
        private float _masterVol = 1f;
        private float _musicVol = 0.7f;
        private float _sfxVol = 0.8f;
        private float _ambienceVol = 0.5f;
        private bool _isMuted = false;
        public bool IsInitialized { get; private set; }
        private bool _isAwake;

        private void Awake()
        {
            if (_isAwake) return;
            EnsureSourcesReady();
            _isAwake = true;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Awake();
            Debug.Log("[AudioService] 🎵 Tuning the Celestial Channels...");

            foreach (var entry in audioLibrary)
            {
                if (!string.IsNullOrEmpty(entry.Name) && entry.Clip != null)
                    _loadedClips[entry.Name] = entry.Clip;
            }

            if (musicSourceA == null) musicSourceA = CreateSource("MusicSourceA", true);
            if (musicSourceB == null) musicSourceB = CreateSource("MusicSourceB", true);
            if (sfxSource == null) sfxSource = CreateSource("SFXSource", false);
            if (ambienceSource == null) ambienceSource = CreateSource("AmbienceSource", true);

            _activeMusicSource = musicSourceA;
            _inactiveMusicSource = musicSourceB;

            LoadSettings();
            UpdateVolumes();
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        private void EnsureSourcesReady()
        {
            if (musicSourceA == null) musicSourceA = CreateSource("MusicSourceA", true);
            if (musicSourceB == null) musicSourceB = CreateSource("MusicSourceB", true);
            if (sfxSource == null) sfxSource = CreateSource("SFXSource", false);
            if (ambienceSource == null) ambienceSource = CreateSource("AmbienceSource", true);

            if (_activeMusicSource == null) _activeMusicSource = musicSourceA;
            if (_inactiveMusicSource == null) _inactiveMusicSource = musicSourceB;
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            var source = obj.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }

        #region Public Control API

        public void SetVolume(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case AudioChannel.Master: _masterVol = value; break;
                case AudioChannel.Music: _musicVol = value; break;
                case AudioChannel.SFX: _sfxVol = value; break;
                case AudioChannel.Ambience: _ambienceVol = value; break;
            }
            UpdateVolumes();
            SaveSettings();
        }

        public void ToggleMute(bool mute)
        {
            _isMuted = mute;
            UpdateVolumes();
            SaveSettings();
        }

        public float GetVolume(AudioChannel channel)
        {
            return channel switch {
                AudioChannel.Master => _masterVol,
                AudioChannel.Music => _musicVol,
                AudioChannel.SFX => _sfxVol,
                AudioChannel.Ambience => _ambienceVol,
                _ => 1f
            };
        }

        #endregion

        #region Playback Logic

        private AudioClip GetClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            if (_loadedClips.TryGetValue(clipName, out var clip)) return clip;

            // Fallback: Try loading from Resources/Audio/
            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                Debug.Log($"[AudioService] 📥 Auto-loaded '{clipName}' from Resources/Audio/");
                _loadedClips[clipName] = clip;
                return clip;
            }

            return null;
        }

        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            var clip = GetClip(clipName);
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }

        public void PlayMusic(string clipName, bool loop = true)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            EnsureSourcesReady();

            if (_activeMusicSource.clip == clip && _activeMusicSource.isPlaying) return;

            // Start crossfade
            StartCoroutine(CrossfadeMusic(clip, loop, 2f));
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip clip, bool loop, float duration)
        {
            Debug.Log($"[Audio] 🎵 Crossfading to: {clip.name}");
            
            _inactiveMusicSource.clip = clip;
            _inactiveMusicSource.loop = loop;
            _inactiveMusicSource.volume = 0;
            _inactiveMusicSource.Play();

            float elapsed = 0;
            float targetMusicVol = _musicVol * (_isMuted ? 0 : _masterVol);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;

                if (_activeMusicSource != null) _activeMusicSource.volume = Mathf.Lerp(targetMusicVol, 0, normalizedTime);
                _inactiveMusicSource.volume = Mathf.Lerp(0, targetMusicVol, normalizedTime);

                yield return null;
            }

            _activeMusicSource.Stop();
            _activeMusicSource.volume = 0;

            // Swap roles
            var temp = _activeMusicSource;
            _activeMusicSource = _inactiveMusicSource;
            _inactiveMusicSource = temp;
        }

        public void PlayAmbience(string clipName)
        {
            var clip = GetClip(clipName);
            if (clip != null && ambienceSource != null)
            {
                if (ambienceSource.clip == clip && ambienceSource.isPlaying) return;
                ambienceSource.clip = clip;
                ambienceSource.Play();
            }
        }

        #endregion

        #region Persistence

        private void UpdateVolumes()
        {
            float m = _isMuted ? 0 : _masterVol;
            if (_activeMusicSource != null) _activeMusicSource.volume = _musicVol * m;
            if (_inactiveMusicSource != null) _inactiveMusicSource.volume = 0;
            if (sfxSource != null) sfxSource.volume = _sfxVol * m;
            if (ambienceSource != null) ambienceSource.volume = _ambienceVol * m;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("Audio_Master", _masterVol);
            PlayerPrefs.SetFloat("Audio_Music", _musicVol);
            PlayerPrefs.SetFloat("Audio_SFX", _sfxVol);
            PlayerPrefs.SetFloat("Audio_Ambience", _ambienceVol);
            PlayerPrefs.SetInt("Audio_Muted", _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            _masterVol = PlayerPrefs.GetFloat("Audio_Master", 1f);
            _musicVol = PlayerPrefs.GetFloat("Audio_Music", 0.7f);
            _sfxVol = PlayerPrefs.GetFloat("Audio_SFX", 0.8f);
            _ambienceVol = PlayerPrefs.GetFloat("Audio_Ambience", 0.5f);
            _isMuted = PlayerPrefs.GetInt("Audio_Muted", 0) == 1;
        }

        #endregion

        // ── Backward Compatibility ──
        public void PlayTransformationAmbience(bool fadeIn = true) => PlayMusic("TransformationAmbience");
        public void StopTransformationAmbience(bool fadeOut = true) { if (_activeMusicSource != null) _activeMusicSource.Stop(); }
        public void PlayButtonClick() => PlaySFX("ButtonClick", 0.6f);
        public void PlayResonanceChime(int level) => PlaySFX("ResonanceChime", 0.5f + (level * 0.1f));
        public async UniTask ShutdownAsync() { IsInitialized = false; await UniTask.Yield(); }
    }
}
