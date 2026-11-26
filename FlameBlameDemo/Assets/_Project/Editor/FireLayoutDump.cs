using System.IO;
using System.Text;
using FlameBlame.Fire;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Batch helper: lists every Flammable in Main with zone, profile and node centre (for fire-chain design).
    public static class FireLayoutDump
    {
        static string Path(Transform t) { string s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var sb = new StringBuilder();
            foreach (var f in Object.FindObjectsByType<Flammable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rs = f.GetComponentsInChildren<Renderer>(true); var b = new Bounds(f.transform.position, Vector3.zero);
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var z = f.GetComponentInParent<FireZone>(); var prof = new SerializedObject(f).FindProperty("profile").objectReferenceValue;
                bool ign = new SerializedObject(f).FindProperty("igniteOnStart").boolValue;
                sb.AppendLine($"{f.name,-16} zone={(z != null ? z.id.ToString() : "-"),-7} prof={(prof != null ? prof.name : "-"),-22} centre={b.center:F2} size={b.size:F2} active={f.gameObject.activeInHierarchy} ignite={ign} path={Path(f.transform)} zoneObj={(z != null ? Path(z.transform) : "-")}");
            }
            File.WriteAllText("Captures/Calibration/fire_layout.txt", sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
