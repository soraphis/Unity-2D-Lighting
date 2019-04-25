using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using LightingTwoD.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LightingTwoD.Techniques.RTLightmapping {
    [ExecuteInEditMode]
    public class Light2D_LM : Light2D<Light2D_LM> {
        //-------------------------------------------------------------------------------------
        //                                      Serializable Variables
        //-------------------------------------------------------------------------------------
        public LayerMask cullingMask = ~0;
        [SerializeField] private float lineLength;
        
        
        private MaterialPropertyBlock _propertyBlock;
        private Material _material;
        private Mesh _mesh;
        
        
        [ReadOnly][SerializeField] private int _orderInLayer = 0;
        [SortingLayer(isReadOnly = true)][SerializeField] private int _sortingLayer;
        
        //-------------------------------------------------------------------------------------
        //                                      Unity Lifecycle Functions
        //-------------------------------------------------------------------------------------
        
        protected override void OnEnable() {
            Lights.Add(this);
        }

        protected override void OnDisable() {
            Lights.Remove(this);
        }

        protected override void OnValidate() {
            base.OnValidate();
            _lightType = SupportedLightTypes(_lightType) ? _lightType : Light2DType.Point;
            
            _intensity = Mathf.Max(0, _intensity);
            cullingMask.value &= 0b00000000_00000000_11111111_11111111;

            _material = _lightType == Light2DType.Line 
                ? new Material(Shader.Find("Soraphis/RTLM/LightLine")) 
                : new Material(Shader.Find("Soraphis/RTLM/Light"));
        }
        
        private void LateUpdate() {
            if(!Application.isPlaying && _material == null) OnValidate();
            Matrix4x4 matrix = Matrix4x4.identity;
            if (_lightType == Light2DType.Line)
            {
                matrix = Matrix4x4.TRS(transform.position + transform.right * _range/2, transform.rotation, new Vector3(_range/2, lineLength/2, 1));
            }
            else
            {
                matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * _range);
            }
            
            Graphics.DrawMesh(_mesh, matrix, _material, gameObject.layer);
        }
        
        //-------------------------------------------------------------------------------------
        //                                      
        //-------------------------------------------------------------------------------------
        
        private void Init() {
            _propertyBlock = new MaterialPropertyBlock();
            
            _mesh = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            verts.Add(new Vector3(-1, -1));
            verts.Add(new Vector3(+1, +1));
            verts.Add(new Vector3(+1, -1));
            verts.Add(new Vector3(-1, +1));
            _mesh.SetVertices(verts);
            _mesh.SetIndices(new int[]{0, 2, 1, 0, 1, 3}, MeshTopology.Triangles, 0);
            _mesh.SetUVs(0, new List<Vector2>{new Vector2(-1, -1), new Vector2(+1, +1), new Vector2(+1, -1), new Vector2(-1, +1)});
        }

        protected override bool SupportedLightTypes(Light2DType type)
        {
            Light2DType[] allowed = {Light2DType.Point, Light2DType.Spot, Light2DType.Line};
            return allowed.Contains(type);
        }

        
        public MaterialPropertyBlock GetMaterialProperties(int slot, int maxSlots, Texture shadowMap) {
            if(_propertyBlock == null) Init(); // can happen in edit mode
            
            Vector4 shadowMapParams = GetShadowMapParams(slot, maxSlots);

            shadowMapParams.z = (uint)cullingMask.value;
            
            var angle = Mathf.Atan2(transform.right.y, transform.right.x);
            
            Vector4 spotDirection = Vector4.zero;
            Vector4 attenuation = new Vector4(0, 0, 0, 1);
            
            var lightPosition = transform.localToWorldMatrix.GetColumn(3);
            attenuation.x = 1f / Mathf.Max(_range * _range, 0.00001f);
            attenuation.y = _range;
            if (LightType == Light2DType.Spot) {
                float outerRad = Mathf.Deg2Rad * 0.5f * _spotAngle;
                float outerCos = Mathf.Cos(outerRad);
                float outerTan = Mathf.Tan(outerRad);
                float innerCos = Mathf.Cos(Mathf.Atan((46f / 64f) * outerTan));
                float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);

                spotDirection = -transform.right;
                attenuation.z = 1f / angleRange;
                attenuation.w = -outerCos * attenuation.z;
            }

            var lightPosition2 = new Vector4(transform.position.x, transform.position.y, angle, _spotAngle * Mathf.Deg2Rad * 0.5f);
            
            if (LightType == Light2DType.Line)
            {
                var p = (Vector2) lightPosition;
                var q = (Vector2) lightPosition;
                p += (Vector2)(transform.up) * lineLength / 2;
                q -= (Vector2)(transform.up) * lineLength / 2;
                
                lightPosition2 = new Vector4(p.x, p.y, q.x, q.y);
            }
            
            _material.SetVector("_Color", _color * _intensity);
            _material.SetVector("_LightPosition", lightPosition);
            _material.SetVector("_SpotDirection", spotDirection);
            
            _material.SetVector("_LightAttenuation", attenuation);
            _material.SetVector("_ShadowMapParams", shadowMapParams);
            _material.SetTexture("_ShadowTex", shadowMap);
            
            _propertyBlock.SetVector("_LightAttenuation", attenuation);
            _propertyBlock.SetVector("_LightPosition", lightPosition2);
            _propertyBlock.SetVector("_ShadowMapParams", shadowMapParams);
            
            return _propertyBlock;
        }
        
        /// <summary>
        /// calculate the parameters used to read and write the 1D shadow map.
        /// x = parameter for reading shadow map (uv space (0,1))
        /// y = parameter for writing shadow map (clip space (-1,+1))
        /// </summary>
        public static Vector4 GetShadowMapParams(int slot, int maxSlots){
            float u1 = (slot + 0.5f) / maxSlots;
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
        
    }
}