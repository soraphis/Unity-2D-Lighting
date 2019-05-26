using System;
using System.Collections.Generic;
using System.Linq;
using LightingTwoD.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Resolution = LightingTwoD.Core.Resolution;

[ExecuteInEditMode]
public class Light2D_MG : Light2D<Light2D_MG>
{
    //-------------------------------------------------------------------------------------
    //                                      Serializable Variables
    //-------------------------------------------------------------------------------------
    
    
    [SerializeField] private Shader _shader;
    [SerializeField] private Resolution _minRayCount;
    [SerializeField] private Texture2D _attenuationTexture;
    //-------------------------------------------------------------------------------------
    //                                      Variables/Properties
    //-------------------------------------------------------------------------------------
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly GeometryCollector.StaticData StaticData = new GeometryCollector.StaticData();
    
    private static Mesh _geometry;
    private static bool _updated;
    
    private Material _material;
    private Mesh _lightMesh;
    
    private List<Vector4> validEdges = new List<Vector4>();
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> indices = new List<int>();
    private List<Vector3> geometryVertices = new List<Vector3>();
    private List<Vector2> geometryUvs = new List<Vector2>();
    private Vector2[] outVectors = new Vector2[2];
    
    //-------------------------------------------------------------------------------------
    //                                      Unity Lifecycle Functions
    //-------------------------------------------------------------------------------------

    private void Awake() { Init(); }

    protected override void OnValidate() {
        base.OnValidate();
        if (_lightType != Light2DType.Spot) {
            _spotAngle = 360;
        } else {
            _spotAngle = Mathf.Clamp(_spotAngle, 0, 179);
        }
    }

    protected override void OnEnable()
    {
        Lights.Add(this);
    }

    protected override void OnDisable()
    {
        Lights.Remove(this);
    }

    private void Update()
    {
        _updated = false;
    }

    private void LateUpdate()
    {
        StaticUpdate();

        UpdateLightMesh();
        
        _material.SetColor(ColorId, _color);
        _material.SetTexture(MainTexId, _attenuationTexture);
        _material.SetFloat(IntensityId, _intensity);
        Graphics.DrawMesh(_lightMesh, Matrix4x4.identity, _material, 0);
    }

    private void OnDrawGizmosSelected()
    {
        if(_lightMesh == null) return;
        var v = _lightMesh.vertices;
        var pos = transform.position;

        Gizmos.color = Color.red;
        Handles.color = Color.blue;
        for (var i = 0; i < v.Length; i++)
        {
            var vector3 = v[i];
            Gizmos.DrawLine(pos, vector3);
            Handles.Label(vector3, i.ToString());
        }
    }

    //-------------------------------------------------------------------------------------
    //                                    Main Logic  
    //-------------------------------------------------------------------------------------

    private void UpdateLightMesh()
    {
        geometryVertices.Clear();
        geometryUvs.Clear();
        
        _geometry.GetVertices(geometryVertices);
        _geometry.GetUVs(0, geometryUvs);

        var position = (Vector2)transform.position;

        validEdges.Clear();
        vertices.Clear();
        uvs.Clear();
        indices.Clear();
        // /////////////////
        // Filter valid corners in geometry
        for (var i = 0; i < geometryVertices.Count; i+=2)
        {
            var pA = (Vector2)geometryVertices[i];
            var pB = geometryUvs[i];

            var closest = Vector2Math.ClosestPointOnEdge(pA, pB, position);
            if((closest - position).sqrMagnitude > _range*_range) continue;
            validEdges.Add(new Vector4(pA.x, pA.y, pB.x, pB.y));
        }
        // /////////////////
        var q1 = Quaternion.AngleAxis(0.4f, Vector3.forward);
        var q2 = Quaternion.Inverse(q1);
        // /////////////////
        // local helper functions
        void AddVertex(Vector2 point) {
            var angle = Vector2.SignedAngle((Vector2)transform.right, point - position);
            if (Mathf.Abs(angle ) > _spotAngle/2f + 0.2) return;
            
            vertices.Add(point);
        }
        
        float GeneratePoint(Vector2 dir)
        {
            float f = 1.0f;
            //var f = Vector2Math.Intersect(pA, pB, position, position + dir * _range);
            foreach(var point2 in validEdges)
            {
                var p2A = new Vector2(point2.x, point2.y);
                var p2B = new Vector2(point2.z, point2.w);
                var f2 = Vector2Math.Intersect(position, position + dir *_range, p2A, p2B);

                if (f2 < 1e-5) continue; 
                
                f = Mathf.Min(f, f2);
            }
            AddVertex(position + f * _range * dir);
            return f;
        }
        
        void SmoothCircle(Vector2 pA, Vector2 pB) {
            var amount = Vector2Math.FarthestPointOnEdge(pA, pB, position, _range, ref outVectors);
            for (int i = 0; i < amount; ++i) {
                var dir2 = outVectors[i] - position;
                var f = GeneratePoint(dir2);
                //AddVertex(position + f * _range * dir2);
            }
        }
        // /////////////////
        // Add base rays, in the case that no obstacle is there
        var degree = (_spotAngle / (int) _minRayCount);
        for (int i = 0; i < (int)_minRayCount; ++i) {
            
            float j = i - (int) _minRayCount / 2f;
            
            var dir = (Vector2)(Quaternion.AngleAxis(j * degree, Vector3.forward) * transform.right);
            var f1 = GeneratePoint(dir);
            // AddVertex(position + f1 * _range * dir);
        }
        
        
        // /////////////////
        // Send a ray to each geometry vertex in range
        foreach(var point in validEdges)
        {
            var pA = new Vector2(point.x, point.y);
            var pB = new Vector2(point.z, point.w);

            var dir = (pA - position).normalized;

            var f1 = GeneratePoint(dir);

            if (f1 > 0.999) { 
                SmoothCircle(pA, pB);
            }else{
                var f = GeneratePoint(q1 * dir);
                bool b = f < 0.999;
                f = GeneratePoint(q2 * dir);
                b |= f < 0.999;
                if (b) {
                    SmoothCircle(pA, pB);
                }    
            }
        }
        // /////////////////
        // 
        vertices.Sort((v1, v2) => 
            Vector2.SignedAngle((Vector2)transform.right, (Vector2)v1 - position).CompareTo(
                Vector2.SignedAngle((Vector2)transform.right, (Vector2)v2 - position)
                )
        );
        vertices.Insert(0, position);
        uvs.Add(new Vector2(0, 0));
        for (var i = 1; i < vertices.Count; i++)
        {
                var distance_n = ((Vector2)vertices[i] - position).magnitude / (_range);
                uvs.Add(new Vector2(distance_n, 0));
                
                indices.Add(0);
                indices.Add(i);
                indices.Add(i - 1);
                
        }
        if (_lightType != Light2DType.Spot)
        {
            indices.Add(0);
            indices.Add(1);
            indices.Add(vertices.Count - 1);
        }
        
        _lightMesh.Clear();
        _lightMesh.SetVertices(vertices);
        _lightMesh.SetTriangles(indices, 0);
        _lightMesh.SetUVs(0, uvs);
    }
    
    //-------------------------------------------------------------------------------------
    //                                      
    //-------------------------------------------------------------------------------------


    private void StaticUpdate()
    {
        if(_updated) return;
        if(!Application.isPlaying && _geometry == null) Init();
        if(!Application.isPlaying) GeometryCollector.CollectStatic(_geometry, StaticData);
        
        GeometryCollector.CollectDynamic(_geometry, StaticData);
        _updated = true;
    }
    
    private void Init()
    {
        if (! _updated) // static init:
        {
            _geometry = new Mesh();
            GeometryCollector.CollectStatic(_geometry, StaticData);
            _updated = true;
        }
        
        _material = new Material(_shader);
        
        _lightMesh = new Mesh();
        _lightMesh.MarkDynamic();
    }
    
    protected override bool SupportedLightTypes(Light2DType type)
    {
        Light2DType[] allowed = {Light2DType.Point , Light2DType.Spot, /*Light2DType.Line*/};
        return allowed.Contains(type);
    }

    public override bool IsLightVisible()
    {
        return true;
    }
}
