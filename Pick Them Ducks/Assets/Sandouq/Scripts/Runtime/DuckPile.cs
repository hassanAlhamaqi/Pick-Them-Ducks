using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckPile : MonoBehaviour
    {
        [Min(1)] public int minimumDucks = 150;
        [Min(1)] public int maximumDucks = 250;
        public int countSeed = 1;
        public DuckPlacement[] SelectedPlacements(int worldSeed)
        {
            var slots = GetComponentsInChildren<DuckPlacement>();
            Array.Sort(slots, (a, b) =>
            {
                var pa = transform.InverseTransformPoint(a.transform.position);
                var pb = transform.InverseTransformPoint(b.transform.position);
                int layer = Mathf.RoundToInt(pa.y * 100).CompareTo(Mathf.RoundToInt(pb.y * 100));
                if (layer != 0) return layer;
                int radius = pa.sqrMagnitude.CompareTo(pb.sqrMagnitude);
                return radius != 0 ? radius : a.duckId.CompareTo(b.duckId);
            });
            int high = Mathf.Min(slots.Length, Mathf.Max(minimumDucks, maximumDucks));
            int low = Mathf.Clamp(minimumDucks, 0, high);
            int count = new System.Random(unchecked(worldSeed ^ countSeed * 397)).Next(low, high + 1);
            Array.Resize(ref slots, count);
            return slots;
        }

        // Fill every layer, then the centre of the last layer. Never distribute only on a cone surface.
        public static List<Vector3> SolidOffsets(int count, float spacing = .48f, float layerHeight = .36f)
        {
            count = Mathf.Max(1, count);
            float radius = Mathf.Max(spacing, Mathf.Pow(count * spacing * spacing * layerHeight * 3 / Mathf.PI, 1f / 3));
            var points = new List<Vector3>();
            for (;; radius += spacing * .1f)
            {
                points.Clear();
                for (int layer = 0; layer * layerHeight < radius; layer++)
                {
                    float r = radius - layer * layerHeight;
                    int extent = Mathf.CeilToInt(r / spacing);
                    float shift = (layer % 2) * spacing * .5f;
                    var row = new List<Vector3>();
                    for (int z = -extent; z <= extent; z++) for (int x = -extent; x <= extent; x++)
                    {
                        var p = new Vector3(x * spacing + shift, layer * layerHeight, z * spacing + shift);
                        if (p.x * p.x + p.z * p.z <= r * r) row.Add(p);
                    }
                    row.Sort((a, b) => { int d = a.sqrMagnitude.CompareTo(b.sqrMagnitude); if (d != 0) return d; d = a.x.CompareTo(b.x); return d != 0 ? d : a.z.CompareTo(b.z); });
                    points.AddRange(row);
                }
                if (points.Count >= count) { points.RemoveRange(count, points.Count - count); return points; }
            }
        }
    }
}
