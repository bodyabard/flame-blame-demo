using System.IO;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Fire;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static class VisualSceneBuilder
    {
        const string Root = "Assets/_Project";
        static Material Mat(string name, Color color)
        {
            string path = ToonMaterialMigration.OriginalPath("Visual_" + name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", .18f);
            mat.EnableKeyword("_EMISSION"); EditorUtility.SetDirty(mat); return mat;
        }

        [MenuItem("Tools/FlameBlame/Build Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before building.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Root + "/Scenes/Main.unity");
            Apply();
            FeelSceneBuilder.Apply();
            ToonMaterialMigration.ReapplyIfConfigured();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Preview();
            Debug.Log("[FlameBlame] Visual scene built. Capacity: 16x36 + 240 + 6x12 + 8x10 + 4x8 = 1000.");
        }

        static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }

        static void Apply()
        {
            Directory.CreateDirectory(Root + "/Prefabs/Visuals");
            string path = Root + "/Settings/VisualTuning.asset";
            var t = AssetDatabase.LoadAssetAtPath<VisualTuning>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<VisualTuning>(); AssetDatabase.CreateAsset(t, path); }
            EditorUtility.SetDirty(t);
            var wall = Mat("Sage", t.wall); var wood = Mat("Walnut", t.wood);
            var fabric = Mat("Teal", t.fabric); var coral = Mat("Coral", t.curtain);
            var green = Mat("Leaf", t.foliage); var cream = Mat("Cream", new Color(.98f,.83f,.54f));
            var purple = Mat("Petal", new Color(.6f,.17f,.62f));
            foreach (var r in GameObject.Find("Room").GetComponentsInChildren<Renderer>())
            {
                if (r.name.StartsWith("Wall") || r.name == "Ceiling") r.sharedMaterial = wall;
                if(r.name=="Ceiling") r.shadowCastingMode=ShadowCastingMode.Off;
            }
            foreach (var f in Object.FindObjectsByType<Flammable>(FindObjectsSortMode.None))
            {
                if(PrefabUtility.IsPartOfPrefabInstance(f.gameObject)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(f.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                for (int child=f.transform.childCount-1;child>=0;child--)
                {
                    var part=f.transform.GetChild(child);
                    if (part.name=="ImportedVisual" || part.name=="Cushion" || part.name=="Book" || part.name=="Fold" || part.name=="Rail" || part.name=="Stem" || part.name=="Petal" || part.name=="Pollen" || part.name=="Spout" || part.name=="HandleTop" || part.name=="HandleSide") Object.DestroyImmediate(part.gameObject);
                }
                string n = f.name;
                if (n == "Sofa" || n == "Armchair")
                {
                    foreach (var r in f.GetComponentsInChildren<Renderer>()) r.sharedMaterial = fabric;
                    if (n == "Sofa") for (int i = -1; i <= 1; i++)
                        Part(f.transform, "Cushion", new Vector3(i*.7f,.57f,.04f), new Vector3(.64f,.18f,.75f), fabric);
                }
                if (n.StartsWith("Shelf"))
                    for (int row = 0; row < 3; row++) for (int i = 0; i < 7; i++)
                    {
                        var book = Part(f.transform, "Book", new Vector3(-.65f+i*.21f,.32f+row*.55f,0), new Vector3(.12f,.32f+(i%3)*.04f,.24f), i%3==0 ? coral : i%3==1 ? cream : green);
                        book.transform.localRotation = Quaternion.Euler(0,0,(i%3-1)*8);
                    }
                if (n == "Curtain")
                {
                    foreach (var r in f.GetComponentsInChildren<Renderer>()) r.sharedMaterial = coral;
                    for (int i = 0; i < 12; i++) Part(f.transform,"Fold",new Vector3(.07f,0,-1.1f+i*.2f),new Vector3(.16f,2.3f,.16f),coral,PrimitiveType.Cylinder);
                    Part(f.transform,"Rail",new Vector3(0,1.22f,0),new Vector3(.12f,.12f,2.8f),wood);
                }
                if (n == "Flowerpot")
                {
                    Part(f.transform,"Stem",new Vector3(0,1.4f,0),new Vector3(.12f,1.2f,.12f),green,PrimitiveType.Cylinder);
                    for (int i=0;i<7;i++)
                    {
                        float a = i*Mathf.PI*2/7;
                        Part(f.transform,"Petal",new Vector3(Mathf.Cos(a)*.48f,2.6f+Mathf.Sin(a)*.48f,0),new Vector3(.42f,.42f,.16f),purple,PrimitiveType.Sphere);
                    }
                    Part(f.transform,"Pollen",new Vector3(0,2.6f,.1f),Vector3.one*.35f,cream,PrimitiveType.Sphere);
                }
                if (n == "WateringCan")
                {
                    var spout = Part(f.transform,"Spout",new Vector3(.9f,.3f,0),new Vector3(1.4f,.25f,.25f),green);
                    spout.transform.localRotation=Quaternion.Euler(0,0,28);
                    Part(f.transform,"HandleTop",new Vector3(-.8f,.6f,0),new Vector3(.9f,.13f,.14f),green);
                    Part(f.transform,"HandleSide",new Vector3(-1.2f,0,0),new Vector3(.13f,1.2f,.14f),green);
                }
                if(n=="Sofa" || n=="Armchair") ImportFurniture(f.transform,n=="Sofa"?"loungeSofaLong":"loungeChair",n=="Sofa"?2.4f:.9f,fabric);
            }
            var hydrant=GameObject.Find("Hydrant").transform;
            if(PrefabUtility.IsPartOfPrefabInstance(hydrant.gameObject)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(hydrant.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            if(hydrant.Find("Cap")!=null) Object.DestroyImmediate(hydrant.Find("Cap").gameObject);
            if(hydrant.Find("Valve")!=null) Object.DestroyImmediate(hydrant.Find("Valve").gameObject);
            Part(hydrant,"Cap",new Vector3(0,.95f,0),new Vector3(.5f,.2f,.5f),coral,PrimitiveType.Sphere);
            Part(hydrant,"Valve",new Vector3(0,1.1f,0),new Vector3(.55f,.08f,.12f),wood);
            // Reusable, connected prefabs retain all original physics and fire profiles.
            foreach (string name in new[]{"Sofa","Armchair","Curtain","Shelf_1","Shelf_2","Flowerpot","WateringCan","Hydrant","SparkCable"})
            {
                var go=GameObject.Find(name); if(go!=null) PrefabUtility.SaveAsPrefabAssetAndConnect(go,Root+"/Prefabs/Visuals/"+name+".prefab",InteractionMode.AutomatedAction);
            }
            Lighting(t);
            Effects(t);
        }

        static void ImportFurniture(Transform target,string model,float width,Material material)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Art/KenneyFurniture/Models/FBX format/"+model+".fbx");
            if(asset==null) { Debug.LogWarning("Furniture unavailable: "+model); return; }
            foreach(var renderer in target.GetComponentsInChildren<Renderer>()) renderer.enabled=false;
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset); instance.name="ImportedVisual";
            var renderers=instance.GetComponentsInChildren<Renderer>();
            Bounds bounds=new Bounds(); bool first=true;
            foreach(var renderer in renderers) {if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);renderer.sharedMaterial=material;}
            float scale=width/Mathf.Max(.001f,bounds.size.x);
            instance.transform.SetParent(target,false);
            instance.transform.localScale*=scale;
            instance.transform.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
        }

        [MenuItem("Tools/FlameBlame/Capture Scene Preview")]
        public static void Preview()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) return;
            var go=new GameObject("Preview Camera"); var camera=go.AddComponent<Camera>();
            go.transform.position=new Vector3(0,2.5f,-4.5f); go.transform.LookAt(new Vector3(-1,1.2f,1));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var rt=new RenderTexture(1280,720,24); var previous=RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=rt }); RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            Directory.CreateDirectory("Captures"); File.WriteAllBytes("Captures/Phase3-scene.png",image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous; rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image); Object.DestroyImmediate(go);
        }

        static void Lighting(VisualTuning t)
        {
            foreach(string rendererPath in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
            {
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if(renderer==null) continue;
                var settings=new SerializedObject(renderer); settings.FindProperty("m_RenderingMode").intValue=0; settings.ApplyModifiedPropertiesWithoutUndo();
                foreach(var feature in renderer.rendererFeatures) if(feature!=null && feature.name.Contains("AmbientOcclusion")) {feature.SetActive(false);EditorUtility.SetDirty(feature);}
                EditorUtility.SetDirty(renderer);
            }
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) Object.DestroyImmediate(l.gameObject);
            var sun=new GameObject("Sunset Key").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.color=t.sun; sun.intensity=t.sunIntensity; sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(38,-55,0);
            var bounce=new GameObject("Warm Interior Bounce").AddComponent<Light>(); bounce.type=LightType.Point;
            bounce.color=t.sun; bounce.intensity=t.bounceIntensity; bounce.range=12; bounce.shadows=LightShadows.None;
            bounce.transform.position=new Vector3(-2,2.6f,1.5f);
            var fill=new GameObject("Cool Fill").AddComponent<Light>(); fill.type=LightType.Directional;
            fill.color=t.fill; fill.intensity=t.fillIntensity; fill.shadows=LightShadows.None; fill.transform.rotation=Quaternion.Euler(30,130,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=t.fill*.4f;
            RenderSettings.sun=sun;
            var old=GameObject.Find("Visual Atmosphere"); if(old!=null) Object.DestroyImmediate(old);
            var volume=new GameObject("Visual Atmosphere").AddComponent<Volume>(); volume.isGlobal=true;
            string path=Root+"/Settings/SunsetVolume.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null) {profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
            if(!profile.TryGet<Bloom>(out var bloom)) {bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(t.bloom); bloom.threshold.Override(1f);
            if(!profile.TryGet<ColorAdjustments>(out var color)) {color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}
            color.saturation.Override(t.saturation); color.contrast.Override(8);
            if(!profile.TryGet<Vignette>(out var vignette)) {vignette=profile.Add<Vignette>(true);AssetDatabase.AddObjectToAsset(vignette,profile);}
            vignette.intensity.Override(t.vignette); volume.sharedProfile=profile; EditorUtility.SetDirty(profile);
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        }

        static void Layer(Transform parent,string name,Material mat,Color color,int cap,float speed,float size,float life,bool smoke=false)
        {
            var ps=new GameObject(name).AddComponent<ParticleSystem>(); ps.transform.SetParent(parent,false);
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.loop=true; main.playOnAwake=false; main.maxParticles=cap;
            main.startLifetime=life; main.startSpeed=speed; main.startSize=size; main.startColor=color;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission; emission.rateOverTime=cap/life;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=12; shape.radius=.15f; shape.rotation=new Vector3(-90,0,0);
            var colors=ps.colorOverLifetime; colors.enabled=true;
            var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(smoke?Color.black:color,.8f)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(color.a,.15f),new GradientAlphaKey(0,1)}); colors.color=gradient;
            var sizes=ps.sizeOverLifetime; sizes.enabled=true; sizes.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,smoke?.5f:1,1,smoke?2:0));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=mat;
        }
        static void Effects(VisualTuning t)
        {
            var bootstrap=Object.FindFirstObjectByType<GameBootstrap>(); var data=new SerializedObject(bootstrap);
            var flame=(ParticleSystem)data.FindProperty("flameTemplate").objectReferenceValue;
            // Existing root supplies tongues. Each clone has 12+6+14+4=36 particles.
            flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=flame.main; main.maxParticles=12; main.startColor=t.flame; main.startSize=t.flameSize; main.startSpeed=t.flameSpeed;
            for(int i=flame.transform.childCount-1;i>=0;i--) Object.DestroyImmediate(flame.transform.GetChild(i).gameObject);
            var flameMat=flame.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            var steam=(ParticleSystem)data.FindProperty("steamTemplate").objectReferenceValue;
            Layer(flame.transform,"Core",flameMat,t.core,6,.6f,.2f,.4f);
            Layer(flame.transform,"Smoke",steam.GetComponent<ParticleSystemRenderer>().sharedMaterial,t.smoke,14,t.smokeSpeed,.55f,2.8f,true);
            Layer(flame.transform,"Embers",flameMat,t.core,4,1.8f,.04f,.6f);
            var emitter=Object.FindFirstObjectByType<FlameBlame.Hose.JetEmitter>();
            var jetData=new SerializedObject(emitter); jetData.FindProperty("maxParticles").intValue=240; jetData.ApplyModifiedPropertiesWithoutUndo();
            var jet=(ParticleSystem)jetData.FindProperty("jet").objectReferenceValue; var jetMain=jet.main; jetMain.maxParticles=240;
        }

        [MenuItem("Tools/FlameBlame/Capture")]
        public static void Capture()
        {
            if (Object.FindFirstObjectByType<LookComparison>() != null)
            {
                LookTransfer.CaptureComparison();
                return;
            }
            Directory.CreateDirectory("Captures");
            ScreenCapture.CaptureScreenshot("Captures/Game-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");
            Debug.Log("[FlameBlame] Game view capture requested (requires Play mode).");
        }
    }
}
