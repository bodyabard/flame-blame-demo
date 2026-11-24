using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FlameBlame.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Batch play session that drives the HUD through every state by raising events and captures frames.
    /// Unity -batchmode -executeMethod FlameBlame.EditorTools.HudVerification.Run   (no -quit, no -nographics)
    [InitializeOnLoad]
    public static class HudVerification
    {
        const string Flag = "FB.HudVerify";
        const string OutDir = "Captures/UI";

        struct Step { public float At; public string Shot; public Action Act; }

        static readonly List<string> errors = new List<string>();
        static List<Step> steps;
        static int next;
        static double started;
        static IGameEvents events;
        static Camera camera;

        static HudVerification()
        {
            if (SessionState.GetInt(Flag, 0) == 0) return;
            Application.logMessageReceived += (msg, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack); };
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            SessionState.SetInt(Flag, 1);
            EditorApplication.EnterPlaymode();
        }

        static Vector3 Ahead() => camera.transform.position + camera.transform.forward * 3f + Vector3.down * 0.3f;

        static void Build()
        {
            var boot = UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            events = (IGameEvents)typeof(GameBootstrap).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boot);
            camera = Camera.main;
            steps = new List<Step>
            {
                new Step { At = 1.5f, Shot = "01_intro" },
                new Step { At = 2.0f, Act = () => { events.RaiseRoundStarted(105); events.RaiseTimerTick(105); } },
                new Step { At = 2.12f, Shot = "start_a" }, new Step { At = 2.25f, Shot = "start_b" },
                new Step { At = 2.4f, Shot = "start_c" }, new Step { At = 2.6f, Shot = "start_d" },
                new Step { At = 5.2f, Shot = "02_playing_extinguish" },
                new Step { At = 5.3f, Act = () => events.RaiseDamageReported(new DamageReport("Диван", "підсмажено", 1200, Ahead())) },
                new Step { At = 5.36f, Shot = "damage_a" }, new Step { At = 5.44f, Shot = "damage_b" },
                new Step { At = 5.54f, Shot = "damage_c" }, new Step { At = 5.7f, Shot = "damage_d" },
                new Step { At = 6.2f, Shot = "damage_e" },
                new Step { At = 6.4f, Act = () => events.RaiseDamageReported(new DamageReport("Сусід", "засмага без SPF", 80, Ahead() + Vector3.right)) },
                new Step { At = 7.0f, Shot = "03_small_damage" },
                new Step { At = 8.0f, Act = () => { events.RaiseNozzleModeChanged(true); events.RaiseHudContextChanged(HudContext.Jet); } },
                new Step { At = 8.07f, Shot = "mode_a" }, new Step { At = 8.15f, Shot = "mode_b" }, new Step { At = 8.25f, Shot = "mode_c" },
                new Step { At = 8.9f, Shot = "04_jet" },
                new Step { At = 9.2f, Act = () => { events.RaiseHudContextChanged(HudContext.Carrying); events.RaisePromptChanged(PromptKind.PlaceLadder); } },
                new Step { At = 10.0f, Shot = "05_carrying" },
                new Step { At = 10.3f, Act = () => { events.RaiseHudContextChanged(HudContext.Climbing); events.RaisePromptChanged(PromptKind.None); } },
                new Step { At = 11.0f, Shot = "06_climbing" },
                new Step { At = 11.2f, Act = () => { events.RaiseHudContextChanged(HudContext.Extinguish); events.RaisePromptChanged(PromptKind.PickLadder); events.RaiseNozzleModeChanged(false); } },
                new Step { At = 12.0f, Shot = "07_pick_ladder_prompt" },
                new Step { At = 12.2f, Act = () => { events.RaisePromptChanged(PromptKind.None); events.RaiseTimerTick(10); } },
                new Step { At = 12.9f, Shot = "08_last_seconds" },
                new Step { At = 13.0f, Act = () => events.RaiseTimerTick(4) },
                new Step { At = 13.05f, Shot = "timer_shake" },
                new Step { At = 13.6f, Act = () => events.RaisePlayerShocked() },
                new Step { At = 13.65f, Shot = "shock_a" }, new Step { At = 13.8f, Shot = "shock_b" }, new Step { At = 14.2f, Shot = "shock_c" },
                new Step { At = 15.0f, Act = () => events.RaisePressureChanged(0.3f) },
                new Step { At = 15.12f, Shot = "pressure_drop_a" }, new Step { At = 15.4f, Shot = "pressure_drop_b" },
                new Step { At = 16.2f, Shot = "09_pressure_low" },
                new Step { At = 16.5f, Act = () => events.RaiseRoundEnded(RoundEnding.FiresOut) },
                new Step { At = 16.7f, Shot = "result_a" }, new Step { At = 17.2f, Shot = "result_b" },
                new Step { At = 18.0f, Shot = "result_c" }, new Step { At = 20.0f, Shot = "10_result" },
                new Step { At = 20.8f, Shot = "end" },
            };
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (SessionState.GetInt(Flag, 0) == 1)
            {
                if (UnityEngine.Object.FindFirstObjectByType<FlameBlame.UI.HudRoot>() == null) return;
                started = EditorApplication.timeSinceStartup;
                SessionState.SetInt(Flag, 2);
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                Build();
            }
            if (SessionState.GetInt(Flag, 0) != 2 || steps == null) return;

            float t = (float)(EditorApplication.timeSinceStartup - started);
            while (next < steps.Count && t >= steps[next].At)
            {
                var step = steps[next++];
                step.Act?.Invoke();
                if (step.Shot != null) { UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); ScreenCapture.CaptureScreenshot(OutDir + "/" + step.Shot + ".png"); }
            }
            if (next >= steps.Count && t > steps[steps.Count - 1].At + 0.5f)
            {
                SessionState.SetInt(Flag, 0);
                File.WriteAllText(OutDir + "/report.txt", errors.Count == 0 ? "OK: no errors\n" : errors.Count + " errors\n" + string.Join("\n---\n", errors));
                EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
            }
        }
    }
}
