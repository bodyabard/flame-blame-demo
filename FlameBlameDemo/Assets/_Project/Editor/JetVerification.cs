using System.IO;
using System.Reflection;
using FlameBlame.Hose;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class JetVerification
    {
        static double started;
        static int stage, errors;
        static TestRunnerApi runner;
        static bool testing;
        static double testEnded;
        sealed class FlowInput : IHoseInput { public bool TogglePressed=>false; public bool ModePressed=>false; public bool ValveHeld=>true; }
        static JetVerification() { EditorApplication.update+=Tick; Application.logMessageReceived+=Log; }
        static void Log(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception) errors++; }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                testing=false; testEnded=EditorApplication.timeSinceStartup;
                Directory.CreateDirectory("Captures/Jet");
                File.WriteAllText("Captures/Jet/tests.txt",result.TestStatus+" Passed="+result.PassCount+" Failed="+result.FailCount+"\n"+result.Message);
            }
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(File.Exists("Temp/FlameBlame-TestJet"))
            {
                File.Delete("Temp/FlameBlame-TestJet");
                testing=true; runner=ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter {testMode=TestMode.EditMode, testNames=new[]{"FlameBlame.Tests.JetpackMotorTests","FlameBlame.Tests.LadderRulesTests","FlameBlame.Tests.HoseReelTests","FlameBlame.Tests.HoseCollisionTests","FlameBlame.Tests.HoseVisualLogicTests"}}));
            }
            if(testing || EditorApplication.timeSinceStartup-testEnded<4) return;
            if(File.Exists("Temp/FlameBlame-SmokeJet") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/FlameBlame-SmokeJet"); SessionState.SetBool("JetSmoke",true); EditorApplication.isPlaying=true;
            }
            if(!SessionState.GetBool("JetSmoke",false) || !EditorApplication.isPlaying) return;
            if(started==0) {started=EditorApplication.timeSinceStartup; errors=0; stage=0;}
            if(EditorApplication.timeSinceStartup-started<2+stage*2) return;
            Directory.CreateDirectory("Captures/Jet");
            var hose=Object.FindFirstObjectByType<HoseController>();
            if(stage==0) { ScreenCapture.CaptureScreenshot("Captures/Jet/nozzle.png"); hose.Modes.Tick(true,true); }
            if(stage==1) { ScreenCapture.CaptureScreenshot("Captures/Jet/jet-pose.png"); typeof(JetController).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Object.FindFirstObjectByType<JetController>(),new FlowInput()); }
            if(stage==2)
            {
                int particles=0;
                foreach(var p in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) particles+=p.particleCount;
                File.WriteAllText("Captures/Jet/smoke.txt","Errors="+errors+" Particles="+particles+" Muzzle alignment="+Vector3.Dot(hose.Muzzle.forward,hose.NozzleDirection));
                SessionState.SetBool("JetSmoke",false); started=0; EditorApplication.isPlaying=false;
            }
            stage++;
        }
    }
}

