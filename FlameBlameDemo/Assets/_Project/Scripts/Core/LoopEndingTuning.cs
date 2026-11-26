using UnityEngine;
namespace FlameBlame.Core
{
    [CreateAssetMenu(menuName="FlameBlame/Loop Ending Tuning")]
    public sealed class LoopEndingTuning : ScriptableObject
    {
        public float readRange=2.5f, openSeconds=.4f, closeSeconds=.25f, revealSeconds=2.5f;
        public float musicFadeSeconds=2f, silenceSeconds=.9f, shutterSeconds=2f, exitFadeSeconds=1.5f;
        public float extinguishSeconds=1.25f;
        public bool restoreColorOnEnding;
    }
}
