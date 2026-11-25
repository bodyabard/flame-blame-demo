using FlameBlame.Core;
using FlameBlame.Water;
using UnityEngine;

namespace FlameBlame.Fire
{
    /// Scene-side wrapper of a FireNode: tints renderers (MaterialPropertyBlock) and receives water.
    public class Flammable : MonoBehaviour, IWaterReceiver
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] FireMaterialProfile profile;
        [SerializeField] bool igniteOnStart;
        [SerializeField] float steamInterval = 0.3f;

        FireSystem fire;
        IGameEvents events;
        FireNode node;
        Renderer[] renderers;
        Color[] baseColors;
        Color[] firstShades, secondShades;
        bool[] toonMaterials;
        MaterialPropertyBlock block;
        float nextSteamTime;
        Vector3 localNodePosition;

        public FireNode Node => node;

        public void Initialize(FireSystem fireSystem, IGameEvents gameEvents)
        {
            fire = fireSystem;
            events = gameEvents;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            firstShades = new Color[renderers.Length];
            secondShades = new Color[renderers.Length];
            toonMaterials = new bool[renderers.Length];
            block = new MaterialPropertyBlock();

            Bounds b = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
                var mat = renderers[i].sharedMaterial;
                baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
                toonMaterials[i] = mat != null && mat.HasProperty("_1st_ShadeColor");
                if (toonMaterials[i])
                {
                    firstShades[i] = mat.GetColor("_1st_ShadeColor");
                    secondShades[i] = mat.GetColor("_2nd_ShadeColor");
                }
            }

            node = new FireNode(profile, b.center, this);
            localNodePosition = transform.InverseTransformPoint(b.center);
            node.VisualChanged += ApplyVisuals;
            var zone=GetComponentInParent<FireZone>();
            if(zone!=null) { node.Zone=zone.id; node.ZoneActive=zone.id!=FireZoneId.Attic; }
            fire.Register(node);
            if (igniteOnStart) fire.Ignite(node);
        }

        public void SyncPosition()
        {
            if (node != null) node.Position = transform.TransformPoint(localNodePosition);
        }

        void OnDestroy()
        {
            if (node == null) return;
            node.VisualChanged -= ApplyVisuals;
            fire.Unregister(node);
        }

        public void ReceiveWater(Vector3 point, Vector3 normal, float amount)
        {
            if (node == null) return;
            bool wasHot = node.Heat > 0f;
            fire.ApplyWater(node, amount);
            if (wasHot && Time.time >= nextSteamTime)
            {
                nextSteamTime = Time.time + steamInterval;
                events.RaiseSteamCreated(point);
            }
        }

        void ApplyVisuals(FireNode n)
        {
            float charred = n.Charred01;
            float glow = n.IsBurning ? 0.22f : n.Heat01 * 0.3f;   // a faint warm rim only: the dark body must stay readable under the flames
            Color emission = profile.glowColor * (glow * profile.glowIntensity);

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.Lerp(baseColors[i], profile.charColor, charred));
                block.SetColor(EmissionColorId, emission);
                if (toonMaterials[i])
                {
                    block.SetColor("_1st_ShadeColor", Color.Lerp(firstShades[i], profile.charColor, charred));
                    block.SetColor("_2nd_ShadeColor", Color.Lerp(secondShades[i], profile.charColor, charred));
                    block.SetColor("_Emissive_Color", emission);
                }
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
