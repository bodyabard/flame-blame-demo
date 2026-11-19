using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FlameBlame.EditorTools
{
    // File-based automation; never runs unless explicitly requested by a local marker.
    [InitializeOnLoad]
    public static class VisualBuildRequest
    {
        static VisualBuildRequest() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if(File.Exists("Temp/FlameBlame-SmokeVisuals") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/FlameBlame-SmokeVisuals"); SessionState.SetInt("FlameBlame.VisualSmoke",1); EditorApplication.isPlaying=true;
            }
            int smoke=SessionState.GetInt("FlameBlame.VisualSmoke",0);
            if(smoke==1 && EditorApplication.isPlaying)
            {
                SessionState.SetFloat("FlameBlame.VisualSmokeStart",(float)EditorApplication.timeSinceStartup); SessionState.SetInt("FlameBlame.VisualSmoke",2);
            }
            if(smoke==2 && EditorApplication.isPlaying && EditorApplication.timeSinceStartup-SessionState.GetFloat("FlameBlame.VisualSmokeStart",0)>4)
            {
                int count=0;
                foreach(var ps in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None)) count+=ps.particleCount;
                UnityEngine.Debug.Log("[FlameBlame] Visual smoke: active particles="+count);
                VisualSceneBuilder.Capture(); SessionState.SetInt("FlameBlame.VisualSmoke",3);
            }
            if(smoke==3 && EditorApplication.timeSinceStartup-SessionState.GetFloat("FlameBlame.VisualSmokeStart",0)>6)
            {
                SessionState.SetInt("FlameBlame.VisualSmoke",0); EditorApplication.isPlaying=false;
            }
            if(File.Exists("Temp/FlameBlame-CaptureVisuals") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete("Temp/FlameBlame-CaptureVisuals"); VisualSceneBuilder.Preview();
            }
            const string request="Temp/FlameBlame-BuildVisuals";
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            File.Delete(request);
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                UnityEngine.Debug.LogWarning("[FlameBlame] Save the current scene before Tools/FlameBlame/Build Scene."); return;
            }
            VisualSceneBuilder.Build();
        }
    }
}
