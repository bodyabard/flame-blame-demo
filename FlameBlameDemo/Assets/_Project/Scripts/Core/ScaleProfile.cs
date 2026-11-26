using UnityEngine;

namespace FlameBlame.Core
{
    /// Single source of truth for camera lenses and "toy scale" proportions. Tuned in the calibration scene, read by the demo level.
    [CreateAssetMenu(menuName = "FlameBlame/Scale Profile")]
    public sealed class ScaleProfile : ScriptableObject
    {
        [Header("Hose visuals")]
        [Range(.07f,.11f)] public float tubeRadius = .09f;
        [Range(1f,1.99f)] public float bendLimitFactor = 1.8f;
        public float hoseDryScale = .85f, hoseWetScale = 1.15f;
        public float hoseWaveSpeed = 8f, hoseSwellTime = .18f, hoseVibration = .025f;
        [Header("Lenses (vertical FOV, degrees)")]
        [Range(40, 120)] public float worldFov = 80f;
        [Range(30, 100)] public float viewmodelFov = 60f;
        [Header("Camera")]
        [Min(.5f)] public float cameraHeight = 1.6f;
        [Range(-30, 45)] public float cameraPitch = 18f;
        [Header("Toy proportions")]
        [Min(.1f)] public float furnitureScale = 1.25f;
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
        [Header("Playable level (applied by Tools/FlameBlame/Rebuild Main Level)")]
        [Tooltip("Furniture size relative to the original greybox, on top of objectScaleMultiplier (the room is ~2x smaller).")]
        [Range(.3f, 1.2f)] public float furnitureFit = .6f;
        [Tooltip("Distances shrank by this factor vs the greybox: fire spread, NPC panic and char radii.")]
        [Range(.2f, 1f)] public float distanceScale = .5f;
        [Min(.5f)] public float moveSpeed = 2.2f;
        [Min(.1f)] public float lookSensitivity = 2f;
        [Range(.5f, 1f)] public float hoseReachMargin = .95f;
        [Min(4)] public int hoseSegments = 32;
        [Range(.1f, 1f)] public float jetRangeFactor = .4f;
        [Min(.02f)] public float jetRadius = .1f;
        [Min(.5f)] public float hoseGrabDistance = 1.6f;
        [Range(.2f, 1.5f)] public float puddleScale = .6f;
        [Range(.2f, 1.5f)] public float particleScale = .75f;
        [Min(.1f)] public float npcMaxSpeed = 1.4f;
        [Header("Fire chain (applied by Tools/FlameBlame/Build Fire Chain)")]
        [Tooltip("Spread radius per material so fire can hop curtain -> furniture -> shelves across the compact room.")]
        [Min(.1f)] public float spreadRadiusFabric = 2f, spreadRadiusPaper = 1.6f, spreadRadiusWood = 1.6f;
    }
}
