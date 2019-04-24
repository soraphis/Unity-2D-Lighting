using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LightingTwoD.Core {


    public abstract class Light2D : MonoBehaviour {
    }

    public abstract class Light2D<T> : Light2D where T : Light2D{
        
        public enum Light2DType {
            Spot = 0,
            Global = 1,
            Point = 2,
            Area = 3
        }
        
        //-------------------------------------------------------------------------------------
        //                                      Static/Constants
        //-------------------------------------------------------------------------------------
        
        private static CullingGroup _cullingGroup;
        private static BoundingSphere[] _boundingSpheres;

        //-------------------------------------------------------------------------------------
        //                                      Serializable Variables
        //-------------------------------------------------------------------------------------
        public static List<T> Lights = new List<T>();
        [SerializeField] protected Light2DType _lightType = Light2DType.Point;
        [SerializeField] protected float _range = 10f;
        [SerializeField][Range(1, 179)] protected float _spotAngle = 45f;
        [SerializeField] protected float _intensity = 1f;
        [SerializeField] protected Color _color = Color.white;
        
        [SerializeField] protected Tilemap.Orientation _2DOrientation = Tilemap.Orientation.XY; // for later usage
        
        //-------------------------------------------------------------------------------------
        //                                      Variables/Properties
        //-------------------------------------------------------------------------------------
        private int _lightCullingIndex = -1;

        public Light2DType LightType => _lightType;
        public float Range => _range;
        public float SpotAngle => _spotAngle;
        public float Intensity => _intensity;
        public Color Color => _color;
        

        //-------------------------------------------------------------------------------------
        //                                      Unity Lifecycle Functions
        //-------------------------------------------------------------------------------------

        protected abstract void OnEnable();
        protected abstract void OnDisable();

        protected virtual void OnValidate() {
            _2DOrientation = Tilemap.Orientation.XY; // fixme: others currently not supported
        }

        //-------------------------------------------------------------------------------------
        //                                      Member Functions
        //-------------------------------------------------------------------------------------

        public Vector4 LightData() {
            var angle = Mathf.Atan2(transform.right.y, transform.right.x);
            
            return new Vector4(transform.position.x, transform.position.y, angle, 360 * Mathf.Deg2Rad * 0.5f); 
        }

        public bool IsLightVisible() {
            return _cullingGroup == null || _cullingGroup.IsVisible(_lightCullingIndex);
        }
        
        private BoundingSphere GetBoundingSphere() {
            var position = transform.position;
            
            if (this._lightType == Light2DType.Point) return new BoundingSphere(new Vector3(position.x, position.y), _range);
            if (this._lightType == Light2DType.Spot
             || this._lightType == Light2DType.Global) 
                return new BoundingSphere(new Vector3(position.x, position.y) + transform.forward * _range/2, 
                    Mathf.Max(Mathf.Sin(_spotAngle) * _range, _range));
            
            throw new NotImplementedException();
        }
        
        //-------------------------------------------------------------------------------------
        //                                      Static Functions
        //-------------------------------------------------------------------------------------

        /*internal static void SetupCulling(Camera camera) {
            if(_cullingGroup == null) return;
            _cullingGroup.targetCamera = camera;
         
            if(_boundingSpheres == null || Lights.Count > _boundingSpheres.Length)
                _boundingSpheres = new BoundingSphere[Mathf.CeilToInt(Lights.Count/1024f) * 1024];

            int currentCullingIndex = 0;
            for (var lightIndex = 0; lightIndex < Lights.Count; lightIndex++) {
                Light2D light = Lights[lightIndex];
                if(light == null) continue;

                _boundingSpheres[lightIndex] = light.GetBoundingSphere();
                light._lightCullingIndex = currentCullingIndex++;

            }
                
            _cullingGroup.SetBoundingSpheres(_boundingSpheres);
            _cullingGroup.SetBoundingSphereCount(currentCullingIndex);
            
        }*/
        
        protected virtual void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
       
    }
}