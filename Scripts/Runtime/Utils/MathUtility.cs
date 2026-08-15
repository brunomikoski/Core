using System;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    /// <summary>
    /// Contains several useful math functions.
    /// </summary>
    public static class MathUtility
    {
        //float.epsilon is usually to small of a value to truly get rounding errors out of the way.
        //Especially in the unit values usually used in unity
        public const float EPSILON = 0.000001f;

        private static float WrapLookAngle(float angle)
        {
            angle = Mathf.DeltaAngle(0.0f, angle);

            // If the target is going straight behind us,
            // slowly translate that back to the middle.
            if (angle > 90)
                return 90 - (angle - 90);
            else if (angle < -90)
                return -90 - (angle + 90);
            else
                return angle;
        }

        private static float ClampLookAngle(float angle, float max = 45.0f)
        {
            angle = Mathf.DeltaAngle(0.0f, angle);
            return Mathf.Clamp(angle, -max, max);
        }

        public static float GetLookYaw(Transform relativeTo, Vector3 worldSpaceDirection)
        {
            // Figure out what the direction is in local space.
            Vector3 localLookDirection = relativeTo
                .InverseTransformDirection(worldSpaceDirection);
            if (localLookDirection == Vector3.zero)
                return 0f;
            return WrapLookAngle(Quaternion.LookRotation(localLookDirection).eulerAngles.y);
        }

        public static float GetLookPitch(Transform relativeTo, Vector3 worldSpaceDirection)
        {
            // Figure out what the direction is in local space.
            Vector3 localLookDirection = relativeTo
                .InverseTransformDirection(worldSpaceDirection);
            if (localLookDirection == Vector3.zero)
                return 0f;
            return ClampLookAngle(-Quaternion.LookRotation(localLookDirection).eulerAngles.x);
        }

        public static Plane CreatePlane(Vector3 a, Vector3 b, Vector3 c)
        {
            return new Plane(a, b, c);
        }

        public static Plane CreatePlane(Vector3 a, Vector3 b, Vector3 c, Vector3 preferredDirection, bool draw = false)
        {
            Plane plane = new Plane(a, b, c);
            float dot = Vector3.Dot(plane.normal, preferredDirection);

            // If the generated plane faces away from the preferred direction, flip it.
            if (dot < 0.0f)
                plane = new Plane(-plane.normal, a);

            if (draw)
            {
                Debug.DrawLine(a, b, Color.white);
                Debug.DrawLine(b, c, Color.white);
                Debug.DrawLine(a, c, Color.white);

                Debug.DrawRay((a + b + c) / 3, plane.normal * 0.1f, Color.white);
            }

            return plane;
        }

        public static bool IsPositiveOrOnPlane(this Plane plane, Vector3 position)
        {
            return (double) Vector3.Dot(plane.normal, position) + (double) plane.distance >= 0.0;
        }


        public static float GetDistanceToLine(Vector3 from, Vector3 to, Vector3 target)
        {
            Vector3 closestOnLine = GetClosestPointOnLine(from, to, target);
            return Vector3.Distance(closestOnLine, target);
        }

        public static bool IsPointOnLine(Vector3 from, Vector3 to, Vector3 target)
        {
            return GetDistanceToLine(from, to, target).Equals(0.0f);
        }

        public static Vector3 GetClosestPointOnLine(Vector3 from, Vector3 to, Vector3 target)
        {
            float projectionFraction;
            Vector3 result = GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);

            if (projectionFraction < 0.0)
                return from; // Beyond the 'v' end of the segment
            if (projectionFraction > 1.0)
                return to; // Beyond the 'w' end of the segment

            return result;
        }

        public static Vector3 GetClosestPointOnLineUnclamped(Vector3 from, Vector3 to, Vector3 target)
        {
            float projectionFraction;
            return GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);
        }

        public static Vector3 GetClosestPointOnLineUnclamped(Vector3 from, Vector3 to, Vector3 target,
            out bool beyondLine)
        {
            float projectionFraction;
            Vector3 result = GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);
            beyondLine = projectionFraction > 1 || projectionFraction < 0;
            return result;
        }

        public static Vector3 GetClosestPointOnLineUnclamped(Vector3 from, Vector3 to, Vector3 target,
            out float projectionFraction)
        {
            float lengthSqr = (from - to).sqrMagnitude;

            if (lengthSqr.Equals(0.0f))
            {
                projectionFraction = 0;
                return from;
            }

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line.
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            projectionFraction = Vector3.Dot(target - from, to - from) / lengthSqr;

            // Projection falls on the segment
            return from + projectionFraction * (to - from);
        }

        public static Vector2 GetClosestPointOnLine(Vector2 from, Vector2 to, Vector2 target)
        {
            float projectionFraction;
            Vector2 result = GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);

            if (projectionFraction < 0.0)
                return from; // Beyond the 'v' end of the segment
            if (projectionFraction > 1.0)
                return to; // Beyond the 'w' end of the segment

            return result;
        }

        public static Vector2 GetClosestPointOnLineUnclamped(Vector2 from, Vector2 to, Vector2 target)
        {
            float projectionFraction;
            return GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);
        }

        public static Vector2 GetClosestPointOnLineUnclamped(Vector2 from, Vector2 to, Vector2 target,
            out bool beyondLine)
        {
            float projectionFraction;
            Vector2 result = GetClosestPointOnLineUnclamped(from, to, target, out projectionFraction);
            beyondLine = projectionFraction > 1 || projectionFraction < 0;
            return result;
        }

        public static Vector2 GetClosestPointOnLineUnclamped(Vector2 from, Vector2 to, Vector2 target,
            out float projectionFraction)
        {
            float lengthSqr = (from - to).sqrMagnitude;

            if (lengthSqr.Equals(0.0f))
            {
                projectionFraction = 0;
                return from;
            }

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line.
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            projectionFraction = Vector2.Dot(target - from, to - from) / lengthSqr;

            // Projection falls on the segment
            return from + projectionFraction * (to - from);
        }

        public static void Encapsulate(
            ref float boundsMinX, ref float boundsMaxX, ref float boundsMinY, ref float boundsMaxY,
            ref float boundsMinZ, ref float boundsMaxZ, params Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].x < boundsMinX)
                    boundsMinX = positions[i].x;
                if (positions[i].x > boundsMaxX)
                    boundsMaxX = positions[i].x;
                if (positions[i].y < boundsMinY)
                    boundsMinY = positions[i].y;
                if (positions[i].y > boundsMaxY)
                    boundsMaxY = positions[i].y;
                if (positions[i].z < boundsMinZ)
                    boundsMinZ = positions[i].z;
                if (positions[i].z > boundsMaxZ)
                    boundsMaxZ = positions[i].z;
            }
        }

        public static Bounds ToBounds(
            float boundsMinX, float boundsMaxX, float boundsMinY, float boundsMaxY,
            float boundsMinZ, float boundsMaxZ)
        {
            Vector3 min = new Vector3(boundsMinX, boundsMinY, boundsMinZ);
            Vector3 max = new Vector3(boundsMaxX, boundsMaxY, boundsMaxZ);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        public static void FromBounds(
            Bounds bounds, out float boundsMinX, out float boundsMaxX, out float boundsMinY,
            out float boundsMaxY, out float boundsMinZ, out float boundsMaxZ)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            boundsMinX = min.x;
            boundsMinY = min.y;
            boundsMinZ = min.z;

            boundsMaxX = max.x;
            boundsMaxY = max.y;
            boundsMaxZ = max.z;
        }

        public static bool EncapsulateAndReset(
            ref float boundsMinX, ref float boundsMaxX, ref float boundsMinY, ref float boundsMaxY,
            ref float boundsMinZ, ref float boundsMaxZ, params Vector3[] positions)
        {
            bool didUpdate;

            if (positions.Length == 0)
            {
                didUpdate = boundsMinX.Approximately(0.0f) && boundsMinY.Approximately(0.0f)
                                                           && boundsMinZ.Approximately(0.0f) &&
                                                           boundsMaxX.Approximately(0.0f)
                                                           && boundsMaxY.Approximately(0.0f) &&
                                                           boundsMaxZ.Approximately(0.0f);

                boundsMinX = 0;
                boundsMinY = 0;
                boundsMinZ = 0;
                boundsMaxX = 0;
                boundsMaxY = 0;
                boundsMaxZ = 0;

                return didUpdate;
            }

            float boundsMinXNew = boundsMinX;
            float boundsMinYNew = boundsMinY;
            float boundsMinZNew = boundsMinZ;
            float boundsMaxXNew = boundsMaxX;
            float boundsMaxYNew = boundsMaxY;
            float boundsMaxZNew = boundsMaxZ;

            boundsMinXNew = positions[0].x;
            boundsMinYNew = positions[0].y;
            boundsMinZNew = positions[0].z;
            boundsMaxXNew = positions[0].x;
            boundsMaxYNew = positions[0].y;
            boundsMaxZNew = positions[0].z;

            for (int i = 1; i < positions.Length; i++)
            {
                if (positions[i].x < boundsMinXNew)
                    boundsMinXNew = positions[i].x;
                if (positions[i].x > boundsMaxXNew)
                    boundsMaxXNew = positions[i].x;
                if (positions[i].y < boundsMinYNew)
                    boundsMinYNew = positions[i].y;
                if (positions[i].y > boundsMaxYNew)
                    boundsMaxYNew = positions[i].y;
                if (positions[i].z < boundsMinZNew)
                    boundsMinZNew = positions[i].z;
                if (positions[i].z > boundsMaxZNew)
                    boundsMaxZNew = positions[i].z;
            }

            didUpdate = Mathf.Approximately(boundsMinXNew, boundsMinX)
                        && Mathf.Approximately(boundsMinYNew, boundsMinY)
                        && Mathf.Approximately(boundsMinZNew, boundsMinZ)
                        && Mathf.Approximately(boundsMaxXNew, boundsMaxX)
                        && Mathf.Approximately(boundsMaxYNew, boundsMaxY)
                        && Mathf.Approximately(boundsMaxZNew, boundsMaxZ);

            boundsMinX = boundsMinXNew;
            boundsMinY = boundsMinYNew;
            boundsMinZ = boundsMinZNew;
            boundsMaxX = boundsMaxXNew;
            boundsMaxY = boundsMaxYNew;
            boundsMaxZ = boundsMaxZNew;

            return didUpdate;
        }

        public static bool BoundsIntersect(
            float boundsMinX1, float boundsMaxX1, float boundsMinY1, float boundsMaxY1,
            float boundsMinZ1, float boundsMaxZ1,
            float boundsMinX2, float boundsMaxX2, float boundsMinY2, float boundsMaxY2,
            float boundsMinZ2, float boundsMaxZ2)
        {
            return boundsMinX1 <= boundsMaxX2 && boundsMaxX1 >= boundsMinX2 &&
                   (boundsMinY1 <= boundsMaxY2 && boundsMaxY1 >= boundsMinY2) &&
                   boundsMinZ1 <= boundsMaxZ2 && boundsMaxZ1 >= boundsMinZ2;
        }

        public static float PlanarDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public static float GetChebyshevDistance(Vector3 from, Vector3 to)
        {
            float x = to.x - from.x;
            if (x < 0)
                x *= -1;
            float y = to.y - from.y;
            if (y < 0)
                y *= -1;
            float z = to.z - from.z;
            if (z < 0)
                z *= -1;
            return x + y + z;
        }

        public static bool Approximately(Vector2 one, Vector2 two, float delta = 0.001f)
        {
            float xDiff = two.x - one.x;
            if (xDiff < 0f)
                xDiff = -xDiff;

            if (xDiff > delta)
                return false;

            float yDiff = two.y - one.y;
            if (yDiff < 0f)
                yDiff = -yDiff;

            return yDiff < delta;
        }

        public static bool Approximately(Vector3 one, Vector3 two, float delta = 0.001f)
        {
            float xDiff = two.x - one.x;
            if (xDiff < 0f)
                xDiff = -xDiff;

            if (xDiff > delta)
                return false;

            float yDiff = two.y - one.y;
            if (yDiff < 0f)
                yDiff = -yDiff;

            if (yDiff > delta)
                return false;

            float zDiff = two.z - one.z;
            if (zDiff < 0f)
                zDiff = -zDiff;

            if (zDiff > delta)
                return false;
            return true;
        }

        //http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
        public static bool CalculateLineIntersection(
            Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2,
            ref Vector2 point, ref float distance)
        {
            float denominator = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

            float absDenominator = denominator;
            if (absDenominator <= 0f)
                absDenominator = absDenominator * -1;
            if (absDenominator <= EPSILON)
            {
                return false;
            }

            float ua = ((b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x)) / denominator;
            if (ua < 0f || ua > 1f)
                return false;

            float ub = ((a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x)) / denominator;
            if (ub < 0f || ub > 1f)
                return false;

            point = new Vector2(
                a1.x + (a2.x - a1.x) * ua,
                a1.y + (a2.y - a1.y) * ua);
            distance = ua;

            return true;
        }

        public static bool DoLinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float denominator = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

            float absDenominator = denominator;
            if (absDenominator <= 0f)
                absDenominator = absDenominator * -1;
            if (absDenominator <= EPSILON)
            {
                return false;
            }

            float ua = ((b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x)) / denominator;
            if (ua < 0f || ua > 1f)
                return false;

            float ub = ((a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x)) / denominator;
            if (ub < 0f || ub > 1f)
                return false;
            return true;
        }

        public static Vector2 CalculateClosestPointOnLine(Vector2 from, Vector2 to, Vector2 p)
        {
            Vector2 fromPoint = p - from;
            Vector2 fromTo = to - from;

            float magnitudeAB = fromTo.sqrMagnitude;
            float aBAPproduct = Vector2.Dot(fromPoint, fromTo);
            float distance = aBAPproduct / magnitudeAB;

            if (distance < 0)
                return from;
            if (distance > 1)
                return to;
            return from + fromTo * distance;
        }

        public static bool IsPointNearLineSegment(Vector2 from, Vector2 to, Vector2 p, float maxDiff = 0.01f)
        {
            Vector2 closestPointOnLine = CalculateClosestPointOnLine(from, to, p);
            return (closestPointOnLine - p).sqrMagnitude <= maxDiff * maxDiff;
        }

        public static Vector2 Rotate(Vector2 vector, float degrees, Vector2 pivot = default(Vector2))
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            vector -= pivot;
            vector.x = cos * vector.x - sin * vector.y;
            vector.y = sin * vector.x + cos * vector.y;
            vector += pivot;

            return vector;
        }

        public static Vector2 Round(Vector2 vector, int decimalPlaces = 3)
        {
            return new Vector2(
                (float) Math.Round(vector.x, decimalPlaces),
                (float) Math.Round(vector.y, decimalPlaces));
        }

        public static float Max(float a, float b, float c)
        {
            return Mathf.Max(Mathf.Max(a, b), c);
        }

        public static float Min(float a, float b, float c)
        {
            return Mathf.Min(Mathf.Min(a, b), c);
        }

        public static Vector2 ClockwiseEdgeNormal(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            return new Vector2(-direction.y, direction.x).normalized;
        }

        public static Vector2 Intersection(Ray2D ray1, Ray2D ray2)
        {
            Vector2 A = ray1.origin;
            Vector2 a = ray1.direction;
            Vector2 B = ray2.origin;
            Vector2 b = ray2.direction;

            if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(a, b)), 1))
                return A + B * 0.5f;

            float u = (A.y * b.x + b.y * B.x - B.y * b.x - b.y * A.x) / (a.x * b.y - a.y * b.x);
            return A + a * u;
        }
    }
}