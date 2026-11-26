using System;
using FlameBlame.Core;
using UnityEngine;

namespace FlameBlame.UI
{
    /// Roller-shutter result screen. One small state machine drives a single "drop" value (0 = rolled into the housing,
    /// 1 = closed); every segment of motion is a UITweenRunner value tween on unscaled time. No coroutines, no allocations.
    /// (arrive closed) -> Raising -> Open -> Lowering -> Bouncing -> ClosedHold -> Raising -> Open -> RestartLowering -> scene reload.
    public sealed class UIShutterScreen : IHudPresenter
    {
        public const string ArriveKey = "FB.ShutterArrive";
        const float Overscan = 40f;

        enum State { Open, Lowering, Bouncing, ClosedHold, Raising, RestartLowering }
        enum Step { Jerk, Pause, Smooth, Settle, Glide }

        readonly HudServices services;
        readonly ShutterView view;
        readonly ResultPresenter result;
        readonly RestartSceneCommand restart = new RestartSceneCommand();
        readonly Action<float> setDrop, setBounce;
        readonly float barHeight, hazardHeight;

        State state;
        Step step;
        int jerks;
        float timer, drop;
        bool revealed;

        public UIShutterScreen(HudServices services, ShutterView view, ResultPresenter result)
        {
            this.services = services; this.view = view; this.result = result;
            setDrop = SetDrop;
            setBounce = SetBounce;
            barHeight = view.bottomBar.sizeDelta.y;
            hazardHeight = view.hazard.sizeDelta.y;

            view.plate.localScale = Vector3.zero;
            view.root.SetActive(true);
            SetDrop(0f);

            services.Events.RoundEnded += OnRoundEnded;
            result.RestartRequested += OnRestart;

            // Arriving from a restart: the new scene starts under the shutter and lifts it over the game.
            if (PlayerPrefs.GetInt(ArriveKey, 0) == 1)
            {
                PlayerPrefs.SetInt(ArriveKey, 0);
                SetDrop(1f);
                BeginGlide();
            }
            else view.root.SetActive(false);   // rolled-up housing would cover the top HUD during play; shown only when the round ends
        }

        // ---- layout ------------------------------------------------------------------

        void SetDrop(float value)
        {
            drop = value;
            float h = view.curtain.rect.height;
            float housingH = h * services.Motion.housingHeight;
            float bottom = Mathf.LerpUnclamped(-(housingH - 6f), -h, value);   // y of the hazard strip's lower edge
            float barTop = bottom + hazardHeight + barHeight;

            view.housing.sizeDelta = new Vector2(view.housing.sizeDelta.x, housingH + Overscan);
            view.housing.anchoredPosition = new Vector2(0f, Overscan);
            view.slats.sizeDelta = new Vector2(view.slats.sizeDelta.x, Mathf.Max(0f, Overscan - barTop));
            view.slats.anchoredPosition = new Vector2(0f, Overscan);
            view.bottomBar.anchoredPosition = new Vector2(0f, barTop);
            view.hazard.anchoredPosition = new Vector2(0f, bottom + hazardHeight);
            view.plate.anchoredPosition = new Vector2(0f, barTop + (h * 0.5f - barHeight - hazardHeight));
        }

        // Rebound after the slam: two decaying hops, expressed as a drop value below 1.
        void SetBounce(float u)
        {
            float hop = Mathf.Abs(Mathf.Sin(u * 2f * Mathf.PI)) * (1f - u) * services.Motion.lowerBounceHeight;
            SetDrop(1f - hop);
        }

        // ---- events ------------------------------------------------------------------

        void OnRoundEnded(RoundEnding ending)
        {
            var m = services.Motion;
            switch (ending)
            {
                case RoundEnding.FiresOut: view.plateLabel.SetText("ПЕРЕМОГА!"); break;
                case RoundEnding.DamageLimit: view.plateLabel.SetText("ЗБИТКІВ ДОСИТЬ!"); break;
                default: view.plateLabel.SetText("ЧАС ВИЙШОВ!"); break;
            }
            view.root.SetActive(true);
            Canvas.ForceUpdateCanvases();
            SetDrop(drop);
            for (int i = 0; i < view.hudGroups.Length; i++)
                services.Tween.Fade(view.hudGroups[i], view.hudGroups[i].alpha, 0f, m.hudHide, Ease.OutCubic);
            revealed = false;
            state = State.Lowering;
            float fall = m.lowerDuration * (1f - m.lowerBounceShare);
            services.Tween.Value(view.slats, 0, setDrop, drop, 1f, fall, Ease.InCubic);
            timer = fall;
        }

        void OnRestart()
        {
            if (state != State.Open) return;
            var m = services.Motion;
            services.Tween.Scale(view.plate, view.plate.localScale, Vector3.zero, 0.1f, Ease.OutCubic);
            state = State.RestartLowering;
            services.Tween.Value(view.slats, 0, setDrop, drop, 1f, m.restartLowerDuration, Ease.InCubic);
            timer = m.restartLowerDuration;
        }

        void Slam()
        {
            var m = services.Motion;
            services.Tween.Shake(view.shakeRoot, m.slamShakePixels, m.slamShakeSeconds);
            Play(view.slam, 1f);
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip != null && view.audioSource != null) view.audioSource.PlayOneShot(clip, volume);
        }

        // ---- raise sequences ----------------------------------------------------------

        void BeginJerks()
        {
            state = State.Raising;
            jerks = 0;
            Jerk();
        }

        void Jerk()
        {
            var m = services.Motion;
            step = Step.Jerk;
            services.Tween.Value(view.slats, 0, setDrop, drop, drop - m.raiseStepTravel, m.raiseStepDuration, Ease.OutBack);
            Play(view.rattle, 0.8f);
            timer = m.raiseStepDuration;
        }

        void BeginGlide()
        {
            state = State.Raising;
            step = Step.Glide;
            revealed = true;   // nothing to reveal: the game is already running behind it
            services.Tween.Value(view.slats, 0, setDrop, drop, 0f, services.Motion.restartRaiseDuration, Ease.InOutSine);
            Play(view.rattle, 0.6f);
            timer = services.Motion.restartRaiseDuration;
        }

        void AdvanceRaise()
        {
            var m = services.Motion;
            switch (step)
            {
                case Step.Jerk:
                    if (++jerks < m.raiseSteps) { step = Step.Pause; timer = m.raiseStepPause; }
                    else
                    {
                        step = Step.Smooth;
                        services.Tween.Value(view.slats, 0, setDrop, drop, -m.raiseOvershoot, m.raiseSmoothDuration, Ease.InOutSine);
                        Play(view.rattle, 1f);
                        timer = m.raiseSmoothDuration;
                    }
                    break;
                case Step.Pause:
                    Jerk();
                    break;
                case Step.Smooth:
                    step = Step.Settle;
                    services.Tween.Value(view.slats, 0, setDrop, drop, 0f, m.raiseSettle, Ease.OutCubic);
                    timer = m.raiseSettle;
                    break;
                case Step.Settle:
                    Play(view.latch, 0.9f);
                    state = State.Open;
                    break;
                default:   // Glide
                    state = State.Open;
                    view.root.SetActive(false);   // back to gameplay: hide the housing again
                    break;
            }
        }

        // ---- tick -----------------------------------------------------------------------

        public void Tick(float dt)
        {
            if (state == State.Open) return;
            timer -= dt;

            // Result animations start as soon as the bar has passed the reveal height, not at the end of the lift.
            if (state == State.Raising && !revealed && drop <= 1f - services.Motion.resultStartHeight)
            {
                revealed = true;
                result.Reveal();
            }

            if (timer > 0f) return;
            var m = services.Motion;
            switch (state)
            {
                case State.Lowering:
                {
                    Slam();
                    state = State.Bouncing;
                    float bounce = m.lowerDuration * m.lowerBounceShare;
                    services.Tween.Value(view.slats, 0, setBounce, 0f, 1f, bounce, Ease.Linear);
                    services.Tween.Scale(view.plate, Vector3.zero, Vector3.one, m.plateIn, Ease.OutBack, 0.08f);
                    timer = bounce;
                    break;
                }
                case State.Bouncing:
                    state = State.ClosedHold;
                    timer = m.closedHold;
                    break;
                case State.ClosedHold:
                    BeginJerks();
                    break;
                case State.Raising:
                    AdvanceRaise();
                    break;
                case State.RestartLowering:
                    Slam();
                    PlayerPrefs.SetInt(ArriveKey, 1);
                    state = State.Open;   // inert until the scene reloads
                    restart.Execute();
                    break;
            }
        }

        public void Dispose()
        {
            services.Events.RoundEnded -= OnRoundEnded;
            result.RestartRequested -= OnRestart;
        }
    }
}
