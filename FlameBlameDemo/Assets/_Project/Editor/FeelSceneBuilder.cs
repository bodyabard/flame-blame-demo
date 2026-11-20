using System;
using System.IO;
using System.Text;
using System.Reflection;
using FlameBlame.Core;
using FlameBlame.Feedback;
using FlameBlame.Hose;
using FlameBlame.Npc;
using FlameBlame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static class FeelSceneBuilder
    {
        const string Root="Assets/_Project";
        static void Ref(Object target,string field,Object value)
        {var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void Refs(Object target,string field,Object[] values)
        {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        static void Clear(Transform parent,string name) {var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        static Material Material(string name,Color color,bool glow=false)
        {
            string path=ToonMaterialMigration.OriginalPath("Feel_"+name);var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.2f);
            if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.35f);}EditorUtility.SetDirty(mat);return mat;
        }
        static Transform Shape(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        public static void Apply()
        {
            var tuning=AssetDatabase.LoadAssetAtPath<VisualTuning>(Root+"/Settings/VisualTuning.asset");
            var camera=Camera.main;var hose=Object.FindFirstObjectByType<HoseController>();
            var feedback=Object.FindFirstObjectByType<CameraFeedback>();Ref(feedback,"tuning",tuning);Ref(feedback,"motor",camera.GetComponentInParent<CharacterController>());Ref(feedback,"hose",hose);
            Hands(camera.transform,hose,tuning);Npcs(camera.transform,tuning);Hud(hose,tuning);Water();Audio(hose,tuning);
        }
        static void Hands(Transform camera,HoseController hose,VisualTuning tuning)
        {
            Clear(camera,"FirefighterHands");var go=new GameObject("FirefighterHands");go.transform.SetParent(camera,false);
            var rig=new GameObject("Arms").transform;rig.SetParent(go.transform,false);
            var sleeve=Material("Sleeve",tuning.sleeve);var glove=Material("Glove",tuning.glove);var stripe=Material("Reflective",tuning.reflectiveStripe,true);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side==1?.25f:-.16f;
                var arm=new GameObject("Forearm").transform;arm.SetParent(rig,false);arm.localPosition=new Vector3(x,-.30f,.35f);arm.localRotation=Quaternion.Euler(side==1?-12:-22,side*8,0);
                Shape(arm,"Sleeve",Vector3.zero,new Vector3(.16f,.16f,.47f),sleeve);
                for(int band=0;band<2;band++)Shape(arm,"ReflectiveBand",new Vector3(0,0,.075f+band*.07f),new Vector3(.165f,.165f,.025f),stripe);
                Shape(arm,"Glove",new Vector3(0,.01f,.29f),new Vector3(.14f,.13f,.19f),glove);
                Shape(arm,"Thumb",new Vector3(-side*.07f,.04f,.30f),new Vector3(.07f,.07f,.12f),glove,PrimitiveType.Capsule);
                for(int finger=0;finger<3;finger++)Shape(arm,"Finger",new Vector3(-.045f+finger*.045f,.05f,.39f),new Vector3(.035f,.06f,.07f),glove);
            }
            var motion=go.AddComponent<FirstPersonHands>();Ref(motion,"arms",rig);Ref(motion,"hose",hose);Ref(motion,"tuning",tuning);
            Ref(Object.FindFirstObjectByType<GameBootstrap>(),"hands",motion);
        }
        static void Npcs(Transform camera,VisualTuning tuning)
        {
            var white=Material("Eye",new Color(.98f,.97f,.87f));var black=Material("Pupil",new Color(.02f,.025f,.035f));
            foreach(var actor in Object.FindObjectsByType<NpcActor>(FindObjectsSortMode.None))
            {
                var data=new SerializedObject(actor);var visual=(Transform)data.FindProperty("visual").objectReferenceValue;
                for(int i=visual.childCount-1;i>=0;i--){string name=visual.GetChild(i).name;if(name=="Eye"||name=="Pupil"||name=="Expression")Object.DestroyImmediate(visual.GetChild(i).gameObject);}
                var face=new GameObject("Expression").transform;face.SetParent(visual,false);
                var eyes=new Transform[2];var pupils=new Transform[2];var arms=new Transform[2];
                var body=(Renderer)data.FindProperty("bodyRenderer").objectReferenceValue;
                for(int i=0;i<2;i++)
                {
                    float side=i==0?-1:1;
                    eyes[i]=Shape(face,"Eye",new Vector3(side*.145f,.5f,.28f),Vector3.one*.23f,white,PrimitiveType.Sphere);
                    pupils[i]=Shape(face,"Pupil",new Vector3(side*.145f,.5f,.385f),Vector3.one*.09f,black,PrimitiveType.Sphere);
                    arms[i]=new GameObject("ArmPivot").transform;arms[i].SetParent(face,false);arms[i].localPosition=new Vector3(side*.30f,.25f,0);
                    Shape(arms[i],"Arm",new Vector3(side*.1f,-.22f,0),new Vector3(.15f,.5f,.15f),body.sharedMaterial,PrimitiveType.Capsule);
                    Shape(face,"Foot",new Vector3(side*.17f,-.8f,.10f),new Vector3(.22f,.15f,.36f),black);
                    Shape(face,"Brow",new Vector3(side*.15f,.67f,.30f),new Vector3(.20f,.035f,.035f),black);
                }
                var mouth=Shape(face,"Mouth",new Vector3(0,.23f,.31f),new Vector3(.12f,.08f,.04f),black,PrimitiveType.Sphere);
                var expression=actor.GetComponent<NpcExpression>();if(expression==null)expression=actor.gameObject.AddComponent<NpcExpression>();
                Ref(expression,"actor",actor);Ref(expression,"tuning",tuning);Ref(expression,"body",body);Ref(expression,"mouth",mouth);
                Refs(expression,"eyes",eyes);Refs(expression,"pupils",pupils);Refs(expression,"arms",arms);Ref(actor,"audience",camera);
            }
        }
        static TMP_FontAsset Font()
        {
            string path=Root+"/Settings/RussoHUD.asset";var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(font!=null)return font;
            font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/RussoOne-Regular.ttf"),64,8,GlyphRenderMode.SDFAA,2048,2048);
            var chars=new StringBuilder();for(int i=32;i<127;i++)chars.Append((char)i);for(int i=0x400;i<=0x4ff;i++)chars.Append((char)i);chars.Append("—–’−");
            font.TryAddCharacters(chars.ToString(),out string missing);font.atlasPopulationMode=AtlasPopulationMode.Static;
            // Liberation Sans covers uncommon punctuation without runtime atlas growth.
            font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Settings/ChaosFont.asset")};
            AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);EditorUtility.SetDirty(font);return font;
        }
        static Image Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static void Hud(HoseController hose,VisualTuning tuning)
        {
            var hud=Object.FindFirstObjectByType<ScorePresenter>();var font=Font();
            foreach(var label in hud.GetComponentsInChildren<TextMeshProUGUI>(true)){label.font=font;label.fontSharedMaterial=font.material;}
            foreach(string name in new[]{"ScoreCard","TimerCard","Pressure"})Clear(hud.transform,name);
            Panel(hud.transform,"ScoreCard",new Vector2(-390,300),new Vector2(455,65),tuning.hudPanel).transform.SetAsFirstSibling();
            Panel(hud.transform,"TimerCard",new Vector2(460,300),new Vector2(310,65),tuning.hudPanel).transform.SetAsFirstSibling();
            var panel=Panel(hud.transform,"Pressure",new Vector2(450,-210),new Vector2(290,120),tuning.hudPanel);
            var fill=Panel(panel.transform,"Flow",new Vector2(0,-35),new Vector2(250,12),tuning.hudAccent);fill.sprite=Disc();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
            var dial=Panel(panel.transform,"Dial",new Vector2(-95,6),new Vector2(55,55),new Color(.1f,.09f,.07f));
            dial.sprite=Disc();
            var needle=Panel(dial.transform,"Needle",Vector2.zero,new Vector2(4,24),tuning.hudAccent).rectTransform;needle.pivot=new Vector2(.5f,.1f);
            var textGo=new GameObject("PressureLabel",typeof(RectTransform));textGo.transform.SetParent(panel.transform,false);var text=textGo.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=25;text.text="ТИСК  0%";text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(220,55);text.rectTransform.anchoredPosition=new Vector2(30,5);
            var gauge=panel.gameObject.AddComponent<PressureGauge>();Ref(gauge,"hose",hose);Ref(gauge,"tuning",tuning);Ref(gauge,"needle",needle);Ref(gauge,"fill",fill);Ref(gauge,"label",text);
            panel.transform.SetAsFirstSibling();
            var result=hud.transform.Find("Result").GetComponent<Image>();result.color=tuning.hudPanel;
        }
        static void Audio(HoseController hose,VisualTuning tuning)
        {
            Directory.CreateDirectory(Root+"/Audio/Generated");
            foreach(string kind in new[]{"Water","Fire","Steam","Spark","Yelp","Music"})Wave(kind);
            AssetDatabase.Refresh();
            var mixer=Mixer();
            var bootstrap=Object.FindFirstObjectByType<GameBootstrap>();Clear(bootstrap.transform,"GameAudio");
            var go=new GameObject("GameAudio");go.transform.SetParent(bootstrap.transform,false);var audio=go.AddComponent<GameAudio>();Ref(audio,"hose",hose);Ref(audio,"tuning",tuning);
            Ref(audio,"water",Source(go.transform,"Water",Clip("Water"),mixer.FindMatchingGroups("Effects")[0],true));
            Ref(audio,"fire",Source(go.transform,"Fire",Clip("Fire"),mixer.FindMatchingGroups("Effects")[0],true));
            Ref(audio,"music",Source(go.transform,"Music",Clip("Music"),mixer.FindMatchingGroups("Music")[0],true));
            var fx=new AudioSource[6];var voices=new AudioSource[3];for(int i=0;i<fx.Length;i++)fx[i]=Source(go.transform,"FX",null,mixer.FindMatchingGroups("Effects")[0],false);for(int i=0;i<voices.Length;i++)voices[i]=Source(go.transform,"Voice",null,mixer.FindMatchingGroups("Voices")[0],false);
            Refs(audio,"effects",fx);Refs(audio,"voices",voices);Ref(audio,"steam",Clip("Steam"));Ref(audio,"spark",Clip("Spark"));Ref(audio,"yelp",Clip("Yelp"));
            Ref(audio,"taunt",AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Voice/Audio/you_lose.ogg"));Ref(bootstrap,"gameAudio",audio);
        }
        static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Generated/"+name+".wav");
        static Sprite Disc()
        {
            const string path=Root+"/Art/PressureDisc.png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){float radius=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((1-radius)*12)));}
                texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Water()
        {
            var emitter=Object.FindFirstObjectByType<JetEmitter>();var data=new SerializedObject(emitter);var ps=(ParticleSystem)data.FindProperty("jet").objectReferenceValue;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();string path=ToonMaterialMigration.OriginalPath("Feel_FoamyWater");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_BaseMap",Disc().texture);mat.SetColor("_BaseColor",new Color(.72f,.9f,1,.65f));EditorUtility.SetDirty(mat);renderer.sharedMaterial=mat;
            var colors=ps.colorOverLifetime;colors.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.65f,.87f,1),0),new GradientColorKey(Color.white,.65f)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(.65f,.65f),new GradientAlphaKey(0,1)});colors.color=gradient;
        }
        static AudioSource Source(Transform parent,string name,AudioClip clip,AudioMixerGroup group,bool loop)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;source.loop=loop;source.outputAudioMixerGroup=group;source.spatialBlend=loop?0:.65f;source.minDistance=2;source.maxDistance=15;return source;}
        static AudioMixer Mixer()
        {
            const string path=Root+"/Audio/FlameBlame.mixer";var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);if(mixer!=null)return mixer;
            // Unity has no public mixer-authoring API. Reflection is confined to Editor automation.
            var type=typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController",true);
            mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{path});
            var master=type.GetProperty("masterGroup").GetValue(mixer);
            foreach(string name in new[]{"Effects","Voices","Music"})
            {var group=type.GetMethod("CreateNewGroup").Invoke(mixer,new object[]{name,false});type.GetMethod("AddChildToParent").Invoke(mixer,new[]{group,master});}
            EditorUtility.SetDirty(mixer);return mixer;
        }
        static void Wave(string kind)
        {
            string path=Root+"/Audio/Generated/"+kind+".wav";if(File.Exists(path))return;
            const int rate=22050;int samples=(int)((kind=="Music"?8:kind=="Yelp"?.6f:kind=="Steam"?.7f:kind=="Spark"?.25f:2)*rate);
            var random=new System.Random(71);float smooth=0;double phase=0;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);
                for(int i=0;i<samples;i++)
                {
                    float t=(float)i/rate,p=(float)i/samples,noise=(float)(random.NextDouble()*2-1);smooth=Mathf.Lerp(smooth,noise,.15f);float value;
                    if(kind=="Music") {int[] notes={48,55,60,63,48,58,55,51};int step=(int)(t*4);float frequency=440*Mathf.Pow(2,(notes[step%8]-69)/12f);value=(Mathf.Sin(t*frequency*2*Mathf.PI)>.0f?.12f:-.12f)*Mathf.Exp(-(t*4-step)*5)+Mathf.Sin(t*2*Mathf.PI*55)*.07f;}
                    else if(kind=="Yelp"){phase+=2*Math.PI*(380+Math.Sin(p*Math.PI)*450)/rate;value=(float)Math.Sin(phase)*Mathf.Sin(p*Mathf.PI)*.4f;}
                    else if(kind=="Spark")value=noise*Mathf.Pow(1-p,3)*.6f;
                    else if(kind=="Steam")value=noise*Mathf.Sin(p*Mathf.PI)*.25f;
                    else if(kind=="Fire")value=smooth*.6f+(Mathf.Abs(noise)>.993f?noise*.35f:0);
                    else value=noise*.18f+smooth*.35f;
                    if(kind=="Music"||kind=="Water"||kind=="Fire")value*=Mathf.Min(1,Mathf.Min(i,samples-1-i)/100f);
                    writer.Write((short)(Mathf.Clamp(value,-1,1)*32767));
                }
            }
        }
    }
}
