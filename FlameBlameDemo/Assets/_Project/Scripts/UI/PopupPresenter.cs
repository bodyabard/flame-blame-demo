using FlameBlame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    /// Floating "Sofa -$1200" labels at the screen position of the event. Fixed pool, no allocations at runtime.
    public sealed class PopupPresenter : IHudPresenter
    {
        sealed class Popup
        {
            public RectTransform Root, Burst, Pill;
            public CanvasGroup Group;
            public Image BurstImage;
            public TextMeshProUGUI Label;
            public bool Active;
            public float Age;
            public readonly char[] Buffer = new char[96];
        }

        readonly HudServices services;
        readonly RectTransform space;
        readonly Camera camera;
        readonly Popup[] popups;
        int next;

        public PopupPresenter(HudServices services, PopupView view, RectTransform space, Camera camera)
        {
            this.services = services; this.space = space; this.camera = camera;
            popups = new Popup[Mathf.Max(1, view.count)];
            for (int i = 0; i < popups.Length; i++)
            {
                var clone = Object.Instantiate(view.template, view.template.parent);
                clone.name = "Popup_" + i;
                var p = new Popup
                {
                    Root = clone,
                    Group = clone.GetComponent<CanvasGroup>(),
                    Burst = (RectTransform)clone.Find("Burst"),
                    Pill = (RectTransform)clone.Find("Pill"),
                };
                p.BurstImage = p.Burst.GetComponent<Image>();
                p.Label = p.Pill.Find("Label").GetComponent<TextMeshProUGUI>();
                p.Label.SetText(new string('W', p.Buffer.Length));
                p.Label.ForceMeshUpdate(true, true);
                clone.gameObject.SetActive(false);
                popups[i] = p;
            }
            view.template.gameObject.SetActive(false);
            services.Events.DamageReported += OnReport;
        }

        void OnReport(DamageReport report)
        {
            if (!report.HasPosition) return;
            Popup p = null;
            for (int i = 0; i < popups.Length; i++)
                if (!popups[i].Active) { p = popups[i]; break; }
            if (p == null) { p = popups[next]; next = (next + 1) % popups.Length; Release(p); }

            int length = 0;
            Append(p.Buffer, ref length, report.Subject);
            Append(p.Buffer, ref length, " -$");
            AppendNumber(p.Buffer, ref length, report.Amount);
            p.Label.SetCharArray(p.Buffer, 0, length);
            float width = p.Label.GetPreferredValues().x + 56f;
            p.Pill.sizeDelta = new Vector2(width, p.Pill.sizeDelta.y);

            Vector2 origin = ToCanvas(report.Position);
            for (int i = 0; i < popups.Length; i++)
                if (popups[i].Active && Mathf.Abs(popups[i].Root.anchoredPosition.x - origin.x) < width * 0.8f
                    && Mathf.Abs(popups[i].Root.anchoredPosition.y - origin.y) < 50f)
                    origin.y += 54f;

            var m = services.Motion;
            p.Active = true;
            p.Age = 0f;
            p.Root.gameObject.SetActive(true);
            services.Tween.Move(p.Root, origin, origin + new Vector2(0f, m.popupRise), m.popupSeconds, Ease.OutCubic);
            services.Tween.Scale(p.Root, Vector3.zero, Vector3.one, m.popupIn, m.popupEase);
            services.Tween.Fade(p.Group, 1f, 0f, m.popupSeconds * 0.45f, Ease.OutCubic, m.popupSeconds * 0.55f);
            var accent = services.Palette.accent;
            services.Tween.Tint(p.BurstImage, new Color(accent.r, accent.g, accent.b, 0.95f), new Color(accent.r, accent.g, accent.b, 0f), m.popupSeconds, Ease.OutCubic);
        }

        Vector2 ToCanvas(Vector3 world)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            Vector2 local;
            if (screen.z <= 0f) return new Vector2(0f, space.rect.height * 0.25f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screen, null, out local);
            float hx = space.rect.width * 0.5f - 220f, hy = space.rect.height * 0.5f - 120f;
            return new Vector2(Mathf.Clamp(local.x, -hx, hx), Mathf.Clamp(local.y, -hy, hy));
        }

        void Release(Popup p)
        {
            p.Active = false;
            services.Tween.Cancel(p.Root);
            services.Tween.Cancel(p.Group);
            services.Tween.Cancel(p.BurstImage);
            p.Root.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < popups.Length; i++)
            {
                var p = popups[i];
                if (!p.Active) continue;
                p.Age += dt;
                if (p.Age >= services.Motion.popupSeconds) { Release(p); continue; }
                p.Burst.localRotation = Quaternion.Euler(0f, 0f, p.Age * services.Motion.burstSpin);
            }
        }

        static void Append(char[] buffer, ref int length, string value)
        {
            for (int i = 0; i < value.Length && length < buffer.Length; i++) buffer[length++] = value[i];
        }

        static void AppendNumber(char[] buffer, ref int length, int number)
        {
            int divisor = 1;
            while (number / divisor >= 10 && divisor <= 100000000) divisor *= 10;
            do
            {
                if (length < buffer.Length) buffer[length++] = (char)('0' + number / divisor % 10);
                divisor /= 10;
            } while (divisor > 0);
        }

        public void Dispose() { services.Events.DamageReported -= OnReport; }
    }
}
