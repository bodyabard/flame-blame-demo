using System.IO;
using System.Reflection;
using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEditor;
using UnityEngine;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class FlightLevelVerification
    {
        sealed class InputSource : IPlayerInput, IHoseInput
        {
            public Vector2 Move {get;set;}
            public Vector2 Look=>Vector2.zero;
            public bool JumpPressed=>false;
            public bool CaptureCursorPressed=>false;
            public bool ReleaseCursorPressed=>false;
            public bool TogglePressed=>false;
            public bool ModePressed=>false;
            public bool ValveHeld {get;set;}
        }
        static InputSource input;
        static double start;
        static int stage, errors;
        static Vector3 initial;
        static string report;
        static FlightLevelVerification() {EditorApplication.update+=Tick; Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error || t==LogType.Exception) errors++;};}
        static void Set(object instance,string field,object value) => instance.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(instance,value);
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(File.Exists("Temp/FlameBlame-VerifyFlight") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/FlameBlame-VerifyFlight"); UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity"); SessionState.SetBool("FlightVerify",true); EditorApplication.isPlaying=true;
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool("FlightVerify",false)) return;
            if(start==0) {start=EditorApplication.timeSinceStartup; stage=0; errors=0; report="";}
            if(EditorApplication.timeSinceStartup-start<1+stage*2) return;
            Directory.CreateDirectory("Captures/Flight");
            var motor=Object.FindFirstObjectByType<PlayerMotor>(); var hose=Object.FindFirstObjectByType<HoseController>();
            if(stage==0)
            {
                input=new InputSource(); Set(motor,"input",input); Set(hose,"input",input); Set(Object.FindFirstObjectByType<JetController>(),"input",input);
                initial=motor.transform.position; hose.Modes.Tick(true,true); input.Move=Vector2.zero;
                ScreenCapture.CaptureScreenshot("Captures/Flight/exterior.png");
            }
            if(stage==1)
            {
                report+="Jet selected, no water: displacement="+Vector3.Distance(initial,motor.transform.position)+" y="+motor.transform.position.y+"\n";
                input.Move=Vector2.zero; initial=motor.transform.position; input.ValveHeld=true;
            }
            if(stage==2)
            {
                report+="Thrust: pressure="+hose.Pressure+" rise="+(motor.transform.position.y-initial.y)+" direction="+hose.Muzzle.forward+"\n";
                float worst=0; int index=1;
                for(int i=1;i<hose.Segments-1;i++)
                {
                    float angle=Vector3.Angle(hose.Points[i]-hose.Points[i-1],hose.Points[i+1]-hose.Points[i]);
                    if(angle>worst) {worst=angle;index=i;}
                }
                report+="Worst bend="+worst+" at="+index+" points="+hose.Points[index-1]+" / "+hose.Points[index]+" / "+hose.Points[index+1]+"\n";
                foreach(var obstacle in Physics.OverlapSphere(hose.Points[index],.16f)) report+="Bend obstacle="+obstacle.name+" root="+obstacle.transform.root.name+"\n";
                ScreenCapture.CaptureScreenshot("Captures/Flight/thrust.png");
                input.ValveHeld=false;
            }
            if(stage==3)
            {
                hose.Modes.Tick(true,true);
                report+="Released: y="+motor.transform.position.y+" speed="+motor.Speed+"\n";
                int particles=0; foreach(var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) particles+=ps.particleCount;
                File.WriteAllText("Captures/Flight/verification.txt",report+"Errors="+errors+" particles="+particles+"\n");
                SessionState.SetBool("FlightVerify",false); start=0; EditorApplication.isPlaying=false;
            }
            stage++;
        }
    }
}
