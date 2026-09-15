using UnityEngine;

namespace Sandouq.Ducks
{
    // Authored terrain and landmarks live in the scene, independently of economy settings.
    public sealed class DuckPark : MonoBehaviour
    {
        public Terrain terrain;
        public DuckMountainArea mountain;
        public bool InMountain(Vector3 p)=>mountain!=null&&mountain.Contains(p);
        public Transform casket; // Legacy scene template, hidden at runtime.
        public GameObject casketPrefab;
        public Vector3 lakeCenter = new Vector3(-62, 0, 100);
        public float waterHeight=-.26f;
        public bool OnBridge(Vector3 p)=>InLake(p)&&(Mathf.Abs(p.x-lakeCenter.x)<2.1f||Mathf.Abs(p.z-lakeCenter.z)<2.1f);
        public DuckLakePlatform[] platforms=System.Array.Empty<DuckLakePlatform>();
        public bool WalkableWater(Vector3 p){if(OnBridge(p))return true;foreach(var platform in platforms)if(platform!=null&&platform.Contains(p))return true;return false;}
        public float WaterSupportHeight(Vector3 p){foreach(var platform in platforms)if(platform!=null&&platform.Contains(p))return platform.Height;return OnBridge(p)?.46f:waterHeight;}
        public Vector2 lakeRadius = new Vector2(38, 52);
        public float Ground(Vector3 p) { float height=terrain == null ? 0 : terrain.SampleHeight(p) + terrain.transform.position.y;return mountain!=null?mountain.SupportHeight(p,height):height; }
        public bool InLake(Vector3 p) => Mathf.Pow((p.x-lakeCenter.x)/lakeRadius.x,2)+Mathf.Pow((p.z-lakeCenter.z)/lakeRadius.y,2)<1;
        public Vector3 Land(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x,-145,145); p.z = Mathf.Clamp(p.z,-18,280);
            if (InLake(p)) { var d = new Vector2((p.x-lakeCenter.x)/lakeRadius.x,(p.z-lakeCenter.z)/lakeRadius.y).normalized; if(d==Vector2.zero)d=Vector2.right; p.x=lakeCenter.x+d.x*(lakeRadius.x+2); p.z=lakeCenter.z+d.y*(lakeRadius.y+2); }
            p.y=Ground(p); return p;
        }
    }
}
