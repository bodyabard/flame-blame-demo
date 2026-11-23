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
        CharacterController controller;
        PlayerMotor motor;
        float cooldown, progress, stepDistance;
        Vector3 mantleStart,mantleEnd;
        float mantle;
        bool mantling;
        public void Initialize(LadderStateMachine[] sceneLadders)
        {ladders=sceneLadders; controller=GetComponent<CharacterController>(); motor=GetComponent<PlayerMotor>();}
        public void SetFlying(bool flying) {if(State!=PlayerLocomotionState.Climb) State=flying ? PlayerLocomotionState.Jet : PlayerLocomotionState.Walk;}
        public bool Tick(FlameBlame.Player.IPlayerInput input,float dt)
        {
            feedback.Climbing=State==PlayerLocomotionState.Climb;
            cooldown=Mathf.Max(0,cooldown-dt);
            if(mantling)
            {
                mantle+=dt/current.tuning.mantleDuration;
                controller.Move(Vector3.Lerp(mantleStart,mantleEnd,Mathf.SmoothStep(0,1,mantle))-transform.position);
                if(mantle>=1) {mantling=false; Detach(Vector3.zero);}
                return true;
            }
            if(State!=PlayerLocomotionState.Climb)
            {
                if(cooldown>0 || interaction.Carried!=null || input.Move.y<=0 || interaction.hose.WaterFlowing && interaction.hose.Modes.Current.UsesThrust) return false;
                foreach(var ladder in ladders)
                {
                    if(ladder.State!=LadderState.Placed) continue;
                    Vector3 offset=transform.position-ladder.Bottom;
                    float along=Vector3.Dot(offset,ladder.transform.up);
                    Vector3 nearest=ladder.Bottom+ladder.transform.up*Mathf.Clamp(along,0,ladder.tuning.length);
                    if(Vector3.Distance(transform.position,nearest)>.85f || Vector3.Angle(Vector3.ProjectOnPlane(view.forward,Vector3.up),-ladder.Outward)>ladder.tuning.attachAngle) continue;
                    current=ladder; progress=Mathf.Clamp(along,0,ladder.tuning.length); State=PlayerLocomotionState.Climb;
                    current.LadderKnockedDown+=Knocked; current.Sound(current.tuning.step); break;
                }
                if(State!=PlayerLocomotionState.Climb) return false;
            }
            if(input.JumpPressed) {Detach(current.Outward*current.tuning.jumpBack+Vector3.up*2); return true;}
            float travel=input.Move.y*current.tuning.climbSpeed*dt;
            progress=Mathf.Clamp(progress+travel,0,current.tuning.length);
            controller.Move(current.Bottom+current.transform.up*progress+current.Outward*.5f-transform.position);
            stepDistance+=Mathf.Abs(travel);
            if(stepDistance>.3f) {stepDistance=0; current.Sound(current.tuning.step,.5f); feedback.JetKick(.08f);}
            if(progress<=.03f && input.Move.y<0) Detach(current.Outward);
            else if(progress>=current.tuning.length-.55f && input.Move.y>0)
            {mantling=true; mantle=0; mantleStart=transform.position; mantleEnd=current.Top-current.Outward*.8f-Vector3.up*.45f;}
            return true;
        }
        void Knocked()
        {var impulse=current.Outward*2+Vector3.up; feedback.LadderFall(); Detach(impulse); PlayerFellOffLadder?.Invoke();}
        void Detach(Vector3 impulse)
        {cooldown=current.tuning.reattachDelay; current.LadderKnockedDown-=Knocked; State=PlayerLocomotionState.Walk; motor.LeaveLadder(impulse);}
        void OnDestroy() {if(current!=null) current.LadderKnockedDown-=Knocked;}
    }
}


