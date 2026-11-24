using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEngine;
namespace FlameBlame.Ladder
{
    [DefaultExecutionOrder(-20)]
    public sealed class PlayerLadderInteraction : MonoBehaviour
    {
        public Transform view;
        public HoseController hose;
        public JetController jet;
        public PlayerMotor motor;
        public PlayerLocomotionStateMachine locomotion;
        public LadderStateMachine Carried {get;private set;}
        public bool HasFocus => focus!=null;
        LadderStateMachine focus;
        readonly Collider[] overlaps=new Collider[32];
        Collider playerCollider;
        Renderer[] previewRenderers;
        Vector3 candidate;
        Quaternion rotation;
        bool valid;
        public void Initialize() {playerCollider=GetComponent<Collider>();}
        public void Pickup(LadderStateMachine ladder)
        {
            if(Carried!=null || locomotion.State==PlayerLocomotionState.Climb) return;
            Carried=ladder; ladder.Carry();
            foreach(var c in ladder.Solids) Physics.IgnoreCollision(c,playerCollider,true);
            previewRenderers=ladder.ghost.GetComponentsInChildren<Renderer>(true);
            hose.SetHolstered(true); jet.StopFlow(); motor.MoveMultiplier=ladder.tuning.carrySpeed;
        }
        void Release(bool placed)
        {
            var ladder=Carried;
            ladder.ghost.gameObject.SetActive(false);
            // A thrown ladder can initially overlap the player; restore collisions after clearance.
            pendingCollision=ladder;
            if(placed) ladder.Place(candidate,rotation); else ladder.Throw(view.forward);
            Carried=null; motor.MoveMultiplier=1; hose.SetHolstered(false);
        }
        LadderStateMachine pendingCollision;
        void Update()
        {
            if(Time.timeScale==0 || !motor.enabled) return;
            if(pendingCollision!=null)
            {
                bool touching=false;
                foreach(var c in pendingCollision.Solids) if(c.bounds.Intersects(playerCollider.bounds)) touching=true;
                if(!touching) {foreach(var c in pendingCollision.Solids) Physics.IgnoreCollision(c,playerCollider,false); pendingCollision=null;}
            }
            focus=null;
            if(Carried==null)
            {
                if(Physics.Raycast(view.position,view.forward,out var hit,2,~(1<<gameObject.layer),QueryTriggerInteraction.Ignore)) focus=hit.collider.GetComponentInParent<LadderStateMachine>();
                if(Input.GetKeyDown(KeyCode.F) && focus!=null) focus.Interact(this);
                return;
            }
            var ladder=Carried;
            Vector3 heading=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
            Vector3 shoulder=view.position+view.right*.4f-view.up*.28f;
            Vector3 carryForward=heading*.5f;
            if(Physics.Raycast(shoulder,heading,out var obstruction,ladder.tuning.length*.5f+.6f,~((1<<gameObject.layer)|(1<<ladder.gameObject.layer)),QueryTriggerInteraction.Ignore)) carryForward=heading*Mathf.Min(.5f,obstruction.distance-ladder.tuning.length*.5f-.15f);
            ladder.transform.SetPositionAndRotation(shoulder+carryForward,Quaternion.LookRotation(Vector3.up,heading));
            valid=Placement(ladder,heading);
            ladder.ghost.gameObject.SetActive(true);
            ladder.ghost.SetPositionAndRotation(candidate,rotation);
            foreach(var r in previewRenderers) r.sharedMaterial=valid ? ladder.validPreview : ladder.invalidPreview;
            if(Input.GetMouseButtonDown(0)) Release(false);
            else if(Input.GetKeyDown(KeyCode.F) && valid) Release(true);
        }
        bool Placement(LadderStateMachine ladder,Vector3 heading)
        {
            var t=ladder.tuning;
            Vector3 wallNormal=-heading;
            Vector3 wallPoint=view.position+heading*2;
            bool wall=Physics.Raycast(view.position,view.forward,out var hit,t.placeRange,~((1<<gameObject.layer)|(1<<ladder.gameObject.layer)),QueryTriggerInteraction.Ignore);
            if(wall) {wallNormal=hit.normal; wallPoint=hit.point;}
            Vector3 horizontal=Vector3.ProjectOnPlane(wallNormal,Vector3.up).normalized;
            if(horizontal.sqrMagnitude<.1f) horizontal=-heading;
            Vector3 groundOrigin=wallPoint+horizontal*(LadderRules.BaseOffset(t.length,t.angle)+.12f);
            bool ground=Physics.Raycast(groundOrigin+Vector3.up,Vector3.down,out var floor,7,~((1<<gameObject.layer)|(1<<ladder.gameObject.layer)),QueryTriggerInteraction.Ignore);
            Vector3 bottom=ground ? floor.point+Vector3.up*.12f : groundOrigin-Vector3.up*1.5f;
            Vector3 axis=Vector3.up*Mathf.Sin(t.angle*Mathf.Deg2Rad)-horizontal*Mathf.Cos(t.angle*Mathf.Deg2Rad);
            candidate=bottom+axis*t.length*.5f;
            rotation=Quaternion.LookRotation(Vector3.Cross(Vector3.Cross(axis,-horizontal),axis),axis);
            int count=Physics.OverlapCapsuleNonAlloc(bottom+axis*.2f,bottom+axis*(t.length-.2f),.13f,overlaps,~0,QueryTriggerInteraction.Ignore);
            bool clear=count<overlaps.Length;
            for(int i=0;i<count;i++) if(overlaps[i]!=playerCollider && overlaps[i].GetComponentInParent<LadderStateMachine>()!=ladder) clear=false;
            return wall && ground && floor.normal.y>.9f && LadderRules.ValidAngle(t.angle) && LadderRules.ValidWall(wallNormal,t.wallTolerance) && clear;
        }
    }
}

