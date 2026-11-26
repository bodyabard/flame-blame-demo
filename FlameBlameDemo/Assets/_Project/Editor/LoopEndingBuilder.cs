using System;
using System.IO;
using System.Linq;
using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class LoopEndingBuilder
    {
        const string Root="Assets/_Project/";
        static LoopEndingBuilder() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(File.Exists("Temp/FlameBlame-LoopVisuals"))
            {
                File.Delete("Temp/FlameBlame-LoopVisuals");
                var scene=EditorSceneManager.OpenScene(Root+"Scenes/Main.unity");
                InstallFlowerVisuals(UnityEngine.Object.FindFirstObjectByType<LoopRevealController>());
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            if(File.Exists("Temp/FlameBlame-LoopWebGL"))
            {
                File.Delete("Temp/FlameBlame-LoopWebGL");
                try { SubmissionBuild.BuildWebGL(); File.WriteAllText("Temp/Phase17-webgl.txt","PASS"); }
                catch(Exception e) {File.WriteAllText("Temp/Phase17-webgl.txt",e.ToString()); Debug.LogException(e);}
            }
            if(!File.Exists("Temp/FlameBlame-BuildLoop")) return;
            File.Delete("Temp/FlameBlame-BuildLoop");
            try { Build(); File.WriteAllText("Temp/Phase17-built.txt","PASS"); }
            catch(Exception e) { Debug.LogException(e); File.WriteAllText("Temp/Phase17-built.txt",e.ToString()); }
        }
        [MenuItem("Tools/FlameBlame/Build Loop Ending")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"Art/UI/Sprites/diary_paper.png");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spriteBorder=new Vector4(40,40,40,40); importer.SaveAndReimport();
            var scene=EditorSceneManager.OpenScene(Root+"Scenes/Main.unity");
            if(UnityEngine.Object.FindFirstObjectByType<LoopRevealController>()!=null) throw new InvalidOperationException("Loop already installed; edit existing tuning/components.");
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            var scenario=UnityEngine.Object.FindFirstObjectByType<LevelScenario>();
            var book=GameObject.Find("SpecialBook").AddComponent<SpecialBook>();
            var candidates=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Neighbor facade" || t.name=="SpecialNeighbor facade");
            var facade=candidates.OrderByDescending(t=>(t.position-scenario.hydrant.position).sqrMagnitude).First();
            var owner=new GameObject("Loop Ending"); var loop=owner.AddComponent<LoopRevealController>();
            loop.book=book; loop.facade=facade.gameObject;
            var center=facade.GetComponent<Renderer>().bounds.center; var size=facade.GetComponent<Renderer>().bounds.size; var mat=facade.GetComponent<Renderer>().sharedMaterial;
            loop.oldRoofAndWindows=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>(t.name=="Neighbor roof" || t.name=="Neighbor window") && Mathf.Abs(t.position.x-center.x)<size.x*.65f && Mathf.Abs(t.position.z-center.z)<size.z*.65f).Select(t=>t.gameObject).ToArray();
            loop.openHouse=new GameObject("SpecialNeighborOpen"); loop.openHouse.transform.SetParent(owner.transform); loop.openHouse.transform.position=center;
            var house=loop.openHouse.transform;
            Cube(house,"West",new Vector3(-size.x*.5f+.15f,0,0),new Vector3(.3f,size.y,size.z),mat);
            Cube(house,"East",new Vector3(size.x*.5f-.15f,0,0),new Vector3(.3f,size.y,size.z),mat);
            Cube(house,"North",new Vector3(0,0,size.z*.5f-.15f),new Vector3(size.x-.6f,size.y,.3f),mat);
            Cube(house,"South",new Vector3(0,0,-size.z*.5f+.15f),new Vector3(size.x-.6f,size.y,.3f),mat);
            Cube(house,"Floor",new Vector3(0,-size.y*.5f+.05f,0),new Vector3(size.x,.1f,size.z),mat);
            var green=Material("FlowerGreen",new Color(.18f,.4f,.12f)); var pot=Material("FlowerPot",new Color(.48f,.22f,.12f)); var petals=Material("FlowerPetals",new Color(.95f,.6f,.16f));
            float ground=-size.y*.5f;
            Cube(house,"Table",new Vector3(0,ground+.45f,0),new Vector3(1,.9f,1),pot);
            var flower=new GameObject("EndingFlower"); flower.transform.SetParent(house,false); flower.transform.localPosition=new Vector3(0,ground+1,0);
            Cube(flower.transform,"Pot",Vector3.zero,new Vector3(.45f,.3f,.45f),pot);
            Cube(flower.transform,"Stem",new Vector3(0,.45f,0),new Vector3(.08f,.7f,.08f),green);
            Cube(flower.transform,"Leaves",new Vector3(0,.4f,0),new Vector3(.55f,.09f,.17f),green);
            for(int i=0;i<5;i++) {float a=i*Mathf.PI*2/5; Cube(flower.transform,"Petal",new Vector3(Mathf.Cos(a)*.2f,.8f,Mathf.Sin(a)*.2f),new Vector3(.25f,.15f,.25f),petals);}
            var profile=ScriptableObject.CreateInstance<FireMaterialProfile>(); profile.burnDuration=100000; profile.spreadRadius=0; profile.spreadHeatPerSecond=0; profile.reheatPerSecond=0; profile.damageOnIgnite=profile.damageOnBurnOut=0; profile.flameScale=.5f; profile.waterCooling=80;
            AssetDatabase.CreateAsset(profile,Root+"Settings/Loop/EndingFlowerProfile.asset");
            var flammable=flower.AddComponent<Flammable>(); var so=new SerializedObject(flammable); so.FindProperty("profile").objectReferenceValue=profile; so.ApplyModifiedPropertiesWithoutUndo();
            var zone=flower.AddComponent<FireZone>(); zone.id=FireZoneId.Ending; zone.members=new[]{flammable};
            var light=flower.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.48f,.16f); light.intensity=3; light.range=8; light.shadows=LightShadows.None;
            loop.flower=flower.AddComponent<EndingFlower>(); loop.flower.flammable=flammable; loop.flower.glow=light;
            InstallFlowerVisuals(loop);
            loop.openHouse.SetActive(false);
            var settings=ScriptableObject.CreateInstance<LoopEndingTuning>(); AssetDatabase.CreateAsset(settings,Root+"Settings/Loop/LoopEndingTuning.asset"); loop.tuning=settings;
            var vp=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(vp,Root+"Settings/Loop/LoopRevealedProfile.asset");
            var color=vp.Add<ColorAdjustments>(true); color.saturation.Override(-100); color.contrast.Override(12);
            var vignette=vp.Add<Vignette>(true); vignette.intensity.Override(.38f);
            AssetDatabase.AddObjectToAsset(color,vp); AssetDatabase.AddObjectToAsset(vignette,vp);
            loop.volume=owner.AddComponent<Volume>(); loop.volume.isGlobal=true; loop.volume.priority=100; loop.volume.weight=0; loop.volume.sharedProfile=vp;
            var font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Root+"Art/Fonts/Caveat.ttf"),40,6,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024);
            AssetDatabase.CreateAsset(font,Root+"Art/Fonts/Caveat SDF.asset");
            string content=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Settings/Loop/DiaryContent.txt").text;
            string glyphs=content+"ЗакритиНарештіКінець.Петлю розірвано.АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюяABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?…—№'\"()-";
            if(!font.TryAddCharacters(glyphs,out string missing)) Debug.LogWarning("Diary font missing: "+missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.AddObjectToAsset(font.material,font); foreach(var tex in font.atlasTextures) AssetDatabase.AddObjectToAsset(tex,font);
            var canvas=new GameObject("Diary Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas.transform.SetParent(owner.transform);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder=500;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=1;
            var diary=canvas.AddComponent<DiaryView>(); loop.diary=diary;
            var dim=Rect("Dim",canvas.transform,new Vector2(1280,720)); dim.anchorMin=Vector2.zero; dim.anchorMax=Vector2.one; dim.offsetMin=dim.offsetMax=Vector2.zero; dim.gameObject.AddComponent<Image>().color=new Color(0,0,0,.6f); diary.dim=dim.gameObject.AddComponent<CanvasGroup>();
            diary.sheet=Rect("Paper",canvas.transform,new Vector2(920,620)); diary.sheetGroup=diary.sheet.gameObject.AddComponent<CanvasGroup>();
            var paper=diary.sheet.gameObject.AddComponent<Image>(); paper.sprite=Sprite("diary_paper"); paper.type=Image.Type.Sliced;
            diary.body=DiaryView.MakeText(diary.sheet,content,font,27); diary.body.color=new Color(.08f,.1f,.16f); diary.body.rectTransform.offsetMin=new Vector2(48,72); diary.body.rectTransform.offsetMax=new Vector2(-48,-35); diary.body.enableAutoSizing=true; diary.body.fontSizeMin=18; diary.body.fontSizeMax=27;
            diary.body.paragraphSpacing=3; diary.content=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Settings/Loop/DiaryContent.txt"); diary.buttonSprite=Sprite("keycap_metal");
            loop.ambience=Sound(owner,"Loop ambience","loop_wind",true,.12f); loop.paperAudio=Sound(owner,"Paper rustle","paper_rustle",false,.4f);
            so=new SerializedObject(bootstrap); so.FindProperty("loop").objectReferenceValue=loop; so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/Phase17-house.txt",$"House center={center}, size={size}; hydrant distance={Vector3.Distance(scenario.hydrant.position,center):F2}");
            Debug.Log("[FlameBlame] Phase 17 scene built.");
        }
        static void InstallFlowerVisuals(LoopRevealController loop)
        {
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>(); var so=new SerializedObject(bootstrap);
            if(loop.flower.flameVisual!=null) return;
            var flame=UnityEngine.Object.Instantiate((ParticleSystem)so.FindProperty("flameTemplate").objectReferenceValue,loop.flower.transform);
            flame.name="Ending flame (reserved)"; flame.transform.localPosition=Vector3.up*.6f; flame.transform.localScale=Vector3.one*.6f;
            int budget=24;
            foreach(var ps in flame.GetComponentsInChildren<ParticleSystem>(true)) {var main=ps.main; int cap=Mathf.Min(budget,main.maxParticles); main.maxParticles=cap; budget-=cap;}
            flame.gameObject.SetActive(true); flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); loop.flower.flameVisual=flame;
            var smoke=UnityEngine.Object.Instantiate((ParticleSystem)so.FindProperty("steamTemplate").objectReferenceValue,loop.flower.transform);
            smoke.name="Ending smoke landmark"; smoke.transform.localPosition=Vector3.up;
            foreach(var child in smoke.GetComponentsInChildren<ParticleSystem>(true)) {var main=child.main; main.maxParticles=0;}
            var sm=smoke.main; sm.maxParticles=16; sm.loop=true; sm.startLifetime=5; sm.startSpeed=1.5f; sm.startSize=.6f;
            sm.startColor=new Color(.3f,.3f,.3f,.28f); var emission=smoke.emission; emission.rateOverTime=3;
            smoke.gameObject.SetActive(true); smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); loop.flower.smokeVisual=smoke;
            EditorUtility.SetDirty(loop.flower);
        }
        static RectTransform Rect(string name,Transform parent,Vector2 size) { var go=new GameObject(name,typeof(RectTransform)); var r=(RectTransform)go.transform; r.SetParent(parent,false); r.sizeDelta=size; return r; }
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/UI/Sprites/"+name+".png");
        static Material Material(string name,Color color) { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color; AssetDatabase.CreateAsset(m,Root+"Materials/"+name+".mat"); return m; }
        static GameObject Cube(Transform parent,string name,Vector3 pos,Vector3 scale,Material material) {var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; return go;}
        static AudioSource Sound(GameObject parent,string name,string clip,bool loop,float volume) {var go=new GameObject(name); go.transform.SetParent(parent.transform); var s=go.AddComponent<AudioSource>(); s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Loop/"+clip+".wav"); s.loop=loop; s.volume=volume; s.playOnAwake=false; return s;}
    }
}
