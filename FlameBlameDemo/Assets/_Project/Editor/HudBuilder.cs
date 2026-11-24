using System.IO;
using FlameBlame.Core;
using FlameBlame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Build UI: palette, motion presets, text material, plate prefabs and the HUD in Main.unity.
    public static class HudBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Main.unity";
        const string UiSettings = Root + "/Settings/UI";
        const string Prefabs = Root + "/Prefabs/UI";
        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        static UIPalette palette;
        static TMP_FontAsset font;
        static Material textMaterial;

        [MenuItem("Tools/FlameBlame/Build UI")]
        public static void Build()
        {
            Directory.CreateDirectory(UiSettings);
            Directory.CreateDirectory(Prefabs);
            UISpriteImporter.Import();
            AssetDatabase.Refresh();

            palette = LoadOrCreate<UIPalette>(UiSettings + "/UIPalette.asset");
            var motion = LoadOrCreate<UIMotionPresets>(UiSettings + "/UIMotionPresets.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Settings/RussoHUD.asset");
            if (font == null) throw new System.InvalidOperationException("RussoHUD font asset missing.");
            textMaterial = BuildTextMaterial();

            var panelPrefab = BuildPlatePrefab("UIPanel", UIPanelView.Role.Panel, "panel_fill", "panel_outline", true);
            var pillPrefab = BuildPlatePrefab("UIPill", UIPanelView.Role.Pill, "pill_fill", "pill_outline", true);
            var keycapPrefab = BuildPlatePrefab("UIKeycap", UIPanelView.Role.Keycap, "keycap_fill", "keycap_outline", true);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveOldHud();
            BuildHud(motion, panelPrefab, pillPrefab, keycapPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[FlameBlame] HUD built in " + ScenePath);
        }

        // ------------------------------------------------------------------ assets

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite S(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UISpriteImporter.SpritesFolder + "/" + name + ".png");
            if (sprite == null) throw new System.InvalidOperationException("Sprite missing: " + name);
            return sprite;
        }

        /// Outline + underlay preset for HUD text: dark outline (~0.2) and a soft drop shadow.
        static Material BuildTextMaterial()
        {
            string path = UiSettings + "/RussoHUD_Outline.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(font.material);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = font.material.shader;
            mat.SetTexture("_MainTex", font.atlasTexture);
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor("_OutlineColor", palette.outline);
            mat.SetFloat("_OutlineWidth", 0.2f);
            mat.SetFloat("_OutlineSoftness", 0f);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(palette.outline.r, palette.outline.g, palette.outline.b, 0.75f));
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", -0.45f);
            mat.SetFloat("_UnderlayDilate", 0.15f);
            mat.SetFloat("_UnderlaySoftness", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ element helpers

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
        }

        static void Stretch(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
        }

        static Image NewImage(string name, Transform parent, string sprite, Color color, bool sliced = false)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = S(sprite);
            image.color = color;
            image.raycastTarget = false;
            if (sliced) image.type = Image.Type.Sliced;
            return image;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            var rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSharedMaterial = textMaterial;
            label.fontSize = size;
            label.text = text;
            label.color = color;
            label.alignment = align;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        static GameObject Instance(GameObject prefab, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            return go;
        }

        // ------------------------------------------------------------------ prefabs

        /// shadow (offset 4,-8) -> fill (tinted) -> gloss -> outline (#2B1410), all raycast-off.
        static GameObject BuildPlatePrefab(string name, UIPanelView.Role role, string fillSprite, string outlineSprite, bool gloss)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var root = (RectTransform)go.transform;
            root.sizeDelta = role == UIPanelView.Role.Pill ? new Vector2(220f, 64f) : role == UIPanelView.Role.Keycap ? new Vector2(64f, 64f) : new Vector2(300f, 100f);

            var shadow = NewImage("Shadow", root, "panel_shadow", Color.black, true);
            Stretch(shadow.rectTransform, -16, -16, -16, -16);
            shadow.rectTransform.anchoredPosition += new Vector2(4f, -8f);
            var fill = NewImage("Fill", root, fillSprite, palette.fill, true);
            Stretch(fill.rectTransform);
            var glossImage = NewImage("Gloss", root, "panel_gloss", Color.white, true);
            Stretch(glossImage.rectTransform);
            glossImage.enabled = gloss;
            var outline = NewImage("Outline", root, outlineSprite, palette.outline, true);
            Stretch(outline.rectTransform);

            var view = go.AddComponent<UIPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("role").enumValueIndex = (int)role;
            so.FindProperty("shadow").objectReferenceValue = shadow;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("gloss").objectReferenceValue = glossImage;
            so.FindProperty("outline").objectReferenceValue = outline;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ------------------------------------------------------------------ scene

        static void RemoveOldHud()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = canvas.gameObject.name;
                if (n == "ChaosHUD" || n == "HUD") Object.DestroyImmediate(canvas.gameObject);
            }
            var stale = GameObject.Find("HUD");
            if (stale != null) Object.DestroyImmediate(stale);
        }

        static Canvas NewCanvas(string name, Transform parent, int order, bool raycaster)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (raycaster) go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void BuildHud(UIMotionPresets motion, GameObject panelPrefab, GameObject pillPrefab, GameObject keycapPrefab)
        {
            // Opening the scene can unload assets created earlier in this run; re-resolve them from disk.
            motion = AssetDatabase.LoadAssetAtPath<UIMotionPresets>(UiSettings + "/UIMotionPresets.asset");
            palette = AssetDatabase.LoadAssetAtPath<UIPalette>(UiSettings + "/UIPalette.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Settings/RussoHUD.asset");
            textMaterial = AssetDatabase.LoadAssetAtPath<Material>(UiSettings + "/RussoHUD_Outline.mat");
            panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIPanel.prefab");
            pillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIPill.prefab");
            keycapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/UIKeycap.prefab");
            var hudRoot = new GameObject("HUD").transform;
            var accent = palette.accent;
            var text = palette.text;

            // ---- static canvas: nothing here is animated, so it never rebuilds with the HUD
            var staticCanvas = NewCanvas("Canvas_Static", hudRoot, 0, false);
            var dotRing = NewImage("CrosshairRing", staticCanvas.transform, "circle_fill", new Color(palette.outline.r, palette.outline.g, palette.outline.b, 0.8f));
            Place(dotRing.rectTransform, Mid, Mid, Vector2.zero, new Vector2(14f, 14f));
            var dot = NewImage("CrosshairDot", staticCanvas.transform, "circle_fill", new Color(text.r, text.g, text.b, 0.9f));
            Place(dot.rectTransform, Mid, Mid, Vector2.zero, new Vector2(8f, 8f));

            // ---- animated canvas
            var hudCanvas = NewCanvas("Canvas_Hud", hudRoot, 10, false);
            var shake = NewRect("ShakeRoot", hudCanvas.transform);
            Stretch(shake);

            var damage = BuildInfoPlate(shake, panelPrefab, "DamagePanel", "icon_coin", accent, "$0",
                new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(430f, 104f), 1.8f, new Vector2(-560f, 0f));
            var timer = BuildInfoPlate(shake, panelPrefab, "TimerPanel", "icon_clock", text, "105 с",
                new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(320f, 104f), -1.5f, new Vector2(460f, 0f));

            var gauge = BuildGauge(shake);
            var mode = BuildMode(shake, pillPrefab);
            var hints = BuildHints(shake, keycapPrefab);
            var prompt = BuildPrompt(shake, keycapPrefab, pillPrefab);
            var banner = BuildBanner(shake, pillPrefab);
            var popups = BuildPopupTemplate(shake, pillPrefab);

            // ---- overlay canvas: shock vignette and the result screen
            var overlay = NewCanvas("Canvas_Overlay", hudRoot, 20, true);
            var vignetteImage = NewImage("ShockVignette", overlay.transform, "shock_vignette", palette.shock);
            Stretch(vignetteImage.rectTransform);
            var vignette = vignetteImage.gameObject.AddComponent<CanvasGroup>();
            vignette.alpha = 0f; vignette.blocksRaycasts = false; vignette.interactable = false;
            var result = BuildResult(overlay.transform, panelPrefab);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ---- wire HudRoot
            var hud = hudRoot.gameObject.AddComponent<HudRoot>();
            var so = new SerializedObject(hud);
            so.FindProperty("palette").objectReferenceValue = palette;
            so.FindProperty("motion").objectReferenceValue = motion;
            so.FindProperty("canvasSpace").objectReferenceValue = shake;
            so.FindProperty("shakeRoot").objectReferenceValue = shake;
            so.FindProperty("shockVignette").objectReferenceValue = vignette;
            WritePlate(so.FindProperty("damage"), damage);
            WritePlate(so.FindProperty("timer"), timer);
            WriteGauge(so.FindProperty("gauge"), gauge);
            WriteMode(so.FindProperty("mode"), mode);
            WriteHints(so.FindProperty("hints"), hints);
            WritePrompt(so.FindProperty("prompt"), prompt);
            WriteBanner(so.FindProperty("banner"), banner);
            so.FindProperty("popups.template").objectReferenceValue = popups;
            so.FindProperty("popups.count").intValue = 8;
            WriteResult(so.FindProperty("result"), result);
            so.ApplyModifiedPropertiesWithoutUndo();

            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var bootData = new SerializedObject(bootstrap);
            bootData.FindProperty("hud").objectReferenceValue = hud;
            bootData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
        }

        // ---- plates ----------------------------------------------------------------

        sealed class Plate
        {
            public RectTransform Root, Icon;
            public UIPanelView Panel;
            public Image IconImage;
            public TextMeshProUGUI Label;
            public Vector2 Rest, Hidden;
            public float Rotation;
        }

        static Plate BuildInfoPlate(Transform parent, GameObject prefab, string name, string icon, Color iconColor, string value,
            Vector2 anchor, Vector2 pos, Vector2 size, float rotation, Vector2 hidden)
        {
            var go = Instance(prefab, parent, name);
            var root = (RectTransform)go.transform;
            Place(root, anchor, anchor, pos, size);
            root.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var iconImage = NewImage("Icon", root, icon, iconColor);
            Place(iconImage.rectTransform, new Vector2(0f, 0.5f), Mid, new Vector2(58f, 2f), new Vector2(68f, 68f));
            var label = NewText("Value", root, value, 58f, TextAlignmentOptions.MidlineLeft, palette.text);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(108f, 0f), size - new Vector2(120f, 8f));

            return new Plate
            {
                Root = root, Panel = go.GetComponent<UIPanelView>(), Icon = iconImage.rectTransform, IconImage = iconImage,
                Label = label, Rest = pos, Hidden = hidden, Rotation = rotation
            };
        }

        static void WritePlate(SerializedProperty p, Plate plate)
        {
            p.FindPropertyRelative("root").objectReferenceValue = plate.Root;
            p.FindPropertyRelative("panel").objectReferenceValue = plate.Panel;
            p.FindPropertyRelative("icon").objectReferenceValue = plate.Icon;
            p.FindPropertyRelative("iconImage").objectReferenceValue = plate.IconImage;
            p.FindPropertyRelative("label").objectReferenceValue = plate.Label;
            p.FindPropertyRelative("restPosition").vector2Value = plate.Rest;
            p.FindPropertyRelative("hiddenOffset").vector2Value = plate.Hidden;
            p.FindPropertyRelative("restRotation").floatValue = plate.Rotation;
        }

        // ---- gauge -----------------------------------------------------------------

        sealed class GaugeParts
        {
            public RectTransform Root, Needle;
            public Image Ring;
            public TextMeshProUGUI Percent;
            public Vector2 Rest, Hidden;
        }

        static GaugeParts BuildGauge(Transform parent)
        {
            var root = NewRect("PressureGauge", parent);
            Vector2 rest = new Vector2(-200f, 200f);
            Place(root, new Vector2(1f, 0f), Mid, rest, new Vector2(270f, 270f));

            var dark = new Color(0.12f, 0.06f, 0.05f, 1f);
            var baseFill = NewImage("Base", root, "circle_fill", dark);
            Stretch(baseFill.rectTransform);
            var baseOutline = NewImage("BaseOutline", root, "circle_outline", palette.accent);
            Stretch(baseOutline.rectTransform);

            var track = NewImage("Track", root, "gauge_ring", new Color(1f, 1f, 1f, 0.16f));
            Stretch(track.rectTransform, 14, 14, 14, 14);
            track.type = Image.Type.Filled; track.fillMethod = Image.FillMethod.Radial360;
            track.fillOrigin = (int)Image.Origin360.Top; track.fillClockwise = true; track.fillAmount = 240f / 360f;
            track.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 120f);

            var ring = NewImage("Ring", root, "gauge_ring", palette.water);
            Stretch(ring.rectTransform, 14, 14, 14, 14);
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top; ring.fillClockwise = true; ring.fillAmount = 240f / 360f;

            var needle = NewImage("Needle", root, "gauge_needle", palette.text);
            Place(needle.rectTransform, Mid, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(24f, 96f));
            var hub = NewImage("Hub", root, "circle_fill", palette.accent);
            Place(hub.rectTransform, Mid, Mid, Vector2.zero, new Vector2(26f, 26f));

            var percent = NewText("Percent", root, "100%", 44f, TextAlignmentOptions.Center, palette.text);
            Place(percent.rectTransform, Mid, Mid, new Vector2(0f, -78f), new Vector2(180f, 56f));

            return new GaugeParts { Root = root, Needle = needle.rectTransform, Ring = ring, Percent = percent, Rest = rest, Hidden = new Vector2(0f, -420f) };
        }

        static void WriteGauge(SerializedProperty p, GaugeParts g)
        {
            p.FindPropertyRelative("root").objectReferenceValue = g.Root;
            p.FindPropertyRelative("ring").objectReferenceValue = g.Ring;
            p.FindPropertyRelative("needle").objectReferenceValue = g.Needle;
            p.FindPropertyRelative("percent").objectReferenceValue = g.Percent;
            p.FindPropertyRelative("restPosition").vector2Value = g.Rest;
            p.FindPropertyRelative("hiddenOffset").vector2Value = g.Hidden;
        }

        // ---- mode pill ---------------------------------------------------------------

        sealed class ModeParts
        {
            public RectTransform Root;
            public UIPanelView Pill;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Vector2 Rest, Hidden;
        }

        static ModeParts BuildMode(Transform parent, GameObject pillPrefab)
        {
            var go = Instance(pillPrefab, parent, "ModePill");
            var root = (RectTransform)go.transform;
            Vector2 rest = new Vector2(-200f, 392f);
            Place(root, new Vector2(1f, 0f), Mid, rest, new Vector2(236f, 70f));
            var icon = NewImage("Icon", root, "icon_water", palette.outline);
            Place(icon.rectTransform, new Vector2(0f, 0.5f), Mid, new Vector2(40f, 0f), new Vector2(44f, 44f));
            var label = NewText("Label", root, "ГАСІННЯ", 28f, TextAlignmentOptions.MidlineLeft, palette.outline);
            label.fontSharedMaterial = font.material;   // dark text on a light pill: no outline
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(150f, 50f));
            return new ModeParts { Root = root, Pill = go.GetComponent<UIPanelView>(), Icon = icon, Label = label, Rest = rest, Hidden = new Vector2(0f, -560f) };
        }

        static void WriteMode(SerializedProperty p, ModeParts m)
        {
            p.FindPropertyRelative("root").objectReferenceValue = m.Root;
            p.FindPropertyRelative("pill").objectReferenceValue = m.Pill;
            p.FindPropertyRelative("icon").objectReferenceValue = m.Icon;
            p.FindPropertyRelative("label").objectReferenceValue = m.Label;
            p.FindPropertyRelative("waterSprite").objectReferenceValue = S("icon_water");
            p.FindPropertyRelative("jetSprite").objectReferenceValue = S("icon_jet");
            p.FindPropertyRelative("restPosition").vector2Value = m.Rest;
            p.FindPropertyRelative("hiddenOffset").vector2Value = m.Hidden;
        }

        // ---- key hints ---------------------------------------------------------------

        sealed class HintParts
        {
            public RectTransform Root;
            public RectTransform[] Slots = new RectTransform[4];
            public CanvasGroup[] Groups = new CanvasGroup[4];
            public RectTransform[] Keycaps = new RectTransform[4];
            public UIPanelView[] KeyPanels = new UIPanelView[4];
            public TextMeshProUGUI[] Keys = new TextMeshProUGUI[4];
            public TextMeshProUGUI[] Captions = new TextMeshProUGUI[4];
            public Vector2 Rest, Hidden;
        }

        static HintParts BuildHints(Transform parent, GameObject keycapPrefab)
        {
            var h = new HintParts { Rest = new Vector2(0f, 54f), Hidden = new Vector2(0f, -200f) };
            h.Root = NewRect("KeyHints", parent);
            Place(h.Root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), h.Rest, new Vector2(1200f, 70f));
            for (int i = 0; i < 4; i++)
            {
                var slot = NewRect("Hint_" + i, h.Root);
                Place(slot, Mid, Mid, Vector2.zero, new Vector2(240f, 64f));
                var group = slot.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;

                var key = Instance(keycapPrefab, slot, "Keycap");
                var keyRect = (RectTransform)key.transform;
                Place(keyRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
                var keyLabel = NewText("Key", keyRect, "E", 32f, TextAlignmentOptions.Center, palette.outline);
                keyLabel.fontSharedMaterial = font.material;
                Stretch(keyLabel.rectTransform);
                var caption = NewText("Caption", slot, "кинути", 36f, TextAlignmentOptions.MidlineLeft, palette.text);
                Place(caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(420f, 56f));

                h.Slots[i] = slot; h.Groups[i] = group; h.Keycaps[i] = keyRect;
                h.KeyPanels[i] = key.GetComponent<UIPanelView>(); h.Keys[i] = keyLabel; h.Captions[i] = caption;
            }
            return h;
        }

        static void WriteHints(SerializedProperty p, HintParts h)
        {
            p.FindPropertyRelative("root").objectReferenceValue = h.Root;
            p.FindPropertyRelative("restPosition").vector2Value = h.Rest;
            p.FindPropertyRelative("hiddenOffset").vector2Value = h.Hidden;
            var slots = p.FindPropertyRelative("slots");
            slots.arraySize = h.Slots.Length;
            for (int i = 0; i < h.Slots.Length; i++)
            {
                var s = slots.GetArrayElementAtIndex(i);
                s.FindPropertyRelative("root").objectReferenceValue = h.Slots[i];
                s.FindPropertyRelative("group").objectReferenceValue = h.Groups[i];
                s.FindPropertyRelative("keycap").objectReferenceValue = h.Keycaps[i];
                s.FindPropertyRelative("keycapPanel").objectReferenceValue = h.KeyPanels[i];
                s.FindPropertyRelative("keyLabel").objectReferenceValue = h.Keys[i];
                s.FindPropertyRelative("caption").objectReferenceValue = h.Captions[i];
            }
        }

        // ---- context prompt ----------------------------------------------------------

        sealed class PromptParts
        {
            public RectTransform Root, Breathe, Keycap, Pill;
            public CanvasGroup Group;
            public UIPanelView KeyPanel, PillPanel;
            public TextMeshProUGUI Key, Label;
        }

        static PromptParts BuildPrompt(Transform parent, GameObject keycapPrefab, GameObject pillPrefab)
        {
            var p = new PromptParts();
            p.Root = NewRect("ContextPrompt", parent);
            Place(p.Root, Mid, Mid, new Vector2(0f, -130f), new Vector2(10f, 70f));
            p.Group = p.Root.gameObject.AddComponent<CanvasGroup>();
            p.Group.interactable = false; p.Group.blocksRaycasts = false;
            p.Breathe = NewRect("Breathe", p.Root);
            Place(p.Breathe, Mid, Mid, Vector2.zero, new Vector2(10f, 70f));

            var key = Instance(keycapPrefab, p.Breathe, "Keycap");
            p.Keycap = (RectTransform)key.transform;
            Place(p.Keycap, Mid, new Vector2(0f, 0.5f), new Vector2(-120f, 0f), new Vector2(64f, 64f));
            p.KeyPanel = key.GetComponent<UIPanelView>();
            p.Key = NewText("Key", p.Keycap, "F", 32f, TextAlignmentOptions.Center, palette.outline);
            p.Key.fontSharedMaterial = font.material;
            Stretch(p.Key.rectTransform);

            var pill = Instance(pillPrefab, p.Breathe, "Pill");
            p.Pill = (RectTransform)pill.transform;
            Place(p.Pill, Mid, new Vector2(0f, 0.5f), new Vector2(-40f, 0f), new Vector2(260f, 64f));
            p.PillPanel = pill.GetComponent<UIPanelView>();
            p.Label = NewText("Label", p.Pill, "взяти драбину", 34f, TextAlignmentOptions.Center, palette.text);
            Stretch(p.Label.rectTransform);
            return p;
        }

        static void WritePrompt(SerializedProperty s, PromptParts p)
        {
            s.FindPropertyRelative("root").objectReferenceValue = p.Root;
            s.FindPropertyRelative("group").objectReferenceValue = p.Group;
            s.FindPropertyRelative("breathe").objectReferenceValue = p.Breathe;
            s.FindPropertyRelative("keycap").objectReferenceValue = p.Keycap;
            s.FindPropertyRelative("keycapPanel").objectReferenceValue = p.KeyPanel;
            s.FindPropertyRelative("keyLabel").objectReferenceValue = p.Key;
            s.FindPropertyRelative("pill").objectReferenceValue = p.Pill;
            s.FindPropertyRelative("pillPanel").objectReferenceValue = p.PillPanel;
            s.FindPropertyRelative("label").objectReferenceValue = p.Label;
        }

        // ---- banner, popups -----------------------------------------------------------

        sealed class BannerParts
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public UIPanelView Pill;
            public TextMeshProUGUI Label;
            public Vector2 Rest;
        }

        static BannerParts BuildBanner(Transform parent, GameObject pillPrefab)
        {
            var go = Instance(pillPrefab, parent, "Banner");
            var b = new BannerParts { Root = (RectTransform)go.transform, Rest = new Vector2(0f, -150f) };
            Place(b.Root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), b.Rest, new Vector2(1180f, 124f));
            b.Group = go.AddComponent<CanvasGroup>();
            b.Group.interactable = false; b.Group.blocksRaycasts = false;
            b.Pill = go.GetComponent<UIPanelView>();
            b.Label = NewText("Label", b.Root, "ПОЖЕЖА!", 66f, TextAlignmentOptions.Center, palette.text);
            Stretch(b.Label.rectTransform, 40, 6, 40, 6);
            b.Label.enableAutoSizing = true;
            b.Label.fontSizeMin = 30f;
            b.Label.fontSizeMax = 66f;
            return b;
        }

        static void WriteBanner(SerializedProperty s, BannerParts b)
        {
            s.FindPropertyRelative("root").objectReferenceValue = b.Root;
            s.FindPropertyRelative("group").objectReferenceValue = b.Group;
            s.FindPropertyRelative("pill").objectReferenceValue = b.Pill;
            s.FindPropertyRelative("label").objectReferenceValue = b.Label;
            s.FindPropertyRelative("restPosition").vector2Value = b.Rest;
        }

        static RectTransform BuildPopupTemplate(Transform parent, GameObject pillPrefab)
        {
            var root = NewRect("PopupTemplate", parent);
            Place(root, Mid, Mid, Vector2.zero, new Vector2(10f, 10f));
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var burst = NewImage("Burst", root, "burst", palette.accent);
            Place(burst.rectTransform, Mid, Mid, Vector2.zero, new Vector2(260f, 260f));
            var pill = Instance(pillPrefab, root, "Pill");
            Place((RectTransform)pill.transform, Mid, Mid, Vector2.zero, new Vector2(300f, 60f));
            var label = NewText("Label", pill.transform, "Диван -$1200", 30f, TextAlignmentOptions.Center, palette.text);
            Stretch(label.rectTransform, 16, 0, 16, 0);
            root.gameObject.SetActive(false);
            return root;
        }

        // ---- result screen -------------------------------------------------------------

        sealed class ResultParts
        {
            public GameObject Root;
            public CanvasGroup Dim, ReasonGroup;
            public RectTransform Panel, Button, ButtonVisual;
            public TextMeshProUGUI Title, Reason;
            public Button ButtonComponent;
            public RectTransform[] Rows = new RectTransform[3];
            public CanvasGroup[] RowGroups = new CanvasGroup[3];
            public TextMeshProUGUI[] Values = new TextMeshProUGUI[3];
        }

        static ResultParts BuildResult(Transform parent, GameObject panelPrefab)
        {
            var r = new ResultParts();
            var root = NewRect("Result", parent);
            Stretch(root);
            r.Root = root.gameObject;

            var dimImage = NewImage("Dim", root, "panel_fill", new Color(0.05f, 0.02f, 0.02f, 0.78f));
            dimImage.sprite = null;
            Stretch(dimImage.rectTransform);
            dimImage.raycastTarget = true;
            r.Dim = dimImage.gameObject.AddComponent<CanvasGroup>();

            var panel = Instance(panelPrefab, root, "ResultPanel");
            r.Panel = (RectTransform)panel.transform;
            Place(r.Panel, Mid, Mid, Vector2.zero, new Vector2(820f, 780f));

            r.Title = NewText("Title", r.Panel, "ПІДСУМОК ХАОСУ", 68f, TextAlignmentOptions.Center, palette.accent);
            Place(r.Title.rectTransform, Mid, Mid, new Vector2(0f, 290f), new Vector2(740f, 90f));

            string[] icons = { "icon_coin", "icon_fire", "icon_clock" };
            string[] names = { "Збитки", "Пригод", "Час" };
            Color[] iconColors = { palette.accent, palette.jet, palette.text };
            for (int i = 0; i < 3; i++)
            {
                var row = NewRect("Row_" + i, r.Panel);
                Place(row, Mid, Mid, new Vector2(0f, 150f - i * 100f), new Vector2(680f, 84f));
                var group = row.gameObject.AddComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;
                var icon = NewImage("Icon", row, icons[i], iconColors[i]);
                Place(icon.rectTransform, new Vector2(0f, 0.5f), Mid, new Vector2(40f, 0f), new Vector2(64f, 64f));
                var label = NewText("Name", row, names[i], 42f, TextAlignmentOptions.MidlineLeft, palette.text);
                Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(300f, 70f));
                var value = NewText("Value", row, "0", 56f, TextAlignmentOptions.MidlineRight, palette.accent);
                Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(340f, 80f));
                r.Rows[i] = row; r.RowGroups[i] = group; r.Values[i] = value;
            }

            var reason = NewText("Reason", r.Panel, "", 34f, TextAlignmentOptions.Center, palette.text);
            Place(reason.rectTransform, Mid, Mid, new Vector2(0f, -170f), new Vector2(740f, 60f));
            reason.enableAutoSizing = true; reason.fontSizeMin = 22f; reason.fontSizeMax = 34f;
            r.Reason = reason;
            r.ReasonGroup = reason.gameObject.AddComponent<CanvasGroup>();

            var button = NewRect("RestartButton", r.Panel);
            Place(button, Mid, Mid, new Vector2(0f, -285f), new Vector2(380f, 104f));
            var hit = button.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            r.ButtonComponent = button.gameObject.AddComponent<Button>();
            r.ButtonComponent.targetGraphic = hit;
            button.gameObject.AddComponent<UIButtonFeedback>();
            r.Button = button;

            var visual = Instance(panelPrefab, button, "Visual");
            r.ButtonVisual = (RectTransform)visual.transform;
            Stretch(r.ButtonVisual);
            foreach (var image in visual.GetComponentsInChildren<Image>(true)) image.raycastTarget = false;
            var buttonLabel = NewText("Label", r.ButtonVisual, "ЩЕ РАЗ", 54f, TextAlignmentOptions.Center, palette.text);
            Stretch(buttonLabel.rectTransform);
            return r;
        }

        static void WriteResult(SerializedProperty s, ResultParts r)
        {
            s.FindPropertyRelative("root").objectReferenceValue = r.Root;
            s.FindPropertyRelative("dim").objectReferenceValue = r.Dim;
            s.FindPropertyRelative("panel").objectReferenceValue = r.Panel;
            s.FindPropertyRelative("title").objectReferenceValue = r.Title;
            s.FindPropertyRelative("reasonGroup").objectReferenceValue = r.ReasonGroup;
            s.FindPropertyRelative("reason").objectReferenceValue = r.Reason;
            s.FindPropertyRelative("button").objectReferenceValue = r.Button;
            s.FindPropertyRelative("buttonVisual").objectReferenceValue = r.ButtonVisual;
            s.FindPropertyRelative("buttonComponent").objectReferenceValue = r.ButtonComponent;
            var rows = s.FindPropertyRelative("rows");
            rows.arraySize = r.Rows.Length;
            for (int i = 0; i < r.Rows.Length; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("root").objectReferenceValue = r.Rows[i];
                row.FindPropertyRelative("group").objectReferenceValue = r.RowGroups[i];
                row.FindPropertyRelative("value").objectReferenceValue = r.Values[i];
            }
        }
    }
}
