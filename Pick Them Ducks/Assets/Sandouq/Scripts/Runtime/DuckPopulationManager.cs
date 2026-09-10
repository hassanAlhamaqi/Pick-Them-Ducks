using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sandouq.Ducks
{
    // Fixed spatial tiles, each <= 1023 instances. Only intersecting tiles are queried.
    // Matrices are immutable except swap-removal; no population-wide per-frame updates.
    public sealed class DuckPopulationManager : MonoBehaviour
    {
        sealed class Tile
        {
            public int count;
            public int[] ids;
            public Matrix4x4[][] matrices;
            public Bounds bounds;
        }
        struct Part { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 local; }
        Tile[] tiles;
        Part[] parts;
        Vector3[] positions;
        float[] angles;
        int[] tileOf, slotOf;
        readonly List<int> removed = new List<int>();
        readonly Plane[] planes = new Plane[6];
        readonly List<Material> ownedMaterials = new List<Material>();
        Material outline;
        GameObject template;
        Camera view;
        int columns, rows, tileColumns, cellWidth;
        float spacing, minX;
        public int HoverId { get; set; } = -1;
        public int Remaining { get; private set; }
        public int Total => positions.Length;
        public int VisibleInstances { get; private set; }
        public int DrawCalls { get; private set; }
        public float Width => columns * spacing + 4;
        public float Length => rows * spacing;
        public int PartCount => parts.Length;
        public int VerticesPerDuck { get; private set; }
        public Vector3 Position(int id) => positions[id];
        public float Angle(int id) => angles[id];
        public bool IsAvailable(int id) => id >= 0 && id < Total && slotOf[id] >= 0;
        public int[] CollectedIds() => removed.ToArray();

        public void Initialize(PrototypeSettings settings, SaveData data, Camera camera)
        {
            view = camera; spacing = settings.spacing; cellWidth = Mathf.Clamp(settings.cellWidth, 2, 31);
            columns = Mathf.CeilToInt(Mathf.Sqrt(data.total)); rows = Mathf.CeilToInt((float)data.total / columns);
            minX = -columns * spacing * .5f;
            tileColumns = Mathf.CeilToInt((float)columns / cellWidth);
            int tileRows = Mathf.CeilToInt((float)rows / cellWidth);
            positions = new Vector3[data.total]; angles = new float[data.total]; tileOf = new int[data.total]; slotOf = new int[data.total];
            removed.Capacity = data.total;
            PrepareTemplate(settings);
            tiles = new Tile[tileColumns * tileRows];
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i] = new Tile { ids = new int[cellWidth * cellWidth], matrices = new Matrix4x4[parts.Length][] };
                for (int p = 0; p < parts.Length; p++) t.matrices[p] = new Matrix4x4[t.ids.Length];
            }
            var random = new System.Random(data.seed);
            for (int id = 0; id < data.total; id++)
            {
                int x = id % columns, z = id / columns;
                float worldX = minX + (x + .5f) * spacing;
                worldX += worldX < 0 ? -2 : 2; // central return lane
                var pos = new Vector3(worldX + ((float)random.NextDouble() - .5f) * spacing * .22f, 0,
                    (z + .5f) * spacing + ((float)random.NextDouble() - .5f) * spacing * .22f);
                positions[id] = pos; angles[id] = (float)random.NextDouble() * 360;
                int tileId = z / cellWidth * tileColumns + x / cellWidth;
                var tile = tiles[tileId]; int slot = tile.count++;
                tile.ids[slot] = id; tileOf[id] = tileId; slotOf[id] = slot;
                var matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0, angles[id], 0), Vector3.one);
                for (int p = 0; p < parts.Length; p++) tile.matrices[p][slot] = matrix * parts[p].local;
                var bounds = new Bounds(pos + Vector3.up * settings.duckSize * .5f, Vector3.one * settings.duckSize * 2);
                if (slot == 0) tile.bounds = bounds; else tile.bounds.Encapsulate(bounds);
            }
            Remaining = data.total;
            foreach (int id in data.collected) Remove(id);
        }

        void PrepareTemplate(PrototypeSettings settings)
        {
            template = new GameObject("Duck visual template (supplied prefab)");
            template.transform.SetParent(transform, false);
            var duck = Instantiate(settings.duckPrefab, template.transform);
            duck.transform.localPosition = Vector3.zero;
            // Keep the imported prefab's rotation: the FBX uses a different up axis.
            var renderers = duck.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float scale = settings.duckSize / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            duck.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            duck.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var list = new List<Part>();
            foreach (var r in renderers)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                VerticesPerDuck += mesh.vertexCount;
                var materials = r.sharedMaterials;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    // Same supplied material; only toggle the instancing capability on a runtime copy.
                    var mat = new Material(materials[Mathf.Min(s, materials.Length - 1)]) { enableInstancing = true };
                    ownedMaterials.Add(mat);
                    list.Add(new Part { mesh = mesh, material = mat, submesh = s, local = r.localToWorldMatrix });
                }
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            parts = list.ToArray();
            foreach (var c in duck.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in duck.GetComponentsInChildren<Rigidbody>()) Destroy(r);
            if (settings.outlineShader != null) outline = new Material(settings.outlineShader);
            template.SetActive(false);
        }

        public GameObject CreateVisual(Transform parent)
        {
            var clone = Instantiate(template, parent); clone.name = "Pooled duck visual"; clone.SetActive(false); return clone;
        }

        public bool Remove(int id)
        {
            if (!IsAvailable(id)) return false;
            var tile = tiles[tileOf[id]]; int slot = slotOf[id], last = --tile.count;
            if (slot != last)
            {
                int moved = tile.ids[last]; tile.ids[slot] = moved; slotOf[moved] = slot;
                for (int p = 0; p < parts.Length; p++) tile.matrices[p][slot] = tile.matrices[p][last];
            }
            slotOf[id] = -1; Remaining--; removed.Add(id); return true;
        }

        // Ray/sphere query through only nearby grid tiles. Best candidate is returned without allocations.
        public int Query(Vector3 origin, Vector3 forward, float range, float coneCos, float rayRadius = 0)
        {
            int best = -1; float score = float.PositiveInfinity;
            int minCol = Mathf.Clamp(Mathf.FloorToInt((origin.x - range - minX - 2) / spacing / cellWidth), 0, tileColumns - 1);
            int maxCol = Mathf.Clamp(Mathf.FloorToInt((origin.x + range - minX + 2) / spacing / cellWidth), 0, tileColumns - 1);
            int minRow = Mathf.Max(0, Mathf.FloorToInt((origin.z - range) / spacing / cellWidth));
            int maxRow = Mathf.Min((tiles.Length / tileColumns) - 1, Mathf.FloorToInt((origin.z + range) / spacing / cellWidth));
            float rangeSq = range * range;
            for (int z = minRow; z <= maxRow; z++) for (int x = minCol; x <= maxCol; x++)
            {
                var tile = tiles[z * tileColumns + x];
                for (int s = 0; s < tile.count; s++)
                {
                    int id = tile.ids[s]; Vector3 delta = positions[id] + Vector3.up * .2f - origin;
                    float sq = delta.sqrMagnitude;
                    if (sq > rangeSq) continue;
                    float along = Vector3.Dot(delta, forward);
                    if (along <= 0 || along * along < sq * coneCos * coneCos) continue;
                    float perpendicular = Mathf.Max(0, sq - along * along);
                    if (rayRadius > 0 && perpendicular > rayRadius * rayRadius) continue;
                    float candidate = rayRadius > 0 ? perpendicular + sq * .002f : sq;
                    if (candidate < score) { score = candidate; best = id; }
                }
            }
            return best;
        }

        void LateUpdate()
        {
            if (tiles == null || view == null) return;
            GeometryUtility.CalculateFrustumPlanes(view, planes);
            DrawCalls = 0; VisibleInstances = 0;
            foreach (var tile in tiles)
            {
                if (tile.count == 0 || !GeometryUtility.TestPlanesAABB(planes, tile.bounds)) continue;
                VisibleInstances += tile.count;
                for (int p = 0; p < parts.Length; p++)
                {
                    var part = parts[p];
                    Graphics.DrawMeshInstanced(part.mesh, part.submesh, part.material, tile.matrices[p], tile.count,
                        null, ShadowCastingMode.Off, false, 0, view, LightProbeUsage.Off);
                    DrawCalls++;
                }
            }
            if (outline != null && IsAvailable(HoverId))
            {
                for (int p = 0; p < parts.Length; p++)
                {
                    var part = parts[p];
                    Graphics.DrawMesh(part.mesh, tiles[tileOf[HoverId]].matrices[p][slotOf[HoverId]],
                        outline, 0, view, part.submesh, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
                }
            }
        }
        void OnDestroy()
        {
            foreach (var mat in ownedMaterials) { if (Application.isPlaying) Destroy(mat); else DestroyImmediate(mat); }
            if (outline != null) { if (Application.isPlaying) Destroy(outline); else DestroyImmediate(outline); }
        }
    }
}
