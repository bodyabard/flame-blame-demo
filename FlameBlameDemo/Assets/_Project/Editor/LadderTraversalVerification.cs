using System.IO;
using System.Reflection;
using FlameBlame.Hose;
using FlameBlame.Player;
using FlameBlame.Ladder;
using UnityEngine;
using UnityEditor;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LadderTraversalVerification
    {
        sealed class InputSource:IPlayerInput
        {
            public Vector2 Move {get;set;} public Vector2 Look=>Vector2.zero;
            public bool JumpPressed=>false; public bool CaptureCursorPressed=>false; public bool ReleaseCursorPressed=>false;
        }
        static InputSource input;
        static double start;
        static int stage;
        static string report; static bool traced, reversed;
        static LadderTraversalVerification() {EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(File.Exists("Temp/FlameBlame-TraverseLadder") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {File.Delete("Temp/FlameBlame-TraverseLadder"); UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity"); SessionState.SetBool("FlightVerify",false); SessionState.SetBool("LadderTraverse",true); EditorApplication.isPlaying=true;}
            if(!EditorApplication.isPlaying || !SessionState.GetBool("LadderTraverse",false)) return;
            if(start==0) {start=Time.time; stage=0; report=""; traced=false; reversed=false;}
            double elapsed=Time.time-start;
            double[] times={1,7,10,14,15,17};
            var locomotion=Object.FindFirstObjectByType<PlayerMotor>()?.Locomotion;
            if(locomotion!=null && traced && stage==1 && locomotion.State==PlayerLocomotionState.Walk && locomotion.transform.position.x>-2.5f) input.Move=Vector2.zero;
            if(locomotion!=null && stage==2 && locomotion.State==PlayerLocomotionState.Climb) reversed=true;
            if(locomotion!=null && !traced && (bool)typeof(PlayerLocomotionStateMachine).GetField("transferring",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(locomotion))
            {traced=true; report+="Transfer start="+typeof(PlayerLocomotionStateMachine).GetField("transferStart",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(locomotion)+" end="+typeof(PlayerLocomotionStateMachine).GetField("transferEnd",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(locomotion)+"\n";}
            if(traced && !report.Contains("Blockers:"))
            {
                var begin=(Vector3)typeof(PlayerLocomotionStateMachine).GetField("transferStart",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(locomotion);
                var end=(Vector3)typeof(PlayerLocomotionStateMachine).GetField("transferEnd",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(locomotion);
                begin.y=Mathf.Max(begin.y,end.y)+.1f; var delta=Vector3.ProjectOnPlane(end-begin,Vector3.up);
                report+="Blockers:";
                foreach(var hit in Physics.CapsuleCastAll(begin+Vector3.up*.3f,begin+Vector3.up*1.5f,.3f,delta.normalized,delta.magnitude,~((1<<8)|(1<<13)),QueryTriggerInteraction.Ignore)) report+=" "+hit.collider.name+"@"+hit.distance;
                report+="\n";
            }
            if(elapsed<times[stage]) return;
            var motor=Object.FindFirstObjectByType<PlayerMotor>();
            var ladder=Object.FindFirstObjectByType<LadderStateMachine>();
            var interaction=Object.FindFirstObjectByType<PlayerLadderInteraction>();
            if(stage==0)
            {
                Object.FindFirstObjectByType<FlameBlame.Core.GameBootstrap>().enabled=false;
                Object.FindFirstObjectByType<PlayerLook>().enabled=false;
                input=new InputSource {Move=Vector2.up};
                typeof(PlayerMotor).GetField("input",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(motor,input);
                Vector3 axis=new Vector3(Mathf.Cos(73*Mathf.Deg2Rad),Mathf.Sin(73*Mathf.Deg2Rad),0);
                Vector3 bottom=new Vector3(-2.82f-LadderRules.BaseOffset(4.5f,73),.13f,0);
                ladder.Place(bottom+axis*2.25f,Quaternion.LookRotation(new Vector3(axis.y,-axis.x,0),axis));
                ladder.enabled=false; ladder.transform.SetPositionAndRotation(bottom+axis*2.25f,Quaternion.LookRotation(new Vector3(axis.y,-axis.x,0),axis)); Physics.SyncTransforms();
                var cc=motor.GetComponent<CharacterController>(); cc.enabled=false; motor.transform.SetPositionAndRotation(bottom+Vector3.left*.6f,Quaternion.Euler(0,90,0)); cc.enabled=true;
            }
            if(stage==1)
            {
                report+="Up: "+motor.transform.position+" state="+motor.Locomotion.State+"\n";
                report+=(motor.transform.position.x>-2.5f ? "PASS" : "FAIL")+" window entry\n";
                Object.FindFirstObjectByType<FlameBlame.Feedback.CameraFeedback>().enabled=false; interaction.view.localRotation=Quaternion.Euler(25,0,0); ScreenCapture.CaptureScreenshot("Captures/Ladder/window-repaired.png"); input.Move=Vector2.down;
            }
            if(stage==2) report+="Return: "+motor.transform.position+" state="+motor.Locomotion.State+"\n";
            if(stage==3)
            {
                report+=(reversed ? "PASS" : "FAIL")+" reverse ladder attachment\n";
                report+="Down: "+motor.transform.position+" state="+motor.Locomotion.State+"\n";
                report+=(motor.transform.position.y<.5f ? "PASS" : "FAIL")+" descent\n";
                input.Move=Vector2.zero; ladder.Interact(interaction);
                report+=(interaction.Carried==ladder && interaction.hose.Holstered ? "PASS" : "FAIL")+" placed pickup and holster\n";
                typeof(PlayerLadderInteraction).GetMethod("Release",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(interaction,new object[]{false});
                report+=(!interaction.hose.Holstered ? "PASS" : "FAIL")+" nozzle restore\n";
            }
            if(stage==4)
            {
                Vector3 axis=new Vector3(Mathf.Cos(73*Mathf.Deg2Rad),Mathf.Sin(73*Mathf.Deg2Rad),0);
                Vector3 bottom=new Vector3(-2.82f-LadderRules.BaseOffset(4.5f,73),.13f,0);
                var rotation=Quaternion.LookRotation(new Vector3(axis.y,-axis.x,0),axis);
                ladder.Place(bottom+axis*2.25f,rotation); ladder.transform.SetPositionAndRotation(bottom+axis*2.25f,rotation); Physics.SyncTransforms();
                ladder.ReceiveWater(ladder.Top,Vector3.left,.5f);
            }
            if(stage==5)
            {
                report+="Knocked top="+ladder.Top+" dynamic="+!ladder.Body.isKinematic+"\n";
                report+=(ladder.Top.y<4 && !ladder.Body.isKinematic ? "PASS" : "FAIL")+" water knockdown falls away from wall\n";
                ladder.ReceiveWater(ladder.transform.position,Vector3.left,.1f);
                report+=(!ladder.Body.IsSleeping() ? "PASS" : "FAIL")+" subsequent water wakes ladder\n";
                File.WriteAllText("Captures/Ladder/traversal.txt",report);
                SessionState.SetBool("LadderTraverse",false); start=0; EditorApplication.isPlaying=false;
            }
            stage++;
        }
    }
}
