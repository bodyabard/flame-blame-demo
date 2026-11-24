using FlameBlame.Hose;
using UnityEngine;

namespace FlameBlame.Feedback
{
    public sealed class FirstPersonHands : MonoBehaviour
    {
        [SerializeField] VisualTuning tuning;
        [SerializeField] HoseController hose;
        [SerializeField] Transform arms;
        Vector3 origin;
        FlameBlame.Player.IPlayerInput input;
        public void Initialize(FlameBlame.Player.IPlayerInput source) {input=source;}
        void Awake() { origin=arms.localPosition; }
        void LateUpdate()
        {
            if(tuning==null || hose==null || input==null) return;
            arms.gameObject.SetActive(hose.Held);
            float motion=input.Move.magnitude;
            float recoil=hose.WaterFlowing ? hose.NozzlePressure : 0;
            Vector3 offset=new Vector3(-input.Look.x,-input.Look.y,-recoil*4)*tuning.handSway;
            offset.y+=Mathf.Sin(Time.time*tuning.bobSpeed)*motion*tuning.bobHeight;
            arms.localPosition=Vector3.Lerp(arms.localPosition,origin+offset,1-Mathf.Exp(-tuning.fovResponse*Time.deltaTime));
            float coupling=hose.ConnectionProgress;
            arms.localRotation=Quaternion.Euler(0,0,coupling>=0 ? Mathf.Sin(coupling*Mathf.PI*4)*12 : 0);
            if(hose.JetTuning!=null && hose.Modes.Current.UsesThrust) arms.localPosition += Vector3.down * (.15f*Time.deltaTime);
        }
    }
}
