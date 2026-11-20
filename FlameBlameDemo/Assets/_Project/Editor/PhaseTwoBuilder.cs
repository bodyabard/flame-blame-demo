using System.IO;
using System.Text;
using FlameBlame.Core;
using FlameBlame.Electric;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Npc;
using FlameBlame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    public static class PhaseTwoBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Tools/FlameBlame/Upgrade Main to Phase 2")]
        public static void UpgradeMain()
        {
            EditorSceneManager.OpenScene(ScenePath);
            UpgradeOpenScene();
            ToonMaterialMigration.ReapplyIfConfigured();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] Phase 2 ready: play Main for milestone-2.");
        }

        public static void UpgradeOpenScene()
        {
            GreyboxBuilder.SetupLayers();
            var oldRoot = GameObject.Find("Phase2");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            var root = new GameObject("Phase2").transform;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var settings = AssetDatabase.LoadAssetAtPath<ChaosSettings>(Root + "/Settings/ChaosSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ChaosSettings>();
                AssetDatabase.CreateAsset(settings, Root + "/Settings/ChaosSettings.asset");
            }
            Ref(bootstrap, "chaosSettings", settings);
            foreach (var flammable in Object.FindObjectsByType<Flammable>(FindObjectsSortMode.None))
            {
                var property = flammable.GetComponent<PropertyDamage>();
                if (property == null) property = flammable.gameObject.AddComponent<PropertyDamage>();
                string label = flammable.name;
                int value = 400;
                if (label == "Sofa") { label = "Диван"; value = 1200; }
                else if (label == "Armchair") { label = "Крісло"; value = 700; }
                else if (label == "Curtain") { label = "Штора"; value = 250; }
                else if (label.StartsWith("PaperStack")) { label = "Документи"; value = 180; }
                else if (label.StartsWith("Shelf")) { label = "Полиця"; value = 650; }
                else if (label == "Table") { label = "Стіл"; value = 800; }
                else if (label == "Crate") { label = "Ящик"; value = 250; }
                ConfigureProperty(property, label, value);
            }
            BuildCable(root);
            BuildNpc(root, "Neighbor_A", "Пан Остап", new Vector3(-3.4f, 0.95f, 2.8f), new Color(0.15f, 0.65f, 0.6f));
            BuildNpc(root, "Neighbor_B", "Пані Іскра", new Vector3(-1.1f, 0.95f, 0.9f), new Color(0.7f, 0.35f, 0.7f));
            BuildLooseProp(root, "WateringCan", "Лійка", new Vector3(0.2f, 0.65f, 0.4f), new Vector3(0.4f, 0.6f, 0.4f), "Metal", 180, PrimitiveType.Cylinder);
            BuildLooseProp(root, "Book", "Книжка", new Vector3(3f, 0.9f, -0.2f), new Vector3(0.4f, 0.1f, 0.3f), "Paper", 90, PrimitiveType.Cube);
            BuildLooseProp(root, "Flowerpot", "Горщик", new Vector3(0.7f, 0.35f, 1.2f), new Vector3(0.4f, 0.3f, 0.4f), "Metal", 220, PrimitiveType.Cylinder);
            var bootData = new SerializedObject(bootstrap);
            var effectsRoot = (Transform)bootData.FindProperty("effectsRoot").objectReferenceValue;
            var oldSpark = effectsRoot.Find("Template_Spark");
            if (oldSpark != null) Object.DestroyImmediate(oldSpark.gameObject);
            var spark = new GameObject("Template_Spark").AddComponent<ParticleSystem>();
            spark.transform.SetParent(effectsRoot, false);
            spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = spark.main;
            main.loop = false; main.playOnAwake = false; main.duration = 0.15f;
            main.startLifetime = 0.3f; main.startSpeed = 2.5f; main.startSize = 0.06f;
            main.startColor = new Color(1f, 0.8f, 0.15f); main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 8;
            var emission = spark.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            spark.GetComponent<ParticleSystemRenderer>().sharedMaterial = Material("Flame");
            spark.gameObject.SetActive(false);
            Ref(bootstrap, "sparkTemplate", spark);
            var emitter = Object.FindFirstObjectByType<JetEmitter>();
            var emitterData = new SerializedObject(emitter);
            emitterData.FindProperty("maxParticles").intValue = 400;
            emitterData.ApplyModifiedPropertiesWithoutUndo();
            var jetParticles = (ParticleSystem)emitterData.FindProperty("jet").objectReferenceValue;
            var jetMain = jetParticles.main; jetMain.maxParticles = 400;
            var presenter = BuildUI(root, BuildFont());
            Ref(bootstrap, "scorePresenter", presenter);
            EditorUtility.SetDirty(bootstrap);
        }

        static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>(ToonMaterialMigration.OriginalPath(name));
        static void Ref(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigureProperty(PropertyDamage property, string label, int value)
        {
            var data = new SerializedObject(property);
            data.FindProperty("displayName").stringValue = label;
            data.FindProperty("propertyValue").intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject Primitive(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static void BuildCable(Transform parent)
        {
            var cable = new GameObject("SparkCable");
            cable.transform.SetParent(parent, false);
            cable.transform.position = new Vector3(-1.3f, 0.04f, 0.8f);
            cable.AddComponent<SparkCable>();
            Primitive("Cable", cable.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.8f), new Vector3(0.055f, 0.055f, 1.6f), Material("Dark"), false);
            Primitive("ExposedCopper", cable.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.09f, 0.07f, 0.13f), Material("Hose"), false);
        }
        static void BuildNpc(Transform parent, string name, string label, Vector3 position, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Interactable");
            var collider = go.AddComponent<CapsuleCollider>(); collider.radius = 0.3f; collider.height = 1.8f;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 8f; rb.linearDamping = 1.2f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var visual = new GameObject("Visual").transform; visual.SetParent(go.transform, false);
            string materialPath = ToonMaterialMigration.OriginalPath(name);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null) { mat = new Material(Material("Fabric")); AssetDatabase.CreateAsset(mat, materialPath); }
            mat.SetColor("_BaseColor", color); EditorUtility.SetDirty(mat);
            var body = Primitive("Body", visual, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.6f, 0.9f, 0.6f), mat, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Eye", visual, PrimitiveType.Sphere, new Vector3(side * 0.14f, 0.45f, 0.27f), Vector3.one * 0.22f, Material("Paper"), false);
                Primitive("Pupil", visual, PrimitiveType.Sphere, new Vector3(side * 0.14f, 0.45f, 0.365f), Vector3.one * 0.09f, Material("Dark"), false);
            }
            var actor = go.AddComponent<NpcActor>();
            Ref(actor, "visual", visual); Ref(actor, "bodyRenderer", body.GetComponent<Renderer>());
            var data = new SerializedObject(actor); data.FindProperty("displayName").stringValue = label; data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void BuildLooseProp(Transform parent, string name, string label, Vector3 position, Vector3 scale, string profile, int value, PrimitiveType type)
        {
            var go = Primitive(name, parent, type, position, scale, Material(profile));
            go.layer = LayerMask.NameToLayer("Interactable");
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 1.2f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var flammable = go.AddComponent<Flammable>();
            Ref(flammable, "profile", AssetDatabase.LoadAssetAtPath<FireMaterialProfile>(Root + "/Settings/Fire/FireProfile_" + profile + ".asset"));
            ConfigureProperty(go.AddComponent<PropertyDamage>(), label, value);
        }

        static TMP_FontAsset BuildFont()
        {
            string fontPath = Root + "/Settings/ChaosFont.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (existing != null) return existing;
            if (!File.Exists("Assets/TextMesh Pro/Fonts/LiberationSans.ttf"))
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
                AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            var font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 2048, 2048);
            font.name = "ChaosFont";
            var characters = new StringBuilder(400);
            for (int i = 32; i < 127; i++) characters.Append((char)i);
            for (int i = 0x400; i <= 0x4ff; i++) characters.Append((char)i);
            characters.Append("—–’−");
            font.TryAddCharacters(characters.ToString(), out string missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, fontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            for (int i = 0; i < font.atlasTextures.Length; i++) AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
            EditorUtility.SetDirty(font);
            return font;
        }
        static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string text,
            Vector2 position, Vector2 size, float fontSize = 26f)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = fontSize;
            label.text = text; label.color = Color.white; label.raycastTarget = false;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return label;
        }
        static ScorePresenter BuildUI(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("ChaosHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            var score = Label("Damage", go.transform, font, "ЗБИТКИ  $0", new Vector2(-390f, 300f), new Vector2(440f, 55f), 32f);
            var timer = Label("Timer", go.transform, font, "ЧАС  105 с", new Vector2(460f, 300f), new Vector2(300f, 55f), 32f);
            var hints = Label("Controls", go.transform, font, "WASD — рух | Миша — огляд | ЛКМ — вода | E — кинути / взяти\nВода + кабель = електротерапія. Жовті калюжі небезпечні!",
                new Vector2(0f, -300f), new Vector2(1200f, 75f), 21f);
            hints.alignment = TextAlignmentOptions.Center;
            var popup = Label("PopupTemplate", go.transform, font, "", new Vector2(-270f, 210f), new Vector2(700f, 40f), 22f);
            var flash = new GameObject("ShockFlash", typeof(RectTransform), typeof(Image)); flash.transform.SetParent(go.transform, false);
            var flashImage = flash.GetComponent<Image>(); flashImage.color = Color.clear; flashImage.raycastTarget = false;
            Stretch(flash.GetComponent<RectTransform>());
            var result = new GameObject("Result", typeof(RectTransform), typeof(Image)); result.transform.SetParent(go.transform, false);
            result.GetComponent<Image>().color = new Color(0.045f, 0.04f, 0.07f, 0.96f); Stretch(result.GetComponent<RectTransform>());
            var summary = Label("Summary", result.transform, font, "ПІДСУМОК ХАОСУ", new Vector2(0f, 80f), new Vector2(700f, 280f), 40f);
            summary.alignment = TextAlignmentOptions.Center;
            var reason = Label("Reason", result.transform, font, "", new Vector2(0f, -100f), new Vector2(1000f, 60f), 26f); reason.alignment = TextAlignmentOptions.Center;
            var buttonObj = new GameObject("Again", typeof(RectTransform), typeof(Image), typeof(Button)); buttonObj.transform.SetParent(result.transform, false);
            buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 70f);
            buttonObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -210f);
            buttonObj.GetComponent<Image>().color = new Color(0.75f, 0.22f, 0.08f);
            var buttonLabel = Label("Label", buttonObj.transform, font, "ЩЕ РАЗ", Vector2.zero, new Vector2(260f, 65f), 30f); buttonLabel.alignment = TextAlignmentOptions.Center;
            var presenter = go.AddComponent<ScorePresenter>();
            Ref(presenter, "scoreLabel", score); Ref(presenter, "timerLabel", timer); Ref(presenter, "summaryLabel", summary);
            Ref(presenter, "reasonLabel", reason); Ref(presenter, "popupTemplate", popup); Ref(presenter, "resultPanel", result);
            Ref(presenter, "restartButton", buttonObj.GetComponent<Button>()); Ref(presenter, "shockFlash", flashImage);
            result.SetActive(false); popup.gameObject.SetActive(false);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                system.transform.SetParent(parent, false);
            }
            return presenter;
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
