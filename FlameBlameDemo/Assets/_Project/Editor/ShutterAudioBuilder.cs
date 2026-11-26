using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Generate Shutter Sounds: procedural roller-shutter rattle, floor slam and latch click.
    /// No third-party audio: short noise bursts plus inharmonic metallic partials, fixed seed so the output is reproducible.
    public static class ShutterAudioBuilder
    {
        public const string Folder = "Assets/_Project/Audio/Generated";
        const int Rate = 44100;

        [MenuItem("Tools/FlameBlame/Generate Shutter Sounds")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Write("ShutterRattle", Rattle());
            Write("ShutterSlam", Slam());
            Write("ShutterLatch", Latch());
            AssetDatabase.Refresh();
            Debug.Log("[FlameBlame] Shutter sounds generated in " + Folder);
        }

        public static void EnsureClips()
        {
            if (!File.Exists(Folder + "/ShutterRattle.wav") || !File.Exists(Folder + "/ShutterSlam.wav") || !File.Exists(Folder + "/ShutterLatch.wav"))
                Generate();
            AssetDatabase.Refresh();
        }

        static float[] Rattle()
        {
            var rng = new System.Random(16);
            var data = new float[(int)(0.3f * Rate)];
            float[] partials = { 1180f, 1930f, 2870f, 3720f };
            for (int hit = 0; hit < 11; hit++)
            {
                float start = (float)rng.NextDouble() * 0.2f + hit * 0.008f;
                float gain = 0.35f + (float)rng.NextDouble() * 0.65f;
                float pitch = 0.85f + (float)rng.NextDouble() * 0.5f;
                Clink(data, start, 0.035f + (float)rng.NextDouble() * 0.04f, partials, pitch, gain, rng, 0.5f);
            }
            Envelope(data, 0.004f, 0.08f);
            return Normalize(data, 0.7f);
        }

        static float[] Slam()
        {
            var rng = new System.Random(17);
            var data = new float[(int)(0.8f * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float thud = Mathf.Sin(2f * Mathf.PI * (48f + 40f * Mathf.Exp(-t * 18f)) * t) * Mathf.Exp(-t * 7f);
                data[i] += thud * 1.1f;
            }
            Clink(data, 0f, 0.5f, new[] { 190f, 330f, 570f, 910f, 1480f }, 1f, 1f, rng, 0.9f);
            for (int k = 0; k < 6; k++)
                Clink(data, 0.07f + k * 0.045f + (float)rng.NextDouble() * 0.03f, 0.06f, new[] { 1250f, 2100f, 3300f }, 0.8f + (float)rng.NextDouble() * 0.5f, 0.45f / (1 + k * 0.5f), rng, 0.6f);
            Envelope(data, 0.001f, 0.12f);
            return Normalize(data, 0.9f);
        }

        static float[] Latch()
        {
            var rng = new System.Random(18);
            var data = new float[(int)(0.14f * Rate)];
            Clink(data, 0f, 0.035f, new[] { 2400f, 3600f }, 1f, 1f, rng, 0.8f);
            Clink(data, 0.045f, 0.05f, new[] { 900f, 1500f }, 1f, 0.8f, rng, 0.7f);
            Envelope(data, 0.0005f, 0.04f);
            return Normalize(data, 0.6f);
        }

        /// One metallic hit: a noise transient plus decaying partials.
        static void Clink(float[] data, float start, float length, float[] partials, float pitch, float gain, System.Random rng, float noise)
        {
            int begin = (int)(start * Rate);
            int count = (int)(length * Rate);
            float decay = 5f / Mathf.Max(0.01f, length);
            for (int i = 0; i < count && begin + i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * decay);
                float sample = ((float)rng.NextDouble() * 2f - 1f) * noise * Mathf.Exp(-t * decay * 2.5f);
                for (int p = 0; p < partials.Length; p++)
                    sample += Mathf.Sin(2f * Mathf.PI * partials[p] * pitch * t) * (0.5f / (1 + p));
                data[begin + i] += sample * env * gain;
            }
        }

        static void Envelope(float[] data, float attack, float release)
        {
            int a = Mathf.Max(1, (int)(attack * Rate)), r = Mathf.Max(1, (int)(release * Rate));
            for (int i = 0; i < data.Length; i++)
            {
                float g = Mathf.Min(1f, i / (float)a) * Mathf.Min(1f, (data.Length - i) / (float)r);
                data[i] *= g;
            }
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (float s in data) max = Mathf.Max(max, Mathf.Abs(s));
            for (int i = 0; i < data.Length; i++) data[i] = data[i] / max * peak;
            return data;
        }

        static void Write(string name, float[] samples)
        {
            using (var w = new BinaryWriter(File.Create(Folder + "/" + name + ".wav")))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (float s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
