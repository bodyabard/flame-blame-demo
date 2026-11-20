using FlameBlame.Core;
using FlameBlame.Npc;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// Pushes a ScaleProfile onto cameras, hands, NPC shapes and props. The demo level and the calibration scene share it.
    public sealed class ScaleApplier : MonoBehaviour
    {
        [SerializeField] ScaleProfile profile;
        [SerializeField] Camera worldCamera, viewmodelCamera;
        [SerializeField] Transform handsRig;
        [SerializeField] ToyNpcShape[] npcs;
        [SerializeField] ScaledProp[] props;
        [SerializeField] Vector3 handsBase;
        public ScaleProfile Profile { get => profile; set => profile = value; }
        public Camera WorldCamera => worldCamera;
        public Camera ViewmodelCamera => viewmodelCamera;

        public void Bind(ScaleProfile source, Camera world, Camera viewmodel, Transform hands, ToyNpcShape[] npcShapes, ScaledProp[] scaledProps)
        {
            profile = source; worldCamera = world; viewmodelCamera = viewmodel; handsRig = hands; npcs = npcShapes; props = scaledProps;
            handsBase = hands != null ? hands.localPosition : Vector3.zero;
        }

        void Start() { Apply(); }

        public void Apply()
        {
            if (profile == null) return;
            if (worldCamera != null)
            {
                worldCamera.fieldOfView = profile.worldFov;
                var t = worldCamera.transform; var p = t.localPosition; p.y = profile.cameraHeight; t.localPosition = p;
                t.localRotation = Quaternion.Euler(profile.cameraPitch, 0, 0);
            }
            if (viewmodelCamera != null) viewmodelCamera.fieldOfView = profile.viewmodelFov;
            if (handsRig != null) { handsRig.localScale = Vector3.one * profile.handsScale; handsRig.localPosition = handsBase + profile.handsOffset; }
            for (int i = 0; i < npcs.Length; i++) npcs[i].Apply(profile.npcHeight, profile.npcHeadRatio);
            for (int i = 0; i < props.Length; i++) props[i].Apply(profile.objectScaleMultiplier);
        }
    }
}
