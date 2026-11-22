using UnityEngine;
namespace FlameBlame.Hose
{
    public sealed class HoseBendConstraint
    {
        public static float ChordFactor(float length, float radius, float factor)
        {
            float ratio = Mathf.Clamp01(length / Mathf.Max(8f * radius, .0001f));
            return Mathf.Clamp(Mathf.Max(factor, 2f * Mathf.Sqrt(1f-ratio*ratio)), 0f, 1.999f);
        }
        public void Solve(Vector3[] p, float chord, bool pinned)
        {
            for (int i=0; i<p.Length-2; i++)
            {
                Vector3 delta=p[i+2]-p[i]; float d=delta.magnitude;
                if(d>=chord) continue;
                Vector3 direction=d>.00001f?delta/d:Vector3.Cross(p[i+1]-p[i],Vector3.up).normalized;
                if(direction.sqrMagnitude<.001f) direction=Vector3.right;
                Vector3 correction=direction*((chord-d)*.7f);
                bool a=i==0, b=pinned && i+2==p.Length-1;
                if(a && b) continue;
                if(a) p[i+2]+=correction;
                else if(b) p[i]-=correction;
                else {p[i]-=correction*.5f; p[i+2]+=correction*.5f;}
            }
        }
    }
}
