using System.IO;
using FlameBlame.Hose;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    [InitializeOnLoad]
    public static class JetModeBuilder
    {
        static JetModeBuilder() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists("Temp/FlameBlame-BuildJet") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete("Temp/FlameBlame-BuildJet"); Apply();
        }
        [MenuItem("Tools/FlameBlame/Apply Jet Mode")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var hose = Object.FindFirstObjectByType<HoseController>();
            var data = new SerializedObject(hose);
            var hand = (Transform)data.FindProperty("handPoint").objectReferenceValue;
            var rig = hose.GetComponent<NozzleRig>();
            if (rig == null)
            {
                rig = hose.gameObject.AddComponent<NozzleRig>();
                rig.pose = new GameObject("NozzlePose").transform;
                rig.pose.SetParent(hose.transform, false);
                var renderer = new SerializedObject(hose.GetComponent<HoseRenderer>());
                var model = (Transform)renderer.FindProperty("nozzleModel").objectReferenceValue;
                model.SetParent(rig.pose, false);
                model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity;
                rig.inlet = new GameObject("NozzleInlet").transform;
                rig.inlet.SetParent(rig.pose, false); rig.inlet.localPosition = new Vector3(0,0,-.175f);
                rig.muzzle = new GameObject("NozzleMuzzle").transform;
                rig.muzzle.SetParent(rig.pose, false); rig.muzzle.localPosition = new Vector3(0,0,.175f);
            }
            rig.Follow(hand, hand.position, hand.forward, true);
            data.FindProperty("nozzleRig").objectReferenceValue = rig;
            data.ApplyModifiedPropertiesWithoutUndo();
            const string path="Assets/_Project/Settings/JetTuning.asset";
            var tuning=AssetDatabase.LoadAssetAtPath<JetTuning>(path);
            if(tuning==null) { tuning=ScriptableObject.CreateInstance<JetTuning>(); AssetDatabase.CreateAsset(tuning,path); }
            data.FindProperty("jetTuning").objectReferenceValue=tuning;
            data.ApplyModifiedPropertiesWithoutUndo();
            var feedback=hose.GetComponent<FlameBlame.Feedback.JetFeedback>();
            if(feedback==null) feedback=hose.gameObject.AddComponent<FlameBlame.Feedback.JetFeedback>();
            var f=new SerializedObject(feedback);
            f.FindProperty("hose").objectReferenceValue=hose;
            f.FindProperty("motor").objectReferenceValue=Object.FindFirstObjectByType<FlameBlame.Player.PlayerMotor>();
            f.FindProperty("cameraFeedback").objectReferenceValue=Object.FindFirstObjectByType<FlameBlame.Feedback.CameraFeedback>();
            f.FindProperty("valveClick").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/JetValve.wav");
            f.FindProperty("windClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/Water.wav");
            f.FindProperty("landing").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Generated/JetLanding.wav");
            f.ApplyModifiedPropertiesWithoutUndo();
            foreach(var text in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                if(text.text.Contains("WASD") && !text.text.Contains("Q вЂ”")) text.text += "   Q вЂ” СЂРµР¶РёРј";
            EditorUtility.SetDirty(rig);
            EditorSceneManager.MarkSceneDirty(hose.gameObject.scene);
            EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] Jet rig applied");
        }
    }
}

