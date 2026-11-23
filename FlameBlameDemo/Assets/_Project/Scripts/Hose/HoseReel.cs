using UnityEngine;
namespace FlameBlame.Hose
{
    public interface IHoseVisibility { bool Clear(Vector3 from,Vector3 to,float radius); }
    // Shortcuts only through visible space. Query exhaustion conservatively retains deployed length.
    public sealed class HoseReel
    {
        readonly IHoseVisibility visibility;
        public HoseReel(IHoseVisibility visibility) {this.visibility=visibility;}
        public float RouteLength(Vector3[] points,int count,float radius,int budget)
        {
            float length=0;
            int from=0;
            while(from<count-1)
            {
                int to=count-1;
                for(;to>from;to--)
                {
                    if(--budget<0) return float.PositiveInfinity;
                    if(visibility.Clear(points[from],points[to],radius)) break;
                }
                if(to==from) return float.PositiveInfinity;
                length+=Vector3.Distance(points[from],points[to]); from=to;
            }
            return length;
        }
        public static int RequiredPoints(float route,float slack,float minimum,float segment,int capacity) =>
            float.IsInfinity(route) ? capacity : Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(minimum,route+slack)/segment)+1,3,capacity);
    }
}
