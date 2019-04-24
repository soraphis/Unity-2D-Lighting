using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace LightingTwoD.Core {
    public static class GeometryCollector {
        public class StaticData {
            public List<Vector3> staticEdges = new List<Vector3>();
            public List<Vector2> staticUvs = new List<Vector2>();
            public List<Color> staticColors = new List<Color>();
        }

        private static List<Vector3> edges = new List<Vector3>();
        private static List<Vector2> uvs = new List<Vector2>();
        private static List<Color> colors = new List<Color>();
        private static int[] indices;

        public static void CollectStatic(Mesh mesh, StaticData data) {
            mesh.MarkDynamic();
            data.staticEdges.Clear();
            data.staticUvs.Clear();
            data.staticColors.Clear();
            // do what you have to do...
            var renderers = Object.FindObjectsOfType<Renderer>().ToList();
            renderers.RemoveAll(r => r is MeshRenderer); // we don't use those here
            renderers.RemoveAll(r => !r.gameObject.isStatic);

            for (var i = renderers.Count - 1; i >= 0; i--) {
                var ren = renderers[i];
                if (ren.gameObject == null) {
                    // GO was deleted in runtime
                    renderers.RemoveAt(i);
                    continue;
                }

                if (ren is SpriteRenderer) {
                    Update_SpriteRenderer(ren as SpriteRenderer, data.staticEdges, data.staticColors, data.staticUvs);
                } else if (ren is TilemapRenderer) {
                    var collider = ren.GetComponent<TilemapCollider2D>();

                    if (collider != null) {
                        if (collider.usedByComposite) {
                            Update_CompositeCollider(ren as TilemapRenderer, collider.composite, data.staticEdges, data.staticColors, data.staticUvs);
                        } else {
                            // todo: no support for tilemaps without composite collider
                            // it would be way too inefficient 
                        }
                    }
                }
            }
        }

        public static void CollectDynamic(Mesh mesh, StaticData data) {
            mesh.Clear();
            edges = data.staticEdges.ToList();
            uvs = data.staticUvs.ToList();
            colors = data.staticColors.ToList();
            var shadowcasters = Runtime2DShadowcaster.Instances;
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                shadowcasters = Object.FindObjectsOfType<Runtime2DShadowcaster>().ToList();
            }
#endif
            foreach (var shadowcaster in shadowcasters) {
                var ren = shadowcaster.renderer;

                if (ren is SpriteRenderer) {
                    Update_SpriteRenderer(ren as SpriteRenderer, edges, colors, uvs);
                } else if (ren is TilemapRenderer) {
                    var collider = shadowcaster.collider;
                    if (collider == null) {
                        continue;
                    }

                    if (collider.usedByComposite) {
                        Update_CompositeCollider(ren as TilemapRenderer, collider.composite, edges, colors, uvs);
                    } else {
                        // todo: no support for tilemaps without composite collider
                        // it would be way too inefficient 
                    }
                }
            }

            if (indices == null || indices.Length != edges.Count) {
                indices = new int[edges.Count];
            }

            for (int i = 0; i < edges.Count; i++) { indices[i] = i; }

            mesh.SetVertices(edges);
            // mesh.SetNormals();
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
        }

        #region ProcessDifferentRenderers

        private static void Update_SpriteRenderer(SpriteRenderer ren, List<Vector3> edges, List<Color> colors, List<Vector2> uvs) {
            byte layer = (byte) ren.gameObject.layer;
            var transform = ren.transform;
            if (ren.sprite == null) return;

            for (int shape_i = 0; shape_i < ren.sprite.GetPhysicsShapeCount(); ++shape_i) {
                var shape = new List<Vector2>(ren.sprite.GetPhysicsShapePointCount(shape_i));
                ren.sprite.GetPhysicsShape(shape_i, shape);

                for (int i = 0; i < shape.Count; ++i) {
                    var k = (i + 1) % shape.Count;
                    edges.Add(transform.TransformPoint(shape[i]));
                    edges.Add(transform.TransformPoint(shape[k])); // this ...

                    uvs.Add(transform.TransformPoint(shape[k]));
                    uvs.Add(transform.TransformPoint(shape[i])); // ... this ...

                    colors.Add(new Color32(layer, 0, 0, 1));
                    colors.Add(new Color32(layer, 0, 0, 1)); //... and this, seem redundant
                }
            }
        }

        private static void Update_CompositeCollider(TilemapRenderer ren, CompositeCollider2D collider, List<Vector3> edges, List<Color> colors, List<Vector2> uvs) {
            byte layer = (byte) ren.gameObject.layer;
            var transform = ren.transform;

            for (int pathIdx = 0; pathIdx < collider.pathCount; ++pathIdx) {
                var pointCount = collider.GetPathPointCount(pathIdx);
                var _shape = new Vector2[pointCount];
                collider.GetPath(pathIdx, _shape);
                var shape = (IList) _shape;

                for (int i = 0; i < pointCount; ++i) {
                    var k = (i + 1) % pointCount;
                    AddEdge(i, k, layer, transform, ref shape, edges, colors, uvs);
                }
            }
        }

        #endregion

        private static void AddEdge(int i, int k, byte layer, Transform transform, ref IList shape, List<Vector3> edges, List<Color> colors, List<Vector2> uvs) {
            edges.Add(transform.TransformPoint((Vector2) shape[i]));
            edges.Add(transform.TransformPoint((Vector2) shape[k])); // this ...

            uvs.Add(transform.TransformPoint((Vector2) shape[k]));
            uvs.Add(transform.TransformPoint((Vector2) shape[i])); // ... this ...

            colors.Add(new Color(layer, 0, 0, 0));
            colors.Add(new Color(layer, 0, 0, 0));
        }
    }
}