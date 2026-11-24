using System;
using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Result screen: dim, panel drops with a bounce, rows reveal one by one while counting up, button breathes.
    public sealed class ResultPresenter : IHudPresenter
    {
        readonly HudServices services;
        readonly ResultView view;
        readonly DamageLedger ledger;
        readonly GameStateMachine game;
        readonly RestartSceneCommand restart = new RestartSceneCommand();
        readonly Vector2[] rowRest;
        readonly Vector2 panelRest;
        readonly Action<float> setDamage, setIncidents, setTime;
        readonly UnityEngine.Events.UnityAction onClick;
        bool shown;
        float clock;

        public ResultPresenter(HudServices services, ResultView view, DamageLedger ledger, GameStateMachine game)
        {
            this.services = services; this.view = view; this.ledger = ledger; this.game = game;
            panelRest = view.panel.anchoredPosition;
            rowRest = new Vector2[view.rows.Length];
            for (int i = 0; i < rowRest.Length; i++) rowRest[i] = view.rows[i].root.anchoredPosition;
            setDamage = v => view.rows[0].value.SetText("${0:0}", v);
            setIncidents = v => view.rows[1].value.SetText("{0:0}", v);
            setTime = v => view.rows[2].value.SetText("{0:0} с", v);
            onClick = restart.Execute;
            view.buttonComponent.onClick.AddListener(onClick);
            var feedback = view.button.GetComponent<UIButtonFeedback>();
            if (feedback != null) feedback.Initialize(services.Tween, services.Motion);
            view.root.SetActive(false);
            services.Events.RoundEnded += OnEnded;
        }

        void OnEnded(RoundEnding ending)
        {
            var m = services.Motion;
            var tween = services.Tween;
            shown = true;
            view.root.SetActive(true);

            switch (ending)
            {
                case RoundEnding.FiresOut: view.reason.SetText("Вогню немає. Рахунок залишився."); break;
                case RoundEnding.DamageLimit: view.reason.SetText("Страхова компанія вийшла з чату."); break;
                default: view.reason.SetText("Зміна закінчилась. Ремонт — ні."); break;
            }

            tween.Fade(view.dim, 0f, 1f, m.resultDim, Ease.OutCubic);
            tween.Move(view.panel, panelRest + new Vector2(0f, 1100f), panelRest, m.panelDrop, m.dropEase);
            view.reasonGroup.alpha = 0f;
            view.button.localScale = Vector3.zero;

            float delay = m.rowFirstDelay;
            for (int i = 0; i < view.rows.Length; i++)
            {
                var row = view.rows[i];
                tween.Move(row.root, rowRest[i] + new Vector2(-90f, 0f), rowRest[i], 0.3f, Ease.OutBack, delay);
                tween.Fade(row.group, 0f, 1f, 0.2f, Ease.OutCubic, delay);
                switch (i)
                {
                    case 0: tween.Value(row.value, 0, setDamage, 0f, ledger.Total, m.rowCount, Ease.OutCubic, delay + 0.1f); break;
                    case 1: tween.Value(row.value, 0, setIncidents, 0f, ledger.Incidents, m.rowCount, Ease.OutCubic, delay + 0.1f); break;
                    default: tween.Value(row.value, 0, setTime, 0f, game.Elapsed, m.rowCount, Ease.OutCubic, delay + 0.1f); break;
                }
                delay += m.rowDelay;
            }
            tween.Fade(view.reasonGroup, 0f, 1f, 0.3f, Ease.OutCubic, delay);
            tween.Scale(view.button, Vector3.zero, Vector3.one, 0.35f, Ease.OutBack, delay + 0.2f);
        }

        public void Tick(float dt)
        {
            if (!shown) return;
            clock += dt;
            float s = 1f + Mathf.Sin(clock / Mathf.Max(0.1f, services.Motion.breathePeriod) * 2f * Mathf.PI) * services.Motion.breatheScale * 1.5f;
            view.buttonVisual.localScale = new Vector3(s, s, 1f);
        }

        public void Dispose()
        {
            services.Events.RoundEnded -= OnEnded;
            view.buttonComponent.onClick.RemoveListener(onClick);
        }
    }
}
