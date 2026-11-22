using UnityEngine;
namespace FlameBlame.Core
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WindowEntryGate : MonoBehaviour
    {
        public bool Entered { get; private set; }
        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<FlameBlame.Player.PlayerMotor>()!=null) Entered=true;
        }
    }
}
