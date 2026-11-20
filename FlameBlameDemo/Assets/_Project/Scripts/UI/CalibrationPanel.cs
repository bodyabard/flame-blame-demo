using System.IO;
using FlameBlame.Feedback;
using UnityEngine;

namespace FlameBlame.UI
{
    /// F1 toggles live tuning of ScaleProfile (IMGUI, WebGL-safe). F2 saves a screenshot.
    public sealed class CalibrationPanel : MonoBehaviour
    {
        [SerializeField] ScaleApplier applier;
        bool visible = true;
        Rect window = new Rect(12, 12, 330, 10);

        public void Bind(ScaleApplier target) { applier = target; }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Directory.CreateDirectory("Captures");
                ScreenCapture.CaptureScreenshot("Captures/Calibration-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
            }
        }

        void OnGUI()
        {
            if (!visible || applier == null || applier.Profile == null) return;
            window = GUILayout.Window(7716, window, Draw, "ScaleProfile (F1 hide, F2 capture)");
        }

        void Draw(int id)
        {
            var p = applier.Profile; bool changed = false;
            changed |= Slider("worldFov", ref p.worldFov, 40, 120);
            changed |= Slider("viewmodelFov", ref p.viewmodelFov, 30, 100);
            changed |= Slider("cameraHeight", ref p.cameraHeight, .8f, 2.2f);
            changed |= Slider("cameraPitch", ref p.cameraPitch, -30, 45);
            changed |= Slider("npcHeight", ref p.npcHeight, .8f, 2f);
            changed |= Slider("npcHeadRatio", ref p.npcHeadRatio, .4f, 1.2f);
            changed |= Slider("objectScale", ref p.objectScaleMultiplier, .5f, 2.5f);
            changed |= Slider("handsScale", ref p.handsScale, .5f, 1.6f);
            changed |= Slider("handsOffset.y", ref p.handsOffset.y, -.4f, .4f);
            changed |= Slider("handsOffset.z", ref p.handsOffset.z, -.4f, .4f);
            GUILayout.Label("room " + p.roomWidth + " x " + p.roomLength + " x " + p.roomHeight + " m, door " + p.doorHeight + " m (rebuild to change)");
            if (changed) applier.Apply();
            GUI.DragWindow();
        }

        static bool Slider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            float next = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(next.ToString("0.00"), GUILayout.Width(44));
            GUILayout.EndHorizontal();
            bool changed = !Mathf.Approximately(next, value); value = next; return changed;
        }
    }
}
