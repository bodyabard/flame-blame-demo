using UnityEngine;
namespace FlameBlame.Hose
{
    [CreateAssetMenu(menuName="FlameBlame/Hose Reel Tuning")]
    public sealed class HoseReelTuning : ScriptableObject
    {
        public bool enabled=true;
        public float slack=1f, minimumLength=2f, retractSpeed=2f, payoutSpeed=12f, updateInterval=.12f;
        public int visibilityBudget=128;
        public float collisionSkin=.008f;
    }
}
