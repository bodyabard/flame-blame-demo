using UnityEngine;
namespace FlameBlame.Ladder
{
    [CreateAssetMenu(menuName="FlameBlame/Ladder Tuning")]
    public sealed class LadderTuning : ScriptableObject
    {
        public float length=4.5f, angle=73, wallTolerance=15, placeRange=2.5f, pickupRange=2;
        public float climbSpeed=1.8f, reattachDelay=.4f, attachAngle=50, carrySpeed=.8f;
        public float walkOffAngle=70;
        public float lookDirectionDeadZone=.15f;
        public float knockThreshold=.4f, knockDecay=.18f, waterForce=25, fallImpulse=2, throwImpulse=5, throwUp=2, torque=2;
        public float settleSpeed=.12f, settleDuration=1, snapDuration=.25f, mantleDuration=.35f, jumpBack=3;
        public AudioClip pickup, place, step, impact;
    }
    public static class LadderRules
    {
        public static float ViewClimbSign(Vector3 forward,float previous,float deadZone)
        {
            float vertical=Vector3.Dot(forward,Vector3.up);
            return Mathf.Abs(vertical)<=deadZone ? previous : vertical>0 ? 1 : -1;
        }
        public static bool FacingLadder(Vector3 eye,Vector3 forward,Vector3 bottom,Vector3 top,float angle)
        {
            float threshold=Mathf.Cos(angle*Mathf.Deg2Rad);
            forward.Normalize();
            for(int i=0;i<=4;i++)
                if(Vector3.Dot(forward,(Vector3.Lerp(bottom,top,i*.25f)-eye).normalized)>=threshold) return true;
            return false;
        }
        public static float BaseOffset(float length,float angle)=>length*Mathf.Cos(angle*Mathf.Deg2Rad);
        public static float Height(float length,float angle)=>length*Mathf.Sin(angle*Mathf.Deg2Rad);
        public static bool ValidWall(Vector3 normal,float tolerance)=>Mathf.Abs(normal.y)<=Mathf.Sin(tolerance*Mathf.Deg2Rad);
        public static bool ValidAngle(float angle)=>angle>=70 && angle<=75;
        public static float AddKnock(float value,float amount,float pressure)=>value+Mathf.Max(0,amount)*Mathf.Clamp01(pressure);
        public static float Decay(float value,float rate,float dt)=>Mathf.Max(0,value-rate*dt);
    }
    public interface IInteractable { void Interact(PlayerLadderInteraction player); }
    public enum LadderState { Resting, Carried, Placed, Falling }
}
