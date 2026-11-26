using UnityEngine;
namespace FlameBlame.Fire
{
    public enum FireZoneId { Floor1, Floor2, Attic, Ending }
    public sealed class FireZone : MonoBehaviour
    {
        public FireZoneId id;
        public Flammable[] members;
        public bool Cleared
        {
            get
            {
                if(members==null || members.Length==0) return false;
                for(int i=0;i<members.Length;i++)
                {
                    var node=members[i]!=null ? members[i].Node : null;
                    if(node==null) continue;
                    if(node.IsBurning || node.Profile.canBurn && node.FuelLeft>0 && node.Heat>=node.Profile.ignitionThreshold) return false;
                }
                return true;
            }
        }
        public void Activate(FireSystem system)
        {
            foreach(var member in members) if(member!=null && member.Node!=null)
            { member.Node.ZoneActive=true; system.Ignite(member.Node); }
        }
    }
}
