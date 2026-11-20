using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Batch smoke test: opens Main, plays a few seconds, writes every error/exception to Captures/Calibration/play_smoke.txt.
    /// Unity -batchmode -executeMethod FlameBlame.EditorTools.PlaySmoke.Run   (no -quit; the smoke exits the editor itself)
    [InitializeOnLoad]
    public static class PlaySmoke
    {
        static readonly List<string> errors = new List<string>();
        static double started;
        static PlaySmoke()
        {
            if (SessionState.GetInt("FB.PlaySmoke", 0) == 0) return;
            Application.logMessageReceived += (msg, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack); };
            EditorApplication.update += Tick;
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            SessionState.SetInt("FB.PlaySmoke", 1);
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            int state = SessionState.GetInt("FB.PlaySmoke", 0);
            if (state == 1 && EditorApplication.isPlaying) { started = EditorApplication.timeSinceStartup; SessionState.SetInt("FB.PlaySmoke", 2); }
            if (state == 2 && EditorApplication.timeSinceStartup - started > 8)
            {
                SessionState.SetInt("FB.PlaySmoke", 0);
                Directory.CreateDirectory("Captures/Calibration");
                File.WriteAllText("Captures/Calibration/play_smoke.txt", errors.Count == 0 ? "OK: 8 s of play, no errors\n" : errors.Count + " errors\n" + string.Join("\n---\n", errors));
                EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
            }
        }
    }
}
