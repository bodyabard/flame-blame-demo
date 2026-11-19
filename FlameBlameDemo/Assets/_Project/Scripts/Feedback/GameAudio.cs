using FlameBlame.Core;
using FlameBlame.Fire;
using FlameBlame.Hose;
using UnityEngine;

namespace FlameBlame.Feedback
{
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] VisualTuning tuning;
        [SerializeField] HoseController hose;
        [SerializeField] AudioSource water, fire, music;
        [SerializeField] AudioSource[] effects, voices;
        [SerializeField] AudioClip steam, spark, yelp, taunt;
        IGameEvents events;
        int fires, effectIndex, voiceIndex;
        float nextSteam, nextSpark, nextVoice;
        bool finished;
        public void Initialize(IGameEvents source)
        {
            events=source;
            events.FireIgnited+=Ignite; events.FireExtinguished+=Extinguish; events.FireBurnedOut+=Burnout;
            events.SteamCreated+=Steam; events.NpcShocked+=Shock; events.PlayerShocked+=PlayerShock;
            events.DamageReported+=Report;
            water.volume=fire.volume=0; water.Play(); fire.Play(); music.volume=tuning.musicVolume; music.Play();
        }
        void Ignite(FireNode node) {fires++;}
        void Extinguish(FireNode node) {fires=Mathf.Max(0,fires-1);Steam(node.Position);}
        void Burnout(FireNode node) {fires=Mathf.Max(0,fires-1);}
        void Steam(Vector3 point)
        {
            if(Time.time<nextSteam)return; nextSteam=Time.time+tuning.soundCooldown;
            Play(effects,ref effectIndex,steam,point,tuning.effectsVolume*.65f);
        }
        void Shock(Vector3 point)
        {
            if(Time.time<nextSpark)return;nextSpark=Time.time+tuning.soundCooldown;
            Play(effects,ref effectIndex,spark,point,tuning.effectsVolume); Voice(point);
        }
        void PlayerShock() {Shock(transform.position);}
        void Report(DamageReport report) {if(report.Amount>=100)Voice(transform.position);}
        void Voice(Vector3 point)
        {
            if(Time.time<nextVoice)return;nextVoice=Time.time+1.4f;
            Play(voices,ref voiceIndex,voiceIndex%2==0?yelp:taunt,point,tuning.voiceVolume);
        }
        static void Play(AudioSource[] pool,ref int index,AudioClip clip,Vector3 point,float volume)
        {
            var source=pool[index++%pool.Length]; source.Stop();source.transform.position=point;
            source.clip=clip;source.volume=volume;source.pitch=1;source.Play();
        }
        void Update()
        {
            if(events==null)return;
            float blend=1-Mathf.Exp(-tuning.audioResponse*Time.unscaledDeltaTime);
            float pressure=!finished && hose.WaterFlowing ? hose.Pressure : 0;
            water.volume=Mathf.Lerp(water.volume,pressure*tuning.effectsVolume,blend);
            water.pitch=Mathf.Lerp(.7f,1.25f,pressure);
            fire.volume=Mathf.Lerp(fire.volume,finished?0:Mathf.Clamp01(fires*.25f)*tuning.effectsVolume,blend);
            music.volume=tuning.musicVolume*(finished?.45f:1);
        }
        public void Finish() {finished=true;}
        void OnDestroy()
        {
            if(events==null)return;
            events.FireIgnited-=Ignite;events.FireExtinguished-=Extinguish;events.FireBurnedOut-=Burnout;
            events.SteamCreated-=Steam;events.NpcShocked-=Shock;events.PlayerShocked-=PlayerShock;events.DamageReported-=Report;
        }
    }
}
