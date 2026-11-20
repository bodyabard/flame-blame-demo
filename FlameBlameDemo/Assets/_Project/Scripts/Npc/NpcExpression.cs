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
        Vector3[] eyeBase, pupilBase;
        Vector3 mouthBase;
        float wet;
        bool toonMaterial;
        // Rest sizes are captured at Awake, so the expression follows whatever proportions ScaleProfile produced.
        void Awake()
        {
            block=new MaterialPropertyBlock(); toonMaterial=body!=null && body.sharedMaterial.HasProperty("_Emissive_Color");
            eyeBase=new Vector3[eyes.Length]; pupilBase=new Vector3[pupils.Length];
            for(int i=0;i<eyes.Length;i++) eyeBase[i]=eyes[i].localScale;
            for(int i=0;i<pupils.Length;i++) pupilBase[i]=pupils[i].localScale;
            mouthBase=mouth.localScale;
        }
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
                eyes[i].localScale=Vector3.Lerp(eyes[i].localScale,eyeBase[i]*(alarm?tuning.eyeAlarmScale:1f),blend);
                pupils[i].localScale=pupilBase[i]*(shocked?.39f:alarm?.72f:1f);
            }
            mouth.localScale=Vector3.Lerp(mouth.localScale,Vector3.Scale(mouthBase,new Vector3(alarm?1.5f:1f,alarm?4.2f:washed?.58f:1f,1f)),blend);
            for(int i=0;i<arms.Length;i++) arms[i].localRotation=Quaternion.Slerp(arms[i].localRotation,Quaternion.Euler(0,0,(i==0?1:-1)*(shocked?100:alarm?65:washed?35:8)),blend);
            body.GetPropertyBlock(block); block.SetFloat("_Smoothness",Mathf.Lerp(.15f,.9f,wet));
            Color emission=shocked ? new Color(.6f,.4f,.01f) : Color.black;
            block.SetColor("_EmissionColor",emission);
            if(toonMaterial) block.SetColor("_Emissive_Color",emission);
            body.SetPropertyBlock(block);
        }
    }
}
