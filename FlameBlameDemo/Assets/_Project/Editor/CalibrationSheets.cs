using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FlameBlame.Core;
using FlameBlame.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace FlameBlame.EditorTools
{
    /// Contact sheets + proportion metrics for the calibration scene.
    /// Batch: Unity -batchmode -executeMethod FlameBlame.EditorTools.CalibrationSheets.RunBatch -calibSheets fov,npc,obj,final -calibTag it1
    ///        [-calibSet "worldFov=80;npcHeight=1.3"] [-calibAxes "world=70,80;vm=50,60;npcH=1,1.2;head=.6,.7;obj=1,1.3"]
    public static class CalibrationSheets
    {
        const string OutDir = "Captures/Calibration";
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly StringBuilder log = new StringBuilder();

        public static void RunBatch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string sheets = Arg(args, "-calibSheets", "final"), tag = Arg(args, "-calibTag", "run");
                string set = Arg(args, "-calibSet", ""), axes = Arg(args, "-calibAxes", "");
                Directory.CreateDirectory(OutDir); if (!File.Exists(CalibrationSceneBuilder.ScenePath) || Array.IndexOf(args, "-calibBuild") >= 0) CalibrationSceneBuilder.Build();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var profile = CalibrationSceneBuilder.LoadProfile();
                ApplySet(profile, set); EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
                var ax = ParseAxes(axes);
                foreach (string sheet in sheets.Split(','))
                {
                    switch (sheet.Trim())
                    {
                        case "fov": Grid(profile, tag + "_fov", ax["vm"], ax["world"], (q, r, c) => { q.viewmodelFov = r; q.worldFov = c; }, r => "V" + F(r), c => "W" + F(c), 640, 360); break;
                        case "npc": Grid(profile, tag + "_npc", ax["head"], ax["npcH"], (q, r, c) => { q.npcHeadRatio = r; q.npcHeight = c; }, r => "N" + F(r), c => "H" + F(c), 640, 360); break;
                        case "obj": Grid(profile, tag + "_obj", new[] { 0f }, ax["obj"], (q, r, c) => { q.objectScaleMultiplier = c; }, r => "", c => "O" + F(c), 960, 540); break;
                        case "final": Final(profile, tag); break;
                    }
                }
                File.WriteAllText(OutDir + "/" + tag + "_metrics.txt", log.ToString());
                Debug.Log("[FlameBlame] Calibration sheets done: " + tag);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); File.WriteAllText(OutDir + "/error.txt", e.ToString()); EditorApplication.Exit(1); }
        }

        static string Arg(string[] a, string key, string def) { for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1]; return def; }
        static string F(float v) => v.ToString("0.##", Inv);

        static void ApplySet(ScaleProfile p, string set)
        {
            if (string.IsNullOrEmpty(set)) return;
            foreach (string kv in set.Split(';'))
            {
                var parts = kv.Split('='); if (parts.Length != 2) continue;
                if (parts[0] == "handsOffset") { var v = parts[1].Split(','); p.handsOffset = new Vector3(Pf(v[0]), Pf(v[1]), Pf(v[2])); continue; }
                var field = typeof(ScaleProfile).GetField(parts[0]);
                if (field == null) throw new Exception("Unknown ScaleProfile field " + parts[0]);
                field.SetValue(p, Pf(parts[1]));
            }
        }
        static float Pf(string s) => float.Parse(s, Inv);

        static Dictionary<string, float[]> ParseAxes(string axes)
        {
            var d = new Dictionary<string, float[]>
            {
                { "world", new[] { 60f, 70f, 80f, 90f, 100f } }, { "vm", new[] { 50f, 60f, 70f } },
                { "npcH", new[] { 1.0f, 1.2f, 1.4f, 1.6f } }, { "head", new[] { .6f, .7f, .85f } }, { "obj", new[] { 1f, 1.3f, 1.6f } }
            };
            if (!string.IsNullOrEmpty(axes))
                foreach (string part in axes.Split(';'))
                {
                    var kv = part.Split('='); var vals = kv[1].Split(',');
                    var arr = new float[vals.Length]; for (int i = 0; i < arr.Length; i++) arr[i] = Pf(vals[i]); d[kv[0]] = arr;
                }
            return d;
        }

        static void Grid(ScaleProfile baseProfile, string name, float[] rows, float[] cols, Action<ScaleProfile, float, float> set,
            Func<float, string> rowLabel, Func<float, string> colLabel, int cw, int ch)
        {
            var sheet = new Texture2D(cols.Length * cw, rows.Length * ch, TextureFormat.RGB24, false);
            log.AppendLine("== " + name);
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols.Length; c++)
                {
                    var q = Object.Instantiate(baseProfile); set(q, rows[r], cols[c]);
                    var tex = RenderCell(q, cw, ch, out string metrics);
                    string label = (rowLabel(rows[r]) + " " + colLabel(cols[c])).Trim();
                    Label(tex, label);
                    sheet.SetPixels(c * cw, (rows.Length - 1 - r) * ch, cw, ch, tex.GetPixels());
                    log.AppendLine(label + " | " + metrics);
                    Object.DestroyImmediate(tex); Object.DestroyImmediate(q);
                }
            sheet.Apply(); File.WriteAllBytes(OutDir + "/" + name + ".png", sheet.EncodeToPNG()); Object.DestroyImmediate(sheet);
        }

        static void Final(ScaleProfile profile, string tag)
        {
            var tex = RenderCell(profile, 1920, 1080, out string metrics);
            log.AppendLine("== final | " + metrics);
            File.WriteAllBytes(OutDir + "/" + tag + "_final.png", tex.EncodeToPNG());
            var reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
            reference.LoadImage(File.ReadAllBytes("Reference/14_locker_room_scale_ref.jpg"));
            var a = Resize(reference, 1280, 720); var b = Resize(tex, 1280, 720);
            var both = new Texture2D(2560, 720, TextureFormat.RGB24, false);
            both.SetPixels(0, 0, 1280, 720, a.GetPixels()); both.SetPixels(1280, 0, 1280, 720, b.GetPixels()); both.Apply();
            File.WriteAllBytes(OutDir + "/" + tag + "_compare_ref_left_ours_right.png", both.EncodeToPNG());
            foreach (var t in new[] { tex, reference, a, b, both }) Object.DestroyImmediate(t);
        }

        static Texture2D Resize(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
            var o = new Texture2D(w, h, TextureFormat.RGB24, false); o.ReadPixels(new Rect(0, 0, w, h), 0, 0); o.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); return o;
        }

        static Texture2D Grab(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); rt.Create();
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = prev; rt.Release(); Object.DestroyImmediate(rt); return tex;
        }

        // SingleCameraRequest ignores the camera stack, so hands are rendered as a base camera over black and white
        // backgrounds; pixels that differ between the two are background, the rest is the viewmodel.
        static Texture2D RenderCell(ScaleProfile p, int w, int h, out string metrics)
        {
            var root = CalibrationSceneBuilder.BuildContent(p, false);
            var applier = root.GetComponent<ScaleApplier>();
            var cam = applier.WorldCamera; var vm = applier.ViewmodelCamera;
            cam.aspect = w / (float)h; vm.aspect = cam.aspect;
            metrics = Measure(root, cam, vm, p);
            cam.GetUniversalAdditionalCameraData().cameraStack.Clear();
            var world = Grab(cam, w, h);
            var vmData = vm.GetUniversalAdditionalCameraData(); vmData.renderType = CameraRenderType.Base;
            vm.clearFlags = CameraClearFlags.SolidColor; vm.backgroundColor = Color.black;
            var black = Grab(vm, w, h); vm.backgroundColor = Color.white; var white = Grab(vm, w, h);
            var wp = world.GetPixels32(); var bp = black.GetPixels32(); var hp = white.GetPixels32();
            int covered = 0, top = h;
            for (int i = 0; i < wp.Length; i++)
            {
                bool bg = Mathf.Abs(bp[i].r - hp[i].r) + Mathf.Abs(bp[i].g - hp[i].g) + Mathf.Abs(bp[i].b - hp[i].b) > 60;
                if (!bg) { wp[i] = bp[i]; covered++; int row = i / w; if (row < top) top = row; }
            }
            world.SetPixels32(wp); world.Apply();
            metrics += string.Format(Inv, " | handsTop(fromTop)={0:0.000} handsCover={1:0.000}", top / (float)h, covered / (float)wp.Length);
            Object.DestroyImmediate(black); Object.DestroyImmediate(white); Object.DestroyImmediate(root);
            return world;
        }

        // ---- metrics: viewport fractions comparable to the pixel measurements of Reference/14 (see SCALE_NOTES.md)
        static Rect View(Camera cam, params Renderer[] rs)
        {
            float x0 = 9, x1 = -9, y0 = 9, y1 = -9;
            foreach (var r in rs)
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var v = cam.WorldToViewportPoint(c);
                    x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
                }
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static Renderer R(GameObject root, string path) => root.transform.Find(path).GetComponent<Renderer>();

        static string Measure(GameObject root, Camera cam, Camera vm, ScaleProfile p)
        {
            var door = View(cam, R(root, "Room/Door/DoorLeaf"));
            var npc02 = root.transform.Find("NPC02/Shape");
            var torso = View(cam, R(root, "NPC02/Shape/Torso")); var helmet = View(cam, R(root, "NPC02/Shape/Helmet"));
            var torso1 = View(cam, R(root, "NPC01/Shape/Torso"));
                        var lineA = R(root, "Room/YellowLineA").transform;
            var e0 = cam.WorldToViewportPoint(lineA.position - lineA.right * lineA.localScale.x * .5f);
            var e1 = cam.WorldToViewportPoint(lineA.position + lineA.right * lineA.localScale.x * .5f);
            float slope = Mathf.Atan2((e1.y - e0.y) * cam.pixelHeight, (e1.x - e0.x) * cam.pixelWidth) * Mathf.Rad2Deg;
            return string.Format(Inv, "doorH={0:0.000} doorW={1:0.000} doorTopY={2:0.000} | npc02 torsoW={3:0.000} helmetTopY={4:0.000} | npc01 torsoW={5:0.000} | lineSlopeDeg={6:0.0}",
                door.height, door.width, 1 - door.yMax, torso.width, 1 - helmet.yMax, torso1.width, slope);
        }

        // ---- tiny 5x7 bitmap font so contact-sheet cells carry their parameters
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            {'W', new[]{"10001","10001","10001","10101","10101","11011","10001"}}, {'V', new[]{"10001","10001","10001","10001","01010","01010","00100"}},
            {'H', new[]{"10001","10001","10001","11111","10001","10001","10001"}}, {'N', new[]{"10001","11001","10101","10011","10001","10001","10001"}},
            {'O', new[]{"01110","10001","10001","10001","10001","10001","01110"}},
            {'0', new[]{"01110","10001","10011","10101","11001","10001","01110"}}, {'1', new[]{"00100","01100","00100","00100","00100","00100","01110"}},
            {'2', new[]{"01110","10001","00001","00110","01000","10000","11111"}}, {'3', new[]{"11110","00001","00001","01110","00001","00001","11110"}},
            {'4', new[]{"00010","00110","01010","10010","11111","00010","00010"}}, {'5', new[]{"11111","10000","11110","00001","00001","10001","01110"}},
            {'6', new[]{"01110","10000","10000","11110","10001","10001","01110"}}, {'7', new[]{"11111","00001","00010","00100","01000","01000","01000"}},
            {'8', new[]{"01110","10001","10001","01110","10001","10001","01110"}}, {'9', new[]{"01110","10001","10001","01111","00001","00001","01110"}},
            {'.', new[]{"00000","00000","00000","00000","00000","01100","01100"}}, {' ', new[]{"00000","00000","00000","00000","00000","00000","00000"}}
        };

        static void Label(Texture2D t, string text)
        {
            int scale = Mathf.Max(2, t.height / 150), x = 6, y0 = t.height - 8 * scale - 6;
            foreach (char ch in text)
            {
                if (!Font.TryGetValue(ch, out var g)) continue;
                for (int pass = 0; pass < 2; pass++)
                    for (int row = 0; row < 7; row++) for (int col = 0; col < 5; col++)
                        if (g[row][col] == '1')
                            for (int sy = 0; sy < scale; sy++) for (int sx = 0; sx < scale; sx++)
                            {
                                int px = x + col * scale + sx, py = y0 + (6 - row) * scale + sy;
                                if (pass == 0) { for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) Put(t, px + dx, py + dy, Color.black); }
                                else Put(t, px, py, Color.white);
                            }
                x += 6 * scale;
            }
            t.Apply();
        }
        static void Put(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c); }
    }
}
