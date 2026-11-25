using System.IO;
using System.Text;
using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Ladder;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static class ScenarioLevelBuilder
    {
        const string Root="Assets/_Project/";
        public static void RebuildAndBuildWebGL()
        {
            MainLevelRebuilder.Rebuild();
            SubmissionBuild.BuildWebGL();
        }
        [MenuItem("Tools/FlameBlame/Audit Furniture Scale")]
        public static void Audit()
        {
            var p=CalibrationSceneBuilder.LoadProfile();
            var report=new StringBuilder("\n## Phase 14 furniture audit (world units)\n\n| Prefab | Bounds XYZ | Height / door | Height / NPC |\n|---|---|---|---|\n");
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs/Visuals"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var go=PrefabUtility.LoadPrefabContents(path);
                try {var b=BoundsOf(go); report.AppendLine($"| {go.name} | {b.size:F2} | {b.size.y/p.doorHeight:F2} | {b.size.y/p.npcHeight:F2} |");}
                finally {PrefabUtility.UnloadPrefabContents(go);}
            }
            string notes=File.ReadAllText("SCALE_NOTES.md"); int start=notes.IndexOf("\n## Phase 14 furniture audit");
            if(start>=0) notes=notes.Substring(0,start);
            File.WriteAllText("SCALE_NOTES.md",notes+report);
            Debug.Log(report);
        }
        static Bounds BoundsOf(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>(true);
            var b=rs.Length>0 ? rs[0].bounds : new Bounds(go.transform.position,Vector3.zero);
            foreach(var r in rs) b.Encapsulate(r.bounds); return b;
        }
        static void Furniture(ScaleProfile p)
        {
            foreach(string name in new[]{"Sofa","Armchair","Table","Book","Shelf_1","Shelf_2","WateringCan","Flowerpot"})
            {
                string path=Root+"Prefabs/Visuals/"+name+".prefab";
                if(!File.Exists(path))
                {
                    var original=GameObject.Find(name); if(original==null) continue;
                    var copy=Object.Instantiate(original); copy.transform.SetParent(null,true);
                    try
                    {
                        if(PrefabUtility.IsPartOfPrefabInstance(copy)) PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        PrefabUtility.SaveAsPrefabAsset(copy,path);
                    }
                    finally {Object.DestroyImmediate(copy);}
                }
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var b=BoundsOf(prefab);
                    float target=name.StartsWith("Shelf") ? p.doorHeight*.9f : name=="Sofa" || name=="Armchair" || name=="Table" ? p.npcHeight*.55f : p.npcHeight*.32f;
                    // Normalize to a target so repeated rebuilds cannot compound scale.
                    float factor=target/Mathf.Max(.01f,b.size.y)*(p.furnitureScale/1.25f);
                    prefab.transform.localScale*=factor;
                    foreach(var rb in prefab.GetComponentsInChildren<Rigidbody>()) rb.mass=Mathf.Clamp(rb.mass*factor*factor*factor,.3f,8f);
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                    var instance=GameObject.Find(name);
                    if(instance!=null)
                    {
                        var pos=instance.transform.position; var rotation=instance.transform.rotation; var parent=instance.transform.parent;
                        Object.DestroyImmediate(instance);
                        var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        replacement.name=name; replacement.transform.SetParent(parent,true); replacement.transform.SetPositionAndRotation(pos,rotation);
                        // Preserve the lowest point at the upper floor.
                        float floor=AssetDatabase.LoadAssetAtPath<FlightLevelSettings>(Root+"Settings/FlightLevelSettings.asset").floorHeight;
                        replacement.transform.position+=Vector3.up*(floor-BoundsOf(replacement).min.y);
                    }
                }
                finally {PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
        public static void Apply()
        {
            var p=CalibrationSceneBuilder.LoadProfile();
            var old=GameObject.Find("Scenario14"); if(old!=null) Object.DestroyImmediate(old);
            var root=new GameObject("Scenario14"); var scenario=root.AddComponent<LevelScenario>();
            string tuningPath=Root+"Settings/GameTuning.asset";
            scenario.tuning=AssetDatabase.LoadAssetAtPath<GameTuning>(tuningPath);
            if(scenario.tuning==null) {scenario.tuning=ScriptableObject.CreateInstance<GameTuning>(); AssetDatabase.CreateAsset(scenario.tuning,tuningPath);}
            scenario.supplyAudio=root.AddComponent<AudioSource>(); scenario.supplyAudio.playOnAwake=false;
            scenario.couplingClick=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/ladder-place.wav");
            Furniture(p);
            var wall=GameObject.Find("GroundFloor").GetComponent<Renderer>().sharedMaterial;
            var metal=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Toon/Visual_Coral_Toon.mat");
            var white=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Toon/Visual_Cream_Toon.mat");
            var black=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/FlightDarkGlass.mat");
            var truck=new GameObject("Fire engine 67").transform; truck.SetParent(root.transform);
            Vector3 t=scenario.truckPosition;
            Box(truck,"Body",t+Vector3.up*.9f,new Vector3(2,1.3f,4.5f),metal);
            Box(truck,"Cab",t+new Vector3(0,1.5f,1.5f),new Vector3(2,1.4f,1.4f),metal);
            Box(truck,"White stripe",t+Vector3.up*1.2f,new Vector3(2.04f,.18f,4.55f),white);
            Box(truck,"Windshield",t+new Vector3(0,1.8f,2.22f),new Vector3(1.7f,.55f,.06f),black);
            for(int i=0;i<3;i++)
            {
                Vector3 home=new Vector3(8+i*7,0,10);
                Box(root.transform,"Neighbor facade",home+Vector3.up*2,new Vector3(5,4,4),wall);
                var roof=Box(root.transform,"Neighbor roof",home+Vector3.up*4.3f,new Vector3(5.5f,.5f,4.5f),black); roof.transform.rotation=Quaternion.Euler(0,0,8);
                for(int window=0;window<2;window++) Box(root.transform,"Neighbor window",home+new Vector3(window*2-1,2.5f,-2.03f),new Vector3(.8f,1,.08f),white);
                Vector3 lamp=new Vector3(-8,0,-8+i*6);
                Box(root.transform,"Street lamp post",lamp+Vector3.up*1.8f,new Vector3(.12f,3.6f,.12f),black);
                Box(root.transform,"Street lamp cap",lamp+Vector3.up*3.6f,new Vector3(.45f,.4f,.45f),white);
                var bush=GameObject.CreatePrimitive(PrimitiveType.Sphere); bush.name="Bush"; bush.transform.SetParent(root.transform); bush.transform.position=new Vector3(4,.5f,-5+i*3); bush.transform.localScale=new Vector3(1.5f,1,1.2f); bush.GetComponent<Renderer>().sharedMaterial=wall; bush.isStatic=true;
            }
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
            {var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder); wheel.transform.SetParent(truck); wheel.transform.SetPositionAndRotation(t+new Vector3(x,.5f,z*1.5f),Quaternion.Euler(0,0,90)); wheel.transform.localScale=new Vector3(.8f,.18f,.8f); wheel.GetComponent<Renderer>().sharedMaterial=black; wheel.isStatic=true;}
            var number=new GameObject("67").AddComponent<TextMeshPro>(); number.transform.SetParent(truck); number.transform.position=t+new Vector3(-1.03f,1.6f,0); number.transform.rotation=Quaternion.Euler(0,90,0); number.text="67"; number.fontSize=5; number.rectTransform.sizeDelta=new Vector2(1,1);
            scenario.reel=Box(truck,"Hose reel — F",t+new Vector3(1.3f,.9f,-.8f),new Vector3(.5f,.9f,.9f),white).transform;
            scenario.reel.gameObject.AddComponent<HoseReelPickup>();
            var player=Object.FindFirstObjectByType<FlameBlame.Player.PlayerMotor>();
            player.transform.SetPositionAndRotation(new Vector3(-13,0,-9),Quaternion.Euler(0,45,0));
            var look=new SerializedObject(player.GetComponent<FlameBlame.Player.PlayerLook>());
            look.FindProperty("initialPitch").floatValue=-8; look.ApplyModifiedPropertiesWithoutUndo();
            scenario.backpackCoil=new GameObject("Hose backpack coil"); scenario.backpackCoil.transform.SetParent(root.transform);
            var coil=scenario.backpackCoil.AddComponent<LineRenderer>(); coil.sharedMaterial=white; coil.useWorldSpace=false; coil.widthMultiplier=.09f; coil.positionCount=65;
            for(int i=0;i<65;i++) {float angle=i*Mathf.PI*8/64; float radius=.2f+i*.0015f; coil.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,i*.001f));}
            scenario.backpackCoil.SetActive(false);
            var hose=Object.FindFirstObjectByType<HoseController>(); var data=new SerializedObject(hose);
            var anchor=(Transform)data.FindProperty("anchor").objectReferenceValue;
            Transform hydrant=anchor; while(hydrant.parent!=null) hydrant=hydrant.parent;
            hydrant.position+=new Vector3(-8,0,6)-new Vector3(hydrant.position.x,0,hydrant.position.z); scenario.hydrant=anchor;
            if(anchor.GetComponent<HydrantConnector>()==null) anchor.gameObject.AddComponent<HydrantConnector>();
            scenario.ladderTarget=Point(root.transform,"Ladder goal",new Vector3(-p.roomWidth*.5f-.1f,4.1f,0));
            foreach(var ladder in Object.FindObjectsByType<LadderStateMachine>(FindObjectsSortMode.None)) ladder.transform.SetPositionAndRotation(t+new Vector3(2,.3f,0),Quaternion.Euler(90,0,0));
            scenario.floor2=Zone(root.transform,"Floor2",FireZoneId.Floor2);
            var lower=new System.Collections.Generic.List<Flammable>();
            foreach(var f in Object.FindObjectsByType<Flammable>(FindObjectsSortMode.None)) lower.Add(f);
            // The old chaos room used ten-second fuel; the full route needs round-long fires.
            foreach(var f in lower)
            {
                var fs=new SerializedObject(f); var original=(FireMaterialProfile)fs.FindProperty("profile").objectReferenceValue;
                if(original==null) continue;
                string baseName=original.name.Replace("Scenario_","");
                string profilePath=Root+"Settings/Fire/Scenario_"+baseName+".asset";
                var tuned=AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(profilePath);
                if(tuned==null) {tuned=Object.Instantiate(original); tuned.name="Scenario_"+baseName; AssetDatabase.CreateAsset(tuned,profilePath);}
                tuned.burnDuration=scenario.roundSeconds+30; EditorUtility.SetDirty(tuned);
                fs.FindProperty("profile").objectReferenceValue=tuned; fs.ApplyModifiedPropertiesWithoutUndo();
            }
            scenario.floor2.members=lower.ToArray();
            // Assign zone membership without moving existing room hierarchies.
            foreach(var f in lower) {var zone=f.gameObject.GetComponent<FireZone>(); if(zone==null) zone=f.gameObject.AddComponent<FireZone>(); zone.id=FireZoneId.Floor2;}
            // Phase 15: no outdoor fire zone any more; everything burns inside the house.
            var source=lower.Find(f=>new SerializedObject(f).FindProperty("profile").objectReferenceValue is FireMaterialProfile fp && fp.canBurn);
            var profile=(FireMaterialProfile)new SerializedObject(source).FindProperty("profile").objectReferenceValue;
            // (porch crate removed in phase 15)
            scenario.attic=Zone(root.transform,"Attic",FireZoneId.Attic);
            float y=6.4f, w=p.roomWidth, l=p.roomLength;
            Box(root.transform,"Attic floor",new Vector3(0,y-.1f,0),new Vector3(w,.2f,l),wall);
            Box(root.transform,"Attic back",new Vector3(w*.5f,y+1.4f,0),new Vector3(.2f,2.8f,l),wall);
            foreach(int sign in new[]{-1,1})
            {
                Box(root.transform,"Attic side",new Vector3(0,y+1.4f,sign*l*.5f),new Vector3(w,2.8f,.2f),wall);
                Box(root.transform,"Gable opening side",new Vector3(-w*.5f,y+1.4f,sign*(l+2.2f)*.25f),new Vector3(.2f,2.8f,(l-2.2f)*.5f),wall);
                var roof=Box(root.transform,"Roof",new Vector3(0,y+3.2f,sign*l*.25f),new Vector3(w+.5f,.18f,l*.6f),metal); roof.transform.rotation=Quaternion.Euler(sign*25,0,0);
            }
            scenario.attic.members=new[]{BurningBox(scenario.attic.transform,"Family Christmas decorations",new Vector3(0,y+.6f,0),new Vector3(1.5f,1.2f,1),wall,profile,false)};
            scenario.atticTarget=Point(root.transform,"Attic goal",new Vector3(-w*.5f,y+1,0));
            var volume=new GameObject("Attic entry"); volume.transform.SetParent(root.transform); volume.transform.position=new Vector3(0,y+1,0); scenario.atticVolume=volume.AddComponent<BoxCollider>(); scenario.atticVolume.isTrigger=true; scenario.atticVolume.size=new Vector3(w,2,l);
            float route=Vector3.Distance(anchor.position,scenario.atticTarget.position)+new Vector2(w,l*.5f).magnitude;
            data.FindProperty("segments").intValue=Mathf.CeilToInt(route*1.15f/(data.FindProperty("segmentLength").floatValue*.92f))+1; data.ApplyModifiedPropertiesWithoutUndo();
            var canvas=Object.FindFirstObjectByType<Canvas>();
            foreach(var panel in Object.FindObjectsByType<FlameBlame.UI.UIPanelView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(panel.name=="Scenario objective panel") Object.DestroyImmediate(panel.gameObject);
            foreach(var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(label.name=="Objective" || label.name=="Hose interaction" || label.name=="Objective marker") Object.DestroyImmediate(label.gameObject);
            scenario.objective=Label(canvas.transform,"Objective",new Vector2(25,-180),new Vector2(500,100),26);
            var panelAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/UI/UIPanel.prefab");
            var objectivePanel=(GameObject)PrefabUtility.InstantiatePrefab(panelAsset,canvas.transform);
            objectivePanel.name="Scenario objective panel";
            objectivePanel.transform.SetAsFirstSibling();
            var panelRect=(RectTransform)objectivePanel.transform; panelRect.anchorMin=panelRect.anchorMax=new Vector2(0,1); panelRect.pivot=new Vector2(0,1); panelRect.anchoredPosition=new Vector2(25,-330); panelRect.sizeDelta=new Vector2(520,120);
            scenario.objective.transform.SetParent(panelRect,false); scenario.objective.rectTransform.anchoredPosition=new Vector2(20,-15);
            scenario.interaction=Label(canvas.transform,"Hose interaction",new Vector2(500,-650),new Vector2(600,60),28);
            scenario.marker=Label(canvas.transform,"Objective marker",Vector2.zero,new Vector2(60,60),34);
            scenario.marker.transform.SetAsFirstSibling(); scenario.interaction.transform.SetAsFirstSibling();
            Audit(); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(root.scene);
        }
        static TMP_Text Label(Transform parent,string name,Vector2 pos,Vector2 size,float fontSize)
        {var label=new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); label.transform.SetParent(parent,false); label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(0,1); label.rectTransform.pivot=new Vector2(0,1); label.rectTransform.anchoredPosition=pos; label.rectTransform.sizeDelta=size; label.fontSize=fontSize; label.raycastTarget=false; return label;}
        static Transform Point(Transform parent,string name,Vector3 position) {var t=new GameObject(name).transform; t.SetParent(parent); t.position=position; return t;}
        static FireZone Zone(Transform parent,string name,FireZoneId id) {var z=Point(parent,name,Vector3.zero).gameObject.AddComponent<FireZone>(); z.id=id; return z;}
        static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material mat) {var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent); go.transform.position=pos; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=mat; go.isStatic=true; return go;}
        static Flammable BurningBox(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,FireMaterialProfile profile,bool ignite)
        {var go=Box(parent,name,pos,size,mat); go.AddComponent<PropertyDamage>(); var f=go.AddComponent<Flammable>(); var s=new SerializedObject(f); s.FindProperty("profile").objectReferenceValue=profile; s.FindProperty("igniteOnStart").boolValue=ignite; s.ApplyModifiedPropertiesWithoutUndo(); return f;}
    }
}
