using System;
using System.Security.Cryptography;
using UnityEngine;

namespace LightingTwoD.Core
{
    public static class Vector2Math
    {
        public const float epsilon = 1f/1024f;
        
        public static Vector2 ClosestPointOnEdge(Vector2 edgePointA, Vector2 edgePointB, Vector2 point) {
            var ap = point - edgePointA;
            var ab = edgePointB - edgePointA;

            var ab2 = ab.sqrMagnitude;
            var dot = Vector2.Dot(ap, ab);

            var t = Mathf.Clamp01(dot / ab2);

            return edgePointA + ab * t;
        }

        public static int FarthestPointOnEdge(Vector2 edgePointA, Vector2 edgePointB, Vector2 point, float range,
            ref Vector2[] outVectors
        ) {
            bool aIsCloser = (edgePointA - point).sqrMagnitude < (edgePointB - point).sqrMagnitude;
            
            var p = aIsCloser ? edgePointA : edgePointB;
            var dun = (edgePointB - edgePointA);
            var d = dun.normalized;
            if (!aIsCloser) d *= -1;
            var q = point;

            var a = Vector2.Dot(d, d);
            var b = 2 * Vector2.Dot(d, p - q);
            var c = Vector2.Dot(p - q, p - q) - range * range;

            var t1 = (-b + Mathf.Sqrt((b * b) - 4 * a * c)) / (2 * a);
            var t2 = (-b - Mathf.Sqrt((b * b) - 4 * a * c)) / (2 * a);

            var t = float.IsNaN(t1) ? t2 : t1;

            var duns = dun.sqrMagnitude;
            
            if(outVectors.Length < 2) outVectors = new Vector2[2];
            int amount = 0;
            if (!float.IsNaN(t1) && t1 > epsilon && (t1*t1) < duns + epsilon) {
                outVectors[amount++] = p + t1 * d;
            }
            if (!float.IsNaN(t2) && t2 > epsilon && (t2*t2) < duns + epsilon) {
                outVectors[amount++] = p + t2 * d;
            }
            return amount;
        }

        
//        
//        public static bool Intersect(Vector2 edgePointA, Vector2 edgePointB, Vector2 dir, out float distance) {
//            distance = float.MaxValue;
//            var edgeDir = edgePointB - edgePointA;
//
//            var perp = new Vector2(-dir.y, dir.x);
//            var d = -Vector2.Dot(edgeDir, perp);
//            if (d < Epsilon && d > -Epsilon) {
//                return false;
//            }
//            var h = (Vector2.Dot(edgePointA, perp)) / d;
//            if (h < 0 - Epsilon || h > 1 + Epsilon) return false;
//            if ((edgePointA + edgeDir * h).x / dir.x <= 0 && (edgePointA + edgeDir * h).y / dir.y <= 0) return false;
//            distance = Vector2.Distance(edgePointA + edgeDir * h, Vector2.zero);
//            return true;
//        }


        public static float Intersect(Vector2 L1P1, Vector2 L1P2, Vector2 L2P1, Vector2 L2P2)
        {
            float C(Vector2 P1, Vector2 P2, out Vector2 Perp)
            {
                Perp = new Vector2(P2.y - P1.y, P1.x - P2.x);
                return Vector2.Dot(Perp, P1);
            }

            float L1C = C(L1P1, L1P2, out Vector2 L1Perp);
            float L2C = C(L2P1, L2P2, out Vector2 L2Perp);

            float det = Vector2.Dot(L1P2 - L1P1, L2Perp);

            if (Mathf.Abs(det) < epsilon) // parallel
                return 0.0f;

            // there should be an easier way
            float x = (   L2Perp.y * L1C - L1Perp.y * L2C) / det;
            float y = ( - L2Perp.x * L1C + L1Perp.x * L2C) / det;

            if (Mathf.Min(L2P1.x, L2P2.x) - x > epsilon) return 0.0f;
            if (Mathf.Max(L2P1.x, L2P2.x) - x < -epsilon) return 0.0f;
            
            if (Mathf.Min(L2P1.y, L2P2.y) - y > epsilon) return 0.0f;
            if (Mathf.Max(L2P1.y, L2P2.y) - y < -epsilon) return 0.0f;

            return Vector2.Dot(L2P1 - L1P1, L2Perp) / det;
        }


//        public static float Intersect(Vector2 lineOneStart, Vector2 lineOneEnd, Vector2 lineTwoStart, Vector2 lineTwoEnd)
//        {
//            Vector2 line2Perp = new Vector2(lineTwoEnd.y - lineTwoStart.y, lineTwoStart.x - lineTwoEnd.x);
//            float det = Vector2.Dot(lineOneEnd - lineOneStart, line2Perp);
//
//            if (Mathf.Abs(det) < 1e-10)
//                return 0.0f;
//
//            float t1 = Vector2.Dot(lineTwoStart-lineOneStart,line2Perp ) / det;
//            
//            
//            
//            // if(Mathf.Min(lineTwoStart.x, lineTwoEnd.x)  
//            
//            return t1;
//        }
        

        public static float ToPolarAngle(Vector2 cartesian, Vector2 center)
        {
            Vector2 d = cartesian - center;
            return Mathf.Atan2(d.y, d.x);
        }

        public static Vector2 ToPolar(Vector2 cartesian, Vector2 center)
        {
            Vector2 d = cartesian - center;
            return new Vector2(Mathf.Atan2(d.y, d.x), d.magnitude);
        }


    }
    
}