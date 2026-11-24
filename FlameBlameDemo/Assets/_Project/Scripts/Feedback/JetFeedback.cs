using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEngine;
namespace FlameBlame.Feedback
{
    public sealed class JetFeedback : MonoBehaviour
    {
        [SerializeField] HoseController hose;
        [SerializeField] PlayerMotor motor;
        [SerializeField] CameraFeedback cameraFeedback;
        [SerializeField] AudioClip valveClick, landing;
        AudioSource sound;
        float pulse;
        FlameBlame.Core.IGameEvents events;
        Renderer[] puddles;
        [SerializeField] AudioClip windClip;
        AudioSource wind;
        public void Initialize(FlameBlame.Core.IGameEvents source, Transform effectsRoot)
        {
            events=source;
            var renderers=effectsRoot.GetComponentsInChildren<Renderer>(true);
            int count=0;
            foreach(var r in renderers) if(r.name.Contains("Puddle")) count++;
            puddles=new Renderer[count]; count=0;
            foreach(var r in renderers) if(r.name.Contains("Puddle")) puddles[count++]=r;
        }
        void Start()
        {
            sound=gameObject.AddComponent<AudioSource>(); sound.spatialBlend=0;
            wind=gameObject.AddComponent<AudioSource>(); wind.clip=windClip; wind.loop=true; wind.volume=0;
            if(windClip!=null) wind.Play();
            hose.Modes.Changed+=Changed; motor.Landed+=Landed;
        }
        void Changed() { pulse=1; cameraFeedback.JetKick(.25f); if(valveClick!=null) sound.PlayOneShot(valveClick); }
        void Landed(float speed)
        {
            cameraFeedback.LandingDip(Mathf.Clamp01(speed/hose.JetTuning.maxFallSpeed)*hose.JetTuning.landingDip);
            if(landing!=null) sound.PlayOneShot(landing,Mathf.Clamp01(speed/6));
            if(events!=null && speed>=6f) events.RaiseHardImpact(Mathf.Clamp01(speed/hose.JetTuning.maxFallSpeed));
            if(events==null || puddles==null) return;
            var feet=motor.transform.position;
            foreach(var p in puddles)
            {
                if(!p.gameObject.activeInHierarchy) continue;
                var bounds=p.bounds;
                if(feet.x>=bounds.min.x && feet.x<=bounds.max.x && feet.z>=bounds.min.z && feet.z<=bounds.max.z && Mathf.Abs(feet.y-bounds.center.y)<.3f)
                { events.RaiseWaterImpact(feet,Vector3.up); break; }
            }
        }
        void Update()
        {
            pulse=Mathf.MoveTowards(pulse,0,Time.deltaTime*4);
            if(wind!=null) wind.volume=Mathf.MoveTowards(wind.volume,motor.Speed/hose.JetTuning.maxAirSpeed*.15f,Time.deltaTime);
        }
        void OnDestroy()
        {
            if(hose!=null && hose.Modes!=null) hose.Modes.Changed-=Changed;
            if(motor!=null) motor.Landed-=Landed;
        }
    }
}
