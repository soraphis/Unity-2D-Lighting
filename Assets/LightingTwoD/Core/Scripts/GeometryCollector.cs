using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LightingTwoD.Core
{
    public static class GeometryCollector
    {
        public class StaticData
        {
            public readonly List<Vector3> StaticEdges = new List<Vector3>();
            public readonly List<Vector2> StaticUvs = new List<Vector2>();
            public readonly List<Color> StaticColors = new List<Color>();
        }

        //-------------------------------------------------------------------------------------
        //                       Static Caching Variables to reduce GC
        //-------------------------------------------------------------------------------------
        
        private static List<Vector3> _edges = new List<Vector3>();
        private static List<Vector2> _uvs = new List<Vector2>();
        private static List<Color> _colors = new List<Color>();
        private static int[] _indices;

        private static List<Vector2> _physicShape;
        
        //-------------------------------------------------------------------------------------
        //                                      Public Methods
        //-------------------------------------------------------------------------------------
        
        
        public static void CollectStatic(Mesh mesh, StaticData data)
        {
            mesh.MarkDynamic();
            data.StaticEdges.Clear();
            data.StaticUvs.Clear();
            data.StaticColors.Clear();
            // do what you have to do...
            var renderers = Object.FindObjectsOfType<Renderer>().ToList();
            renderers.RemoveAll(r => r is MeshRenderer); // we don't use those here
            renderers.RemoveAll(r => !r.gameObject.isStatic);

            for (var i = renderers.Count - 1; i >= 0; i--)
            {
                var ren = renderers[i];
                if (ren.gameObject == null)
                {
                    continue; // GO was deleted in runtime
                }
                Collect(ren, data.StaticEdges,data.StaticColors,  data.StaticUvs);
            }
        }

        

        public static void CollectDynamic(Mesh mesh, StaticData data)
        {
            mesh.Clear();
            if (_edges.Count >= data.StaticEdges.Count)
            {
                _edges.RemoveRange(data.StaticEdges.Count, _edges.Count - data.StaticEdges.Count);
                _uvs.RemoveRange(data.StaticUvs.Count, _uvs.Count - data.StaticUvs.Count);
                _colors.RemoveRange(data.StaticColors.Count, _colors.Count - data.StaticColors.Count);
            }
            else
            {
                _edges = data.StaticEdges.ToList();
                _uvs = data.StaticUvs.ToList();
                _colors = data.StaticColors.ToList();
            }

            var shadowcasters = Runtime2DShadowcaster.Instances;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                shadowcasters = Object.FindObjectsOfType<Runtime2DShadowcaster>().ToList();
            }
#endif
            foreach (var shadowcaster in shadowcasters)
            {
                var ren = shadowcaster.renderer;

                Collect(ren, _edges, _colors, _uvs);
            }

            if (_indices == null || _indices.Length != _edges.Count)
            {
                _indices = new int[_edges.Count];
            }

            for (int i = 0; i < _edges.Count; i++)
            {
                _indices[i] = i;
            }

            mesh.SetVertices(_edges);
            // mesh.SetNormals();
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);
            mesh.SetIndices(_indices, MeshTopology.Lines, 0);
        }

        //-------------------------------------------------------------------------------------
        //                                Helper Methods      
        //-------------------------------------------------------------------------------------
        
        private static void Collect(Renderer ren, List<Vector3> edges, List<Color> colors, List<Vector2> uvs)
        {
            switch (ren)
            {
                case SpriteRenderer spriteRenderer:
                    Update_SpriteRenderer(spriteRenderer, edges, colors, uvs);
                    break;
                case TilemapRenderer tilemapRenderer:
                {
                    var collider = tilemapRenderer.GetComponent<TilemapCollider2D>();

                    if (collider != null)
                    {
                        if (collider.usedByComposite)
                        {
                            Update_CompositeCollider(tilemapRenderer, collider.composite,  edges, colors, uvs);
                        }/*
                        else
                        {
                            // todo: support for tilemaps without composite collider
                            // but it would be way too inefficient ... so its just not supported at this time 
                        }*/
                    }

                    break;
                }
            }
        }

        private static void AddEdge(int i, int k, byte layer, Transform transform, List<Vector2> shape,
            List<Vector3> edges, List<Color> colors, List<Vector2> uvs)
        {
            edges.Add(transform.TransformPoint(shape[i]));
            edges.Add(transform.TransformPoint(shape[k]));

            uvs.Add(transform.TransformPoint(shape[k]));
            uvs.Add(transform.TransformPoint(shape[i]));

            colors.Add(new Color(layer, 0, 0, 0));
            colors.Add(new Color(layer, 0, 0, 0));
        }
        

        //-------------------------------------------------------------------------------------
        //                       Supported Renderer-Types Specific Implementations
        //-------------------------------------------------------------------------------------
        
        #region ProcessDifferentRenderers

        private static void Update_SpriteRenderer(SpriteRenderer ren, List<Vector3> edges, List<Color> colors,
            List<Vector2> uvs)
        {
            byte layer = (byte) ren.gameObject.layer;
            var transform = ren.transform;
            if (ren.sprite == null) return;

            if (_physicShape == null)
            {
                _physicShape = new List<Vector2>(32);
            }

            for (int shapeIdx = 0; shapeIdx < ren.sprite.GetPhysicsShapeCount(); ++shapeIdx)
            {
                _physicShape.Clear();
                ren.sprite.GetPhysicsShape(shapeIdx, _physicShape);

                for (int i = 0; i < _physicShape.Count; ++i)
                {
                    var k = (i + 1) % _physicShape.Count;

                    AddEdge(i, k, layer, transform, _physicShape, edges, colors, uvs);
                }
            }
        }

        private static void Update_CompositeCollider(TilemapRenderer ren, CompositeCollider2D collider,
            List<Vector3> edges, List<Color> colors, List<Vector2> uvs)
        {
            byte layer = (byte) ren.gameObject.layer;
            var transform = ren.transform;

            if (_physicShape == null)
            {
                _physicShape = new List<Vector2>(32);
            }

            for (int pathIdx = 0; pathIdx < collider.pathCount; ++pathIdx)
            {
                var pointCount = collider.GetPathPointCount(pathIdx);
                collider.GetPath(pathIdx, _physicShape);
                for (int i = 0; i < pointCount; ++i)
                {
                    var k = (i + 1) % pointCount;
                    AddEdge(i, k, layer, transform, _physicShape, edges, colors, uvs);
                }
            }
        }

        #endregion


    }
}