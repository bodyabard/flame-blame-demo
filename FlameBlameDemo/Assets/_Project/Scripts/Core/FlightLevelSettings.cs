using UnityEngine;
namespace FlameBlame.Core
{
    [CreateAssetMenu(menuName="FlameBlame/Flight Level Settings")]
    public sealed class FlightLevelSettings : ScriptableObject
    {
        public float floorHeight=3.2f, windowWidth=2.2f, windowBottom=.65f, windowHeight=2.15f;
        public Vector3 spawn=new Vector3(-7,0,-1.2f), hydrant=new Vector3(-6,0,1.8f);
        public float yardSize=40, hoseSlack=1.2f;
    }
}
