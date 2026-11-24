using System;
using FlameBlame.Core;

namespace FlameBlame.UI
{
    /// A HUD presenter reacts to game events and drives its view through the tween runner.
    public interface IHudPresenter : IDisposable
    {
        void Tick(float dt);
    }

    /// Everything a presenter may depend on. Injected by HudRoot; no statics, no singletons.
    public readonly struct HudServices
    {
        public readonly IGameEvents Events;
        public readonly UITweenRunner Tween;
        public readonly UIMotionPresets Motion;
        public readonly UIPalette Palette;

        public HudServices(IGameEvents events, UITweenRunner tween, UIMotionPresets motion, UIPalette palette)
        {
            Events = events; Tween = tween; Motion = motion; Palette = palette;
        }
    }
}
