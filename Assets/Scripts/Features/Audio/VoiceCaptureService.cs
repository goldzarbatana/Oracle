using System;
using UnityEngine;

namespace TimeAura.Core.Services
{
    public class VoiceCaptureService : MonoBehaviour
    {
        private AudioClip _recordedClip;
        private string _microphoneDevice;
        private const int MaxRecordingTime = 15; // seconds
        private const int SampleRate = 16000; // Gemini recommended sample rate

        public bool IsRecording { get; private set; }

        private void Start()
        {
            if (Microphone.devices.Length > 0)
            {
                _microphoneDevice = Microphone.devices[0];
            }
            else
            {
                Debug.LogWarning("[Voice] No microphone devices found!");
            }
        }

        public void StartRecording()
        {
            if (string.IsNullOrEmpty(_microphoneDevice)) return;
            
            _recordedClip = Microphone.Start(_microphoneDevice, false, MaxRecordingTime, SampleRate);
            IsRecording = true;
            Debug.Log("[Voice] Recording started...");
        }

        public void StopRecording(Action<string> onAudioBase64Ready)
        {
            if (!IsRecording) return;
            
            Microphone.End(_microphoneDevice);
            IsRecording = false;
            Debug.Log("[Voice] Recording stopped.");

            if (_recordedClip != null)
            {
                string base64 = ConvertAudioClipToBase64(_recordedClip);
                onAudioBase64Ready?.Invoke(base64);
            }
            else
            {
                onAudioBase64Ready?.Invoke(null);
            }
        }

        private string ConvertAudioClipToBase64(AudioClip clip)
        {
            // Convert AudioClip float data to 16-bit PCM WAV byte array
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] wavBytes = EncodeToWAV(samples, clip.channels, clip.frequency);
            return Convert.ToBase64String(wavBytes);
        }

        private byte[] EncodeToWAV(float[] samples, int channels, int hz)
        {
            int sampleCount = samples.Length;
            int byteCount = sampleCount * 2; // 16-bit PCM = 2 bytes per sample
            byte[] wavBytes = new byte[44 + byteCount];

            using (var memStream = new System.IO.MemoryStream(wavBytes))
            using (var writer = new System.IO.BinaryWriter(memStream))
            {
                // RIFF header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + byteCount); // ChunkSize
                writer.Write("WAVE".ToCharArray());

                // fmt subchunk
                writer.Write("fmt ".ToCharArray());
                writer.Write(16); // Subchunk1Size
                writer.Write((short)1); // AudioFormat (1 = PCM)
                writer.Write((short)channels);
                writer.Write(hz);
                writer.Write(hz * channels * 2); // ByteRate
                writer.Write((short)(channels * 2)); // BlockAlign
                writer.Write((short)16); // BitsPerSample

                // data subchunk
                writer.Write("data".ToCharArray());
                writer.Write(byteCount);

                // Audio data
                for (int i = 0; i < sampleCount; i++)
                {
                    short intSample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                    writer.Write(intSample);
                }
            }

            return wavBytes;
        }
    }
}
