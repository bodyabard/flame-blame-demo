using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Ladder;
using FlameBlame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Capture Showcase: plays Main, prepares each bookmark of ShowcaseShots (fires, ladder, hose, spray, whip...)
    /// and saves every format with and without HUD to Captures/Showcase/.
    /// Batch: Unity -batchmode -projectPath ... -executeMethod FlameBlame.EditorTools.ShowcaseCapture.RunBatch   (needs a GPU, no -nographics, no -quit)
    [InitializeOnLoad]
    public static class ShowcaseCapture
    {
        const string Flag = "FB.ShowcaseCapture";
        const string OutDir = "Captures/Showcase";

        sealed class ScriptedInput : IHoseInput, IPlayerInput
        {
            public bool Valve, Toggle;
            public bool TogglePressed { get { bool t = Toggle; Toggle = false; return t; } }
            public bool ModePressed => false;
            public bool ValveHeld => Valve;
            public Vector2 Move => Vector2.zero;
            public Vector2 Look => Vector2.zero;
            public bool JumpPressed => false;
            public bool CaptureCursorPressed => false;
            public bool ReleaseCursorPressed => false;
        }

        static readonly List<string> errors = new List<string>();
        static IEnumerator routine;
        static double waitUntil;
        static Func<bool> waitFor;
        static double started;

        static ShowcaseCapture()
        {
            if (SessionState.GetInt(Flag, 0) == 0) return;
            Application.logMessageReceived += (msg, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack); };
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/FlameBlame/Capture Showcase")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            SessionState.SetInt(Flag, 1);
            EditorApplication.EnterPlaymode();
        }

        public static void RunBatch() => Run();

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (SessionState.GetInt(Flag, 0) == 1)
            {
                if (UnityEngine.Object.FindFirstObjectByType<PhotoMode>() == null || UnityEngine.Object.FindFirstObjectByType<LevelScenario>() == null) return;
                SessionState.SetInt(Flag, 2);
                EditorApplication.ExecuteMenuItem("Window/General/Game");   // batch mode only renders cameras when a Game view exists
                started = EditorApplication.timeSinceStartup;
                routine = Routine();
            }
            if (routine == null) return;
            if (EditorApplication.timeSinceStartup - started > 420) { errors.Add("Showcase timed out"); Finish(); return; }
            if (waitFor != null) { if (!waitFor()) return; waitFor = null; }
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            try
            {
                if (!routine.MoveNext()) { Finish(); return; }
                if (routine.Current is float seconds) waitUntil = EditorApplication.timeSinceStartup + seconds;
                else if (routine.Current is Func<bool> condition) waitFor = condition;
            }
            catch (Exception e) { errors.Add(e.ToString()); Finish(); }
        }

        static void Finish()
        {
            routine = null;
            SessionState.SetInt(Flag, 0);
            File.WriteAllText(OutDir + "/report.txt", errors.Count == 0 ? "OK: no errors\n" : errors.Count + " errors\n" + string.Join("\n---\n", errors));
            if (Application.isBatchMode) EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        // ------------------------------------------------------------------ the capture sequence

        static IEnumerator Routine()
        {
            var photo = UnityEngine.Object.FindFirstObjectByType<PhotoMode>();
            var boot = UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            var scenario = UnityEngine.Object.FindFirstObjectByType<LevelScenario>();
            var hose = UnityEngine.Object.FindFirstObjectByType<HoseController>();
            var jet = UnityEngine.Object.FindFirstObjectByType<JetController>();
            var fire = (FireSystem)typeof(GameBootstrap).GetField("fire", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boot);
            var events = (IGameEvents)typeof(GameBootstrap).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boot);
            var shots = AssetDatabase.LoadAssetAtPath<ShowcaseShots>(ShowcaseDefaults.Path);
            var input = new ScriptedInput();
            typeof(HoseController).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hose, input);
            typeof(JetController).GetField("input", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(jet, input);
            typeof(LevelScenario).GetProperty("HasStarted").GetSetMethod(true).Invoke(scenario, new object[] { true });
            yield return 1.5f;

            photo.Enter(false);
            photo.SetHud(false);
            string only = Environment.GetEnvironmentVariable("FB_SHOT");
            File.WriteAllText(OutDir + "/progress.txt", "started" + Environment.NewLine);
            foreach (var shot in shots.shots)
            {
                if (!string.IsNullOrEmpty(only) && !shot.name.Contains(only)) continue;
                File.AppendAllText(OutDir + "/progress.txt", shot.name + Environment.NewLine);
                Time.timeScale = 1f;
                photo.Place(shot.position, shot.eulerAngles, shot.fieldOfView);
                yield return 0.1f;
                Prepare(shot, scenario, hose, fire, events, input, photo);
                yield return Mathf.Max(0.1f, shot.settleSeconds);
                if (shot.settleSeconds > 0f) Time.timeScale = 0f;
                yield return 0.1f;

                foreach (var format in shots.formats)
                    foreach (bool hud in new[] { false, true })
                    {
                        photo.SetHud(hud);
                        string path = OutDir + "/" + shot.name + "_" + format.name + (hud ? "_hud" : "_nohud") + ".png";
                        photo.Capture(Path.GetFullPath(path), format.width, format.height, hud);
                        yield return (Func<bool>)(() => !photo.Busy);
                        yield return 0.05f;
                    }
                var canNpc = GameObject.Find("Neighbor_Can");
                if (canNpc != null) File.AppendAllText(OutDir + "/progress.txt", "  can npc at " + canNpc.transform.position + " state " + canNpc.GetComponent<FlameBlame.Npc.NpcActor>().State + Environment.NewLine);
                photo.SetHud(false);
                Time.timeScale = 1f;
                input.Valve = false;
            }
            yield return 0.2f;
        }

        static void Prepare(ShowcaseShot s, LevelScenario scenario, HoseController hose, FireSystem fire, IGameEvents events, ScriptedInput input, PhotoMode photo)
        {
            var setup = s.setup;
            if ((setup & ShotSetup.PlaceLadder) != 0) PlaceLadder();
            if ((setup & ShotSetup.ConnectHose) != 0 && !hose.Connected)
            {
                scenario.TakeHose();
                hose.Connect();
            }
            if ((setup & ShotSetup.IgniteLivingRoom) != 0)
                foreach (var f in UnityEngine.Object.FindObjectsByType<Flammable>(FindObjectsSortMode.None))
                    if (f.Node != null && f.Node.Zone == FireZoneId.Floor2 && f.Node.Profile.canBurn) fire.Ignite(f.Node);
            if ((setup & ShotSetup.AtticFire) != 0) scenario.attic.Activate(fire);

            bool thrust = (setup & ShotSetup.Thrust) != 0;
            if (thrust != hose.Modes.Current.UsesThrust && hose.Connected) hose.Modes.Tick(true, true);
            input.Valve = (setup & (ShotSetup.Spray | ShotSetup.Thrust | ShotSetup.ThrowHose)) != 0;
            if ((setup & ShotSetup.ThrowHose) != 0) input.Toggle = true;

            if ((setup & ShotSetup.KickProps) != 0)
            {
                var rnd = new System.Random(7);
                foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                    if (!body.isKinematic && (body.position - s.position).sqrMagnitude < 36f)
                        body.AddForce(new Vector3((float)rnd.NextDouble() - 0.5f, 0.8f + (float)rnd.NextDouble(), (float)rnd.NextDouble() - 0.5f) * 4f, ForceMode.VelocityChange);
            }
            if ((setup & ShotSetup.Shock) != 0)
            {
                Vector3 at = s.position + Quaternion.Euler(0, s.eulerAngles.y, 0) * Vector3.forward * 2.5f; at.y = 3.4f;
                events.RaiseNpcShocked(at);
                events.RaisePuddleCreated(at, 1.4f);
                events.RaiseNpcShocked(at + Vector3.right * 0.6f);
            }
        }

        static void PlaceLadder()
        {
            var ladder = UnityEngine.Object.FindFirstObjectByType<LadderStateMachine>();
            float length = ladder.tuning.length, angle = ladder.tuning.angle;
            Vector3 horizontal = Vector3.left;                      // wall normal of the facade (-x)
            Vector3 bottom = new Vector3(-2.7f - (LadderRules.BaseOffset(length, angle) + 0.12f), 0.12f, 0f);
            Vector3 axis = Vector3.up * Mathf.Sin(angle * Mathf.Deg2Rad) - horizontal * Mathf.Cos(angle * Mathf.Deg2Rad);
            Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(axis, -horizontal), axis), axis);
            ladder.Place(bottom + axis * length * 0.5f, rotation);
        }
    }
}
