using UnityEngine;
using UnityEngine.Rendering;
namespace FlameBlame.Hose
{
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer),typeof(HoseSwellController))]
    public sealed class HoseTubeRenderer : MonoBehaviour, IHoseRenderer
    {
        [SerializeField] Transform startCoupling, endCoupling;
        HoseController hose;
        HoseSwellController swell;
        Mesh mesh;
        Vector3[] centers, vertices, normals;
        const int Sides=8, Stride=9, Subdivisions=3;
        public void Initialize(HoseController controller)
        {
            hose=controller;
            if(hose.Scale==null) {enabled=false; return;}
            int rings=Mathf.Min(109,(hose.Segments-1)*Subdivisions+1);
            centers=new Vector3[rings]; vertices=new Vector3[rings*Stride]; normals=new Vector3[vertices.Length];
            var uv=new Vector2[vertices.Length]; var indices=new int[(rings-1)*Sides*6];
            for(int i=0;i<rings;i++) for(int j=0;j<Stride;j++) uv[i*Stride+j]=new Vector2((float)j/Sides,(float)i/(rings-1));
            int k=0;
            for(int i=0;i<rings-1;i++) for(int j=0;j<Sides;j++)
            {
                int a=i*Stride+j,b=a+Stride;
                indices[k++]=a; indices[k++]=a+1; indices[k++]=b;
                indices[k++]=a+1; indices[k++]=b+1; indices[k++]=b;
            }
            if(mesh!=null) Destroy(mesh);
            mesh=new Mesh {name="Dynamic hose tube"}; mesh.MarkDynamic();
            mesh.vertices=vertices; mesh.uv=uv; mesh.triangles=indices;
            mesh.bounds=new Bounds(Vector3.zero,Vector3.one*1000f);
            GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=GetComponent<MeshRenderer>(); renderer.shadowCastingMode=ShadowCastingMode.On; renderer.receiveShadows=true;
            swell=GetComponent<HoseSwellController>(); swell.Initialize(hose,hose.Scale,rings);
        }
        Vector3 Point(int index)
        {
            index=Mathf.Clamp(index,0,hose.Segments-1);
            return index==hose.Segments-1 && hose.Held? hose.NozzlePosition:hose.Points[index];
        }
        void LateUpdate()
        {
            if(mesh==null || hose==null) return;
            int count=centers.Length;
            for(int i=0;i<count;i++)
            {
                float u=(float)i/(count-1)*(hose.Segments-1); int s=Mathf.Min((int)u,hose.Segments-2); float t=u-s;
                Vector3 a=Point(s-1),b=Point(s),c=Point(s+1),d=Point(s+2);
                centers[i]=.5f*((2f*b)+(-a+c)*t+(2f*a-5f*b+4f*c-d)*t*t+(-a+3f*b-3f*c+d)*t*t*t);
            }
            swell.Tick(hose.WaterFlowing,Time.deltaTime);
            Vector3 tangent=(centers[1]-centers[0]).normalized;
            if(tangent.sqrMagnitude<.001f) tangent=Vector3.forward;
            Vector3 normal=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.right).normalized;
            float distance=0f;
            for(int i=0;i<count;i++)
            {
                Vector3 next=(centers[Mathf.Min(i+1,count-1)]-centers[Mathf.Max(0,i-1)]).normalized;
                if(next.sqrMagnitude<.001f) next=tangent;
                normal=Quaternion.FromToRotation(tangent,next)*normal;
                normal=Vector3.ProjectOnPlane(normal,next).normalized;
                tangent=next; Vector3 binormal=Vector3.Cross(tangent,normal);
                if(i>0) distance+=Vector3.Distance(centers[i-1],centers[i]);
                float radius=swell.Radius(i,distance);
                for(int j=0;j<Stride;j++)
                {
                    float angle=j*Mathf.PI*2f/Sides; Vector3 radial=normal*Mathf.Cos(angle)+binormal*Mathf.Sin(angle);
                    int v=i*Stride+j; vertices[v]=transform.InverseTransformPoint(centers[i]+radial*radius);
                    normals[v]=transform.InverseTransformDirection(radial);
                }
            }
            mesh.SetVertices(vertices,0,vertices.Length,MeshUpdateFlags.DontRecalculateBounds);
            mesh.SetNormals(normals,0,normals.Length,MeshUpdateFlags.DontRecalculateBounds);
            if(startCoupling!=null) startCoupling.SetPositionAndRotation(centers[0],Quaternion.LookRotation(centers[1]-centers[0]));
            if(endCoupling!=null) endCoupling.SetPositionAndRotation(centers[count-1],Quaternion.LookRotation(centers[count-1]-centers[count-2]));
        }
        void SetVisible(bool visible)
        {
            GetComponent<MeshRenderer>().enabled=visible;
            if(startCoupling!=null) startCoupling.gameObject.SetActive(visible);
            if(endCoupling!=null) endCoupling.gameObject.SetActive(visible);
        }
        void OnEnable() {SetVisible(true);}
        void OnDisable() {SetVisible(false);}
        void OnDestroy() {if(mesh!=null) Destroy(mesh);}
    }
}
