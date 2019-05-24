using System;
using System.Diagnostics;
using System.Numerics;
using LightingTwoD.Core;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Matrix4x4 = UnityEngine.Matrix4x4;
using RenderPipeline = UnityEngine.Rendering.RenderPipeline;
using Vector4 = UnityEngine.Vector4;

namespace LightingTwoD.Techniques.Pipeline {
    
    // Highly inspired by: https://catlikecoding.com/unity/tutorials
    // to create an custom implementation of: https://github.com/Unity-Technologies/ScriptableRenderPipeline
    public class Pipeline2D : RenderPipeline{
        
        //-------------------------------------------------------------------------------------
        //                                      Settings
        //-------------------------------------------------------------------------------------
        private readonly int _shadowmapResolution;
        private readonly int _maxLightSources;
        
        //-------------------------------------------------------------------------------------
        //                                      Cached Variables
        //-------------------------------------------------------------------------------------
        
        CullingResults _cull;
        private readonly CommandBuffer _buffer = new CommandBuffer(){ name = "Render Camera"};
        private readonly CommandBuffer _shadowbuffer = new CommandBuffer(){ name = "Shadow Camera"};
        private Material _errorMaterial;
        private Material _shadowMaterial;
        private Material _litSprite;
        
        //-------------------------------------------------------------------------------------
        //                                      Lights
        //-------------------------------------------------------------------------------------

        private const int MaxVisibleLights = 16;

        private static readonly int VisibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
        private static readonly int VisibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
        private static readonly int VisibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotDirections");
        private static readonly int VisibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");

        private readonly Vector4[] _visibleLightColors = new Vector4[MaxVisibleLights];
        private readonly Vector4[] _visibleLightDirectionsOrPositions  = new Vector4[MaxVisibleLights];
        private readonly Vector4[] _visibleLightSpotDirections = new Vector4[MaxVisibleLights];
        private readonly Vector4[] _visibleLightAttenuations  = new Vector4[MaxVisibleLights];
        
        //-------------------------------------------------------------------------------------
        //                                      Shadows
        //-------------------------------------------------------------------------------------
        
        private static readonly int WorldToShadowMapMatrixId = Shader.PropertyToID("_WorldToShadowMatrices");
        private static readonly int ShadowMapParamId = Shader.PropertyToID("_ShadowMapParams");
        private static readonly int LightAttenuation = Shader.PropertyToID("_LightAttenuation");
        private static readonly int ShadowMapParams = Shader.PropertyToID("_ShadowMapParams");
        private static readonly int LightPosition = Shader.PropertyToID("_LightPosition");
        private static readonly int ShadowTexId = Shader.PropertyToID("_ShadowTex");
        
        private readonly Vector4[] _worldToShadowMatrices  = new Vector4[MaxVisibleLights];
        private readonly Vector4[] _shadowMapParams  = new Vector4[MaxVisibleLights];
        private RenderTexture _shadowMapInitialTexture;
        private RenderTexture _shadowMapFinalTexture;
        
        readonly MaterialPropertyBlock _shadowPropertyBlock = new MaterialPropertyBlock();

        private Mesh _geometryMesh;
        private readonly GeometryCollector.StaticData _staticData = new GeometryCollector.StaticData();
        public Mesh GeometryMesh {
            get {
                if(_geometryMesh != null) return _geometryMesh;
                _geometryMesh = new Mesh();
                GeometryCollector.CollectStatic(_geometryMesh, _staticData);
                return _geometryMesh;
            }
        }

        //-------------------------------------------------------------------------------------
        //                                      
        //-------------------------------------------------------------------------------------
        
        private void Init()
        {
            _shadowMapInitialTexture = new RenderTexture(_shadowmapResolution, _maxLightSources, 0, RenderTextureFormat.RFloat);
            _shadowMapInitialTexture.wrapMode = TextureWrapMode.Clamp;
            _shadowMapInitialTexture.filterMode = FilterMode.Point;
            _shadowMapFinalTexture = new RenderTexture(_shadowmapResolution, _maxLightSources, 0, RenderTextureFormat.RFloat);

            _shadowMaterial = new Material(Shader.Find("Soraphis/Pipeline/Mapping"));

            _litSprite = new Material(Shader.Find("Soraphis/Pipeline/LitSprite"));
            
            Debug.Log("Updated Pipeline Settings");
        }
        
        public Pipeline2D(int shadowmapResolution, int maxLightSources) {
            GraphicsSettings.lightsUseLinearIntensity = true;
            
            _shadowmapResolution = shadowmapResolution;
            _maxLightSources = maxLightSources;
            Init();
        }

        private void Render(ScriptableRenderContext context, Camera camera) {
            // Culling 
            ScriptableCullingParameters cullingParameters;

            if (!camera.TryGetCullingParameters(out cullingParameters)) {
                return;
            }

            _cull = context.Cull(ref cullingParameters);
            //CullResults.Cull(, context, ref cull);
            
            ConfigureLights();
            RenderShadows(context);
            
            context.SetupCameraProperties(camera);
            
            CameraClearFlags clearFlags = camera.clearFlags;
            _buffer.ClearRenderTarget(
                (clearFlags & CameraClearFlags.Depth) != 0,
                (clearFlags & CameraClearFlags.Color) != 0,
                camera.backgroundColor
            );
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView) {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif            
           
            _buffer.BeginSample("Render Camera");
            
            _buffer.SetGlobalTexture(ShadowTexId, _shadowMapFinalTexture);
            _buffer.SetGlobalVectorArray(VisibleLightColorsId, _visibleLightColors);
            _buffer.SetGlobalVectorArray(VisibleLightDirectionsOrPositionsId, _visibleLightDirectionsOrPositions);
            _buffer.SetGlobalVectorArray(VisibleLightSpotDirectionsId, _visibleLightSpotDirections);
            _buffer.SetGlobalVectorArray(VisibleLightAttenuationsId, _visibleLightAttenuations);
            _buffer.SetGlobalVectorArray(WorldToShadowMapMatrixId, _worldToShadowMatrices);
            
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
            var drawingSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), new SortingSettings(camera)
            {
                criteria = SortingCriteria.SortingLayer
            });
            drawingSettings.SetShaderPassName(1, new ShaderTagId(""));
            drawingSettings.enableDynamicBatching = true;
            drawingSettings.enableInstancing = true;
            
            drawingSettings.overrideMaterial = _litSprite;
            drawingSettings.overrideMaterialPassIndex = -1;
            // if (cull.visibleLights.Length > 0) drawingSettings.perObjectData = PerObjectData.LightData | PerObjectData.LightIndices;
            
            // opaque
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            context.DrawRenderers(_cull, ref drawingSettings, ref filterSettings);
            
            context.DrawSkybox(camera);
            
            // transparent
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(_cull, ref drawingSettings, ref filterSettings);

            DrawDefaultPipeline(context, camera);
            
            _buffer.EndSample("Render Camera");
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
            
            context.Submit();
        }

        // private void ConfigureCustomLights() {
        //     
        // }
        
        private void ConfigureLights() {
            if(_shadowMapInitialTexture == null) Init(); // may happen in scene switches
            
            int i = 0;
            //var lightIndices = new NativeArray<int>(maxVisibleLights, Allocator.Temp); 
            foreach (var light in Light2D_Pipeline.Lights) {
                if(! light.IsLightVisible()) return;

                _worldToShadowMatrices[i] = light.transform.position;
                
                _visibleLightColors[i] = light.Color * light.Intensity;
                Vector4 attenuation = new Vector4(0, 0, 0, 1);

                if (light.LightType == Light2D.Light2DType.Area) {
                    throw new NotImplementedException();
                }else 
                if (light.LightType == Light2D.Light2DType.Global) {
                    //Vector4 v = light.transform.localToWorldMatrix.GetColumn(2);
                    //v.x = -v.x;
                    //v.y = -v.y;
                    //v.z = 0;
                    _visibleLightDirectionsOrPositions[i] = new Vector4(0, 0, 1, 0);
                } else {
                    
                    _visibleLightDirectionsOrPositions[i] = light.transform.localToWorldMatrix.GetColumn(3);
                    
                    attenuation.x = 1f / Mathf.Max(light.Range * light.Range, 0.00001f);
                    attenuation.y = light.Range;
                    
                    if (light.LightType == Light2D.Light2DType.Spot) {
//                        Vector4 v = light.transform.localToWorldMatrix.GetColumn(2);
//                        v.x = -v.x;
//                        v.y = -v.y;
//                        v.z = 0;
                        _visibleLightSpotDirections[i] = -light.transform.right;
                        
                        float outerRad = Mathf.Deg2Rad * 0.5f * light.SpotAngle;
                        float outerCos = Mathf.Cos(outerRad);
                        float outerTan = Mathf.Tan(outerRad);
                        float innerCos = Mathf.Cos(Mathf.Atan((46f / 64f) * outerTan));
                        float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
                        
                        attenuation.z = 1f / angleRange;
                        attenuation.w = -outerCos * attenuation.z;
                    }
                }
                
                _visibleLightAttenuations[i] = attenuation;
                
                // ----
                //lightIndices[j++] = i;
                ++i;
                if(i >= MaxVisibleLights) break;
                if (i >= _shadowMapInitialTexture.height) break;
            }

            
            _buffer.SetGlobalVector("_LightData", new Vector4(0, i, 0, 0));

//            
//            Vector4[] LightIndices0 = new []{new Vector4(-1, -1, -1, -1), -Vector4.one };
//            buffer.SetGlobalVector("_LightData", new Vector4(0, Mathf.Min(j, 8), 0, 0));
//            for (int x = 0; x < 4 && x < j; ++x)
//            {
//                LightIndices0[0][x] = lightIndices[x];
//            }
//            for (int x = 0; x < 4 && (4+x) < j; ++x)
//            {
//                LightIndices0[1][x] = lightIndices[4+x];
//            }
//            
//            buffer.SetGlobalVectorArray("_LightIndices", LightIndices0);
//            
            /*
            var lightIndices = cull.GetLightIndexMap(Allocator.Temp);
            for (; i < cull.visibleLights.Length; i++) {
                lightIndices[i] = -1;
            }
            cull.SetLightIndexMap(lightIndices);
            lightIndices.Dispose();
            */
        }

        
        private void RenderShadows(ScriptableRenderContext context) {
            GeometryCollector.CollectDynamic(GeometryMesh, _staticData);

            if(_shadowMapInitialTexture == null) Init(); // may happen in scene switches
            
            _shadowbuffer.Clear();
            context.ExecuteCommandBuffer(_shadowbuffer);
            
            
            _shadowbuffer.SetRenderTarget(_shadowMapInitialTexture);
            _shadowbuffer.ClearRenderTarget(false, true, Color.white);
            context.ExecuteCommandBuffer(_shadowbuffer);
            
            /*CoreUtils.SetRenderTarget(_shadowbuffer, _ShadowMapInitialTexture,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                ClearFlag.All, Color.white);*/

            //using (new ProfilingSample(_shadowbuffer, "Mapping Shadows")) {
                int slot = 0;
                int maxSlots = Mathf.Min(_shadowMapInitialTexture.height, _maxLightSources);
                foreach (var light in Light2D_Pipeline.Lights) {
                    if (!light.IsLightVisible()) continue;
                    
                    
                    _shadowMapParams[slot] = GetShadowMapParams(slot, maxSlots);
                    _shadowPropertyBlock.Clear();
                    _shadowPropertyBlock.SetVector(LightAttenuation, _visibleLightAttenuations[slot]);
                    _shadowPropertyBlock.SetVector(ShadowMapParams, _shadowMapParams[slot]);
                    _shadowPropertyBlock.SetVector(LightPosition, light.LightData());
                    _shadowbuffer.DrawMesh(GeometryMesh, Matrix4x4.identity, _shadowMaterial, 0, 0, _shadowPropertyBlock);
                    ++slot;
                    if (slot >= maxSlots) break;
                }
                _shadowbuffer.SetGlobalVectorArray(ShadowMapParamId, _shadowMapParams);
                context.ExecuteCommandBuffer(_shadowbuffer);
                _shadowbuffer.Clear();
            //}
            
            context.ExecuteCommandBuffer(_shadowbuffer);

            //using (new ProfilingSample(_shadowbuffer, "Refit Shadowmap"))
            //{
                _shadowbuffer.SetRenderTarget(_shadowMapFinalTexture);
                _shadowbuffer.ClearRenderTarget(true, true, Color.white, 1.0f);
                context.ExecuteCommandBuffer(_shadowbuffer);
                _shadowbuffer.Clear();
                
                _shadowbuffer.SetGlobalTexture("_MainTex", _shadowMapInitialTexture); // fixes a weird behaviour in Unity with the blit function on command buffers
                _shadowbuffer.Blit(_shadowMapInitialTexture, _shadowMapFinalTexture, _shadowMaterial, 1);
                context.ExecuteCommandBuffer(_shadowbuffer);
                _shadowbuffer.Clear();
            //}
            
            context.ExecuteCommandBuffer(_shadowbuffer);
        }
        
        private static Vector4 GetShadowMapParams(int slot, int maxSlots){
            float u1 = ((float)slot + 0.5f) / maxSlots;
            float u2 = (u1 - 0.5f) * 2.0f;

            if (   //(SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGL2) // OpenGL2 is no longer supported in Unity 5.5+
                (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
                || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
            )
            {
                return new Vector4(u1, u2, 0.0f, 0.0f);
            }
            else
            {
                return new Vector4(1.0f - u1, u2, 0.0f, 0.0f);
            }
        }

        

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera) {
            _errorMaterial = _errorMaterial ? _errorMaterial : new Material(Shader.Find("Hidden/InternalErrorShader")){hideFlags = HideFlags.HideAndDontSave };
            
            var drawSettings = new DrawingSettings(new ShaderTagId("ForwardBase"), new SortingSettings(camera));
            drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
            drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
            drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
            drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
            drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));
            drawSettings.overrideMaterial = _errorMaterial;
            drawSettings.overrideMaterialPassIndex = -1;
            var filterSettings = new FilteringSettings();
            
            context.DrawRenderers(
                _cull, ref drawSettings, ref filterSettings
            );
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            foreach (var camera in cameras) {
                BeginCameraRendering(camera);
                Render(context, camera);
            }
        }
    }
}