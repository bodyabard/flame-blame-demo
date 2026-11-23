using System.IO;
using FlameBlame.Ladder;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace FlameBlame.EditorTools
{
    public static class LadderBuilder
    {
        const string Root="Assets/_Project/";
        [MenuItem("Tools/FlameBlame/Build Ladder Prefab")]
        public static void Build()
        {
            var tuning=AssetDatabase.LoadAssetAtPath<LadderTuning>(Root+"Settings/LadderTuning.asset");
            if(tuning==null) {tuning=ScriptableObject.CreateInstance<LadderTuning>(); AssetDatabase.CreateAsset(tuning,Root+"Settings/LadderTuning.asset");}
            var red=Material("LadderRed",new Color(.6f,.055f,.025f),false);
            var grey=Material("LadderAluminium",new Color(.62f,.64f,.61f),false);
            var rubber=Material("LadderRubber",new Color(.055f,.045f,.04f),false);
            var green=Material("LadderPreviewValid",new Color(.1f,.9f,.2f,.35f),true);
            var invalid=Material("LadderPreviewInvalid",new Color(.9f,.08f,.03f,.35f),true);
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); tags.FindProperty("layers").GetArrayElementAtIndex(13).stringValue="Ladder"; tags.ApplyModifiedProperties();
            var go=new GameObject("PortableLadder"); go.layer=13;
            var ladder=go.AddComponent<LadderStateMachine>(); ladder.tuning=tuning;
            var body=go.GetComponent<Rigidbody>(); body.mass=12; body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            for(int side=-1;side<=1;side+=2)
            {
                Part(go.transform,"Rail",new Vector3(side*.28f,0,0),new Vector3(.085f,tuning.length,.1f),red,true);
                Part(go.transform,"Foot",new Vector3(side*.28f,-tuning.length*.5f+.08f,0),new Vector3(.13f,.16f,.15f),rubber,false);
                Part(go.transform,"Hook",new Vector3(side*.28f,tuning.length*.5f-.05f,.1f),new Vector3(.09f,.1f,.28f),grey,false);
            }
            for(float y=-tuning.length*.5f+.25f;y<tuning.length*.5f-.1f;y+=.3f) Part(go.transform,"Rung",new Vector3(0,y,0),new Vector3(.5f,.055f,.085f),grey,false);
            var rungs=go.AddComponent<BoxCollider>(); rungs.size=new Vector3(.52f,tuning.length-.2f,.08f);
            var trigger=new GameObject("ClimbTrigger"); trigger.layer=13; trigger.transform.SetParent(go.transform,false); var climb=trigger.AddComponent<BoxCollider>(); climb.isTrigger=true; climb.center=new Vector3(0,0,-.4f); climb.size=new Vector3(1,tuning.length,1);
            var ghost=new GameObject("PlacementGhost"); ghost.transform.SetParent(go.transform,false);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
            {var copy=new GameObject("GhostPart"); copy.transform.SetParent(ghost.transform,false); copy.transform.localPosition=renderer.transform.localPosition; copy.transform.localScale=renderer.transform.localScale; copy.AddComponent<MeshFilter>().sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh; var r=copy.AddComponent<MeshRenderer>(); r.sharedMaterial=green; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            ghost.SetActive(false); ladder.ghost=ghost.transform; ladder.validPreview=green; ladder.invalidPreview=invalid;
            tuning.pickup=Tone("ladder-pickup",320,.12f); tuning.place=Tone("ladder-place",760,.25f); tuning.step=Tone("ladder-step",190,.09f); tuning.impact=Tone("ladder-impact",95,.4f); EditorUtility.SetDirty(tuning);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/PortableLadder.prefab"); Object.DestroyImmediate(go); AssetDatabase.SaveAssets();
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collider)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.layer=parent.gameObject.layer; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material; if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());}
        static Material Material(string name,Color color,bool transparent)
        {
            string path=Root+"Materials/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.05f); m.SetFloat("_Metallic",0);
            if(transparent) {m.SetFloat("_Surface",1); m.SetFloat("_SrcBlend",5); m.SetFloat("_DstBlend",10); m.SetFloat("_ZWrite",0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue=3000;}
            EditorUtility.SetDirty(m); return m;
        }
        static AudioClip Tone(string name,float frequency,float duration)
        {
            string path=Root+"Audio/"+name+".wav";
            using(var writer=new BinaryWriter(File.Create(path)))
            {int count=(int)(22050*duration); writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+count*2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(22050); writer.Write(44100); writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count*2); for(int i=0;i<count;i++) {float t=i/22050f; float value=(Mathf.Sin(t*frequency*6.283f)+.3f*Mathf.Sin(t*frequency*15.37f))*Mathf.Exp(-t/duration*8); writer.Write((short)(value*9000));}}
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        [MenuItem("Tools/FlameBlame/Place Ladder In Main")]
        public static void PlaceInMain()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=Root+"Scenes/Main.unity") EditorSceneManager.OpenScene(Root+"Scenes/Main.unity");
            Build(); var existing=Object.FindFirstObjectByType<LadderStateMachine>(); if(existing!=null) Object.DestroyImmediate(existing.gameObject);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/PortableLadder.prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetPositionAndRotation(new Vector3(-5.3f,.18f,2.8f),Quaternion.Euler(90,0,0));
            EditorSceneManager.MarkSceneDirty(go.scene); EditorSceneManager.SaveScene(go.scene);
        }
    }
}



