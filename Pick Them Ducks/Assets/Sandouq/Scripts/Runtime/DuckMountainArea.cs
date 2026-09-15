using UnityEngine;

namespace Sandouq.Ducks
{
    // Authored surfaces, not a runtime terrain/prefab generator.
    public sealed class DuckMountainArea : MonoBehaviour
    {
        public float footprintRadius = 34;
        public Collider[] walkingSurfaces = System.Array.Empty<Collider>();
        public Transform[] trailWaypoints = System.Array.Empty<Transform>();
        public Transform caveEntrance, cavePile, summitPile;
        public bool Contains(Vector3 point)
        {
            var local = transform.InverseTransformPoint(point);
            return new Vector2(local.x, local.z).sqrMagnitude < footprintRadius * footprintRadius;
        }
        public float SupportHeight(Vector3 point, float fallback)
        {
            if (!Contains(point)) return fallback;
            var ray = new Ray(point + Vector3.up * .6f, Vector3.down);
            foreach (var surface in walkingSurfaces)
                if (surface != null && surface.enabled && surface.Raycast(ray, out var hit, 100)) fallback = Mathf.Max(fallback, hit.point.y);
            return fallback;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, .78f, 0);
            for (int i = 1; i < trailWaypoints.Length; i++)
                if (trailWaypoints[i - 1] != null && trailWaypoints[i] != null) Gizmos.DrawLine(trailWaypoints[i - 1].position + Vector3.up * .2f, trailWaypoints[i].position + Vector3.up * .2f);
        }
    }
}
