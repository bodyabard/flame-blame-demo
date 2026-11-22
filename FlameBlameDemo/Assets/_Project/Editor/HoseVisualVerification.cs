using System;
using System.IO;
using System.Reflection;
using FlameBlame.Hose;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class HoseVisualVerification
    {
        static double started;
        static int stage;
        static int errors;
        static HoseVisualVerification()
        {
            if(!SessionState.GetBool("HoseVerify",false)) return;
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(message,stack,type)=> {if(type==LogType.Exception || type==LogType.Error) errors++;};
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            SessionState.SetBool("HoseVerify",true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying) return;
            if(started==0) started=EditorApplication.timeSinceStartup;
            double elapsed=EditorApplication.timeSinceStartup-started;
            if(elapsed<2+stage*2) return;
            try
            {
                var hose=UnityEngine.Object.FindFirstObjectByType<HoseController>();
                if(stage==0)
                {
                    foreach(var jet in UnityEngine.Object.FindObjectsByType<JetController>(FindObjectsSortMode.None)) jet.enabled=false;
                    hose.WaterFlowing=false;
                }
                if(stage==1)
                {
                    hose.GetComponent<MeshRenderer>().enabled=false;
                    hose.GetComponent<LineRenderer>().enabled=true;
                    Capture("Hose-Before");
                    hose.GetComponent<MeshRenderer>().enabled=true;
                    hose.GetComponent<LineRenderer>().enabled=false;
                    Capture("Hose-Dry");
                }
                if(stage==2) hose.WaterFlowing=true;
                if(stage==3) Capture("Hose-Wet");
                if(stage==4) hose.WaterFlowing=false;
                if(stage==5) Capture("Hose-Closed");
                if(stage==6)
                {
                    var hold=typeof(HoseController).GetField("hold",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hose) as HoseHoldState;
                    hold.Toggle(0,1); hose.WaterFlowing=true;
                }
                if(stage==7) Capture("Hose-Dropped-Moving");
                if(stage==8)
                {
                    Directory.CreateDirectory("Captures/Hose");
                    File.WriteAllText("Captures/Hose/verification.txt",$"Play smoke: {errors} errors; dry/wet/closed/dropped captures. Tube vertices: {hose.GetComponent<MeshFilter>().sharedMesh.vertexCount}.\n");
                    SessionState.SetBool("HoseVerify",false); EditorApplication.Exit(errors==0?0:1);
                }
                stage++;
            }
            catch(Exception e) {Debug.LogException(e); SessionState.SetBool("HoseVerify",false); EditorApplication.Exit(1);}
        }
        static void Capture(string label)
        {
            Directory.CreateDirectory("Captures/Look");
            var camera=Camera.main;
            var position=camera.transform.position; var rotation=camera.transform.rotation;
            var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var hidden=new System.Collections.Generic.List<Renderer>();
            foreach(var renderer in renderers)
                if(renderer.gameObject.layer==LayerMask.NameToLayer("ViewModel") && renderer.enabled)
                {hidden.Add(renderer); renderer.enabled=false;}
            camera.transform.SetPositionAndRotation(new Vector3(0,2f,-2f),Quaternion.LookRotation(new Vector3(-1.4f,-1.85f,.4f)));
            typeof(LookTransfer).GetMethod("CapturePair",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{label});
            camera.transform.SetPositionAndRotation(position,rotation);
            foreach(var renderer in hidden) renderer.enabled=true;
        }
    }
}
