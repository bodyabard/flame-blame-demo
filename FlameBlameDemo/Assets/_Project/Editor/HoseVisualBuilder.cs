using FlameBlame.Core;
using FlameBlame.Hose;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace FlameBlame.EditorTools
{
    public static class HoseVisualBuilder
    {
        [MenuItem("Tools/FlameBlame/Apply Hose Visuals")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var hose=Object.FindFirstObjectByType<HoseController>();
            var profile=AssetDatabase.LoadAssetAtPath<ScaleProfile>("Assets/_Project/Settings/ScaleProfile.asset");
            var serialized=new SerializedObject(hose);
            serialized.FindProperty("scale").objectReferenceValue=profile;
            serialized.FindProperty("radius").floatValue=profile.tubeRadius*.9f;
            serialized.FindProperty("collisionMask").intValue=LayerMask.GetMask("Default","Floor","Interactable");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var tube=hose.GetComponent<HoseTubeRenderer>();
            if(tube==null) tube=hose.gameObject.AddComponent<HoseTubeRenderer>();
            var yellow=Material("Hose_Lit",new Color(.93f,.78f,.27f));
            var brass=Material("Hose_Brass",new Color(.68f,.43f,.16f));
            hose.GetComponent<MeshRenderer>().sharedMaterial=yellow;
            var tubeData=new SerializedObject(tube);
            foreach(string end in new[]{"startCoupling","endCoupling"})
            {
                var old=hose.transform.Find(end);
                if(old!=null) Object.DestroyImmediate(old.gameObject);
                var root=new GameObject(end).transform; root.SetParent(hose.transform,false);
                Part(root,"Sleeve",PrimitiveType.Cylinder,new Vector3(.23f,.10f,.23f),Vector3.zero,brass);
                Part(root,"Ring",PrimitiveType.Cylinder,new Vector3(.27f,.025f,.27f),new Vector3(0,0,.07f),brass);
                tubeData.FindProperty(end).objectReferenceValue=root;
            }
            tubeData.ApplyModifiedPropertiesWithoutUndo();
            var fallback=new SerializedObject(hose.GetComponent<HoseRenderer>());
            var nozzle=fallback.FindProperty("nozzleModel").objectReferenceValue as Transform;
            if(nozzle!=null) foreach(var renderer in nozzle.GetComponentsInChildren<Renderer>())
            {renderer.sharedMaterial=brass; renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true;}
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hose.gameObject.scene); EditorSceneManager.SaveOpenScenes();
        }
        static Material Material(string name,Color color)
        {
            string path="Assets/_Project/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) {mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color); mat.SetFloat("_Metallic",0); mat.SetFloat("_Smoothness",.08f);
            mat.SetFloat("_ReceiveShadows",1); EditorUtility.SetDirty(mat); return mat;
        }
        static void Part(Transform parent,string name,PrimitiveType type,Vector3 scale,Vector3 position,Material material)
        {
            var part=GameObject.CreatePrimitive(type); part.name=name; part.transform.SetParent(parent,false);
            Object.DestroyImmediate(part.GetComponent<Collider>()); part.transform.localPosition=position;
            part.transform.localRotation=Quaternion.Euler(90,0,0); part.transform.localScale=scale;
            if(name=="Ring")
            {
                const string path="Assets/_Project/Materials/HoseHexRing.asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null)
                {
                    mesh=new Mesh {name="Hexagonal coupling ring"};
                    var vertices=new Vector3[24]; var triangles=new int[36];
                    for(int i=0;i<6;i++)
                    {
                        float a=i*Mathf.PI/3f,b=(i+1)*Mathf.PI/3f;
                        int v=i*4,t=i*6;
                        vertices[v]=new Vector3(Mathf.Cos(a)*.5f,-1,Mathf.Sin(a)*.5f);
                        vertices[v+1]=new Vector3(Mathf.Cos(b)*.5f,-1,Mathf.Sin(b)*.5f);
                        vertices[v+2]=new Vector3(Mathf.Cos(a)*.5f,1,Mathf.Sin(a)*.5f);
                        vertices[v+3]=new Vector3(Mathf.Cos(b)*.5f,1,Mathf.Sin(b)*.5f);
                        triangles[t]=v; triangles[t+1]=v+2; triangles[t+2]=v+1;
                        triangles[t+3]=v+1; triangles[t+4]=v+2; triangles[t+5]=v+3;
                    }
                    mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals();
                    AssetDatabase.CreateAsset(mesh,path);
                }
                part.GetComponent<MeshFilter>().sharedMesh=mesh;
            }
            part.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}



