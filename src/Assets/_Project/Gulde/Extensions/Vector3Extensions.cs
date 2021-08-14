using UnityEngine;

namespace Gulde.Extensions
{
    public static class Vector3Extensions
    {

        public static Vector3Int ToCell(this Vector3 vector) =>
            new Vector3Int(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y), Mathf.FloorToInt(vector.z));

        public static float DistanceTo(this Vector3 vector, Vector3 other) =>
            (vector - other).magnitude;

        public static Vector3 DirectionTo(this Vector3 vector, Vector3 other) =>
            (other - vector).normalized;
    }
}