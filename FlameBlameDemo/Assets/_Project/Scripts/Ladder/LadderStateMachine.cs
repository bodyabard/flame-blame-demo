using System;
using FlameBlame.Water;
using FlameBlame.Core;
using UnityEngine;
namespace FlameBlame.Ladder
{
    [RequireComponent(typeof(Rigidbody),typeof(AudioSource))]
    public sealed class LadderStateMachine : MonoBehaviour,IWaterReceiver,IInteractable
    {
        public LadderTuning tuning;
        public Transform ghost;
        public Material validPreview, invalidPreview;
        public LadderState State {get;private set;}=LadderState.Resting;
        public event Action LadderPlaced, LadderKnockedDown;
        public Rigidbody Body {get;private set;}
        public Vector3 Bottom=>transform.position-transform.up*(tuning.length*.5f);
        public Vector3 Top=>transform.position+transform.up*(tuning.length*.5f);
        public Vector3 Outward=>-transform.forward;
        public Collider[] Solids {get;private set;}
        IGameEvents events;
        AudioSource audioSource;
        float knock, lastWater=-10, settled;
        Vector3 snapStart, snapTarget;
        Quaternion snapRotation, snapFrom;
        float snapTime;
        public void Initialize(IGameEvents hub) {events=hub;}
        void Awake()
        {
            Body=GetComponent<Rigidbody>(); Solids=GetComponentsInChildren<Collider>(); audioSource=GetComponent<AudioSource>();
            if(ghost!=null) ghost.gameObject.SetActive(false);
            Body.Sleep();
        }
        public void Sound(AudioClip clip,float volume=1) {if(clip!=null) audioSource.PlayOneShot(clip,volume);}
        public void Interact(PlayerLadderInteraction player) {if(State!=LadderState.Carried) player.Pickup(this);}
        public void Carry() {SetState(LadderState.Carried); Sound(tuning.pickup);}
        void SetState(LadderState state)
        {
            if(!Body.isKinematic) {Body.linearVelocity=Vector3.zero; Body.angularVelocity=Vector3.zero;}
            State=state; Body.isKinematic=state==LadderState.Carried || state==LadderState.Placed;
            knock=0; settled=0;
        }
        public void Place(Vector3 position,Quaternion rotation)
        {
            snapStart=transform.position; snapFrom=transform.rotation; snapTarget=position; snapRotation=rotation; snapTime=0;
            SetState(LadderState.Placed); Sound(tuning.place); LadderPlaced?.Invoke();
        }
        public void Throw(Vector3 direction)
        {
            SetState(LadderState.Falling);
            Body.AddForce(direction*tuning.throwImpulse+Vector3.up*tuning.throwUp,ForceMode.VelocityChange);
            Body.AddTorque(transform.right*tuning.torque,ForceMode.VelocityChange);
        }
        void Update()
        {
            if(State==LadderState.Placed)
            {
                snapTime+=Time.deltaTime;
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(snapTime/tuning.snapDuration));
                transform.SetPositionAndRotation(Vector3.Lerp(snapStart,snapTarget,t),Quaternion.Slerp(snapFrom,snapRotation,t));
                if(Time.time-lastWater>.05f) knock=LadderRules.Decay(knock,tuning.knockDecay,Time.deltaTime);
            }
        }
        void FixedUpdate()
        {
            if(State!=LadderState.Falling && State!=LadderState.Resting) return;
            if(Time.time-lastWater>.2f && Body.linearVelocity.sqrMagnitude<tuning.settleSpeed*tuning.settleSpeed && Body.angularVelocity.sqrMagnitude<.02f)
            {settled+=Time.fixedDeltaTime; if(settled>=tuning.settleDuration) {State=LadderState.Resting; Body.Sleep();}}
            else {settled=0; State=LadderState.Falling;}
        }
        public void ReceiveWater(Vector3 point,Vector3 normal,float amount)
        {
            if(State==LadderState.Carried) return;
            // Water amount already contains source pressure and frame duration.
            lastWater=Time.time;
            if(State==LadderState.Placed)
            {
                knock=LadderRules.AddKnock(knock,amount,1);
                if(knock<tuning.knockThreshold) return;
                SetState(LadderState.Falling);
                // Break wall contact rather than driving both ends into their supports.
                Vector3 away=Vector3.ProjectOnPlane(Outward,Vector3.up).normalized;
                Body.AddForceAtPosition((-normal+away*1.5f)*tuning.fallImpulse,point,ForceMode.VelocityChange);
                Body.AddTorque(-transform.right*tuning.torque,ForceMode.VelocityChange);
                LadderKnockedDown?.Invoke();
            }
            else
            {
                settled=0; State=LadderState.Falling; Body.WakeUp();
                Body.AddForceAtPosition((-normal+Vector3.ProjectOnPlane(Outward,Vector3.up)*.35f)*tuning.waterForce*amount,point,ForceMode.Impulse);
                Body.AddTorque(-transform.right*tuning.torque*amount,ForceMode.VelocityChange);
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if(State==LadderState.Carried || State==LadderState.Placed) return;
            float speed=collision.relativeVelocity.magnitude;
            if(speed>1) Sound(tuning.impact,Mathf.Clamp01(speed/5));
            // Existing property observer owns valuation and damage accounting.
            if(speed>2 && collision.collider.TryGetComponent<FlameBlame.Fire.PropertyDamage>(out var property)) property.Hit(speed);
        }
    }
}
