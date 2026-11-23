using System;
using FlameBlame.Ladder;
using UnityEngine;
namespace FlameBlame.Player
{
    public enum PlayerLocomotionState { Walk, Climb, Jet }
    public sealed class PlayerLocomotionStateMachine : MonoBehaviour
    {
        public PlayerLocomotionState State {get;private set;}
        public PlayerLadderInteraction interaction;
        public Transform view;
        public FlameBlame.Feedback.CameraFeedback feedback;
        public event Action PlayerFellOffLadder;
        LadderStateMachine[] ladders;
        LadderStateMachine current;
        LadderStateMachine ignoredLadder;
        CharacterController controller;
        PlayerMotor motor;
        float cooldown, progress, stepDistance, transfer;
        Vector3 transferStart, transferEnd;
        bool transferring, entering;
        float climbInputSign=1;
        Vector3 Outside => Vector3.ProjectOnPlane(current.Outward,Vector3.up).normalized;
        public void Initialize(LadderStateMachine[] sceneLadders)
        {ladders=sceneLadders; controller=GetComponent<CharacterController>(); motor=GetComponent<PlayerMotor>();}
        public void SetFlying(bool flying) {if(State!=PlayerLocomotionState.Climb) State=flying ? PlayerLocomotionState.Jet : PlayerLocomotionState.Walk;}
        public bool Tick(IPlayerInput input,float dt)
        {
            feedback.Climbing=State==PlayerLocomotionState.Climb;
            cooldown=Mathf.Max(0,cooldown-dt);
            if(State!=PlayerLocomotionState.Climb && cooldown<=0 && ignoredLadder!=null)
            {
                bool overlapping=false;
                foreach(var solid in ignoredLadder.Solids)
                    if(!solid.isTrigger && solid.bounds.Intersects(controller.bounds)) overlapping=true;
                if(!overlapping) RestoreLadderCollision();
            }
            if(State==PlayerLocomotionState.Climb && interaction.hose.WaterFlowing && interaction.hose.Modes.Current.UsesThrust)
            {
                Detach(Vector3.up*1.5f);
                return false;
            }
            if(transferring)
            {
                if(current.State!=LadderState.Placed) {Detach(Vector3.zero); return true;}
                transfer=Mathf.Min(1,transfer+dt/current.tuning.mantleDuration);
                // Lift above the sill before crossing it, in either direction.
                float clearance=Mathf.Max(transferStart.y,transferEnd.y)+.1f;
                Vector3 raised=new Vector3(transferStart.x,clearance,transferStart.z);
                Vector3 crossed=new Vector3(transferEnd.x,clearance,transferEnd.z);
                Vector3 target=transfer<.3f ? Vector3.Lerp(transferStart,raised,transfer/.3f) : transfer<.8f ? Vector3.Lerp(raised,crossed,(transfer-.3f)/.5f) : Vector3.Lerp(crossed,transferEnd,(transfer-.8f)/.2f);
                controller.Move(target-transform.position);
                if(transfer>=1) {transferring=false; if(!entering || Vector3.Distance(transform.position,transferEnd)>.25f) Detach(Vector3.zero);}
                return true;
            }
            if(State!=PlayerLocomotionState.Climb)
            {
                if(cooldown>0 || interaction.Carried!=null || input.Move.sqrMagnitude<.01f || interaction.hose.WaterFlowing && interaction.hose.Modes.Current.UsesThrust) return false;
                foreach(var ladder in ladders)
                {
                    if(ladder.State!=LadderState.Placed) continue;
                    Vector3 outward=Vector3.ProjectOnPlane(ladder.Outward,Vector3.up).normalized;
                    Vector3 offset=transform.position-ladder.Bottom;
                    float along=Vector3.Dot(offset,ladder.transform.up);
                    Vector3 nearest=ladder.Bottom+ladder.transform.up*Mathf.Clamp(along,0,ladder.tuning.length);
                    Vector3 movement=transform.TransformDirection(new Vector3(input.Move.x,0,input.Move.y));
                    Vector3 toward=Vector3.ProjectOnPlane(nearest-transform.position,Vector3.up);
                    bool upper=transform.position.y>ladder.Top.y-1.4f && Vector3.Dot(transform.position-ladder.Top,outward)<0;
                    float distance=upper ? Vector3.ProjectOnPlane(transform.position-ladder.Top,Vector3.up).magnitude : Vector3.Distance(transform.position,nearest);
                    if(distance>(upper ? 1.6f : .9f) || Vector3.Angle(movement,toward)>ladder.tuning.attachAngle) continue;
                    current=ladder; progress=Mathf.Clamp(along,0,ladder.tuning.length-.5f); State=PlayerLocomotionState.Climb;
                    climbInputSign=upper ? -1 : 1;
                    RestoreLadderCollision(); ignoredLadder=current;
                    foreach(var solid in current.Solids) Physics.IgnoreCollision(controller,solid,true);
                    current.LadderKnockedDown+=Knocked; current.Sound(current.tuning.step);
                    if(upper) {progress=current.tuning.length-.5f; BeginTransfer(current.Bottom+current.transform.up*progress+Outside*.55f,true);}
                    break;
                }
                if(State!=PlayerLocomotionState.Climb) return false;
                if(transferring) return true;
            }
            if(input.JumpPressed || !LadderRules.FacingLadder(view.position,view.forward,current.Bottom,
                current.Top+current.transform.up*controller.height,current.tuning.walkOffAngle))
            {
                Detach(Outside*current.tuning.jumpBack+Vector3.up*2);
                return true;
            }
            climbInputSign=LadderRules.ViewClimbSign(view.forward,climbInputSign,current.tuning.lookDirectionDeadZone);
            float travel=input.Move.y*climbInputSign*current.tuning.climbSpeed*dt;
            progress=Mathf.Clamp(progress+travel,0,current.tuning.length);
            controller.Move(current.Bottom+current.transform.up*progress+Outside*.55f-transform.position);
            stepDistance+=Mathf.Abs(travel);
            if(stepDistance>.3f) {stepDistance=0; current.Sound(current.tuning.step,.5f); feedback.JetKick(.08f);}
            if(progress<=.03f && travel<0) Detach(Outside);
            else if(progress>=current.tuning.length-.5f && travel>0)
            {
                Vector3 exit=current.Top-Outside*.8f;
                // Find the sill/ledge, rather than deriving foot height from ladder length.
                Vector3 probe=current.Top-Outside*.2f+Vector3.up*.2f;
                if(Physics.Raycast(probe,Vector3.down,out var support,1.5f,~((1<<gameObject.layer)|(1<<current.gameObject.layer)),QueryTriggerInteraction.Ignore)) exit.y=support.point.y+.04f;
                else exit.y=current.Top.y-.4f;
                BeginTransfer(exit,false);
            }
            return true;
        }
        void BeginTransfer(Vector3 end,bool ontoLadder)
        {transferStart=transform.position; transferEnd=end; transfer=0; entering=ontoLadder; transferring=true;}
        void Knocked()
        {var impulse=Outside*2+Vector3.up; feedback.LadderFall(); Detach(impulse); PlayerFellOffLadder?.Invoke();}
        void Detach(Vector3 impulse)
        {transferring=false; cooldown=current.tuning.reattachDelay; current.LadderKnockedDown-=Knocked; State=PlayerLocomotionState.Walk; motor.LeaveLadder(impulse);}
        void RestoreLadderCollision()
        {if(ignoredLadder!=null) {foreach(var solid in ignoredLadder.Solids) if(solid!=null && controller!=null) Physics.IgnoreCollision(controller,solid,false); ignoredLadder=null;}}
        void OnDestroy() {RestoreLadderCollision(); if(current!=null) current.LadderKnockedDown-=Knocked;}
    }
}

