using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Sandouq.Ducks
{
    // Only this bounded pool participates in physics. Sleeping ducks return to GPU batches.
    public sealed class DuckPhysics : MonoBehaviour
    {
        sealed class Body { public GameObject go; public Rigidbody rb; public int id=-1; public float still, age; public bool held, flying, pushing; public int berth,flightBatch; }
        public GameObject rollingDuckPrefab;
        readonly Body[] pool = new Body[96];
        DuckGame game; Vector3 previousPlayer;
        public int ActiveCount { get; private set; }
        public void Initialize(DuckGame owner)
        {
            game=owner; previousPlayer=game.Player.transform.position;
            for(int i=0;i<pool.Length;i++)
            {
                var root=Instantiate(rollingDuckPrefab,transform);
                var rb=root.GetComponent<Rigidbody>();rb.isKinematic=true;
                root.SetActive(false); pool[i]=new Body{go=root,rb=rb};
            }
        }
        public bool Launch(int id, Vector3 position, Vector3 velocity, bool fromInventory=false)
        {
            Body free=null; foreach(var b in pool) if(b.id<0){free=b;break;}
            if(free==null || (!fromInventory && !game.Population.IsAvailable(id)))return false;
            if(!game.Population.Detach(id,fromInventory))return false;
            game.Progress.Touch();
            free.held=free.flying=free.pushing=false; free.id=id; free.age=free.still=0; free.go.transform.SetPositionAndRotation(position+Vector3.up*.23f,game.Population.Rotation(id));
            free.rb.detectCollisions=true;free.go.transform.localScale=Vector3.one;free.go.SetActive(true); free.rb.isKinematic=false; free.rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            game.Population.UpdatePose(id,position,free.go.transform.rotation);
            free.rb.linearVelocity=velocity; free.rb.angularVelocity=new Vector3(velocity.z,1,-velocity.x)*3; ActiveCount++; return true;
        }
        [Header("Pile settling, independent of the Rigidbody pool")]
        public float collapseRadius=4.2f,collapseGravity=22,collapseSpread=.65f;
        struct Falling {public Vector3 velocity;public float spin;}
        readonly Dictionary<int,Falling> falling=new Dictionary<int,Falling>();
        readonly List<int> nearby=new List<int>(),fallingIds=new List<int>();
        public int FallingCount=>falling.Count;
        public void CollapsePile(Vector3 center)
        {
            if(game.Park.InLake(center)||center.y-game.Park.Ground(center)>4.5f)return;
            game.Population.Nearby(center,collapseRadius,nearby);int added=0;
            foreach(int id in nearby){var p=game.Population.Position(id);float height=p.y-game.Park.Ground(p);if(height<=.06f||height>4.5f||falling.ContainsKey(id)||game.Population.supportedDucks.Contains(id))continue;falling[id]=new Falling{velocity=new Vector3(Mathf.Sin(id*2.4f)*collapseSpread,-.2f,Mathf.Cos(id*2.4f)*collapseSpread),spin=id%2==0?130:-130};added++;}
            if(added>0)game.particles?.Play(game.particles.pileCollapse,center);
        }
        void TickCollapse()
        {
            fallingIds.Clear();fallingIds.AddRange(falling.Keys);
            foreach(int id in fallingIds){if(!game.Population.IsAvailable(id)){falling.Remove(id);continue;}var fall=falling[id];fall.velocity.y-=collapseGravity*Time.fixedDeltaTime;var p=game.Population.Position(id)+fall.velocity*Time.fixedDeltaTime;float ground=game.Park.InLake(p)?game.Park.WaterSupportHeight(p):game.Park.Ground(p);bool landed=p.y<=ground+.025f;if(landed)p.y=ground+.025f;var rotation=landed?Quaternion.Euler(0,game.Population.Angle(id),0):game.Population.Rotation(id)*Quaternion.Euler(fall.spin*Time.fixedDeltaTime,0,0);game.Population.MoveInstance(id,p,rotation);if(landed){falling.Remove(id);if(game.Park.InLake(p)&&!game.Park.WalkableWater(p))game.particles?.Play(game.particles.waterSplash,p);}else falling[id]=fall;}
        }
        sealed class CollectionBatch {public int remaining,count;}
        readonly Dictionary<int,CollectionBatch> batches=new Dictionary<int,CollectionBatch>();
        int pendingCollected,nextBatch;bool shuttingDown;
        void CompleteFlight(int batchId){if(!batches.TryGetValue(batchId,out var batch))return;if(--batch.remaining>0)return;batches.Remove(batchId);if(!shuttingDown)game.RollerCollectionFinished(batch.count);}
        public void Release(int id)
        { foreach(var b in pool)if(b.id==id){Disable(b);return;} }
        void Disable(Body b) { bool wasFlying=b.flying;int batch=b.flightBatch;b.flying=false;b.go.transform.DOKill(); if(!b.rb.isKinematic){b.rb.linearVelocity=Vector3.zero; b.rb.angularVelocity=Vector3.zero;} b.held=b.pushing=false; b.rb.collisionDetectionMode=CollisionDetectionMode.Discrete; b.rb.isKinematic=true; b.go.SetActive(false); b.id=-1; ActiveCount--;if(wasFlying)CompleteFlight(batch); }
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
            int added=0;
            // Front slots can lie outside the intake. Reconsider every world-owned pushed duck first.
            foreach(var body in pool)if(body.pushing&&game.Progress.FreeSpace>0&&CaptureRoller(body.id))added++;
            for(int i=0;i<512&&game.Progress.FreeSpace>0;i++){int id=game.Population.QueryBox(game.Player.transform,area);if(id<0||!CaptureRoller(id))break;added++;}
            return added;
        }
        bool CaptureRoller(int id)
        {
            bool showFront=FrontCount<48;Body body=null;foreach(var candidate in pool)if(candidate.id==id){body=candidate;break;}
            if(showFront&&body==null&&Launch(id,game.Population.Position(id),Vector3.zero))foreach(var candidate in pool)if(candidate.id==id){body=candidate;break;}
            if(!game.RecordRollerPickup(id))return false;pendingCollected++;
            if(!showFront){if(body!=null)Disable(body);return true;}if(body==null)return true;
            int berth=0;for(;berth<pool.Length;berth++){bool taken=false;foreach(var other in pool)if((other.held||other.pushing)&&other.berth==berth){taken=true;break;}if(!taken)break;}
            // Bag-owned front and pull-in visuals must never displace the player controller.
            body.rb.detectCollisions=false;body.pushing=false;body.held=true;body.berth=berth;body.rb.linearVelocity=body.rb.angularVelocity=Vector3.zero;body.rb.collisionDetectionMode=CollisionDetectionMode.Discrete;body.rb.isKinematic=true;return true;
        }
        public void PushRoller(Bounds area)
        {
            var frame=game.Player.transform;
            int budget=Mathf.Min(16,Mathf.Max(0,40-PushedCount));
            for(int i=0;i<budget;i++){int id=game.Population.QueryBox(frame,area,false);if(id<0||!Launch(id,game.Population.Position(id),Vector3.zero))break;}
            foreach(var body in pool)if(body.id>=0&&!body.held&&!body.flying&&!body.pushing&&area.Contains(frame.InverseTransformPoint(body.go.transform.position))){int berth=0;for(;berth<pool.Length;berth++){bool taken=false;foreach(var other in pool)if((other.held||other.pushing)&&other.berth==berth){taken=true;break;}if(!taken)break;}body.pushing=true;body.berth=berth;body.rb.linearVelocity=body.rb.angularVelocity=Vector3.zero;body.rb.collisionDetectionMode=CollisionDetectionMode.Discrete;body.rb.isKinematic=true;}
        }
        void ReleasePush(Body body){body.pushing=false;body.rb.isKinematic=false;body.rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.rb.linearVelocity=Vector3.zero;body.still=0;}
        public int PushedCount {get{int n=0;foreach(var body in pool)if(body.pushing)n++;return n;}}
        public int FrontCount {get{int n=0;foreach(var b in pool)if(b.held)n++;return n;}}
        public void FlushFront()
        {
            foreach(var body in pool)if(body.pushing)ReleasePush(body);
            int count=pendingCollected;pendingCollected=0;int batchId=++nextBatch;int visuals=FrontCount;
            if(visuals>0)batches[batchId]=new CollectionBatch{remaining=visuals,count=count};else if(count>0)game.RollerCollectionFinished(count);
            int order=0;foreach(var b in pool)if(b.held)
            {
                b.held=false;b.flying=true;b.flightBatch=batchId;var start=b.go.transform.position;float t=0;
                DOTween.To(()=>t,v=>{t=v;b.go.transform.position=Vector3.Lerp(start,game.Player.CarryTarget.position,v)+Vector3.up*Mathf.Sin(v*Mathf.PI)*.5f;b.go.transform.localScale=Vector3.one*Mathf.Lerp(1,.1f,v);},1,.38f).SetDelay(order++*.012f).SetTarget(b.go.transform).OnComplete(()=>{Disable(b);b.go.transform.localScale=Vector3.one;});
            }
        }
        void MoveCorral(Body body)
        {
            if(!game.CanDrive||game.Placing)
            {if(body.pushing)ReleasePush(body);else FlushFront();return;}
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
            if(game!=null&&!game.MenuOpen)TickCollapse();
            if(game==null)return;
            if(!game.CanDrive&&pendingCollected>0)FlushFront();
            var player=game.Player.transform.position; var movement=player-previousPlayer; previousPlayer=player;
            if(!game.MenuOpen && !game.CanDrive && movement.sqrMagnitude>.0001f) Push(player+Vector3.up*.2f,movement.normalized,1.25f,4,2.2f);
            foreach(var b in pool)
            {
                if(b.id<0||b.flying)continue;
                if(b.held||b.pushing){if(!game.MenuOpen)MoveCorral(b);}
                if(b.pushing){var pushedPosition=b.go.transform.position-Vector3.up*.19f;if(game.Deposits.TryIntake(b.id,game.Population.Position(b.id),pushedPosition)){Disable(b);continue;}game.Population.UpdatePose(b.id,pushedPosition,b.go.transform.rotation);continue;}
                if(b.held||b.flying)continue;
                b.age+=Time.fixedDeltaTime;
                var p=b.go.transform.position-Vector3.up*.19f;
                if(game.Deposits!=null && game.Deposits.TryIntake(b.id,game.Population.Position(b.id),p)){Disable(b);continue;}
                game.Population.UpdatePose(b.id,p,b.go.transform.rotation);
                if(b.held)continue;
                if(game.Park.InLake(p)&&p.y<game.Park.waterHeight+.05f){p.y=game.Park.waterHeight;game.particles?.Play(game.particles.waterSplash,p);game.Population.Settle(b.id,p,Quaternion.Euler(0,b.go.transform.eulerAngles.y,0));Disable(b);continue;}
                b.still=b.rb.linearVelocity.sqrMagnitude<.025f && b.rb.angularVelocity.sqrMagnitude<.08f ? b.still+Time.fixedDeltaTime : 0;
                if(b.still>.65f || b.rb.IsSleeping() || p.y < -10)
                {
                    if(game.Park!=null) {
                        if(game.Park.InLake(p)&&!game.Park.WalkableWater(p)){p.y=game.Park.waterHeight;b.go.transform.rotation=Quaternion.Euler(0,b.go.transform.eulerAngles.y,0);}
                        else if(Mathf.Abs(p.x)>145 || p.z< -18 || p.z>280 || p.y<game.Park.Ground(p)-.1f)p=game.Park.Land(p);
                    }
                    game.particles?.Play(game.particles.duckSettle,p);game.Population.Settle(b.id,p,b.go.transform.rotation); Disable(b);
                }
            }
        }
        void OnDestroy(){shuttingDown=true;foreach(var body in pool)if(body!=null&&body.go!=null)body.go.transform.DOKill();}
    }
}
