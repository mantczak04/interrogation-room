using InterrogationRoom.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace InterrogationRoom.Graphics
{
    /// <summary>Applies saved graphics preferences to a runtime pipeline copy, leaving project assets intact.</summary>
    public sealed class GraphicsQualityController : MonoBehaviour
    {
        private UniversalRenderPipelineAsset pipeline;
        private RenderPipelineAsset previousOverride;
        private int previousTextureLimit;
        private AnisotropicFiltering previousFiltering;
        private int appliedQuality = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var owner = new GameObject("GraphicsQualityController");
            DontDestroyOnLoad(owner);
            owner.AddComponent<GraphicsQualityController>();
        }

        private void Awake()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset source))
            {
                enabled = false;
                return;
            }
            previousOverride = QualitySettings.renderPipeline;
            previousTextureLimit = QualitySettings.globalTextureMipmapLimit;
            previousFiltering = QualitySettings.anisotropicFiltering;
            pipeline = Instantiate(source);
            pipeline.name = "Station graphics (runtime)";
            QualitySettings.renderPipeline = pipeline;
            GameSettingsService.Current.Changed += Apply;
            RenderPipelineManager.beginCameraRendering += ConfigureCamera;
            Apply();
        }

        private void Apply()
        {
            int quality = GameSettingsService.Current.GraphicsQuality;
            if (pipeline == null || appliedQuality == quality) return;
            appliedQuality = quality;
            pipeline.renderScale = quality == 0 ? .75f : quality == 1 ? .9f : 1f;
            pipeline.msaaSampleCount = quality < 2 ? 2 : quality == 2 ? 4 : 8;
            pipeline.mainLightShadowmapResolution = quality == 0 ? 1024 : quality == 3 ? 4096 : 2048;
            pipeline.additionalLightsShadowmapResolution = quality < 2 ? 1024 : quality == 3 ? 4096 : 2048;
            pipeline.shadowDistance = quality == 0 ? 20 : quality == 1 ? 35 : quality == 2 ? 50 : 70;
            pipeline.shadowCascadeCount = quality < 2 ? 2 : 4;
            QualitySettings.globalTextureMipmapLimit = quality == 0 ? 1 : 0;
            QualitySettings.anisotropicFiltering = quality < 2 ? AnisotropicFiltering.Enable : AnisotropicFiltering.ForceEnable;
        }

        private void ConfigureCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game ||
                !camera.TryGetComponent<UniversalAdditionalCameraData>(out var data) ||
                data.renderType == CameraRenderType.Overlay) return;
            data.antialiasing = appliedQuality < 2 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = appliedQuality == 3 ? AntialiasingQuality.High : AntialiasingQuality.Medium;
        }

        private void OnDestroy()
        {
            if (pipeline == null) return;
            GameSettingsService.Current.Changed -= Apply;
            RenderPipelineManager.beginCameraRendering -= ConfigureCamera;
            QualitySettings.renderPipeline = previousOverride;
            QualitySettings.globalTextureMipmapLimit = previousTextureLimit;
            QualitySettings.anisotropicFiltering = previousFiltering;
            Destroy(pipeline);
        }
    }
}
