using FlameBlame.Feedback;
using UnityEngine;

namespace FlameBlame.Npc
{
    public sealed class NpcExpression : MonoBehaviour
    {
        [SerializeField] NpcActor actor;
        [SerializeField] VisualTuning tuning;
        [SerializeField] Transform[] eyes, pupils, arms;
        [SerializeField] Transform mouth;
        [SerializeField] Renderer body;
        MaterialPropertyBlock block;
        float wet;
        bool toonMaterial;
        void Awake() { block=new MaterialPropertyBlock(); toonMaterial=body!=null && body.sharedMaterial.HasProperty("_Emissive_Color"); }
        void LateUpdate()
        {
            if(tuning==null) return;
            var state=actor.State;
            bool shocked=state==NpcState.Shocked, washed=state==NpcState.Washed;
            bool alarm=shocked || state==NpcState.Panic;
            wet=washed ? 1 : Mathf.MoveTowards(wet,0,Time.deltaTime/Mathf.Max(.01f,tuning.wetSeconds));
            float blend=1-Mathf.Exp(-tuning.expressionSpeed*Time.deltaTime);
            for(int i=0;i<eyes.Length;i++)
            {
                eyes[i].localScale=Vector3.Lerp(eyes[i].localScale,Vector3.one*(alarm?tuning.eyeAlarmScale*.23f:.23f),blend);
                pupils[i].localScale=Vector3.one*(shocked?.035f:alarm?.065f:.09f);
            }
            mouth.localScale=Vector3.Lerp(mouth.localScale,new Vector3(alarm?.18f:.12f,alarm?.25f:washed?.035f:.06f,.04f),blend);
            for(int i=0;i<arms.Length;i++) arms[i].localRotation=Quaternion.Slerp(arms[i].localRotation,Quaternion.Euler(0,0,(i==0?1:-1)*(shocked?100:alarm?65:washed?35:8)),blend);
            body.GetPropertyBlock(block); block.SetFloat("_Smoothness",Mathf.Lerp(.15f,.9f,wet));
            Color emission=shocked ? new Color(.6f,.4f,.01f) : Color.black;
            block.SetColor("_EmissionColor",emission);
            if(toonMaterial) block.SetColor("_Emissive_Color",emission);
            body.SetPropertyBlock(block);
        }
    }
}
