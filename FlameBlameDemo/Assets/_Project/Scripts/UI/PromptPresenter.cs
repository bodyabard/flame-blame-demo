using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Contextual "F - action" prompt under the crosshair: pops in, breathes, squashes on key press.
    public sealed class PromptPresenter : IHudPresenter
    {
        const float KeySize = 64f, Gap = 12f, PillHeight = 64f, PillPadding = 56f;

        readonly HudServices services;
        readonly PromptView view;
        PromptKind kind;
        float clock;

        public PromptPresenter(HudServices services, PromptView view)
        {
            this.services = services; this.view = view;
            view.group.alpha = 0f;
            view.keyLabel.SetText("F");
            view.keycap.sizeDelta = new Vector2(KeySize, KeySize);
            services.Events.PromptChanged += OnPrompt;
        }

        void OnPrompt(PromptKind next)
        {
            if (next == kind) return;
            kind = next;
            var m = services.Motion;
            if (next == PromptKind.None)
            {
                services.Tween.Fade(view.group, view.group.alpha, 0f, 0.12f, Ease.OutCubic);
                return;
            }

            string text = next == PromptKind.PickLadder ? "взяти драбину" : "поставити";
            if(next==PromptKind.PickHose) text="взяти шланг";
            else if(next==PromptKind.ConnectHose) text="підключити";
            view.label.SetText(text);
            float pillW = view.label.GetPreferredValues(text).x + PillPadding;
            view.pill.sizeDelta = new Vector2(pillW, PillHeight);
            float total = KeySize + Gap + pillW;
            view.keycap.anchoredPosition = new Vector2(-total * 0.5f, 0f);
            view.pill.anchoredPosition = new Vector2(-total * 0.5f + KeySize + Gap, 0f);

            services.Tween.Scale(view.root, Vector3.one * 0.6f, Vector3.one, m.promptIn, Ease.OutBack);
            services.Tween.Fade(view.group, 0f, 1f, 0.1f, Ease.OutCubic);
        }

        public void Tick(float dt)
        {
            if (kind == PromptKind.None) return;
            clock += dt;
            float period = Mathf.Max(0.1f, services.Motion.breathePeriod);
            view.breathe.localScale = Vector3.one * (1f + Mathf.Sin(clock / period * 2f * Mathf.PI) * services.Motion.breatheScale);
            if (Input.GetKeyDown(KeyCode.F))
                services.Tween.Scale(view.keycap, new Vector3(1f, services.Motion.squash, 1f), Vector3.one,
                    services.Motion.squashDuration, Ease.OutBack);
        }

        public void Dispose() { services.Events.PromptChanged -= OnPrompt; }
    }
}
