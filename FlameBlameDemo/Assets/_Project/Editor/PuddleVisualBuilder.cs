using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class PuddleVisualBuilder
    {
        const string Art="Assets/_Project/Art/Puddles/";
        static PuddleVisualBuilder() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(!File.Exists("Temp/ApplyPuddles") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Temp/ApplyPuddles"); Apply();
        }
        [MenuItem("Tools/FlameBlame/Apply Puddle Visuals")]
        public static void Apply()
        {
            if(Application.isBatchMode) EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Shaders/PuddleToon.shadergraph");
            if(shader==null) throw new System.Exception("Puddle shader missing");
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/PuddleToon.mat");
            if(mat==null) { mat=new Material(shader); AssetDatabase.CreateAsset(mat,"Assets/_Project/Materials/PuddleToon.mat"); }
            mat.shader=shader; mat.SetFloat("_EnvironmentReflections",0); mat.SetFloat("_Surface",0); mat.SetFloat("_AlphaClip",1);
            for(int v=0;v<5;v++)
            {
                string path=Art+(v==4?"Noise":"Mask"+v)+".png";
                if(!File.Exists(path)) Generate(path,v);
                AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture=false; importer.mipmapEnabled=false; importer.wrapMode=v==4?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                mat.SetTexture(v==4?"_Noise":"_Mask"+v,AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }
                        System.Type importerType=null; foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) { var candidate=assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter"); if(candidate!=null) importerType=candidate; }
            if(importerType!=null)
            foreach(var method in importerType.GetMethods(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public))
                if(method.Name=="GetShaderText" && method.GetParameters().Length==4 && method.GetParameters()[3].IsOut)
                    File.WriteAllText("Temp/PuddleGenerated.shader",(string)method.Invoke(null,new object[]{"Assets/_Project/Shaders/PuddleToon.shadergraph",null,null,null}));
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); mat.EnableKeyword("_ALPHATEST_ON");
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name="Puddle"; quad.transform.rotation=Quaternion.Euler(90,0,0);
            var renderer=quad.GetComponent<MeshRenderer>(); renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=true;
            PrefabUtility.SaveAsPrefabAsset(quad,"Assets/_Project/Prefabs/Puddle.prefab");
            Object.DestroyImmediate(quad);
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(r.name!="Template_Puddle") continue;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Puddle.prefab");
                r.GetComponent<MeshFilter>().sharedMesh=prefab.GetComponent<MeshFilter>().sharedMesh;
                r.sharedMaterial=mat; r.transform.localRotation=Quaternion.Euler(90,0,0); r.transform.localScale=Vector3.one;
                r.shadowCastingMode=ShadowCastingMode.Off; r.receiveShadows=true;
                EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
            }
            EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes();
            Capture();
            if(Application.isBatchMode) VerifyPool();
            File.WriteAllText("Captures/Puddles/verification.txt","Applied; shader supported="+shader.isSupported+"; errors="+ShaderUtil.ShaderHasError(shader));
        }
        static void VerifyPool()
        {
            var root=new GameObject("PuddlePoolVerification");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Puddle.prefab");
            var template=Object.Instantiate(prefab,root.transform);
            var factory=new FlameBlame.Feedback.PuddleFactory(root.transform,template.transform,new FlameBlame.Electric.ElectricitySystem(),2);
            factory.Spawn(new Vector3(0,100,0),2); factory.Tick(Time.time+.5f);
            var block=new MaterialPropertyBlock(); bool checkedView=false;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(block);
                if(block.GetFloat("_Fill")<.99f || renderer.sharedMaterial!=prefab.GetComponent<Renderer>().sharedMaterial) throw new System.Exception("Puddle pool fill/shared material check failed");
                checkedView=true;
            }
            if(!checkedView) throw new System.Exception("Puddle pool spawn missing");
            factory.Tick(Time.time+23);
            if(root.GetComponentsInChildren<Renderer>().Length!=0) throw new System.Exception("Puddle pool expiration failed");
            factory.Spawn(new Vector3(0,100,0),2);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>()) { renderer.GetPropertyBlock(block); if(block.GetFloat("_Fill")!=0) throw new System.Exception("Puddle reuse retained fill"); }
            Object.DestroyImmediate(root); Debug.Log("Puddle pool smoke PASS: spawn, fill, shared material, expiration, reuse");
        }
        [MenuItem("Tools/FlameBlame/Capture Puddles")]
        public static void Capture()
        {
            Directory.CreateDirectory("Captures/Puddles");
            var root=new GameObject("PuddleCapture");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Puddle.prefab");
            var views=new GameObject[3];
            for(int i=0;i<3;i++) { views[i]=Object.Instantiate(prefab,root.transform); views[i].transform.position=new Vector3(i*.65f,100+i*.0007f,0); views[i].transform.localScale=Vector3.one*2.5f; }
                        var shadow=GameObject.CreatePrimitive(PrimitiveType.Cylinder); shadow.transform.SetParent(root.transform); shadow.transform.position=new Vector3(0,100.3f,0); shadow.transform.rotation=Quaternion.Euler(0,0,90); shadow.transform.localScale=new Vector3(.06f,.8f,.06f); shadow.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Hose_Lit.mat");
            var cameraObject=new GameObject("PuddleCamera"); cameraObject.transform.SetParent(root.transform);
            var camera=cameraObject.AddComponent<Camera>(); camera.transform.position=new Vector3(.6f,104,-2.8f); camera.transform.LookAt(new Vector3(.6f,100,0));
            camera.orthographic=true; camera.orthographicSize=1.8f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.24f,.19f,.13f);
            camera.farClipPlane=10;
            var target=new RenderTexture(1024,768,24); camera.targetTexture=target;
            foreach(var mode in new[]{"Single","Overlap","Shadow","Electrified","Grow","Dry"})
            {
                shadow.SetActive(mode=="Shadow");
                for(int i=0;i<3;i++)
                {
                    views[i].SetActive(mode=="Overlap" || i==0);
                    var block=new MaterialPropertyBlock(); block.SetFloat("_Fill",mode=="Grow"?.45f:mode=="Dry"?.2f:1f); block.SetFloat("_MaskIndex",i); block.SetFloat("_Electrified",mode=="Electrified"?1:0);
                    views[i].GetComponent<Renderer>().SetPropertyBlock(block);
                }
                camera.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
                var image=new Texture2D(1024,768,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1024,768),0,0); image.Apply();
                File.WriteAllBytes("Captures/Puddles/"+mode+".png",image.EncodeToPNG()); RenderTexture.active=previous; Object.DestroyImmediate(image);
            }
            camera.targetTexture=null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(root);
        }
        static void Generate(string path,int variant)
        {
            var tex=new Texture2D(256,256,TextureFormat.RGBA32,false,true);
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                float u=x/256f,v=y/256f;
                if(variant==4)
                {
                    // Periodic Fourier noise: opposite edges join without seams.
                    float a=0,b=0;
                    for(int k=1;k<=5;k++)
                    { a+=Mathf.Sin((u*k+v*(k%3+1))*Mathf.PI*2+k)*.16f/k;
                      b+=Mathf.Cos((u*(k%2+1)-v*k)*Mathf.PI*2+k*2)*.2f/k; }
                    pixels[y*256+x]=new Color(.5f+a,.5f+b,0,1); continue;
                }
                float angle=Mathf.Atan2(v-.5f,u-.5f);
                float radius=.29f+.045f*Mathf.Sin(angle*5+variant*2)+.028f*Mathf.Sin(angle*8-variant);
                float distance=radius-new Vector2(u-.5f,(v-.5f)*1.07f).magnitude;
                for(int d=0;d<3+variant%2;d++)
                {
                    float da=d*2.1f+variant*.7f;
                    var center=new Vector2(.5f+Mathf.Cos(da)*.405f,.5f+Mathf.Sin(da)*.405f);
                    distance=Mathf.Max(distance,.027f+(d%2)*.009f-Vector2.Distance(new Vector2(u,v),center));
                }
                float value=Mathf.SmoothStep(0,1,Mathf.Clamp01((distance+.02f)/.045f));
                pixels[y*256+x]=new Color(value,value,value,1);
            }
            tex.SetPixels(pixels); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        }
    }
}
