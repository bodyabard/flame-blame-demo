# Look transfer baseline
## Quick comparison (before → final)

| Setting | User-tuned baseline | Look, iteration 2 |
|---|---|---|
| Key intensity / shadow strength | 1.2 / 0.687 | 1.8 / 0.75 |
| Key angle | (21.8, 89.2, 0) | unchanged |
| Ambient | Flat, warm | Gradient; warm sky, neutral equator, cooler ground |
| Global Volume | inactive | active; Neutral tonemapping |
| Contrast / saturation (stored profile) | +5 / +5, inactive | +22 / +22 |
| Exposure (stored profile) | +1, inactive | 0 |
| Bloom / vignette (stored profile) | 0.45 / 0.22, inactive | 0.6 / 0.26 |
| Mobile render scale / MSAA | 0.8 / off | 1 / 2x |
| Mobile shadow map / distance / soft support | 1024 / 50m / off | 2048 / 20m / on |
| PC MSAA / distance | 8x / 150m | 2x / 20m |
| Opaque materials | existing Toon highlights | 37 matte materials; shader/base/shade colors preserved |

Iteration 1 used key 2, contrast/saturation +25, exposure +0.1, bloom 0.7. Iteration 2 reduces these to protect warm highlights. No further visual iterations performed.

The originally disabled volume includes effects such as Depth of Field and Channel Mixer; the new Look profile includes only Tonemapping, Color Adjustments, Split Toning, Bloom and Vignette.

## Use and rollback

- In Game view during Play: F2 switches Backup/Look. In Editor: F2 shortcut or Tools/FlameBlame/Toggle Look.
- Tune `Assets/_Project/Settings/VisualTuning.asset` Look fields; apply with Tools/FlameBlame/Apply Look or F2 twice. No recompilation needed.
- Tools/FlameBlame/Capture captures two fixed angles through the full camera stack, including hands. Backup/Look pairs use the same matte materials and pipeline; Before/After pairs show the complete baseline versus final change. Left = before/backup; right = after/look.
- Tools/FlameBlame/Restore User Tuned Look restores the backed-up full scene, materials, renderer/pipeline assets and QualitySettings in one action. Backups are retained. This restores the scene snapshot, including its layout at the start of phase 8.
- No WebGL rebuild/deployment was performed during this art pass.

## Verification

Unity compilation succeeded. Live play smoke verified profile/light switching, unchanged key angle, property-block charring on a burnable prop (base and both Toon shades), and an NPC. Palette/shader comparison against all material snapshots: 0 mismatches. Full restore test: 0 serialized asset mismatches; original PC AA 8x restored. Post-processing is enabled only on the final ViewmodelCamera overlay, so grading runs once over the full stack. Additional-light shadow texture support remains enabled for the existing WebGL sampler workaround; only the key light casts shadows.


Source: https://github.com/KaganAyten/RestaurantGame3DUnity (MIT); settings only, no art imported.

Key (called WarmKey in prompt): Sunset Key; color=RGBA(1.000, 0.914, 0.667, 1.000); intensity=1,2; rotation=(21.80, 89.20, 0.00); shadows=Soft; strength=0,687; bias=0,05; normalBias=0,4
Ambient: Flat; sky=RGBA(0.792, 0.478, 0.431, 0.400); equator=RGBA(0.114, 0.125, 0.133, 1.000); ground=RGBA(0.047, 0.043, 0.035, 1.000); intensity=1
Volume source: Assets/_Project/Settings/SunsetVolume.asset
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "SunsetVolume",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Core.Runtime::UnityEngine.Rendering.VolumeProfile",
        "components": [
            {
                "fileID": -8486617192213401274,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            },
            {
                "fileID": -4236281876028980877,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            },
            {
                "fileID": -8277038271554071259,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            },
            {
                "fileID": -844607900539599161,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            },
            {
                "fileID": -5226131581864249202,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            },
            {
                "fileID": -940623653250004107,
                "guid": "bf7f95e2e1eed69488edfc931363776e",
                "type": 2
            }
        ]
    }
}
```
Volume originally active: False; enabled=True
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "Bloom",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.Bloom",
        "active": true,
        "skipIterations": {
            "m_OverrideState": true,
            "m_Value": 1
        },
        "threshold": {
            "m_OverrideState": true,
            "m_Value": 1.0
        },
        "intensity": {
            "m_OverrideState": true,
            "m_Value": 0.44999998807907107
        },
        "scatter": {
            "m_OverrideState": true,
            "m_Value": 0.699999988079071
        },
        "clamp": {
            "m_OverrideState": true,
            "m_Value": 65472.0
        },
        "tint": {
            "m_OverrideState": true,
            "m_Value": {
                "r": 1.0,
                "g": 1.0,
                "b": 1.0,
                "a": 1.0
            }
        },
        "highQualityFiltering": {
            "m_OverrideState": true,
            "m_Value": false
        },
        "filter": {
            "m_OverrideState": true,
            "m_Value": 0
        },
        "downscale": {
            "m_OverrideState": true,
            "m_Value": 0
        },
        "maxIterations": {
            "m_OverrideState": true,
            "m_Value": 6
        },
        "dirtTexture": {
            "m_OverrideState": true,
            "m_Value": {
                "instanceID": 0
            },
            "dimension": 1
        },
        "dirtIntensity": {
            "m_OverrideState": true,
            "m_Value": 0.0
        }
    }
}
```
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "ColorAdjustments",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.ColorAdjustments",
        "active": true,
        "postExposure": {
            "m_OverrideState": true,
            "m_Value": 1.0
        },
        "contrast": {
            "m_OverrideState": true,
            "m_Value": 5.0
        },
        "colorFilter": {
            "m_OverrideState": true,
            "m_Value": {
                "r": 1.0,
                "g": 1.0,
                "b": 1.0,
                "a": 1.0
            }
        },
        "hueShift": {
            "m_OverrideState": true,
            "m_Value": 0.0
        },
        "saturation": {
            "m_OverrideState": true,
            "m_Value": 5.0
        }
    }
}
```
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "Vignette",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.Vignette",
        "active": true,
        "color": {
            "m_OverrideState": true,
            "m_Value": {
                "r": 0.0,
                "g": 0.0,
                "b": 0.0,
                "a": 1.0
            }
        },
        "center": {
            "m_OverrideState": true,
            "m_Value": {
                "x": 0.5,
                "y": 0.5
            }
        },
        "intensity": {
            "m_OverrideState": true,
            "m_Value": 0.2199999988079071
        },
        "smoothness": {
            "m_OverrideState": true,
            "m_Value": 0.20000000298023225
        },
        "rounded": {
            "m_OverrideState": true,
            "m_Value": false
        }
    }
}
```
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "Tonemapping",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.Tonemapping",
        "active": true,
        "mode": {
            "m_OverrideState": true,
            "m_Value": 2
        },
        "neutralHDRRangeReductionMode": {
            "m_OverrideState": false,
            "m_Value": 2
        },
        "acesPreset": {
            "m_OverrideState": false,
            "m_Value": 3
        },
        "hueShiftAmount": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "detectPaperWhite": {
            "m_OverrideState": false,
            "m_Value": false
        },
        "paperWhite": {
            "m_OverrideState": false,
            "m_Value": 300.0
        },
        "detectBrightnessLimits": {
            "m_OverrideState": false,
            "m_Value": true
        },
        "minNits": {
            "m_OverrideState": false,
            "m_Value": 0.004999999888241291
        },
        "maxNits": {
            "m_OverrideState": false,
            "m_Value": 1000.0
        }
    }
}
```
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "ChannelMixer",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.ChannelMixer",
        "active": true,
        "redOutRedIn": {
            "m_OverrideState": false,
            "m_Value": 100.0
        },
        "redOutGreenIn": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "redOutBlueIn": {
            "m_OverrideState": true,
            "m_Value": -10.0
        },
        "greenOutRedIn": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "greenOutGreenIn": {
            "m_OverrideState": false,
            "m_Value": 100.0
        },
        "greenOutBlueIn": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "blueOutRedIn": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "blueOutGreenIn": {
            "m_OverrideState": false,
            "m_Value": 0.0
        },
        "blueOutBlueIn": {
            "m_OverrideState": false,
            "m_Value": 100.0
        }
    }
}
```
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "DepthOfField",
        "m_EditorClassIdentifier": "Unity.RenderPipelines.Universal.Runtime::UnityEngine.Rendering.Universal.DepthOfField",
        "active": true,
        "mode": {
            "m_OverrideState": false,
            "m_Value": 0
        },
        "gaussianStart": {
            "m_OverrideState": false,
            "m_Value": 10.0
        },
        "gaussianEnd": {
            "m_OverrideState": false,
            "m_Value": 30.0
        },
        "gaussianMaxRadius": {
            "m_OverrideState": false,
            "m_Value": 1.0
        },
        "highQualitySampling": {
            "m_OverrideState": false,
            "m_Value": false
        },
        "focusDistance": {
            "m_OverrideState": false,
            "m_Value": 10.0
        },
        "aperture": {
            "m_OverrideState": false,
            "m_Value": 5.599999904632568
        },
        "focalLength": {
            "m_OverrideState": false,
            "m_Value": 50.0
        },
        "bladeCount": {
            "m_OverrideState": false,
            "m_Value": 5
        },
        "bladeCurvature": {
            "m_OverrideState": false,
            "m_Value": 1.0
        },
        "bladeRotation": {
            "m_OverrideState": false,
            "m_Value": 0.0
        }
    }
}
```
URP: Assets/Settings/Mobile_RPAsset.asset
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "Mobile_RPAsset",
        "m_EditorClassIdentifier": "",
        "k_AssetVersion": 13,
        "k_AssetPreviousVersion": 13,
        "m_RendererType": 1,
        "m_RendererData": {
            "instanceID": 0
        },
        "m_RendererDataList": [
            {
                "fileID": 11400000,
                "guid": "65bc7dbf4170f435aa868c779acfb082",
                "type": 2
            }
        ],
        "m_DefaultRendererIndex": 0,
        "m_RequireDepthTexture": false,
        "m_RequireOpaqueTexture": false,
        "m_OpaqueDownsampling": 0,
        "m_SupportsTerrainHoles": true,
        "m_SupportsHDR": true,
        "m_HDRColorBufferPrecision": 0,
        "m_MSAA": 1,
        "m_RenderScale": 0.800000011920929,
        "m_UpscalingFilter": 0,
        "m_FsrOverrideSharpness": false,
        "m_FsrSharpness": 0.9200000166893005,
        "m_EnableLODCrossFade": true,
        "m_LODCrossFadeDitheringType": 1,
        "m_ShEvalMode": 0,
        "m_LightProbeSystem": 0,
        "m_ProbeVolumeMemoryBudget": 1024,
        "m_ProbeVolumeBlendingMemoryBudget": 256,
        "m_SupportProbeVolumeGPUStreaming": false,
        "m_SupportProbeVolumeDiskStreaming": false,
        "m_SupportProbeVolumeScenarios": false,
        "m_SupportProbeVolumeScenarioBlending": false,
        "m_ProbeVolumeSHBands": 1,
        "m_MainLightRenderingMode": 1,
        "m_MainLightShadowsSupported": true,
        "m_MainLightShadowmapResolution": 1024,
        "m_AdditionalLightsRenderingMode": 1,
        "m_AdditionalLightsPerObjectLimit": 4,
        "m_AdditionalLightShadowsSupported": true,
        "m_AdditionalLightsShadowmapResolution": 2048,
        "m_AdditionalLightsShadowResolutionTierLow": 256,
        "m_AdditionalLightsShadowResolutionTierMedium": 512,
        "m_AdditionalLightsShadowResolutionTierHigh": 1024,
        "m_ReflectionProbeBlending": true,
        "m_ReflectionProbeBoxProjection": true,
        "m_ReflectionProbeAtlas": true,
        "m_ShadowDistance": 50.0,
        "m_ShadowCascadeCount": 1,
        "m_Cascade2Split": 0.25,
        "m_Cascade3Split": {
            "x": 0.10000000149011612,
            "y": 0.30000001192092898
        },
        "m_Cascade4Split": {
            "x": 0.06700000166893006,
            "y": 0.20000000298023225,
            "z": 0.46700000762939455
        },
        "m_CascadeBorder": 0.20000000298023225,
        "m_ShadowDepthBias": 1.0,
        "m_ShadowNormalBias": 1.0,
        "m_AnyShadowsSupported": true,
        "m_SoftShadowsSupported": false,
        "m_ConservativeEnclosingSphere": true,
        "m_NumIterationsEnclosingSphere": 64,
        "m_SoftShadowQuality": 2,
        "m_AdditionalLightsCookieResolution": 1024,
        "m_AdditionalLightsCookieFormat": 1,
        "m_UseSRPBatcher": true,
        "m_SupportsDynamicBatching": false,
        "m_MixedLightingSupported": true,
        "m_SupportsLightCookies": true,
        "m_SupportsLightLayers": true,
        "m_DebugLevel": 0,
        "m_StoreActionsOptimization": 0,
        "m_UseAdaptivePerformance": true,
        "m_ColorGradingMode": 0,
        "m_ColorGradingLutSize": 32,
        "m_AllowPostProcessAlphaOutput": false,
        "m_UseFastSRGBLinearConversion": true,
        "m_SupportDataDrivenLensFlare": true,
        "m_SupportScreenSpaceLensFlare": true,
        "m_GPUResidentDrawerMode": 0,
        "m_SmallMeshScreenPercentage": 0.0,
        "m_GPUResidentDrawerEnableOcclusionCullingInCameras": false,
        "m_ShadowType": 1,
        "m_LocalShadowsSupported": false,
        "m_LocalShadowsAtlasResolution": 256,
        "m_MaxPixelLights": 0,
        "m_ShadowAtlasResolution": 256,
        "m_VolumeFrameworkUpdateMode": 0,
        "m_VolumeProfile": {
            "fileID": 11400000,
            "guid": "10fc4df2da32a41aaa32d77bc913491c",
            "type": 2
        },
        "apvScenesData": {
            "obsoleteSceneBounds": {
                "m_Keys": [],
                "m_Values": []
            },
            "obsoleteHasProbeVolumes": {
                "m_Keys": [],
                "m_Values": []
            }
        },
        "m_PrefilteringModeMainLightShadows": 3,
        "m_PrefilteringModeAdditionalLight": 3,
        "m_PrefilteringModeAdditionalLightShadows": 2,
        "m_PrefilterXRKeywords": true,
        "m_PrefilteringModeForwardPlus": 0,
        "m_PrefilteringModeDeferredRendering": 0,
        "m_PrefilteringModeScreenSpaceOcclusion": 0,
        "m_PrefilterDebugKeywords": true,
        "m_PrefilterWriteRenderingLayers": true,
        "m_PrefilterHDROutput": true,
        "m_PrefilterAlphaOutput": true,
        "m_PrefilterSSAODepthNormals": true,
        "m_PrefilterSSAOSourceDepthLow": true,
        "m_PrefilterSSAOSourceDepthMedium": true,
        "m_PrefilterSSAOSourceDepthHigh": true,
        "m_PrefilterSSAOInterleaved": true,
        "m_PrefilterSSAOBlueNoise": true,
        "m_PrefilterSSAOSampleCountLow": true,
        "m_PrefilterSSAOSampleCountMedium": true,
        "m_PrefilterSSAOSampleCountHigh": true,
        "m_PrefilterDBufferMRT1": true,
        "m_PrefilterDBufferMRT2": true,
        "m_PrefilterDBufferMRT3": true,
        "m_PrefilterSoftShadowsQualityLow": true,
        "m_PrefilterSoftShadowsQualityMedium": true,
        "m_PrefilterSoftShadowsQualityHigh": true,
        "m_PrefilterSoftShadows": false,
        "m_PrefilterScreenCoord": true,
        "m_PrefilterScreenSpaceIrradiance": true,
        "m_PrefilterNativeRenderPass": true,
        "m_PrefilterUseLegacyLightmaps": false,
        "m_PrefilterBicubicLightmapSampling": true,
        "m_PrefilterReflectionProbeRotation": true,
        "m_PrefilterReflectionProbeBlending": false,
        "m_PrefilterReflectionProbeBoxProjection": false,
        "m_PrefilterReflectionProbeAtlas": true,
        "m_ShaderVariantLogLevel": 0,
        "m_ShadowCascades": 0,
        "m_Textures": {
            "blueNoise64LTex": {
                "fileID": 2800000,
                "guid": "e3d24661c1e055f45a7560c033dbb837",
                "type": 3
            },
            "bayerMatrixTex": {
                "fileID": 2800000,
                "guid": "f9ee4ed84c1d10c49aabb9b210b0fc44",
                "type": 3
            }
        }
    }
}
```
URP: Assets/Settings/PC_RPAsset.asset
```json
{
    "MonoBehaviour": {
        "m_Enabled": true,
        "m_EditorHideFlags": 0,
        "m_Name": "PC_RPAsset",
        "m_EditorClassIdentifier": "",
        "k_AssetVersion": 13,
        "k_AssetPreviousVersion": 13,
        "m_RendererType": 1,
        "m_RendererData": {
            "instanceID": 0
        },
        "m_RendererDataList": [
            {
                "fileID": 11400000,
                "guid": "f288ae1f4751b564a96ac7587541f7a2",
                "type": 2
            }
        ],
        "m_DefaultRendererIndex": 0,
        "m_RequireDepthTexture": true,
        "m_RequireOpaqueTexture": true,
        "m_OpaqueDownsampling": 1,
        "m_SupportsTerrainHoles": true,
        "m_SupportsHDR": true,
        "m_HDRColorBufferPrecision": 0,
        "m_MSAA": 8,
        "m_RenderScale": 1.0,
        "m_UpscalingFilter": 0,
        "m_FsrOverrideSharpness": false,
        "m_FsrSharpness": 0.9200000166893005,
        "m_EnableLODCrossFade": true,
        "m_LODCrossFadeDitheringType": 1,
        "m_ShEvalMode": 0,
        "m_LightProbeSystem": 0,
        "m_ProbeVolumeMemoryBudget": 1024,
        "m_ProbeVolumeBlendingMemoryBudget": 256,
        "m_SupportProbeVolumeGPUStreaming": false,
        "m_SupportProbeVolumeDiskStreaming": false,
        "m_SupportProbeVolumeScenarios": false,
        "m_SupportProbeVolumeScenarioBlending": false,
        "m_ProbeVolumeSHBands": 1,
        "m_MainLightRenderingMode": 1,
        "m_MainLightShadowsSupported": true,
        "m_MainLightShadowmapResolution": 2048,
        "m_AdditionalLightsRenderingMode": 1,
        "m_AdditionalLightsPerObjectLimit": 4,
        "m_AdditionalLightShadowsSupported": true,
        "m_AdditionalLightsShadowmapResolution": 4096,
        "m_AdditionalLightsShadowResolutionTierLow": 256,
        "m_AdditionalLightsShadowResolutionTierMedium": 512,
        "m_AdditionalLightsShadowResolutionTierHigh": 1024,
        "m_ReflectionProbeBlending": true,
        "m_ReflectionProbeBoxProjection": true,
        "m_ReflectionProbeAtlas": true,
        "m_ShadowDistance": 150.0,
        "m_ShadowCascadeCount": 4,
        "m_Cascade2Split": 0.25,
        "m_Cascade3Split": {
            "x": 0.10000000149011612,
            "y": 0.30000001192092898
        },
        "m_Cascade4Split": {
            "x": 0.12299998849630356,
            "y": 0.29260000586509707,
            "z": 0.5359999537467957
        },
        "m_CascadeBorder": 0.10775860399007797,
        "m_ShadowDepthBias": 0.10000000149011612,
        "m_ShadowNormalBias": 0.0,
        "m_AnyShadowsSupported": true,
        "m_SoftShadowsSupported": true,
        "m_ConservativeEnclosingSphere": true,
        "m_NumIterationsEnclosingSphere": 64,
        "m_SoftShadowQuality": 3,
        "m_AdditionalLightsCookieResolution": 2048,
        "m_AdditionalLightsCookieFormat": 3,
        "m_UseSRPBatcher": true,
        "m_SupportsDynamicBatching": false,
        "m_MixedLightingSupported": true,
        "m_SupportsLightCookies": true,
        "m_SupportsLightLayers": true,
        "m_DebugLevel": 0,
        "m_StoreActionsOptimization": 0,
        "m_UseAdaptivePerformance": true,
        "m_ColorGradingMode": 0,
        "m_ColorGradingLutSize": 32,
        "m_AllowPostProcessAlphaOutput": false,
        "m_UseFastSRGBLinearConversion": false,
        "m_SupportDataDrivenLensFlare": true,
        "m_SupportScreenSpaceLensFlare": true,
        "m_GPUResidentDrawerMode": 0,
        "m_SmallMeshScreenPercentage": 0.0,
        "m_GPUResidentDrawerEnableOcclusionCullingInCameras": false,
        "m_ShadowType": 1,
        "m_LocalShadowsSupported": false,
        "m_LocalShadowsAtlasResolution": 256,
        "m_MaxPixelLights": 0,
        "m_ShadowAtlasResolution": 256,
        "m_VolumeFrameworkUpdateMode": 0,
        "m_VolumeProfile": {
            "fileID": 11400000,
            "guid": "10fc4df2da32a41aaa32d77bc913491c",
            "type": 2
        },
        "apvScenesData": {
            "obsoleteSceneBounds": {
                "m_Keys": [],
                "m_Values": []
            },
            "obsoleteHasProbeVolumes": {
                "m_Keys": [],
                "m_Values": []
            }
        },
        "m_PrefilteringModeMainLightShadows": 3,
        "m_PrefilteringModeAdditionalLight": 3,
        "m_PrefilteringModeAdditionalLightShadows": 2,
        "m_PrefilterXRKeywords": true,
        "m_PrefilteringModeForwardPlus": 0,
        "m_PrefilteringModeDeferredRendering": 0,
        "m_PrefilteringModeScreenSpaceOcclusion": 0,
        "m_PrefilterDebugKeywords": true,
        "m_PrefilterWriteRenderingLayers": true,
        "m_PrefilterHDROutput": true,
        "m_PrefilterAlphaOutput": true,
        "m_PrefilterSSAODepthNormals": true,
        "m_PrefilterSSAOSourceDepthLow": true,
        "m_PrefilterSSAOSourceDepthMedium": true,
        "m_PrefilterSSAOSourceDepthHigh": true,
        "m_PrefilterSSAOInterleaved": true,
        "m_PrefilterSSAOBlueNoise": true,
        "m_PrefilterSSAOSampleCountLow": true,
        "m_PrefilterSSAOSampleCountMedium": true,
        "m_PrefilterSSAOSampleCountHigh": true,
        "m_PrefilterDBufferMRT1": true,
        "m_PrefilterDBufferMRT2": true,
        "m_PrefilterDBufferMRT3": true,
        "m_PrefilterSoftShadowsQualityLow": true,
        "m_PrefilterSoftShadowsQualityMedium": true,
        "m_PrefilterSoftShadowsQualityHigh": true,
        "m_PrefilterSoftShadows": false,
        "m_PrefilterScreenCoord": true,
        "m_PrefilterScreenSpaceIrradiance": true,
        "m_PrefilterNativeRenderPass": true,
        "m_PrefilterUseLegacyLightmaps": false,
        "m_PrefilterBicubicLightmapSampling": true,
        "m_PrefilterReflectionProbeRotation": true,
        "m_PrefilterReflectionProbeBlending": false,
        "m_PrefilterReflectionProbeBoxProjection": false,
        "m_PrefilterReflectionProbeAtlas": true,
        "m_ShaderVariantLogLevel": 0,
        "m_ShadowCascades": 0,
        "m_Textures": {
            "blueNoise64LTex": {
                "fileID": 2800000,
                "guid": "e3d24661c1e055f45a7560c033dbb837",
                "type": 3
            },
            "bayerMatrixTex": {
                "fileID": 2800000,
                "guid": "f9ee4ed84c1d10c49aabb9b210b0fc44",
                "type": 3
            }
        }
    }
}
```

## Applied look
Final iteration 2: Neutral; contrast +22; saturation +22; exposure 0; split balance -20; bloom 0,6/threshold 1; vignette 0,26/smoothness 0,4. Key cream (0.99,0.90,0.71), intensity 1,8, soft shadow strength 0,75, bias 0.02/0.1; rotation unchanged. Gradient ambient sky (0.62,0.59,0.51), equator (0.36,0.34,0.32), ground (0.20,0.22,0.28), intensity 1.
URP: HDR on, MSAA 2x, scale 1, main shadow map 2048, soft shadows on, distance 20m, LDR LUT32. Additional-light shadow texture support stays enabled for WebGL, while only the key casts shadows.
F2 compares original profile/key/ambient with Look; matte materials and pipeline are shared. Before captures predate all changes. Restore User Tuned Look restores materials, pipeline/renderer assets and the full backed-up scene in one action. No reference assets imported.
Tune VisualTuning look fields and Apply Look (or toggle F2 twice). Archived Originals and transparent water/VFX are excluded. Toon shaders remain Toon; high color, MatCap and rim reflection effects are disabled instead of changing shaders.
