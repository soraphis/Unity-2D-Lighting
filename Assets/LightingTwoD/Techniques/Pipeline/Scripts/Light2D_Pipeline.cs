using System;
using System.Linq;
using LightingTwoD.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LightingTwoD.Techniques.Pipeline
{
    [ExecuteInEditMode]
    public sealed class Light2D_Pipeline : Light2D<Light2D_Pipeline>
    {
        //-------------------------------------------------------------------------------------
        //                                      Static/Constants
        //-------------------------------------------------------------------------------------
        private static CullingGroup _cullingGroup;

        private static BoundingSphere[] _boundingSpheres;
        //-------------------------------------------------------------------------------------
        //                                      Serializable Variables
        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------
        //                                      Variables/Properties
        //-------------------------------------------------------------------------------------
        private int _lightCullingIndex = -1;

        //-------------------------------------------------------------------------------------
        //                                      Unity Lifecycle Functions
        //-------------------------------------------------------------------------------------
        protected override void OnEnable()
        {
            Lights.Add(this);
            if (_cullingGroup == null)
            {
                _cullingGroup = new CullingGroup();
                RenderPipelineManager.beginCameraRendering += SetupCulling;
            }
        }

        protected override void OnDisable()
        {
            Lights.Remove(this);
            if (_cullingGroup != null && Lights.Count < 1)
            {
                _cullingGroup.Dispose();
                _cullingGroup = null;
                RenderPipelineManager.beginCameraRendering -= SetupCulling;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _lightType = SupportedLightTypes(_lightType) ? _lightType : Light2DType.Point;
        }

        //-------------------------------------------------------------------------------------
        //                                      
        //-------------------------------------------------------------------------------------

        public override bool IsLightVisible()
        {
            return _cullingGroup == null || _cullingGroup.IsVisible(_lightCullingIndex);
        }

        protected override bool SupportedLightTypes(Light2DType type)
        {
            Light2DType[] allowed = {Light2DType.Point, Light2DType.Spot, Light2DType.Global};
            return allowed.Contains(type);
        }

        //-------------------------------------------------------------------------------------
        //                                      Member Functions
        //-------------------------------------------------------------------------------------

        public new Vector4 LightData()
        {
            var angle = Mathf.Atan2(transform.right.y, transform.right.x);

            return new Vector4(transform.position.x, transform.position.y, angle, 360 * Mathf.Deg2Rad * 0.5f);
        }

        private BoundingSphere GetBoundingSphere()
        {
            var position = transform.position;

            if (this._lightType == Light2DType.Point)
                return new BoundingSphere(new Vector3(position.x, position.y), _range);
            if (this._lightType == Light2DType.Spot
                || this._lightType == Light2DType.Global)
                return new BoundingSphere(new Vector3(position.x, position.y) + transform.forward * _range / 2,
                    Mathf.Max(Mathf.Sin(_spotAngle) * _range, _range));

            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------------------
        //                                      Static Functions
        //-------------------------------------------------------------------------------------

        internal static void SetupCulling(ScriptableRenderContext scriptableRenderContext, Camera camera)
        {
            if (_cullingGroup == null) return;
            _cullingGroup.targetCamera = camera;

            if (_boundingSpheres == null || Lights.Count > _boundingSpheres.Length)
                _boundingSpheres = new BoundingSphere[Mathf.CeilToInt(Lights.Count / 1024f) * 1024];

            int currentCullingIndex = 0;
            for (var lightIndex = 0; lightIndex < Lights.Count; lightIndex++)
            {
                Light2D_Pipeline light = Lights[lightIndex];
                if (light == null) continue;

                _boundingSpheres[lightIndex] = light.GetBoundingSphere();
                light._lightCullingIndex = currentCullingIndex++;
            }

            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(currentCullingIndex);
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
    }
}