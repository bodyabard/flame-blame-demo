using System.IO;
using FlameBlame.Core;
using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class TwoStoryLevelBuilder
    {
        static TwoStoryLevelBuilder() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(!File.Exists("Temp/FlameBlame-TwoStory") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Temp/FlameBlame-TwoStory"); Apply();
        }
        [MenuItem("Tools/FlameBlame/Build Two Story Flight Level")]
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/_Project/Scenes/Main.unity") throw new System.InvalidOperationException("Open Main first.");
            const string path="Assets/_Project/Settings/FlightLevelSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<FlightLevelSettings>(path);
            if(settings==null) {settings=ScriptableObject.CreateInstance<FlightLevelSettings>(); AssetDatabase.CreateAsset(settings,path);}
            var scale=AssetDatabase.LoadAssetAtPath<ScaleProfile>("Assets/_Project/Settings/ScaleProfile.asset");
            var hose=Object.FindFirstObjectByType<HoseController>();
            var data=new SerializedObject(hose);
            var anchor=(Transform)data.FindProperty("anchor").objectReferenceValue;
            var hydrant=anchor;
            while(hydrant.parent!=null && hydrant.parent.name!="Room" && hydrant.parent.name!="Decor") hydrant=hydrant.parent;
            hydrant.SetParent(null,true); hydrant.position+=settings.hydrant-new Vector3(hydrant.position.x,0,hydrant.position.z);
            var upper=GameObject.Find("SecondFloorContents");
            if(upper==null)
            {
                upper=new GameObject("SecondFloorContents");
                foreach(string name in new[]{"Room","Zone_A_Fabric_Paper","Zone_B_Wood_Metal","Phase2","Cool Fill","Warm Interior Bounce"})
                {
                    var go=GameObject.Find(name); if(go!=null && go!=hydrant.gameObject) go.transform.SetParent(upper.transform,true);
                }
            }
            upper.transform.position=Vector3.up*settings.floorHeight;
            var room=upper.transform.Find("Room");
            var wall=room.Find("Wall_W");
            var material=wall.GetComponentInChildren<Renderer>().sharedMaterial;
            foreach(var r in wall.GetComponentsInChildren<Renderer>()) r.enabled=false;
            foreach(var c in wall.GetComponentsInChildren<Collider>()) c.enabled=false;
            var wainscot=room.Find("Decor/WainscotW"); if(wainscot!=null) wainscot.gameObject.SetActive(false);
            var old=GameObject.Find("FlightExterior"); if(old!=null) Object.DestroyImmediate(old);
            var exterior=new GameObject("FlightExterior").transform;
            float x=-scale.roomWidth*.5f-.1f, length=scale.roomLength;
            float side=(length-settings.windowWidth)*.5f;
            Box(exterior,"WindowWallLeft",new Vector3(x,settings.floorHeight+scale.roomHeight*.5f,-(settings.windowWidth+side)*.5f),new Vector3(.2f,scale.roomHeight,side),material);
            Box(exterior,"WindowWallRight",new Vector3(x,settings.floorHeight+scale.roomHeight*.5f,(settings.windowWidth+side)*.5f),new Vector3(.2f,scale.roomHeight,side),material);
            Box(exterior,"WindowBelow",new Vector3(x,settings.floorHeight+settings.windowBottom*.5f,0),new Vector3(.2f,settings.windowBottom,settings.windowWidth),material);
            float top=scale.roomHeight-settings.windowBottom-settings.windowHeight;
            Box(exterior,"WindowAbove",new Vector3(x,settings.floorHeight+scale.roomHeight-top*.5f,0),new Vector3(.2f,top,settings.windowWidth),material);
            var trim=Mat("FlightTrim",new Color(.62f,.38f,.2f));
            Rail(exterior,"RoundedSill",new Vector3(x-.08f,settings.floorHeight+settings.windowBottom,0),settings.windowWidth+.25f,2,trim);
            foreach(int sign in new[]{-1,1}) Rail(exterior,"RoundedJamb",new Vector3(x-.08f,settings.floorHeight+settings.windowBottom+settings.windowHeight*.5f,sign*settings.windowWidth*.5f),settings.windowHeight,1,trim);
            var ground=Mat("FlightGround",new Color(.37f,.36f,.21f));
            var pavement=Mat("FlightPavement",new Color(.57f,.47f,.37f));
            Box(exterior,"Yard",new Vector3(-3,-.15f,0),new Vector3(settings.yardSize,.3f,settings.yardSize),ground,LayerMask.NameToLayer("Floor"));
            Box(exterior,"Sidewalk",new Vector3(-5,-.02f,0),new Vector3(4,.06f,12),pavement,LayerMask.NameToLayer("Floor"));
            // Leave the room floor as the only visible surface at the storey boundary.
            Box(exterior,"GroundFloor",new Vector3(0,(settings.floorHeight-.22f)*.5f,0),new Vector3(scale.roomWidth+.2f,settings.floorHeight-.22f,scale.roomLength+.2f),material);
            Box(exterior,"BlockedDoor",new Vector3(x-.06f,1.05f,-1.6f),new Vector3(.12f,2.1f,.8f),trim);
            for(int i=0;i<2;i++) Box(exterior,"BoardedDoor",new Vector3(x-.15f,.6f+i*.8f,-1.6f),new Vector3(.1f,.12f,1),pavement);
            var dark=Mat("FlightDarkGlass",new Color(.18f,.15f,.16f));
            Box(exterior,"ClosedWindow",new Vector3(x-.06f,1.7f,1.3f),new Vector3(.1f,1,.9f),dark);
            for(int i=0;i<3;i++)
            {
                Box(exterior,"TreeTrunk",new Vector3(-8,1,4+i*2),new Vector3(.25f,2,.25f),trim);
                var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere); leaf.name="TreeCanopy"; leaf.transform.SetParent(exterior,false); leaf.transform.position=new Vector3(-8,2.5f,4+i*2); leaf.transform.localScale=Vector3.one*1.7f; leaf.GetComponent<Renderer>().sharedMaterial=ground; leaf.isStatic=true;
            }
            for(int i=0;i<12;i++) Box(exterior,"Fence",new Vector3(-14,.6f,-6+i),new Vector3(.15f,1.2f,.6f),trim);
            var player=Object.FindFirstObjectByType<PlayerMotor>();
            player.transform.SetPositionAndRotation(settings.spawn,Quaternion.Euler(0,90,0));
            var look=new SerializedObject(player.GetComponent<PlayerLook>()); look.FindProperty("initialPitch").floatValue=-18; look.ApplyModifiedPropertiesWithoutUndo();
            float route=Vector3.Distance(anchor.position,new Vector3(x,settings.floorHeight+settings.windowBottom,0))+new Vector2(scale.roomWidth,scale.roomLength*.5f).magnitude;
            float segment=data.FindProperty("segmentLength").floatValue;
            data.FindProperty("segments").intValue=Mathf.CeilToInt(route*settings.hoseSlack/(segment*.92f))+1;
            data.FindProperty("confineToRoom").boolValue=false;
            data.ApplyModifiedPropertiesWithoutUndo();
            var gate=new GameObject("WindowEntryGate"); gate.transform.SetParent(exterior,false);
            gate.transform.position=new Vector3(x+.35f,settings.floorHeight+settings.windowBottom+settings.windowHeight*.5f,0);
            var trigger=gate.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(.35f,settings.windowHeight,settings.windowWidth);
            var entry=gate.AddComponent<WindowEntryGate>();
            var boot=new SerializedObject(Object.FindFirstObjectByType<GameBootstrap>()); boot.FindProperty("entryGate").objectReferenceValue=entry; boot.ApplyModifiedPropertiesWithoutUndo();
            foreach(var asset in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Mobile_RPAsset.asset"}) {var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(asset); if(rp!=null) {rp.shadowDistance=35; EditorUtility.SetDirty(rp);}}
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Physics.SyncTransforms();
            var opening=new Vector3(x,settings.floorHeight+settings.windowBottom+settings.windowHeight*.5f,0);
            foreach(var collider in exterior.GetComponentsInChildren<Collider>())
                if(!collider.isTrigger && collider.Raycast(new Ray(opening-Vector3.right,Vector3.right),out _,2))
                    throw new System.InvalidOperationException("Window opening obstructed by "+collider.name);
            Directory.CreateDirectory("Captures/Flight");
            File.WriteAllText("Captures/Flight/level.txt","Floor="+settings.floorHeight+" Window="+settings.windowWidth+"x"+settings.windowHeight+" sill="+(settings.floorHeight+settings.windowBottom)+"\nSpawn="+player.transform.position+" Hydrant="+anchor.position+"\nSegments="+data.FindProperty("segments").intValue+" SegmentLength="+segment+" Reach="+(data.FindProperty("segments").intValue-1)*segment*.92f+"\nWindow ray clear; yard="+settings.yardSize+"\n");
            Debug.Log("[FlameBlame] Two-story level: floor="+settings.floorHeight+" reach="+route*settings.hoseSlack+" segments="+data.FindProperty("segments").intValue);
        }
        static Material Mat(string name,Color color)
        {
            string path="Assets/_Project/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
            m.color=color; return m;
        }
        static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,int layer=0)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.position=position; go.transform.localScale=size; go.layer=Mathf.Max(0,layer); go.GetComponent<Renderer>().sharedMaterial=material; go.isStatic=true; return go;
        }
        static void Rail(Transform parent,string name,Vector3 position,float length,int axis,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name=name; go.transform.SetParent(parent,false); go.transform.position=position;
            go.transform.localScale=new Vector3(.12f,length*.5f,.12f); if(axis==2) go.transform.rotation=Quaternion.Euler(90,0,0);
            go.GetComponent<Renderer>().sharedMaterial=material; go.isStatic=true;
        }
    }
}
