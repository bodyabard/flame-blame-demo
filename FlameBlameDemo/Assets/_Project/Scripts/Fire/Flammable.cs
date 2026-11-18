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
            block = new MaterialPropertyBlock();

            Bounds b = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
                var mat = renderers[i].sharedMaterial;
                baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            }

            node = new FireNode(profile, b.center, this);
            localNodePosition = transform.InverseTransformPoint(b.center);
            node.VisualChanged += ApplyVisuals;
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
            float glow = n.IsBurning ? 1f : n.Heat01 * 0.6f;
            Color emission = profile.glowColor * (glow * profile.glowIntensity);

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.Lerp(baseColors[i], profile.charColor, charred));
                block.SetColor(EmissionColorId, emission);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
