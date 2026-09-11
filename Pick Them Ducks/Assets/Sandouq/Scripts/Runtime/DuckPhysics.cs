using UnityEngine;

namespace Sandouq.Ducks
{
    // Only this bounded pool participates in physics. Sleeping ducks return to GPU batches.
    public sealed class DuckPhysics : MonoBehaviour
    {
        sealed class Body { public GameObject go; public Rigidbody rb; public int id=-1; public float still, age; }
        readonly Body[] pool = new Body[96];
        DuckGame game; Vector3 previousPlayer;
        public int ActiveCount { get; private set; }
        public void Initialize(DuckGame owner)
        {
            game=owner; previousPlayer=game.Player.transform.position;
            for(int i=0;i<pool.Length;i++)
            {
                var root=new GameObject("Reusable rolling duck"); root.transform.SetParent(transform);
                var visual=game.Population.CreateVisual(root.transform); visual.SetActive(true); visual.transform.localPosition=Vector3.down*.19f;
                var shape=root.AddComponent<SphereCollider>(); shape.radius=.22f;
                var rb=root.AddComponent<Rigidbody>(); rb.mass=.18f; rb.linearDamping=.7f; rb.angularDamping=.9f; rb.maxAngularVelocity=18; rb.isKinematic=true;
                root.SetActive(false); pool[i]=new Body{go=root,rb=rb};
            }
        }
        public bool Launch(int id, Vector3 position, Vector3 velocity, bool fromInventory=false)
        {
            Body free=null; foreach(var b in pool) if(b.id<0){free=b;break;}
            if(free==null || (!fromInventory && !game.Population.IsAvailable(id)))return false;
            if(!game.Population.Detach(id,fromInventory))return false;
            game.Progress.Touch();
            free.id=id; free.age=free.still=0; free.go.transform.SetPositionAndRotation(position+Vector3.up*.23f,game.Population.Rotation(id));
            free.go.SetActive(true); free.rb.isKinematic=false; free.rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            game.Population.UpdatePose(id,position,free.go.transform.rotation);
            free.rb.linearVelocity=velocity; free.rb.angularVelocity=new Vector3(velocity.z,1,-velocity.x)*3; ActiveCount++; return true;
        }
        public void Release(int id)
        { foreach(var b in pool)if(b.id==id){Disable(b);return;} }
        void Disable(Body b) { b.rb.linearVelocity=Vector3.zero; b.rb.angularVelocity=Vector3.zero; b.rb.collisionDetectionMode=CollisionDetectionMode.Discrete; b.rb.isKinematic=true; b.go.SetActive(false); b.id=-1; ActiveCount--; }
        public int Push(Vector3 origin, Vector3 forward, float range, int count, float force)
        {
            int pushed=0;
            for(int i=0;i<count;i++) { int id=game.Population.Query(origin,forward,range,.15f,0,false); if(id<0)break; var d=game.Population.Position(id)-origin; d.y=0; if(!Launch(id,game.Population.Position(id),d.normalized*force+Vector3.up*.6f))break; pushed++; }
            return pushed;
        }
        void FixedUpdate()
        {
            if(game==null)return;
            var player=game.Player.transform.position; var movement=player-previousPlayer; previousPlayer=player;
            if(!game.MenuOpen && movement.sqrMagnitude>.0001f) Push(player+Vector3.up*.2f,movement.normalized,1.25f,4,2.2f);
            foreach(var b in pool)
            {
                if(b.id<0)continue;
                b.age+=Time.fixedDeltaTime;
                var p=b.go.transform.position-Vector3.up*.19f;
                game.Population.UpdatePose(b.id,p,b.go.transform.rotation);
                b.still=b.rb.linearVelocity.sqrMagnitude<.025f && b.rb.angularVelocity.sqrMagnitude<.08f ? b.still+Time.fixedDeltaTime : 0;
                if(b.still>.65f || b.rb.IsSleeping() || p.y < -10)
                {
                    if(game.Park!=null) {
                        if(game.Park.InLake(p) || Mathf.Abs(p.x)>145 || p.z< -18 || p.z>280 || p.y<game.Park.Ground(p)-.1f)p=game.Park.Land(p);
                    }
                    game.Population.Settle(b.id,p,b.go.transform.rotation); Disable(b);
                }
            }
        }
    }
}
