using System.Collections.Generic;
using System.IO;
using FlameBlame.Ladder;
using FlameBlame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LadderVerification
    {
        static double started; static bool captured;
        static readonly List<string> errors=new List<string>();
        static LadderVerification()
        {
            if(!SessionState.GetBool("LadderSmoke",false)) return;
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception) errors.Add(message+"\n"+stack);};
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            SessionState.SetBool("LadderSmoke",true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying) return;
            if(started==0) started=EditorApplication.timeSinceStartup;
            if(!captured && EditorApplication.timeSinceStartup-started>3)
            {
                captured=true;
                var camera=Object.FindFirstObjectByType<PlayerLadderInteraction>().view;
                Object.FindFirstObjectByType<FlameBlame.Feedback.CameraFeedback>().enabled=false;
                Object.FindFirstObjectByType<PlayerLook>().enabled=false;
                camera.position=new Vector3(-7,2,3); camera.LookAt(new Vector3(-5.3f,.2f,2.8f));
                Directory.CreateDirectory("Captures/Ladder"); var lens=camera.GetComponent<Camera>();
                var rt=new RenderTexture(1280,720,24); var previous=RenderTexture.active;
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(lens,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=rt});
                RenderTexture.active=rt; var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(); File.WriteAllBytes("Captures/Ladder/resting.png",image.EncodeToPNG());
                RenderTexture.active=previous; rt.Release(); Object.Destroy(rt); Object.Destroy(image);
            }
            if(EditorApplication.timeSinceStartup-started<8) return;
            var ladder=Object.FindFirstObjectByType<LadderStateMachine>();
            var player=Object.FindFirstObjectByType<PlayerMotor>();
            if(ladder==null || player.Locomotion==null) errors.Add("Missing ladder or locomotion integration");
            else if(ladder.State!=LadderState.Resting || ladder.Body.isKinematic) errors.Add("Initial ladder state is not dynamic Resting");
            Directory.CreateDirectory("Captures/Ladder");
            File.WriteAllText("Captures/Ladder/smoke.txt",errors.Count==0 ? "PASS: 8 seconds; ladder Resting/dynamic; locomotion initialized; no errors." : string.Join("\n",errors));
            SessionState.SetBool("LadderSmoke",false); EditorApplication.Exit(errors.Count==0 ? 0 : 1);
        }
    }
}


