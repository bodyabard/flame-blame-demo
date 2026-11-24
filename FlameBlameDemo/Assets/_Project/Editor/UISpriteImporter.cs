using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Tools/FlameBlame/Import UI Sprites: sprite import settings, 9-slice borders from sprites_meta.json,
    /// plus the generated shock vignette.
    public static class UISpriteImporter
    {
        public const string SpritesFolder = "Assets/_Project/Art/UI/Sprites";
        const string MetaPath = "UI_Sprites/sprites_meta.json";

        [MenuItem("Tools/FlameBlame/Import UI Sprites")]
        public static void Import()
        {
            Directory.CreateDirectory(SpritesFolder);
            GenerateVignette();
            AssetDatabase.Refresh();

            var borders = ReadBorders();
            foreach (string path in Directory.GetFiles(SpritesFolder, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(assetPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 100f;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                if (name == "gauge_needle")
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    importer.spritePivot = new Vector2(0.5f, 0.06f);
                }
                importer.SetTextureSettings(settings);
                if (name == "gauge_needle") importer.spritePivot = new Vector2(0.5f, 0.06f);

                importer.spriteBorder = borders.TryGetValue(name, out Vector4 b) ? b : Vector4.zero;
                importer.SaveAndReimport();
            }
            Debug.Log("[FlameBlame] UI sprites imported with 9-slice borders.");
        }

        /// sprites_meta.json stores borders as [L, B, R, T], same order as TextureImporter.spriteBorder (x=L, y=B, z=R, w=T).
        static System.Collections.Generic.Dictionary<string, Vector4> ReadBorders()
        {
            var result = new System.Collections.Generic.Dictionary<string, Vector4>();
            if (!File.Exists(MetaPath)) { Debug.LogWarning("[FlameBlame] " + MetaPath + " not found; sprites keep zero borders."); return result; }
            string json = File.ReadAllText(MetaPath);
            var matches = Regex.Matches(json, "\"(\\w+)\"\\s*:\\s*\\{[^{}]*?\"border_LBRT\"\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\]");
            foreach (Match m in matches)
                result[m.Groups[1].Value] = new Vector4(float.Parse(m.Groups[2].Value), float.Parse(m.Groups[3].Value),
                    float.Parse(m.Groups[4].Value), float.Parse(m.Groups[5].Value));
            return result;
        }

        /// White radial vignette: transparent centre, opaque rim. Tinted at runtime for the electric shock.
        static void GenerateVignette()
        {
            string path = SpritesFolder + "/shock_vignette.png";
            if (File.Exists(path)) return;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1.3f, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
