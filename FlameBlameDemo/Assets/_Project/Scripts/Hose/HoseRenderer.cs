using UnityEngine;

namespace FlameBlame.Hose
{
    /// Draws the hose with a LineRenderer and places the nozzle model.
    [RequireComponent(typeof(LineRenderer))]
    public class HoseRenderer : MonoBehaviour, IHoseRenderer
    {
        [SerializeField] bool useTube = true;
        [SerializeField] Transform nozzleModel;   // optional
        HoseTubeRenderer tube;

        HoseController hose;
        LineRenderer line;
        Vector3[] buffer;

        public void Initialize(HoseController controller)
        {
            hose = controller;
            tube=GetComponent<HoseTubeRenderer>();
            if(tube!=null) tube.Initialize(controller);
            line = GetComponent<LineRenderer>();
            line.positionCount = hose.Segments;
            buffer = new Vector3[hose.Capacity];
        }

        public void HideForLoop()
        {
            line.enabled=false; if(tube!=null) tube.enabled=false;
            var mesh=GetComponent<MeshRenderer>(); if(mesh!=null) mesh.enabled=false; enabled=false;
        }

        void LateUpdate()
        {
            if (hose == null) return;
            line.enabled=hose.Connected && (!useTube || tube==null);
            if(tube!=null) tube.enabled=useTube && hose.Connected;
            var mesh=GetComponent<MeshRenderer>(); if(mesh!=null) mesh.enabled=hose.Connected;
            line.positionCount=hose.Segments;
            System.Array.Copy(hose.Points, buffer, hose.Segments);
            // The held end follows the camera every frame, not only on physics ticks.
            if (hose.Held) buffer[hose.Segments - 1] = hose.NozzlePosition;
            line.SetPositions(buffer);

            if (nozzleModel != null && hose.GetComponent<NozzleRig>() == null)
                nozzleModel.SetPositionAndRotation(hose.NozzlePosition, Quaternion.LookRotation(hose.NozzleDirection));
        }
    }
}
