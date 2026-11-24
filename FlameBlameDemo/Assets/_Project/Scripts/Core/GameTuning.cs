using UnityEngine;
namespace FlameBlame.Core
{
    [CreateAssetMenu(menuName="FlameBlame/Game Tuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Min(60)] public float roundSeconds=270;
        [Min(.5f)] public float interactionRange=2.2f;
        [Range(.5f,.8f)] public float connectionSeconds=.65f;
        [Min(.5f)] public float smokeInterval=1;
    }
}
