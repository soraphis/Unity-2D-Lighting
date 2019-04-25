using UnityEngine;
using LightingTwoD.Core;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Resolution = LightingTwoD.Core.Resolution;

namespace LightingTwoD.Techniques.RTLightmapping {
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class LightLayer : UnityEngine.MonoBehaviour {
        //-------------------------------------------------------------------------------------
        //                                      Serializable Variables
        //-------------------------------------------------------------------------------------
        [SerializeField] private Resolution maxShadowCastingLights = Resolution._64;
        [SerializeField] private Resolution lightmapResolution = Resolution._512;
        
        //-------------------------------------------------------------------------------------
        //                                      Variables/Properties
        //-------------------------------------------------------------------------------------
        private RenderTexture _shadowMapInitialTexture;
        private RenderTexture _shadowMapFinalTexture;
        
        private readonly GeometryCollector.StaticData _staticData = new GeometryCollector.StaticData();
        private CommandBuffer _buffer;
        private Material _material;
        private Mesh _geometry;
        private Camera _camera;
        
        
        //-------------------------------------------------------------------------------------
        //                                      Unity Lifecycle Functions
        //-------------------------------------------------------------------------------------
        
        /*
         *     WITH THE UNITY SRP ENABLED OnPreRender() and Camera.onPreRenderer() DON'T WORK ANYMORE
         *     AS A WORKAROUND, WERE SETTING UP COMMAND BUFFERS IN THE UPDATE METHOD
         */

        private void Awake() { Init(); }

        private void OnEnable() {
            if(!Application.isPlaying && _buffer == null) Init(); // possible in EditMode
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _buffer);
        }

        private void OnDisable() {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _buffer);
        }

        private void LateUpdate() {
            if(!Application.isPlaying && _geometry == null) Init(); // possible in EditMode
            if(!Application.isPlaying) GeometryCollector.CollectStatic(_geometry, _staticData); 
            GeometryCollector.CollectDynamic(_geometry, _staticData);
            
            _buffer.Clear();
            _buffer.SetRenderTarget(_shadowMapInitialTexture);
            _buffer.ClearRenderTarget(true, true, Color.white, 0f);

            int slot = 0;
            foreach (var light2D in Light2D_LM.Lights) {
                var properties = light2D.GetMaterialProperties(slot, (int)maxShadowCastingLights, _shadowMapFinalTexture);

                int shaderPass = light2D.LightType == Light2D.Light2DType.Line ? 2 : 0; 
                
                _buffer.DrawMesh(_geometry, Matrix4x4.identity, _material, 0, shaderPass, properties);
                
                
                ++slot;
                if (slot >= (int)maxShadowCastingLights) break;
            }

            _buffer.SetRenderTarget(_shadowMapFinalTexture);
            _buffer.ClearRenderTarget(true, true, Color.white, 1.0f);
            _buffer.SetGlobalTexture("_MainTex", _shadowMapInitialTexture);
            _buffer.Blit(_shadowMapInitialTexture, _shadowMapFinalTexture, _material, 1);
        }
        
        //-------------------------------------------------------------------------------------
        //                                      
        //-------------------------------------------------------------------------------------

        private void Init() {
            _geometry = new Mesh();
            _buffer = new CommandBuffer();
            _camera = GetComponent<Camera>();
            
            _shadowMapInitialTexture = new RenderTexture((int)lightmapResolution, (int)maxShadowCastingLights, 0, RenderTextureFormat.RFloat);
            _shadowMapFinalTexture = new RenderTexture((int)lightmapResolution,(int)maxShadowCastingLights, 0, RenderTextureFormat.RFloat);

            _material = new Material(Shader.Find("Soraphis/RTLM/Mapping"));
            
            GeometryCollector.CollectStatic(_geometry, _staticData);
        }
        
        
    }
}