using System.IO;
using FlameBlame.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace FlameBlame.EditorTools
{
    public static class ScenarioCapture
    {
        [MenuItem("Tools/FlameBlame/Capture Phase 14")]
        public static void RebuildAndCapture()
        {
            MainLevelRebuilder.Rebuild();
            SubmissionBuild.Validate();
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) throw new System.InvalidOperationException("Capture needs a graphics device; omit -nographics.");
            var camera=Object.FindFirstObjectByType<GameBootstrap>().GetComponentInChildren<Camera>();
            if(camera==null) camera=Camera.main;
            if(camera==null) foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if(c.GetUniversalAdditionalCameraData().renderType==CameraRenderType.Base) {camera=c; break;}
            var pos=camera.transform.position; var rot=camera.transform.rotation;
            bool asyncShaders=ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation=false;
            try
            {
                Capture(camera,new Vector3(-16,6,-11),new Vector3(-1,4,0),"Street");
                Capture(camera,new Vector3(-2,5.1f,-2),new Vector3(.5f,4.2f,1),"Furniture");
            }
            finally {camera.transform.SetPositionAndRotation(pos,rot); ShaderUtil.allowAsyncCompilation=asyncShaders;}
        }
        static void Capture(Camera camera,Vector3 pos,Vector3 target,string name)
        {
            camera.transform.position=pos; camera.transform.LookAt(target);
            var rt=new RenderTexture(1280,720,24); var previous=RenderTexture.active;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=rt});
                RenderTexture.active=rt; pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                Directory.CreateDirectory("Captures/Phase14"); File.WriteAllBytes("Captures/Phase14/"+name+".png",pixels.EncodeToPNG());
            }
            finally {RenderTexture.active=previous; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(pixels);}
        }
    }
}
