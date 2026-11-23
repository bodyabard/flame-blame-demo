using System.IO;
using System.Reflection;
using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEditor;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class HoseReelVerification
    {
        sealed class InputSource : IPlayerInput
        {
            public Vector2 Move {get;set;} public Vector2 Look=>Vector2.zero;
            public bool JumpPressed=>false; public bool CaptureCursorPressed=>false; public bool ReleaseCursorPressed=>false;
        }
        static double start;
        static int stage,initial,away,dropped,errors;
        static string report;
        static InputSource input;
        static HoseReelVerification() {EditorApplication.update+=Tick; Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error || t==LogType.Exception) errors++;};}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(File.Exists("Temp/FlameBlame-VerifyReel") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {File.Delete("Temp/FlameBlame-VerifyReel"); SessionState.SetBool("ReelVerify",true); EditorApplication.isPlaying=true;}
            if(!EditorApplication.isPlaying || !SessionState.GetBool("ReelVerify",false)) return;
            if(start==0) {start=EditorApplication.timeSinceStartup;stage=0;errors=0;report="";}
            double elapsed=EditorApplication.timeSinceStartup-start;
            if(elapsed<(stage<3 ? 1+stage*2 : stage==3 ? 8 : 10)) return;
            var hose=Object.FindFirstObjectByType<HoseController>();
            Directory.CreateDirectory("Captures/Reel");
            if(stage==0)
            {
                input=new InputSource {Move=Vector2.right};
                var motor=Object.FindFirstObjectByType<PlayerMotor>();
                typeof(PlayerMotor).GetField("input",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(motor,input);
                Object.FindFirstObjectByType<PlayerLook>().enabled=false;
                initial=hose.Segments; report+="Initial="+initial+" capacity="+hose.Capacity+"\n";
            }
            if(stage==1) {away=hose.Segments; report+="Away="+away+"\n"; input.Move=Vector2.left;}
            if(stage==2) {input.Move=Vector2.zero; report+="Returned="+hose.Segments+"\n";}
            if(stage==3)
            {
                report+="Retracted="+hose.Segments+"\n";
                report+=(initial<hose.Capacity && away>initial && hose.Segments<away ? "PASS" : "FAIL")+" payout and return\n";
                ScreenCapture.CaptureScreenshot("Captures/Reel/retracted.png"); dropped=hose.Segments;
                var hold=(HoseHoldState)typeof(HoseController).GetField("hold",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hose);
                hold.Toggle(0,1); input.Move=Vector2.right;
            }
            if(stage==4)
            {
                report+=(hose.Segments==dropped ? "PASS" : "FAIL")+" dropped length frozen\n";
                report+="Errors="+errors+" tube vertices="+hose.GetComponent<MeshFilter>().sharedMesh.vertexCount+"\n";
                File.WriteAllText("Captures/Reel/play.txt",report); SessionState.SetBool("ReelVerify",false); start=0; EditorApplication.isPlaying=false;
            }
            stage++;
        }
    }
}
