using LightingTwoD.Core;
using UnityEngine;
using UnityEngine.Rendering;
using RenderPipeline = UnityEngine.Rendering.RenderPipeline;
using Resolution = LightingTwoD.Core.Resolution;

namespace LightingTwoD.Techniques.Pipeline
{
    [CreateAssetMenu(menuName = "Rendering/Soraphis2DPipeline")]
    public class Soraphis2DPipeline : RenderPipelineAsset
    {
        [ConditionalEnum(nameof(SupportedResolution))] public Resolution shadowMapResolution;
        [ConditionalEnum(nameof(SupportedLightSources))] public Resolution maxLightSourcesCount;

        bool SupportedResolution(Resolution resolution)
        {
            return (int) resolution > 8;
        }
        
        bool SupportedLightSources(Resolution resolution)
        {
            return (int) resolution < 32;
        }
        
        protected override RenderPipeline CreatePipeline()
        {
            return new Pipeline2D((int)shadowMapResolution, (int)maxLightSourcesCount);
        }
    }
}
