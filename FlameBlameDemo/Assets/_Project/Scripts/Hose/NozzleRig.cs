using UnityEngine;

namespace FlameBlame.Hose
{
    public sealed class NozzleRig : MonoBehaviour
    {
        public Transform pose, inlet, muzzle;
        Renderer[] models;
        int viewLayer, hoseLayer;
        void Awake() { models=pose.GetComponentsInChildren<Renderer>(); viewLayer=LayerMask.NameToLayer("ViewModel"); hoseLayer=LayerMask.NameToLayer("Hose"); }
        public void Follow(Transform hand, Vector3 end, Vector3 direction, bool held, bool holstered=false)
        {
            pose.SetPositionAndRotation(held ? hand.position : end,
                held ? hand.rotation : Quaternion.LookRotation(direction));
            if(!held) pose.position += end-inlet.position;
            if(models!=null) foreach(var model in models) model.gameObject.layer=held && !holstered && viewLayer>=0 ? viewLayer : Mathf.Max(0,hoseLayer);
        }
        void OnDrawGizmosSelected()
        {
            if (muzzle == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(muzzle.position, muzzle.forward);
            Gizmos.DrawWireSphere(muzzle.position + muzzle.forward, .035f);
        }
    }
}
