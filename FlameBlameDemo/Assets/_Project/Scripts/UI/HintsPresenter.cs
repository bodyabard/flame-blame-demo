using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Row of keycaps for the current context only. Old set slides down and fades, the new one staggers in.
    public sealed class HintsPresenter : IHudPresenter
    {
        readonly struct Hint
        {
            public readonly string Key, Caption;
            public readonly KeyCode Code;
            public Hint(string key, string caption, KeyCode code) { Key = key; Caption = caption; Code = code; }
        }

        static readonly Hint[][] Sets =
        {
            /* Extinguish */ new[] { new Hint("ЛКМ", "вода", KeyCode.Mouse0), new Hint("Q", "режим", KeyCode.Q), new Hint("E", "кинути", KeyCode.E) },
            /* Jet        */ new[] { new Hint("ЛКМ", "тяга", KeyCode.Mouse0), new Hint("Q", "режим", KeyCode.Q), new Hint("E", "кинути", KeyCode.E) },
            /* Dropped    */ new[] { new Hint("E", "взяти брандспойт", KeyCode.E) },
            /* Carrying   */ new[] { new Hint("F", "поставити", KeyCode.F), new Hint("ЛКМ", "кинути", KeyCode.Mouse0) },
            /* Climbing   */ new[] { new Hint("W S", "рух", KeyCode.W), new Hint("Space", "відстрибнути", KeyCode.Space) },
        };

        const float KeyHeight = 64f, KeyMinWidth = 64f, Gap = 34f, Inner = 12f;

        readonly HudServices services;
        readonly HintsView view;
        HudContext pending, shown;
        int visibleCount;
        bool swapPending;
        float swapTimer;

        public HintsPresenter(HudServices services, HintsView view)
        {
            this.services = services; this.view = view;
            for (int i = 0; i < view.slots.Length; i++) view.slots[i].group.alpha = 0f;
            Populate(HudContext.Extinguish, false);
            services.Events.HudContextChanged += OnContext;
        }

        void OnContext(HudContext context)
        {
            if (context == shown && !swapPending) return;
            if (context == pending && swapPending) return;
            pending = context;
            if (!swapPending)
            {
                var m = services.Motion;
                for (int i = 0; i < visibleCount; i++)
                {
                    var s = view.slots[i];
                    services.Tween.Fade(s.group, s.group.alpha, 0f, m.hintFade, Ease.OutCubic);
                    services.Tween.Move(s.root, s.root.anchoredPosition, s.root.anchoredPosition + new Vector2(0f, -18f), m.hintFade, Ease.OutCubic);
                }
                swapTimer = m.hintFade;
                swapPending = true;
            }
        }

        void Populate(HudContext context, bool animate)
        {
            shown = context;
            var set = Sets[(int)context];
            visibleCount = Mathf.Min(set.Length, view.slots.Length);

            float total = 0f;
            Span widths = default;
            for (int i = 0; i < visibleCount; i++)
            {
                var slot = view.slots[i];
                slot.keyLabel.SetText(set[i].Key);
                slot.caption.SetText(set[i].Caption);
                float keyW = Mathf.Max(KeyMinWidth, slot.keyLabel.GetPreferredValues(set[i].Key).x + 34f);
                float capW = slot.caption.GetPreferredValues(set[i].Caption).x;
                slot.keycap.sizeDelta = new Vector2(keyW, KeyHeight);
                ((RectTransform)slot.caption.transform).anchoredPosition = new Vector2(keyW + Inner, 0f);
                float w = keyW + Inner + capW;
                slot.root.sizeDelta = new Vector2(w, KeyHeight);
                widths.Set(i, w);
                total += w + (i > 0 ? Gap : 0f);
            }

            float x = -total * 0.5f;
            var m = services.Motion;
            for (int i = 0; i < view.slots.Length; i++)
            {
                var slot = view.slots[i];
                if (i >= visibleCount) { slot.group.alpha = 0f; continue; }
                float w = widths.Get(i);
                Vector2 pos = new Vector2(x + w * 0.5f, 0f);
                x += w + Gap;
                if (animate)
                {
                    services.Tween.Move(slot.root, pos + new Vector2(0f, -18f), pos, 0.25f, Ease.OutBack, i * m.hintStagger);
                    services.Tween.Fade(slot.group, 0f, 1f, 0.15f, Ease.OutCubic, i * m.hintStagger);
                }
                else
                {
                    slot.root.anchoredPosition = pos;
                    slot.group.alpha = 1f;
                }
            }
        }

        // Fixed-size scratch for slot widths (no heap allocation).
        struct Span
        {
            float a, b, c, d;
            public void Set(int i, float v) { if (i == 0) a = v; else if (i == 1) b = v; else if (i == 2) c = v; else d = v; }
            public float Get(int i) => i == 0 ? a : i == 1 ? b : i == 2 ? c : d;
        }

        public void Tick(float dt)
        {
            if (swapPending)
            {
                swapTimer -= dt;
                if (swapTimer <= 0f) { swapPending = false; Populate(pending, true); }
            }
            if (swapPending) return;
            var set = Sets[(int)shown];
            for (int i = 0; i < visibleCount; i++)
                if (Input.GetKeyDown(set[i].Code))
                    services.Tween.Scale(view.slots[i].keycap, new Vector3(1f, services.Motion.squash, 1f), Vector3.one,
                        services.Motion.squashDuration, Ease.OutBack);
        }

        public void Dispose() { services.Events.HudContextChanged -= OnContext; }
    }
}
