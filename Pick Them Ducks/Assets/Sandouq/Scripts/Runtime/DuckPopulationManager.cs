using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sandouq.Ducks
{
    // World-space cells contain bounded instance batches. Moving ducks rejoin their
    // destination cell, keeping rendering and queries local even after long throws.
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
        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<Vector2Int, List<int>> cells = new Dictionary<Vector2Int, List<int>>();
        float cellSize;
        Part[] parts;
        Vector3[] positions;
        float[] angles;
        Quaternion[] rotations;
        readonly System.Collections.Generic.Dictionary<int, DuckPose> movedPoses = new System.Collections.Generic.Dictionary<int, DuckPose>();
        int[] tileOf, slotOf;
        readonly HashSet<int> removed = new HashSet<int>();
        readonly HashSet<int> physical = new HashSet<int>();
        readonly Plane[] planes = new Plane[6];
        readonly List<Material> ownedMaterials = new List<Material>();
        Material outline;
        GameObject template;
        Camera view;
        int columns, rows, cellWidth;
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
        public Quaternion Rotation(int id) => rotations[id];
        public bool IsAvailable(int id) => id >= 0 && id < Total && slotOf[id] >= 0;
        public bool IsPhysical(int id) => id >= 0 && id < Total && slotOf[id] == -2;
        public bool IsReserved(int id)=>id>=0&&id<Total&&slotOf[id]==-3;
        public bool Reserve(int id){if(IsAvailable(id)){Detach(id,false);}else if(!IsPhysical(id))return false;physical.Remove(id);slotOf[id]=-3;UpdatePose(id,positions[id],rotations[id]);return true;}
        public DuckPose[] Poses() => new List<DuckPose>(movedPoses.Values).ToArray();
        public int[] CollectedIds() => new List<int>(removed).ToArray();

        public void Initialize(PrototypeSettings settings, SaveData data, Camera camera)
        {
            view = camera; spacing = Mathf.Min(settings.spacing, 275f / Mathf.CeilToInt(Mathf.Sqrt(data.total))); cellWidth = Mathf.Clamp(settings.cellWidth, 2, 31);
            columns = Mathf.CeilToInt(Mathf.Sqrt(data.total)); rows = Mathf.CeilToInt((float)data.total / columns);
            minX = -columns * spacing * .5f;
            rotations = new Quaternion[data.total];
            var park = FindAnyObjectByType<DuckPark>();
            positions = new Vector3[data.total]; angles = new float[data.total]; tileOf = new int[data.total]; slotOf = new int[data.total];
            PrepareTemplate(settings);
            cellSize=spacing*cellWidth;
            var random = new System.Random(data.seed);
            var pileRandom=new System.Random(data.seed^73129);
            var piles=new Vector3[90];
            for(int i=0;i<piles.Length;i++){Vector3 center;do{center=new Vector3(-125+(float)pileRandom.NextDouble()*250,0,18+(float)pileRandom.NextDouble()*235);}while(Mathf.Abs(center.x)<10||(park!=null&&park.InLake(center)));piles[i]=center;}
            for (int id = 0; id < data.total; id++)
            {
                int x = id % columns, z = id / columns;
                float worldX = minX + (x + .5f) * spacing;
                worldX += worldX < 0 ? -2 : 2; // central return lane
                var pos = new Vector3(worldX + ((float)random.NextDouble() - .5f) * spacing * .72f, 0,
                    (z + .5f) * spacing + ((float)random.NextDouble() - .5f) * spacing * .72f);
                if(park != null) {
                    // Redistribute lake cells across dry meadow instead of stacking ducks on the bank.
                    while(park.InLake(pos))pos=new Vector3(12+(float)random.NextDouble()*117,0,8+(float)random.NextDouble()*255);
                    pos = park.Land(pos);
                }
                if(park!=null && id%3==0)
                {
                    var center=piles[(id/3)%piles.Length];float radius=Mathf.Sqrt((float)pileRandom.NextDouble())*1.75f;
                    float angle=(float)pileRandom.NextDouble()*Mathf.PI*2;
                    var candidate=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                    if(!park.InLake(candidate)){pos=park.Land(candidate);pos.y+=Mathf.Floor(Mathf.Max(0,1-radius/1.75f)*7)*.23f;}
                }
                if(park!=null&&id>=data.total-500)
                {
                    do { float angle=(float)pileRandom.NextDouble()*Mathf.PI*2;float radius=Mathf.Sqrt((float)pileRandom.NextDouble())*.92f;
                    pos=park.lakeCenter+new Vector3(Mathf.Cos(angle)*radius*park.lakeRadius.x,0,Mathf.Sin(angle)*radius*park.lakeRadius.y);
                    } while(park.WalkableWater(pos));
                    pos.y=park.waterHeight;
                }
                positions[id] = pos; angles[id] = (float)random.NextDouble() * 360;
                rotations[id] = Quaternion.Euler(0, angles[id], 0);
                Insert(id,pos,rotations[id]);
            }
            Remaining = data.total;
            foreach (int id in data.collected) Remove(id);
            if(data.poses != null) foreach(var pose in data.poses) if(IsAvailable(pose.id)) { Detach(pose.id,false); Settle(pose.id,pose.position,pose.rotation); }
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
            foreach (var c in duck.GetComponentsInChildren<Collider>()) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            foreach (var r in duck.GetComponentsInChildren<Rigidbody>()) { if (Application.isPlaying) Destroy(r); else DestroyImmediate(r); }
            if (settings.outlineShader != null) outline = new Material(settings.outlineShader);
            template.SetActive(false);
        }

        public GameObject CreateVisual(Transform parent)
        {
            var clone = Instantiate(template, parent); clone.name = "Pooled duck visual"; clone.SetActive(false); return clone;
        }

        public void Nearby(Vector3 center,float radius,List<int> result)
        {
            result.Clear();float radiusSq=radius*radius;
            for(int z=Mathf.FloorToInt((center.z-radius)/cellSize);z<=Mathf.FloorToInt((center.z+radius)/cellSize);z++)
            for(int x=Mathf.FloorToInt((center.x-radius)/cellSize);x<=Mathf.FloorToInt((center.x+radius)/cellSize);x++)
            {if(!cells.TryGetValue(new Vector2Int(x,z),out var batches))continue;foreach(int batch in batches){var tile=tiles[batch];for(int i=0;i<tile.count;i++){int id=tile.ids[i];if((positions[id]-center).sqrMagnitude<=radiusSq)result.Add(id);}}}
        }
        public bool Remove(int id)
        {
            if(IsPhysical(id) || IsReserved(id)) { physical.Remove(id); slotOf[id]=-1; Remaining--; removed.Add(id); movedPoses.Remove(id); return true; }
            if (!IsAvailable(id)) return false;
            var tile = tiles[tileOf[id]]; int slot = slotOf[id], last = --tile.count;
            if (slot != last)
            {
                int moved = tile.ids[last]; tile.ids[slot] = moved; slotOf[moved] = slot;
                for (int p = 0; p < parts.Length; p++) tile.matrices[p][slot] = tile.matrices[p][last];
            }
            slotOf[id] = -1; Remaining--; removed.Add(id); movedPoses.Remove(id); return true;
        }

        // Ray/sphere query through only nearby grid tiles. Best candidate is returned without allocations.
        public int Query(Vector3 origin, Vector3 forward, float range, float coneCos, float rayRadius = 0, bool includePhysical = true)
        {
            int best = -1; float score = float.PositiveInfinity;
            float rangeSq = range * range;
            int minCellX=Mathf.FloorToInt((origin.x-range)/cellSize), maxCellX=Mathf.FloorToInt((origin.x+range)/cellSize);
            int minCellZ=Mathf.FloorToInt((origin.z-range)/cellSize), maxCellZ=Mathf.FloorToInt((origin.z+range)/cellSize);
            for(int z=minCellZ;z<=maxCellZ;z++)for(int x=minCellX;x<=maxCellX;x++)
            {
                if(!cells.TryGetValue(new Vector2Int(x,z),out var batches))continue;
                foreach(int tileIndex in batches)
                {
                var tile=tiles[tileIndex];
                if(tile.bounds.SqrDistance(origin)>rangeSq)continue;
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
            }
            if(includePhysical)foreach(int id in physical)
            {
                Vector3 delta=positions[id]+Vector3.up*.2f-origin;
                float sq=delta.sqrMagnitude, along=Vector3.Dot(delta,forward);
                if(sq>rangeSq || along<=0 || along*along<sq*coneCos*coneCos)continue;
                float perpendicular=Mathf.Max(0,sq-along*along);
                if(rayRadius>0 && perpendicular>rayRadius*rayRadius)continue;
                float candidate=rayRadius>0 ? perpendicular+sq*.002f : sq;
                if(candidate<score){score=candidate;best=id;}
            }
            return best;
        }

        public int QueryBox(Transform frame,Bounds box,bool includePhysical=true)
        {
            Vector3 center=frame.TransformPoint(box.center);float radius=box.extents.magnitude;
            int x0=Mathf.FloorToInt((center.x-radius)/cellSize),x1=Mathf.FloorToInt((center.x+radius)/cellSize);
            int z0=Mathf.FloorToInt((center.z-radius)/cellSize),z1=Mathf.FloorToInt((center.z+radius)/cellSize);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)if(cells.TryGetValue(new Vector2Int(x,z),out var batches))foreach(int index in batches)
            {
                var tile=tiles[index];for(int i=0;i<tile.count;i++){int id=tile.ids[i];if(box.Contains(frame.InverseTransformPoint(positions[id]+Vector3.up*.15f)))return id;}
            }
            if(includePhysical)foreach(int id in physical)if(box.Contains(frame.InverseTransformPoint(positions[id]+Vector3.up*.15f)))return id;
            return -1;
        }

        public bool Detach(int id, bool collected)
        {
            if(id<0 || id>=Total)return false;
            if(collected) { if(slotOf[id]!=-1 || !removed.Remove(id))return false; Remaining++; }
            else { if(!IsAvailable(id))return false; Remove(id); removed.Remove(id); Remaining++; }
            slotOf[id]=-2; physical.Add(id); return true;
        }
        public void UpdatePose(int id, Vector3 p, Quaternion rotation)
        { positions[id]=p; rotations[id]=rotation; angles[id]=rotation.eulerAngles.y; movedPoses[id]=new DuckPose{id=id,position=p,rotation=rotation}; }
        public void Settle(int id, Vector3 p, Quaternion rotation)
        {
            if(!IsPhysical(id)&&!IsReserved(id))return;
            physical.Remove(id); UpdatePose(id,p,rotation);
            Insert(id,p,rotation);
        }
        void Insert(int id, Vector3 position, Quaternion rotation)
        {
            var key=new Vector2Int(Mathf.FloorToInt(position.x/cellSize),Mathf.FloorToInt(position.z/cellSize));
            if(!cells.TryGetValue(key,out var batches)) { batches=new List<int>();cells.Add(key,batches); }
            int index=-1;
            foreach(int candidate in batches)if(tiles[candidate].count<256){index=candidate;break;}
            if(index<0)
            {
                index=tiles.Count;
                var created=new Tile{ids=new int[256],matrices=new Matrix4x4[parts.Length][]};
                for(int p=0;p<parts.Length;p++)created.matrices[p]=new Matrix4x4[256];
                tiles.Add(created);batches.Add(index);
            }
            var tile=tiles[index];int slot=tile.count++;tile.ids[slot]=id;tileOf[id]=index;slotOf[id]=slot;
            var matrix=Matrix4x4.TRS(position,rotation,Vector3.one);
            for(int p=0;p<parts.Length;p++)tile.matrices[p][slot]=matrix*parts[p].local;
            var bounds=new Bounds(position,Vector3.one*2);
            if(slot==0)tile.bounds=bounds;else tile.bounds.Encapsulate(bounds);
        }

        void LateUpdate() => RenderForCamera(view, true);

        // The editor can render these same cached batches for a Scene view camera.
        // Keep its culling and diagnostics separate from the player's camera.
        public void RenderForCamera(Camera camera, bool updatePlayerStatistics = false)
        {
            if (positions == null || camera == null) return;
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            int calls = 0, visible = 0;
            foreach (var tile in tiles)
            {
                if (tile.count == 0 || !GeometryUtility.TestPlanesAABB(planes, tile.bounds)) continue;
                visible += tile.count;
                for (int p = 0; p < parts.Length; p++)
                {
                    var part = parts[p];
                    Graphics.DrawMeshInstanced(part.mesh, part.submesh, part.material, tile.matrices[p], tile.count,
                        null, ShadowCastingMode.Off, false, 0, camera, LightProbeUsage.Off);
                    calls++;
                }
            }
            if (outline != null && (IsAvailable(HoverId) || IsPhysical(HoverId)))
            {
                for (int p = 0; p < parts.Length; p++)
                {
                    var part = parts[p];
                    Graphics.DrawMesh(part.mesh, IsPhysical(HoverId) ? Matrix4x4.TRS(positions[HoverId],rotations[HoverId],Vector3.one)*part.local : tiles[tileOf[HoverId]].matrices[p][slotOf[HoverId]],
                        outline, 0, camera, part.submesh, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
                }
            }
            if (updatePlayerStatistics) { DrawCalls = calls; VisibleInstances = visible; }
        }
        void OnDestroy()
        {
            foreach (var mat in ownedMaterials) { if (Application.isPlaying) Destroy(mat); else DestroyImmediate(mat); }
            if (outline != null) { if (Application.isPlaying) Destroy(outline); else DestroyImmediate(outline); }
        }
    }
}
