using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Sandouq.Ducks
{
    // Only this bounded pool participates in physics. Sleeping ducks return to GPU batches.
    public sealed class DuckPhysics : MonoBehaviour
    {
        sealed class Body { public GameObject go; public Rigidbody rb; public int id=-1; public float still, age; public bool held, flying; public int berth; }
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
            free.held=free.flying=false; free.id=id; free.age=free.still=0; free.go.transform.SetPositionAndRotation(position+Vector3.up*.23f,game.Population.Rotation(id));
            free.go.transform.localScale=Vector3.one;free.go.SetActive(true); free.rb.isKinematic=false; free.rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            game.Population.UpdatePose(id,position,free.go.transform.rotation);
            free.rb.linearVelocity=velocity; free.rb.angularVelocity=new Vector3(velocity.z,1,-velocity.x)*3; ActiveCount++; return true;
        }
        readonly LinkedList<int> collapsing=new LinkedList<int>();
        readonly Dictionary<int,LinkedListNode<int>> collapseIds=new Dictionary<int,LinkedListNode<int>>();
        readonly List<int> nearby=new List<int>();
        public void CollapsePile(Vector3 center)
        {
            if(game.Park.InLake(center)||center.y-game.Park.Ground(center)>2)return;
            game.Population.Nearby(center,2.6f,nearby);
            int woken=0;
            foreach(int id in nearby){var p=game.Population.Position(id);float height=p.y-game.Park.Ground(p);if(height<=.12f||height>=2)continue;
                if(collapseIds.TryGetValue(id,out var node)){collapsing.Remove(node);collapseIds.Remove(id);}
                if(woken<4&&ActiveCount<48&&!game.Throwing&&Launch(id,p,new Vector3(Mathf.Sin(id*2.4f),-.3f,Mathf.Cos(id*2.4f))*.45f)){woken++;continue;}
                collapseIds[id]=collapsing.AddFirst(id);
            }
        }
        void TickCollapse()
        {
            for(int n=0;n<12&&collapsing.Count>0&&ActiveCount<32;n++){
                int id=collapsing.First.Value;collapsing.RemoveFirst();collapseIds.Remove(id);if(!game.Population.IsAvailable(id))continue;
                var p=game.Population.Position(id);Launch(id,p,new Vector3(Mathf.Sin(id*2.4f),-.3f,Mathf.Cos(id*2.4f))*.45f);
            }
        }
        public void Release(int id)
        { foreach(var b in pool)if(b.id==id){Disable(b);return;} }
        void Disable(Body b) { b.go.transform.DOKill(); b.flying=false; if(!b.rb.isKinematic){b.rb.linearVelocity=Vector3.zero; b.rb.angularVelocity=Vector3.zero;} b.held=false; b.rb.collisionDetectionMode=CollisionDetectionMode.Discrete; b.rb.isKinematic=true; b.go.SetActive(false); b.id=-1; ActiveCount--; }
        public int Push(Vector3 origin, Vector3 forward, float range, int count, float force)
        {
            int pushed=0;
            for(int i=0;i<count;i++) { int id=game.Population.Query(origin,forward,range,.15f,0,false); if(id<0)break; var d=game.Population.Position(id)-origin; d.y=0; if(!Launch(id,game.Population.Position(id),d.normalized*force+Vector3.up*.6f))break; pushed++; }
            return pushed;
        }
        public void SweepActive(Transform frame,Bounds box,Vector3 velocity)
        {
            foreach(var body in pool)if(body.id>=0 && !body.held && !body.flying && box.Contains(frame.InverseTransformPoint(body.go.transform.position)))
            {body.rb.linearVelocity=new Vector3(velocity.x,body.rb.linearVelocity.y,velocity.z);body.rb.angularVelocity=new Vector3(velocity.z,0,-velocity.x)*3;}
        }
        public int CorralRoller(Bounds area)
        {
            var frame=game.Player.transform;int added=0;
            for(int i=0;i<game.Progress.PickupAmount&&game.Progress.FreeSpace>0;i++)
            {
                int id=game.Population.QueryBox(frame,area);if(id<0)break;
                Body body=null;foreach(var candidate in pool)if(candidate.id==id){body=candidate;break;}
                if(body==null&&Launch(id,game.Population.Position(id),Vector3.zero))foreach(var candidate in pool)if(candidate.id==id){body=candidate;break;}
                if(!game.RecordRollerPickup(id))break;added++;
                if(body==null)continue;
                int berth=0;for(;berth<pool.Length;berth++){bool taken=false;foreach(var other in pool)if(other.held&&other.berth==berth){taken=true;break;}if(!taken)break;}
                body.held=true;body.berth=berth;body.rb.linearVelocity=Vector3.zero;body.rb.angularVelocity=Vector3.zero;
                body.rb.collisionDetectionMode=CollisionDetectionMode.Discrete;body.rb.isKinematic=true;
            }
            return added;
        }
        public int FrontCount {get{int n=0;foreach(var b in pool)if(b.held)n++;return n;}}
        public void FlushFront()
        {
            int order=0;foreach(var b in pool)if(b.held)
            {
                b.held=false;b.flying=true;var start=b.go.transform.position;float t=0;
                DOTween.To(()=>t,v=>{t=v;b.go.transform.position=Vector3.Lerp(start,game.Player.CarryTarget.position,v)+Vector3.up*Mathf.Sin(v*Mathf.PI)*.5f;b.go.transform.localScale=Vector3.one*Mathf.Lerp(1,.1f,v);},1,.38f).SetDelay(order++*.012f).SetTarget(b.go.transform).OnComplete(()=>{Disable(b);b.go.transform.localScale=Vector3.one;});
            }
        }
        void MoveCorral(Body body)
        {
            if(!game.CanDrive||game.Placing)
            {FlushFront();return;}
            float width=game.Progress.WorkingWidth;int columns=Mathf.Max(2,Mathf.FloorToInt(width/.4f));
            int row=(body.berth/columns)%3,layer=body.berth/(columns*3);
            float reach=game.Progress.Tool==DuckTool.RollerCar?2.65f:2f;
            var target=game.Player.transform.TransformPoint(new Vector3(((body.berth%columns)+.5f)/columns*width-width*.5f,0,reach+row*.39f));
            target.y=(game.Park.WalkableWater(target)?game.Park.WaterSupportHeight(target):game.Park.Ground(target))+.23f+layer*.3f;
            // Kinematic front slots prevent high-speed launches. Ducks remain world-owned.
            body.rb.MovePosition(target);body.rb.MoveRotation(body.rb.rotation*Quaternion.Euler(12,0,0));
            body.still=0;
        }
        void FixedUpdate()
        {
            if(game!=null&&!game.MenuOpen&&!game.Throwing)TickCollapse();
            if(game==null)return;
            var player=game.Player.transform.position; var movement=player-previousPlayer; previousPlayer=player;
            if(!game.MenuOpen && movement.sqrMagnitude>.0001f) Push(player+Vector3.up*.2f,movement.normalized,1.25f,4,2.2f);
            foreach(var b in pool)
            {
                if(b.id<0||b.flying)continue;
                if(b.held){if(!game.MenuOpen)MoveCorral(b);}
                if(b.held||b.flying)continue;
                b.age+=Time.fixedDeltaTime;
                var p=b.go.transform.position-Vector3.up*.19f;
                if(game.Deposits!=null && game.Deposits.TryIntake(b.id,game.Population.Position(b.id),p)){Disable(b);continue;}
                game.Population.UpdatePose(b.id,p,b.go.transform.rotation);
                if(b.held)continue;
                if(game.Park.InLake(p)&&p.y<game.Park.waterHeight+.05f){p.y=game.Park.waterHeight;game.Population.Settle(b.id,p,Quaternion.Euler(0,b.go.transform.eulerAngles.y,0));Disable(b);continue;}
                b.still=b.rb.linearVelocity.sqrMagnitude<.025f && b.rb.angularVelocity.sqrMagnitude<.08f ? b.still+Time.fixedDeltaTime : 0;
                if(b.still>.65f || b.rb.IsSleeping() || p.y < -10)
                {
                    if(game.Park!=null) {
                        if(game.Park.InLake(p)&&!game.Park.WalkableWater(p)){p.y=game.Park.waterHeight;b.go.transform.rotation=Quaternion.Euler(0,b.go.transform.eulerAngles.y,0);}
                        else if(Mathf.Abs(p.x)>145 || p.z< -18 || p.z>280 || p.y<game.Park.Ground(p)-.1f)p=game.Park.Land(p);
                    }
                    game.Population.Settle(b.id,p,b.go.transform.rotation); Disable(b);
                }
            }
        }
    }
}
