using UnityEngine;

namespace FlameBlame.UI
{
    /// Big pill at the top centre: flies in, holds, flies out. Later banners replace earlier ones.
    public sealed class BannerPresenter : IHudPresenter
    {
        enum Phase { Hidden, In, Hold, Out }

        struct Scheduled
        {
            public bool Active;
            public float At;
            public string Text;
            public float Hold;
        }

        readonly BannerView view;
        readonly UITweenRunner tween;
        readonly UIMotionPresets motion;
        readonly Scheduled[] queue = new Scheduled[4];
        Phase phase;
        float timer, clock;
        bool persistent;

        /// While locked (the pre-round intro), low-priority banners such as mode switches are ignored.
        public bool Locked { get; set; }

        public BannerPresenter(HudServices services, BannerView view)
        {
            tween = services.Tween; motion = services.Motion; this.view = view;
            view.group.alpha = 0f;
        }

        public void Show(string text, float hold, bool lowPriority = false) => Present(text, -1, hold, lowPriority);
        public void ShowNumber(string format, int number, float hold) => Present(format, number, hold, false);

        public void Schedule(float delay, string text, float hold)
        {
            for (int i = 0; i < queue.Length; i++)
                if (!queue[i].Active) { queue[i] = new Scheduled { Active = true, At = clock + delay, Text = text, Hold = hold }; return; }
        }

        void Present(string text, int number, float hold, bool lowPriority)
        {
            if (lowPriority && Locked) return;
            if (number >= 0) view.label.SetText(text, number); else view.label.SetText(text);
            persistent = hold < 0f;
            Vector2 rest = view.restPosition;
            if (phase == Phase.Hidden || phase == Phase.Out)
            {
                tween.Move(view.root, rest + new Vector2(0f, 260f), rest, motion.bannerIn, motion.bannerEase);
                tween.Scale(view.root, Vector3.one * 0.7f, Vector3.one, motion.bannerIn, motion.bannerEase);
                tween.Fade(view.group, 0f, 1f, 0.15f, Ease.OutCubic);
                phase = Phase.In;
                timer = motion.bannerIn;
                holdAfterIn = hold;
            }
            else
            {
                tween.Punch(view.root, 0.1f, 0.3f);
                if (phase == Phase.In) holdAfterIn = hold;
                else { phase = Phase.Hold; timer = hold; }
            }
        }

        float holdAfterIn;

        public void Tick(float dt)
        {
            clock += dt;
            for (int i = 0; i < queue.Length; i++)
                if (queue[i].Active && clock >= queue[i].At)
                {
                    queue[i].Active = false;
                    Present(queue[i].Text, -1, queue[i].Hold, false);
                }

            if (phase == Phase.Hidden) return;
            if (phase == Phase.Hold && persistent) return;
            timer -= dt;
            if (timer > 0f) return;
            switch (phase)
            {
                case Phase.In:
                    phase = Phase.Hold;
                    persistent = holdAfterIn < 0f;
                    timer = holdAfterIn;
                    break;
                case Phase.Hold:
                    phase = Phase.Out;
                    timer = motion.bannerOut;
                    tween.Move(view.root, view.restPosition, view.restPosition + new Vector2(0f, 220f), motion.bannerOut, Ease.OutCubic);
                    tween.Fade(view.group, 1f, 0f, motion.bannerOut, Ease.OutCubic);
                    break;
                case Phase.Out:
                    phase = Phase.Hidden;
                    break;
            }
        }

        public void Dispose() { }
    }
}
