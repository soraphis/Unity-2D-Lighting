using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LightingTwoD.Core {


    public abstract class Light2D : MonoBehaviour {
        public enum Light2DType {
            Spot = 0,
            Global = 1,
            Point = 2,
            Area = 3,
            Line = 4,
        }
    }

    public abstract class Light2D<T> : Light2D where T : Light2D{
        
        //-------------------------------------------------------------------------------------
        //                                      Static/Constants
        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------
        //                                      Serializable Variables
        //-------------------------------------------------------------------------------------
        public static List<T> Lights = new List<T>();
        
        [ConditionalEnum(nameof(SupportedLightTypes))]
        [SerializeField] protected Light2DType _lightType = Light2DType.Point;
        
        [SerializeField] protected float _range = 10f;
        [SerializeField][Range(1, 179)] protected float _spotAngle = 45f;
        [SerializeField] protected float _intensity = 1f;
        [SerializeField] protected Color _color = Color.white;
        
        [SerializeField] protected Tilemap.Orientation _2DOrientation = Tilemap.Orientation.XY; // for later usage
        
        //-------------------------------------------------------------------------------------
        //                                      Variables/Properties
        //-------------------------------------------------------------------------------------
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
            _range = Mathf.Max(0, _range);
        }

        //-------------------------------------------------------------------------------------
        //                                      Member Functions
        //-------------------------------------------------------------------------------------

        protected virtual bool SupportedLightTypes(Light2DType type)
        {
            return true;
        }
        
        public Vector4 LightData() {
            var angle = Mathf.Atan2(transform.right.y, transform.right.x);
            
            return new Vector4(transform.position.x, transform.position.y, angle, 360 * Mathf.Deg2Rad * 0.5f); 
        }

        public abstract bool IsLightVisible(); 
        
        private BoundingSphere GetBoundingSphere() {
            var position = transform.position;
            
            if (this._lightType == Light2DType.Point) return new BoundingSphere(new Vector3(position.x, position.y), _range);
            if (this._lightType == Light2DType.Spot
             || this._lightType == Light2DType.Global) 
                return new BoundingSphere(new Vector3(position.x, position.y) + transform.forward * _range/2, 
                    Mathf.Max(Mathf.Sin(_spotAngle) * _range, _range));
            
            throw new NotImplementedException();
        }
        
        protected virtual void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
       
    }
}