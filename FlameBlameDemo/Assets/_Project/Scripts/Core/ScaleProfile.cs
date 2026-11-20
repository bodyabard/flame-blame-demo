using UnityEngine;

namespace FlameBlame.Core
{
    /// Single source of truth for camera lenses and "toy scale" proportions. Tuned in the calibration scene, read by the demo level.
    [CreateAssetMenu(menuName = "FlameBlame/Scale Profile")]
    public sealed class ScaleProfile : ScriptableObject
    {
        [Header("Lenses (vertical FOV, degrees)")]
        [Range(40, 120)] public float worldFov = 80f;
        [Range(30, 100)] public float viewmodelFov = 60f;
        [Header("Camera")]
        [Min(.5f)] public float cameraHeight = 1.6f;
        [Range(-30, 45)] public float cameraPitch = 18f;
        [Header("Toy proportions")]
        [Min(.5f)] public float npcHeight = 1.3f;
        [Range(.4f, 1.2f)] public float npcHeadRatio = .7f;
        [Range(.5f, 2.5f)] public float objectScaleMultiplier = 1.3f;
        [Header("Room (m)")]
        [Min(2)] public float roomWidth = 5f;
        [Min(2)] public float roomLength = 5.6f;
        [Min(2)] public float roomHeight = 3f;
        [Min(1)] public float doorHeight = 2.1f;
        [Header("First-person hands")]
        [Min(.2f)] public float handsScale = 1f;
        public Vector3 handsOffset = Vector3.zero;
    }
}
