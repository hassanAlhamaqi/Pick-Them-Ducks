using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Sandouq.Ducks
{
    // Inventory/world ownership changes only on landing. In-flight saves remain recoverable.
    public sealed class DuckDeposits : MonoBehaviour
    {
        struct Request { public int id, kind; public Vector3 from; public DuckDepositStation station; }
        sealed class Flight { public GameObject visual; public Tween tween; public Request request; public float t; public bool active; }
        readonly Flight[] flights=new Flight[256];
        readonly Queue<Request> waiting=new Queue<Request>();
        readonly HashSet<int> inventoryReservations=new HashSet<int>();
        readonly HashSet<int> legacyReservations=new HashSet<int>();
        public readonly List<DuckDepositStation> Stations=new List<DuckDepositStation>();
        DuckGame game;
        DuckDepositStation transfer;
        float nextFlight,nextScan,burstStart,lastLaunch=-10;
        public int InFlight {get;private set;}
        public bool Transferring=>transfer!=null||inventoryReservations.Count>0;
        public void Initialize(DuckGame owner)
        {
            game=owner;var station=game.Stage.Box.GetComponent<DuckDepositStation>();
            if(station==null)throw new System.InvalidOperationException("Assign DuckDepositStation on the Collection Box prefab.");
            Stations.Add(station);
            foreach(var placement in game.Progress.Data.stations)AddStation(placement);
            for(int i=0;i<flights.Length;i++)
            {
                var f=flights[i]=new Flight{visual=game.Population.CreateVisual(transform)};
                f.tween=DOTween.To(()=>f.t,v=>Animate(f,v),1,game.Settings.depositFlightTime).SetEase(Ease.Linear).SetAutoKill(false).Pause().OnComplete(()=>Land(f));
            }
        }
        public DuckDepositStation AddStation(CasketPlacement placement)
        {
            var go=Instantiate(game.Park.casketPrefab,game.Park.transform);go.name="Installed Duck Casket";
            go.transform.SetPositionAndRotation(placement.position,Quaternion.Euler(0,placement.yaw,0));go.SetActive(true);
            var station=go.GetComponent<DuckDepositStation>();Stations.Add(station);return station;
        }
        public DuckDepositStation Nearest(Vector3 position,float range=3.4f)
        {
            DuckDepositStation found=null;float sq=range*range;
            foreach(var station in Stations){float d=(station.transform.position-position).sqrMagnitude;if(d<sq){sq=d;found=station;}}
            return found;
        }
        public int Begin()
        {
            if(Transferring){transfer=null;lastLaunch=-10;return 0;}
            lastLaunch=-10;
            transfer=Nearest(game.Player.transform.position);return transfer==null?0:game.Progress.Data.carried;
        }
        public bool TryIntake(int id,Vector3 from,Vector3 to)
        {
            if(!game.Population.IsAvailable(id)&&!game.Population.IsPhysical(id))return false;
            foreach(var station in Stations)if(station.Intersects(from,to))
            {
                game.Population.UpdatePose(id,to,game.Population.Rotation(id));
                if(!game.Population.Reserve(id))return false;
                waiting.Enqueue(new Request{id=id,kind=1,from=to,station=station});game.Progress.Touch();return true;
            }
            return false;
        }
        void Update()
        {
            if(game==null||game.MenuOpen)return;
            if(Time.time>=nextScan)
            {
                nextScan=Time.time+.12f;
                foreach(var station in Stations)
                {
                    int id=game.Population.QueryBox(station.transform,station.intake,false);
                    if(id>=0)TryIntake(id,game.Population.Position(id),game.Population.Position(id));
                }
            }
            if(Time.time<nextFlight)return;
            if(Time.time-lastLaunch>.4f)burstStart=Time.time;
            float acceleration=Mathf.Clamp01((Time.time-burstStart)/3f);
            int launched=0;
            for(int i=0;i<game.Progress.PickupAmount;i++){if(!LaunchNext(acceleration))break;launched++;}
            if(launched>0){nextFlight=Time.time+Mathf.Lerp(game.Settings.depositInterval,.015f,acceleration);lastLaunch=Time.time;}
        }
        bool LaunchNext(float acceleration)
        {
            Flight free=null;foreach(var f in flights)if(!f.active){free=f;break;}if(free==null)return false;
            Request request;
            if(waiting.Count>0)request=waiting.Dequeue();
            else if(game.Progress.Data.casketDucks.Length>legacyReservations.Count)
            {
                int id=-1;foreach(int candidate in game.Progress.Data.casketDucks)if(!legacyReservations.Contains(candidate)){id=candidate;break;}
                legacyReservations.Add(id);var station=Stations.Count>1?Stations[1]:Stations[0];request=new Request{id=id,kind=2,station=station,from=station.transform.position+Vector3.up*1.2f};
            }
            else
            {
                if(transfer==null)return false;
                if((transfer.transform.position-game.Player.transform.position).sqrMagnitude>16){transfer=null;return false;}
                int id=-1;foreach(int candidate in game.Progress.Data.inventory)if(!inventoryReservations.Contains(candidate)){id=candidate;break;}
                if(id<0){transfer=null;return false;}
                inventoryReservations.Add(id);request=new Request{id=id,kind=0,station=transfer,from=game.Player.CarryTarget.position};
            }
            free.tween.timeScale=Mathf.Lerp(1,2,acceleration);
            free.request=request;free.active=true;InFlight++;free.visual.SetActive(true);
            free.visual.transform.rotation=game.Population.Rotation(request.id);free.tween.Restart();return true;
        }
        void Animate(Flight f,float t)
        {
            f.t=t;var destination=f.request.station.landing.position;
            f.visual.transform.position=Vector3.Lerp(f.request.from,destination,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*(.85f+(f.request.id%3)*.14f)+game.Player.transform.right*Mathf.Sin(t*Mathf.PI)*((f.request.id%5)-2)*.16f;
            float squash=1+.12f*Mathf.Sin(t*Mathf.PI*3);
            f.visual.transform.localScale=new Vector3(1/squash,squash,1/squash)*Mathf.Lerp(1,.55f,t*t);
        }
        void Land(Flight f)
        {
            bool credited=false;var r=f.request;
            if(r.kind==0){inventoryReservations.Remove(r.id);credited=game.Progress.DepositInventory(r.id);}
            else if(r.kind==2){legacyReservations.Remove(r.id);credited=game.Progress.DepositLegacy(r.id);}
            else if(game.Population.Remove(r.id)){game.Progress.Credit(r.id);credited=true;}
            if(credited){r.station.Arrived(r.id,game.Settings.ValueFor(r.id),game.Settings.VariantFor(r.id));game.DepositLanded();}
            f.active=false;InFlight--;f.visual.SetActive(false);
        }
        void OnDestroy(){foreach(var f in flights)f?.tween?.Kill();}
    }
}
