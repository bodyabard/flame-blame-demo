namespace FlameBlame.Core
{
    /// What the player is doing, as far as on-screen key hints care.
    public enum HudContext { Extinguish, Jet, Dropped, Carrying, Climbing, Stowed, Unconnected }

    /// Contextual "press F" prompt shown near the crosshair.
    public enum PromptKind { None, PickLadder, PlaceLadder, PickHose, ConnectHose }
}
