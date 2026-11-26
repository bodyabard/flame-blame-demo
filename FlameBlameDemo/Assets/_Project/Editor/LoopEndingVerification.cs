using System;
using System.IO;
using System.Reflection;
using FlameBlame.Core;
using FlameBlame.Player;
using UnityEditor;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LoopEndingVerification
    {
        static double at;
        static int step;
        static LoopRevealController loop;
        static int reads, broken;
        static string errors="";
        static LoopEndingVerification() { EditorApplication.update+=Tick; Application.logMessageReceived+=Log; }
        static void Log(string text,string trace,LogType type) { if(SessionState.GetBool("FB.VerifyLoop",false) && (type==LogType.Error || type==LogType.Exception)) errors+=text+"\n"+trace+"\n"; }
        static void Check(bool condition,string message) { if(!condition) errors+="FAIL: "+message+"\n"; }
        static void Shot(string name) { ScreenCapture.CaptureScreenshot("Captures/Loop/"+name+".png"); }
        static void Size(int width,int height)
        {
            var assembly=typeof(Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance").GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{Enum.ToObject(sizesType.GetMethod("GetGroup").GetParameters()[0].ParameterType,0)});
            var sizeType=assembly.GetType("UnityEditor.GameViewSize"); var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(enumType,1),width,height,"Loop "+width});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,count-1); view.Repaint();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && File.Exists("Temp/FlameBlame-VerifyLoop"))
            {
                File.Delete("Temp/FlameBlame-VerifyLoop"); Directory.CreateDirectory("Captures/Loop");
                SessionState.SetBool("FB.VerifyLoop",true); step=0; errors=""; Size(1280,720); EditorApplication.isPlaying=true; return;
            }
            if(!SessionState.GetBool("FB.VerifyLoop",false) || !EditorApplication.isPlaying) return;
            if(EditorApplication.timeSinceStartup<at) return;
            try
            {
                if(step==0)
                {
                    loop=UnityEngine.Object.FindFirstObjectByType<LoopRevealController>(); if(loop==null) return;
                    Check(!loop.Revealed && loop.facade.activeSelf && !loop.openHouse.activeSelf && loop.volume.weight==0,"normal scene remains unchanged");
                    var boot=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
                    var events=(IGameEvents)typeof(GameBootstrap).GetField("events",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(boot);
                    events.DiaryRead+=()=>reads++; events.LoopBroken+=()=>broken++;
                    foreach(var feedback in UnityEngine.Object.FindObjectsByType<FlameBlame.Feedback.CameraFeedback>(FindObjectsSortMode.None)) feedback.enabled=false;
                    foreach(var motor in UnityEngine.Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None)) motor.enabled=false;
                    foreach(var look in UnityEngine.Object.FindObjectsByType<PlayerLook>(FindObjectsSortMode.None)) look.enabled=false;
                    loop.book.GetComponent<Rigidbody>().isKinematic=true;
                    loop.book.transform.position=new Vector3(-9,1.2f,-8);
                    var camera=Camera.main;
                    UnityEngine.Object.FindFirstObjectByType<PlayerMotor>().transform.position=new Vector3(-9,.2f,-9.4f);
                    camera.transform.position=new Vector3(-9,1.9f,-9.4f); camera.transform.LookAt(loop.book.transform);
                }
                if(step==1) {Check(loop.BookFocused,"book raycast focus and context prompt"); Shot("01-book-prompt");}
                if(step==2) loop.OpenDiary();
                if(step==3)
                {
                    Check(Time.timeScale==0 && loop.State==LoopState.DiaryOpen,"diary pauses gameplay");
                    loop.diary.body.ForceMeshUpdate(); Check(!loop.diary.body.isTextOverflowing,"letter fits at 1280x720"); Shot("02-letter-1280x720");
                }
                if(step==4) Size(1920,1080);
                if(step==5) { loop.diary.body.ForceMeshUpdate(); Check(!loop.diary.body.isTextOverflowing,"letter fits at 1920x1080"); Shot("03-letter-1920x1080"); }
                if(step==6) {Size(1280,720); loop.CloseDiary();}
                if(step==7)
                {
                    Check(loop.Revealed && reads==1 && !loop.facade.activeSelf && loop.openHouse.activeSelf,"first read reveals exactly once");
                    Check(loop.volume.weight>.99f,"grayscale completes");
                    Shot("04-grayscale"); var camera=Camera.main; UnityEngine.Object.FindFirstObjectByType<PlayerMotor>().transform.position=loop.openHouse.transform.position+new Vector3(0,4,-1); camera.transform.position=loop.openHouse.transform.position+new Vector3(0,4,-1); camera.transform.LookAt(loop.flower.transform);
                    foreach(var overlay in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {var data=overlay.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if(data!=null && data.renderType==UnityEngine.Rendering.Universal.CameraRenderType.Overlay) overlay.enabled=false;}
                    File.WriteAllText("Captures/Loop/hose.txt","Detached="+UnityEngine.Object.FindFirstObjectByType<FlameBlame.Hose.HoseController>().LoopDetached);
                }
                if(step==8) { Shot("05-open-house-flower"); loop.OpenDiary(); }
                if(step==9) loop.CloseDiary();
                if(step==10)
                {
                    Check(reads==1,"repeat reading emits no second DiaryRead");
                    Check(loop.flower.flammable.Node.IsBurning,"flower is burning");
                    loop.flower.flammable.ReceiveWater(loop.flower.transform.position,Vector3.up,2);
                    Check(broken==1,"water breaks loop exactly once");
                }
                if(step==11) Shot("06-ending-shutter");
                if(step==12) loop.BrowserEnding();
                if(step==13) {Shot("07-browser-ending-preview"); Check(Time.timeScale==0 && AudioListener.volume==0,"browser ending freezes and mutes");}
                if(step==14) { UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex); }
                if(step==15)
                {
                    loop=UnityEngine.Object.FindFirstObjectByType<LoopRevealController>();
                    Check(loop.Revealed && loop.openHouse.activeSelf && loop.volume.weight==1,"restart preserves session reveal");
                    File.WriteAllText("Captures/Loop/report.txt",errors.Length==0 ? "PASS: normal state, pause, both letter sizes, reveal, repeat read, extinguish, browser exit preview and session persistence after scene reload.\n" : errors);
                    SessionState.SetBool("FB.VerifyLoop",false); EditorApplication.isPlaying=false; return;
                }
                step++; at=EditorApplication.timeSinceStartup+((step==7 || step==11) ? 4 : 1.2);
            }
            catch(Exception e) {errors+=e+"\n"; File.WriteAllText("Captures/Loop/report.txt",errors); SessionState.SetBool("FB.VerifyLoop",false); EditorApplication.isPlaying=false;}
        }
    }
}
