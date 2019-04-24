using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LightingTwoD.Core {
    
    [RequireComponent(typeof(Renderer))]
    public sealed class Runtime2DShadowcaster : MonoBehaviour {
        public readonly static List<Runtime2DShadowcaster> Instances = new List<Runtime2DShadowcaster>();
        [HideInInspector] public new Renderer renderer;
        [HideInInspector] public new Collider2D collider;

        private void Reset() {
            renderer = GetComponent<Renderer>();
            if (collider == null && renderer is TilemapRenderer) collider = GetComponent<Collider2D>();
        }

        private void OnEnable() {
            renderer = GetComponent<Renderer>();
            if (renderer is MeshRenderer) {
                throw new NotSupportedException("Meshrendereres are not supported as 2D Shadowcasters");
            }

            if (collider == null && renderer is TilemapRenderer) collider = GetComponent<Collider2D>();
            Instances.Add(this);
        }
        private void OnDisable() {    Instances.Remove(this);    }
    }
}