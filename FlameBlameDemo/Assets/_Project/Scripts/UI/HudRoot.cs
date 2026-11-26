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
        [SerializeField] ShutterView shutter;

        // Every glyph the HUD can show, added to the dynamic atlas up front so no hitch or rebuild happens mid-round.
        const string Glyphs = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯабвгґдеєжзиіїйклмнопрстуфхцчшщьюя0123456789 .,:;!?-+=$%/()'\"WASDEQFSpaceABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        readonly List<IHudPresenter> presenters = new List<IHudPresenter>(12);
        UITweenRunner tween;
        IGameEvents scenarioEvents;
        BannerPresenter scenarioBanner;
        LevelScenario scenario;
        void OnObjective(int current)
        {
            if(scenario==null) return;
            tween.Punch(scenario.objective.rectTransform,.12f,.3f);
            if(current==4) scenarioBanner.Show("ГОРИЩЕ ГОРИТЬ!",2f);
            if(current==6) scenarioBanner.Show("ПЕРЕМОГА!",2f);
        }
        void OnScenario(ScenarioSignal signal) {if(signal==ScenarioSignal.HoseConnected) scenarioBanner.Show("ВОДА Є!",2f);}

        public void Initialize(IGameEvents events, DamageLedger ledger, GameStateMachine game, Camera view, int roundSeconds)
        {
            foreach (var panel in GetComponentsInChildren<UIPanelView>(true)) panel.Apply(palette);
            PrewarmGlyphs();

            tween = new UITweenRunner(192);
            var services = new HudServices(events, tween, motion, palette);

            var bannerPresenter = new BannerPresenter(services, banner);
            presenters.Add(bannerPresenter);
            scenarioBanner=bannerPresenter; scenarioEvents=events; scenario=FindFirstObjectByType<LevelScenario>();
            events.ObjectiveChanged+=OnObjective; events.ScenarioSignaled+=OnScenario;
            presenters.Add(new RoundIntroPresenter(services, bannerPresenter, damage, timer, gauge, mode, hints));
            presenters.Add(new DamagePresenter(services, damage, ledger));
            presenters.Add(new TimerPresenter(services, timer, roundSeconds));
            presenters.Add(new GaugePresenter(services, gauge));
            presenters.Add(new ModePresenter(services, mode, bannerPresenter));
            presenters.Add(new HintsPresenter(services, hints));
            presenters.Add(new PromptPresenter(services, prompt));
            presenters.Add(new PopupPresenter(services, popups, canvasSpace, view));
            presenters.Add(new ImpactPresenter(services, shockVignette, shakeRoot, GatherPlates()));
            var resultPresenter = new ResultPresenter(services, result, ledger, game);
            presenters.Add(resultPresenter);
            presenters.Add(new UIShutterScreen(services, shutter, resultPresenter));
        }

        /// Full-size metal plates (damage, timer, objective) whose outline flashes on an electric shock.
        UIPanelView[] GatherPlates()
        {
            var all = GetComponentsInChildren<UIPanelView>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++) if (all[i].PanelRole == UIPanelView.Role.Panel) count++;
            var plates = new UIPanelView[count];
            for (int i = 0, n = 0; i < all.Length; i++) if (all[i].PanelRole == UIPanelView.Role.Panel) plates[n++] = all[i];
            return plates;
        }

        void PrewarmGlyphs()
        {
            TMP_FontAsset last = null;
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.font == null || label.font == last) continue;
                last = label.font;
                label.font.TryAddCharacters(Glyphs + "✓•>", out _);
            }
        }

        public ShutterView EndingShutter => shutter;
        public void HideLoopTimer() { scenarioBanner.Hide(); tween.Scale(timer.root,timer.root.localScale,Vector3.zero,.5f,Ease.InOutSine); }

        void Update()
        {
            if (tween == null) return;
            float dt = Time.unscaledDeltaTime;
            tween.Tick(dt);
            for (int i = 0; i < presenters.Count; i++) presenters[i].Tick(dt);
        }

        void OnDestroy()
        {
            if(scenarioEvents!=null) {scenarioEvents.ObjectiveChanged-=OnObjective; scenarioEvents.ScenarioSignaled-=OnScenario;}
            for (int i = 0; i < presenters.Count; i++) presenters[i].Dispose();
        }
    }
}
