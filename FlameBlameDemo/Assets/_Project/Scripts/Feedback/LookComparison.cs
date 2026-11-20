using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlameBlame.Feedback
{
    [System.Serializable]
    public sealed class LookLightingState
    {
        public Color keyColor, sky, equator, ground;
        public float keyIntensity, shadowStrength, bias, normalBias, ambientIntensity;
        public LightShadows shadows;
        public AmbientMode ambientMode;
        public bool fog;

        public void Capture(Light key)
        {
            keyColor = key.color; keyIntensity = key.intensity; shadows = key.shadows;
            shadowStrength = key.shadowStrength; bias = key.shadowBias; normalBias = key.shadowNormalBias;
            sky = RenderSettings.ambientSkyColor; equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor; ambientMode = RenderSettings.ambientMode;
            ambientIntensity = RenderSettings.ambientIntensity; fog = RenderSettings.fog;
        }

        public void Apply(Light key)
        {
            key.color = keyColor; key.intensity = keyIntensity; key.shadows = shadows;
            key.shadowStrength = shadowStrength; key.shadowBias = bias; key.shadowNormalBias = normalBias;
            RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientIntensity = ambientIntensity; RenderSettings.fog = fog;
        }
    }

    public sealed class LookComparison : MonoBehaviour
    {
        public VisualTuning tuning;
        public Light key;
        public Volume volume;
        public VolumeProfile backupProfile, lookProfile;
        public LookLightingState originalLighting = new LookLightingState();
        public bool originalVolumeActive = true, originalVolumeEnabled = true;
        [SerializeField] bool useLook = true;
        public bool UsingLook => useLook;

        void OnEnable() { if (Application.isPlaying) SetLook(useLook); }
        void Update() { if (Input.GetKeyDown(KeyCode.F2)) SetLook(!useLook); }

        public void SetLook(bool enabled)
        {
            useLook = enabled;
            if (key == null || volume == null || tuning == null) return;
            volume.sharedProfile = enabled ? lookProfile : backupProfile;
            volume.gameObject.SetActive(enabled || originalVolumeActive);
            volume.enabled = enabled || originalVolumeEnabled;
            if (!enabled) { originalLighting.Apply(key); return; }
            key.color = tuning.lookKey; key.intensity = tuning.lookKeyIntensity;
            key.shadows = LightShadows.Soft; key.shadowStrength = tuning.lookShadowStrength;
            key.shadowBias = .02f; key.shadowNormalBias = .1f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = tuning.lookAmbientSky;
            RenderSettings.ambientEquatorColor = tuning.lookAmbientEquator;
            RenderSettings.ambientGroundColor = tuning.lookAmbientGround;
            RenderSettings.ambientIntensity = tuning.lookAmbientIntensity;
            RenderSettings.fog = false;
            ConfigureProfile();
        }

        public void ConfigureProfile()
        {
            if (lookProfile.TryGet<Tonemapping>(out var tone)) tone.mode.Override(TonemappingMode.Neutral);
            if (lookProfile.TryGet<ColorAdjustments>(out var color))
            {
                color.contrast.Override(tuning.lookContrast); color.saturation.Override(tuning.lookSaturation);
                color.postExposure.Override(tuning.lookExposure); color.colorFilter.Override(Color.white);
            }
            if (lookProfile.TryGet<SplitToning>(out var split))
            {
                split.shadows.Override(tuning.lookShadows); split.highlights.Override(tuning.lookHighlights);
                split.balance.Override(tuning.lookSplitBalance);
            }
            if (lookProfile.TryGet<Bloom>(out var bloom)) { bloom.threshold.Override(1); bloom.intensity.Override(tuning.lookBloom); }
            if (lookProfile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.Override(tuning.lookVignette); vignette.smoothness.Override(tuning.lookVignetteSmoothness);
            }
        }
    }
}
