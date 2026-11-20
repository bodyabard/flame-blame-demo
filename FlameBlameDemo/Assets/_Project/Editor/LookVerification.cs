using System;
using System.IO;
using System.Reflection;
using FlameBlame.Core;
using FlameBlame.Electric;
using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Npc;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    // Editor-only smoke probe; reflection never enters a player build.
    [InitializeOnLoad]
    public static class LookVerification
    {
        static LookVerification() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (File.Exists("Temp/FlameBlame-StopLookSmoke"))
            {
                File.Delete("Temp/FlameBlame-StopLookSmoke"); SessionState.SetInt("FlameBlame.LookVerify", 0);
                EditorApplication.isPlaying = false; return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request = "Temp/FlameBlame-VerifyLook";
            if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(request); SessionState.SetInt("FlameBlame.LookVerify", 1);
                SessionState.SetFloat("FlameBlame.LookVerifyStart", (float)EditorApplication.timeSinceStartup); EditorApplication.isPlaying = true;
            }
            if (!EditorApplication.isPlaying || SessionState.GetInt("FlameBlame.LookVerify", 0) != 1) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat("FlameBlame.LookVerifyStart", 0) > 30)
            {
                SessionState.SetInt("FlameBlame.LookVerify", 0); EditorApplication.isPlaying = false;
                File.WriteAllText("Logs/look-verification.txt", "FAIL: initialization timeout."); return;
            }
            var comparison = Object.FindFirstObjectByType<LookComparison>();
            if (comparison == null || Object.FindFirstObjectByType<Flammable>()?.Node == null) return;
            SessionState.SetInt("FlameBlame.LookVerify", 0);
            try { Verify(comparison); }
            catch (Exception e) { File.WriteAllText("Logs/look-verification.txt", e.ToString()); Debug.LogException(e); }
            finally { Time.timeScale = 1f; EditorApplication.isPlaying = false; }
        }

        static void Verify(LookComparison comparison)
        {
            Quaternion rotation = comparison.key.transform.rotation;
            comparison.SetLook(false);
            if (comparison.volume.sharedProfile != comparison.backupProfile || comparison.UsingLook)
                throw new InvalidOperationException("Backup toggle failed.");
            comparison.SetLook(true);
            if (comparison.volume.sharedProfile != comparison.lookProfile || !comparison.UsingLook
                || Quaternion.Angle(rotation, comparison.key.transform.rotation) > .001f)
                throw new InvalidOperationException("Look toggle or key rotation failed.");

            Flammable prop = null;
            foreach (var candidate in Object.FindObjectsByType<Flammable>(FindObjectsSortMode.None))
                if (candidate.Node != null && candidate.Node.Profile.canBurn && candidate.Node.Profile.burnDuration > 0) { prop = candidate; break; }
            if (prop == null) throw new InvalidOperationException("No burnable prop found.");
            var fuel = typeof(FireNode).GetProperty("FuelLeft");
            fuel.SetValue(prop.Node, 0f);
            typeof(Flammable).GetMethod("ApplyVisuals", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(prop, new object[] { prop.Node });
            var renderer = prop.GetComponentInChildren<Renderer>(); var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            CheckColor(block.GetColor("_BaseColor"), prop.Node.Profile.charColor, "Prop base char");
            if (renderer.sharedMaterial.HasProperty("_1st_ShadeColor"))
            {
                CheckColor(block.GetColor("_1st_ShadeColor"), prop.Node.Profile.charColor, "Prop first shade char");
                CheckColor(block.GetColor("_2nd_ShadeColor"), prop.Node.Profile.charColor, "Prop second shade char");
            }
            var npc = Object.FindFirstObjectByType<NpcActor>();
            var state = (NpcStateMachine)typeof(NpcActor).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(npc);
            state.React(NpcState.Charred, 1);
            var settings = ScriptableObject.CreateInstance<ChaosSettings>();
            npc.Tick(0, new FireSystem(new GameEvents()), new ElectricitySystem(), settings);
            var body = (Renderer)typeof(NpcActor).GetField("bodyRenderer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(npc);
            body.GetPropertyBlock(block); CheckColor(block.GetColor("_BaseColor"), new Color(.09f, .07f, .06f), "NPC char");
            Object.Destroy(settings);
            File.WriteAllText("Logs/look-verification.txt", "PASS: Backup/Look switching; key angle preserved; prop base + Toon shades char correctly; NPC char tint works. Editor play smoke completed.");
            Debug.Log("[FlameBlame] Look verification PASS.");
        }
        static void CheckColor(Color actual, Color expected, string label)
        {
            if (Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) + Mathf.Abs(actual.b - expected.b) > .001f)
                throw new InvalidOperationException(label + ": " + actual + " != " + expected);
        }
    }
}
