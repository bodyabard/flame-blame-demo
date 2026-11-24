using System.Collections.Generic;
using FlameBlame.Core;
using TMPro;
using UnityEngine;

namespace FlameBlame.UI
{
    /// HUD composition: owns the tween runner, builds the presenters, ticks them on unscaled time.
    /// Scene references are wired by Tools/FlameBlame/Build UI.
    public sealed class HudRoot : MonoBehaviour
    {
        [Header("Style")]
        [SerializeField] UIPalette palette;
        [SerializeField] UIMotionPresets motion;

        [Header("Layout")]
        [SerializeField] RectTransform canvasSpace;     // the animated canvas root (popup coordinates)
        [SerializeField] RectTransform shakeRoot;       // parent of every animated HUD element

        [Header("Views")]
        [SerializeField] PanelWidget damage;
        [SerializeField] PanelWidget timer;
        [SerializeField] GaugeView gauge;
        [SerializeField] ModeView mode;
        [SerializeField] HintsView hints;
        [SerializeField] PromptView prompt;
        [SerializeField] BannerView banner;
        [SerializeField] PopupView popups;
        [SerializeField] CanvasGroup shockVignette;
        [SerializeField] ResultView result;

        // Every glyph the HUD can show, added to the dynamic atlas up front so no hitch or rebuild happens mid-round.
        const string Glyphs = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя0123456789 .,:;!?-+=$%/()'\"WASDEQFSpaceABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        readonly List<IHudPresenter> presenters = new List<IHudPresenter>(12);
        UITweenRunner tween;

        public void Initialize(IGameEvents events, DamageLedger ledger, GameStateMachine game, Camera view, int roundSeconds)
        {
            foreach (var panel in GetComponentsInChildren<UIPanelView>(true)) panel.Apply(palette);
            PrewarmGlyphs();

            tween = new UITweenRunner(192);
            var services = new HudServices(events, tween, motion, palette);

            var bannerPresenter = new BannerPresenter(services, banner);
            presenters.Add(bannerPresenter);
            presenters.Add(new RoundIntroPresenter(services, bannerPresenter, damage, timer, gauge, mode, hints));
            presenters.Add(new DamagePresenter(services, damage, ledger));
            presenters.Add(new TimerPresenter(services, timer, roundSeconds));
            presenters.Add(new GaugePresenter(services, gauge));
            presenters.Add(new ModePresenter(services, mode, bannerPresenter));
            presenters.Add(new HintsPresenter(services, hints));
            presenters.Add(new PromptPresenter(services, prompt));
            presenters.Add(new PopupPresenter(services, popups, canvasSpace, view));
            presenters.Add(new ImpactPresenter(services, shockVignette, shakeRoot));
            presenters.Add(new ResultPresenter(services, result, ledger, game));
        }

        void PrewarmGlyphs()
        {
            TMP_FontAsset last = null;
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.font == null || label.font == last) continue;
                last = label.font;
                label.font.TryAddCharacters(Glyphs, out _);
            }
        }

        void Update()
        {
            if (tween == null) return;
            float dt = Time.unscaledDeltaTime;
            tween.Tick(dt);
            for (int i = 0; i < presenters.Count; i++) presenters[i].Tick(dt);
        }

        void OnDestroy()
        {
            for (int i = 0; i < presenters.Count; i++) presenters[i].Dispose();
        }
    }
}
