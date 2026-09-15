using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Sandouq.Ducks
{
    public sealed class DuckParkPlayChecks : MonoBehaviour
    {
        public static bool Running {get;private set;}
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--duck-park-check");if(index<0||index+1>=args.Length)return;
            Running=true;var runner=new GameObject("Tool interaction checks").AddComponent<DuckParkPlayChecks>();runner.output=args[index+1];Directory.CreateDirectory(runner.output);Application.logMessageReceived+=runner.Log;DontDestroyOnLoad(runner);Application.runInBackground=true;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        void Fail(Exception e){File.WriteAllText(Path.Combine(output,"FAILED.txt"),e.ToString());Application.Quit(2);}
        static void Use(DuckGame game,bool active)=>typeof(DuckGame).GetProperty("ToolActive").SetValue(game,active);
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);yield return null;yield return null;
            var game=FindAnyObjectByType<DuckGame>();
            try
            {
                Check(game!=null&&game.Deposits!=null,"Updated scene initialization");
                Check(!game.TickHand(0,false),"Hands require a click");
                Check(game.TickHand(0,true)&&game.Progress.Data.carried==1,"Click picks up immediately");
                Check(!game.TickHand(1,true)&&game.PickupCooldownProgress>0,"Cooldown blocks rapid clicks");
                for(int i=1;i<5;i++)Check(game.CollectId(i),"Fill transfer bag");
                game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*2);
                Check(game.Deposit()==5&&game.Progress.Data.money==0&&game.Progress.Data.carried==5,"Deposit begins without instant credit");
                Check(!game.ThrowDuck(),"Inventory reserved during transfer");
                game.Progress.Data.collected=game.Population.CollectedIds();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Save during transfer");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(.4f);
            try{Check(game.Progress.Data.deposited>0&&game.Progress.Data.deposited<5,"Deposits arrive incrementally");}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1);
            DuckDepositStation installed=null;
            try
            {
                Check(game.Progress.Data.deposited==5&&game.Progress.Data.carried==0&&game.Deposits.Stations[0].Landed==5,"Every duck landed and bounced once");
                game.Progress.Data.money=10000;game.BuyCasket();game.BuyCasket();Check(game.Progress.Data.casketKits==2,"Multiple casket purchases");
                game.BeginPlacement();Check(game.Placing,"Placement preview starts");game.PreviewPlacement(game.Park.Land(new Vector3(0,0,18)),0);game.CancelPlacement();
                Check(!game.InstallCasket(game.Park.lakeCenter,0)&&game.Progress.Data.casketKits==2,"Invalid install preserves kit");
                var position=game.Park.Land(new Vector3(0,0,18));Check(game.InstallCasket(position,0),"Install casket on valid ground");installed=game.Deposits.Stations[1];
                Check(game.InstallCasket(game.Park.Land(new Vector3(0,0,35)),45)&&game.Progress.Data.stations.Length==2,"Second independent station");
                game.Player.Teleport(position+Vector3.back*3);Check(game.CollectId(1000),"Casket bag pickup");Check(game.Deposit()==1,"Transfer to installed casket");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(.8f);
            int before=game.Progress.Data.deposited;
            try
            {
                Check(installed.Landed==1&&before==6,"Casket transfer landed");
                Check(game.Physics.Launch(1001,installed.transform.position+Vector3.back*1.9f+Vector3.up*.1f,Vector3.forward*7),"Launch duck into casket");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1.5f);
            try
            {
                Check(game.Progress.Data.deposited==before+1&&installed.Landed==2,"Pushed duck auto-deposits once");
                game.BuyTool(3);float width=game.Progress.WorkingWidth;game.BuyToolUpgrade();Check(game.Progress.WorkingWidth>width,"Sweeper grows");
                game.Player.Teleport(game.Park.Land(new Vector3(30,0,10))+Vector3.up*.05f);game.Player.transform.rotation=Quaternion.identity;
                game.Population.Detach(2000,false);game.Population.Settle(2000,game.Park.Land(game.Player.transform.position+Vector3.forward*1.5f),Quaternion.identity);
                Use(game,true);Check(game.SweepFloor(Vector3.forward*.15f)>0&&game.Physics.PushedCount>0,"Sweeper holds contacted ducks in front corral");Check(game.Population.IsPhysical(2000),"Sweeper keeps pushed duck world-owned");Use(game,false);game.Physics.FlushFront();Check(game.Physics.PushedCount==0,"Sweeper releases front ducks when switched off");
                game.BuyTool(1);float speed=game.Progress.DriveSpeed;game.BuyToolUpgrade();Check(game.Progress.DriveSpeed>speed,"Collector grows faster");
                game.Population.Detach(3000,false);game.Population.Settle(3000,game.Park.Land(game.Player.transform.position+Vector3.forward*1.5f),Quaternion.identity);
                Check(game.SweepFloor(Vector3.forward*.2f)>0,"Roller pushes floor contact");
                game.BuyTool(4);Check(game.Progress.Capacity>=600&&game.Car!=null&&!game.RidingCar,"Car purchase delivers a parked field vehicle");game.Player.Teleport(game.Car.transform.position+Vector3.right*2);Check(game.Car.Enter(),"Enter the nearby car");
                game.Player.Teleport(game.Park.Land(new Vector3(0,0,70))+Vector3.up*.05f);game.SetMenu(false);Use(game,true);
            }
            catch(Exception e){Fail(e);yield break;}
            Vector3 start=game.Player.transform.position;yield return new WaitForSeconds(.3f);Use(game,false);
            try{Check((game.Player.transform.position-start).magnitude>.15f&&game.Car.CurrentSpeed<game.Progress.DriveSpeed,"Vehicle accelerates into forward movement");game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Final conservation");}
            catch(Exception e){Fail(e);yield break;}
            // Exercise the polished mechanics with real physics and arrival callbacks.
            game.Equip(0);game.Player.Teleport(game.Park.Land(new Vector3(0,0,70))+Vector3.up*.02f);
            yield return new WaitForSeconds(.25f);
            float ground=game.Player.transform.position.y;
            try{Check(game.Progress.Capacity>=600,"Hands retain unlocked vehicle capacity");Check(game.Player.TryJump(),"Grounded jump starts");}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(.15f);
            try{Check(game.Player.transform.position.y>ground+.4f,"Jump raises player");Check(!game.Player.TryJump(),"No airborne double jump");}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1);
            game.Player.enabled=false;
            foreach(var station in new[]{game.Deposits.Stations[0],installed})
            {
                int expected=station.Landed+4;
                for(int side=0;side<4;side++)
                {
                    var outward=Quaternion.Euler(0,side*90,0)*Vector3.forward;int id=12000+side+(station==installed?4:0);
                    game.Physics.Launch(id,station.transform.position+outward*1.65f+Vector3.up*.12f,-outward*5);
                }
                yield return new WaitForSeconds(2);
                try{Check(station.Landed>=expected,"Physical intake from all four sides: "+station.name);for(int side=0;side<4;side++){int id=12000+side+(station==installed?4:0);Check(!game.Population.IsAvailable(id)&&!game.Population.IsPhysical(id),"Specific side duck deposited: "+id);}}
                catch(Exception e){Fail(e);yield break;}
            }
            foreach(int tool in new[]{1,4})
            {
                if(tool==4){game.Player.Teleport(game.Car.transform.position+Vector3.right*2);Check(game.Car.Enter(),"Re-enter parked car");}else game.Equip(tool);game.Player.Teleport(game.Park.Land(new Vector3(0,0,70)));game.Player.transform.rotation=Quaternion.identity;
                int duck=13000+tool;float reach=tool==4?2.3f:1.7f;
                game.Population.Detach(duck,false);game.Population.Settle(duck,game.Player.transform.position+new Vector3(.3f,.03f,reach),Quaternion.identity);
                int bagBefore=game.Progress.Data.carried;Use(game,true);game.SweepFloor(Vector3.forward*.2f);
                yield return new WaitForSeconds(.2f);
                game.Player.Teleport(game.Player.transform.position+Vector3.forward*4);
                yield return new WaitForSeconds(.2f);
                try{Check(!game.Population.IsAvailable(duck)&&!game.Population.IsPhysical(duck)&&game.Progress.Data.carried>bagBefore,"Roller credits bag while displaying front pile: "+tool);Check(game.Physics.FrontCount>0,"Visible front pile: "+tool);if(tool==4)Check(game.Progress.DriveSpeed>=20,"Fast car speed");}
                catch(Exception e){Fail(e);yield break;}
                Use(game,false);yield return new WaitForSeconds(.7f);
                try{Check(game.Physics.FrontCount==0,"Front pile pulls into player on release");}catch(Exception e){Fail(e);yield break;}
            }
            game.Equip(0);
            game.Progress.Data.levels[0]=2;
            if(game.RidingCar){game.Player.Teleport(game.Park.Land(new Vector3(0,0,70)));Check(game.Car.Exit(),"Leave car in the field");}game.Equip(0);
            for(int i=15000;i<15003;i++)game.CollectId(i);
            game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*3);int beforeGrouped=game.Progress.Data.deposited;game.Deposit();
            yield return null;
            try{Check(game.Deposits.BatchFor(500)==10&&game.Deposits.BatchFor(1000)==20&&game.Deposits.BatchFor(1)==1,"Bag-scaled batch sizes");Check(game.Deposits.InFlight>=1,"Bag transfer launches independently of pickup amount: flights="+game.Deposits.InFlight+", arrivals="+(game.Progress.Data.deposited-beforeGrouped));}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1.6f);game.Progress.Data.levels[0]=0;
            for(int i=14000;i<14200;i++)game.CollectId(i);
            game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*3);game.Deposit();
            int first=game.Progress.Data.deposited;
            yield return new WaitForSeconds(1);int slow=game.Progress.Data.deposited-first;
            yield return new WaitForSeconds(2);first=game.Progress.Data.deposited;
            yield return new WaitForSeconds(1);int fast=game.Progress.Data.deposited-first;
            try{Check(fast>slow*2||game.Progress.Data.carried==0,"Sustained deposit accelerates or finishes the bag: "+slow+" -> "+fast);}
            catch(Exception e){Fail(e);yield break;}
            game.Deposit();yield return new WaitForSeconds(.5f);
            game.Player.Teleport(game.Park.Land(new Vector3(0,0,100)));game.Player.View.transform.rotation=Quaternion.Euler(-15,0,0);
            for(int id=16000;id<16300;id++)game.CollectId(id);
            int carried=game.Progress.Data.carried;float elapsed=0;
            while(elapsed<1){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            int slowThrow=carried-game.Progress.Data.carried;elapsed=0;
            while(elapsed<2){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            carried=game.Progress.Data.carried;elapsed=0;
            while(elapsed<1){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            int fastThrow=carried-game.Progress.Data.carried;game.TickThrow(false,0);
            try{Check(fastThrow>slowThrow*2,"Held throws accelerate: "+slowThrow+" -> "+fastThrow);game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Polish conservation");}
            catch(Exception e){Fail(e);yield break;}
            var habitats=game.Park.GetComponentsInChildren<DuckHabitat>();DuckHabitat bush=null,tree=null;foreach(var habitat in habitats){if(habitat.bush&&bush==null)bush=habitat;else if(!habitat.bush&&tree==null)tree=habitat;}
            try{Check(bush!=null&&tree!=null,"Interactive bushes and trees authored");Check(bush.Interact()&&!bush.Interact(),"Bush breaks once");Check(System.Array.IndexOf(game.Progress.Data.brokenBushes,bush.Key)>=0,"Broken bush persists");Check(tree.Interact(),"Tree shake starts");Check(game.Park.terrain.terrainData.detailPrototypes.Length>0,"Dense grass authored");}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1.5f);
            try{int id=game.Population.Total-501-bush.index*10;Check(game.Population.IsAvailable(id)&&game.Population.Position(id).y>game.Park.Ground(game.Population.Position(id))-.1f,"Bush reveals ducks");Check(game.Population.Position(game.Population.Total-1).y==game.Park.waterHeight,"Floating lake ducks");}
            catch(Exception e){Fail(e);yield break;}
            game.Player.Teleport(game.Park.lakeCenter+Vector3.up*1.2f);game.Player.enabled=true;yield return new WaitForSeconds(.5f);game.Player.enabled=false;
            try{Check(game.Park.InLake(game.Player.transform.position)&&game.Player.transform.position.y>game.Park.waterHeight,"Bridge permits lake traversal");game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Habitat save conservation");}
            catch(Exception e){Fail(e);yield break;}
            try{
                Check(tree.spawnPoints.Length==tree.duckCount,"Editable tree spawn markers");
                Check(game.Park.platforms.Length>20,"Authored lake stepping prefabs");
                int scattered=0;for(int i=game.Population.Total-500;i<game.Population.Total;i++){var p=game.Population.Position(i);if(Mathf.Abs(p.x-game.Park.lakeCenter.x)>8&&Mathf.Abs(p.z-game.Park.lakeCenter.z)>8)scattered++;}Check(scattered>150,"Ducks distributed beyond bridges");
            }catch(Exception e){Fail(e);yield break;}
            var landing=game.Park.platforms[0];game.Player.Teleport(landing.transform.position+Vector3.up*1.2f);game.Player.enabled=true;yield return new WaitForSeconds(.6f);game.Player.enabled=false;
            try{Check((game.Player.transform.position-landing.transform.position).sqrMagnitude<4,"Player lands on stepping prefab");}catch(Exception e){Fail(e);yield break;}
            var water=game.Park.lakeCenter+new Vector3(10,0,10);while(game.Park.WalkableWater(water)||game.Park.WalkableWater(water+Vector3.right*.5f)||game.Park.WalkableWater(water-Vector3.right*.5f)||game.Park.WalkableWater(water+Vector3.forward*.5f)||game.Park.WalkableWater(water-Vector3.forward*.5f))water.x+=.3f;water.y=game.Park.waterHeight+.8f;
            // Reset active bodies before the isolated water assertion: earlier tests deliberately saturate the pool.
            for(int i=0;i<game.Population.Total;i++)if(game.Population.IsPhysical(i)){game.Physics.Release(i);game.Population.Settle(i,game.Population.Position(i),game.Population.Rotation(i));}
            int waterId=17000;while(waterId<game.Population.Total&&(!game.Population.IsAvailable(waterId)||game.Population.Position(waterId).y-game.Park.Ground(game.Population.Position(waterId))>.05f))waterId++;
            try{Check(game.Physics.Launch(waterId,water,Vector3.down*2),"Controlled water launch succeeds");}catch(Exception e){Fail(e);yield break;}yield return new WaitForSeconds(2);
            try{Check(!game.Population.IsPhysical(waterId)&&Mathf.Abs(game.Population.Position(waterId).y-game.Park.waterHeight)<.01f,"Physical duck settles afloat");}catch(Exception e){Fail(e);yield break;}
            game.Equip(0);
            var pile=game.Park.Land(new Vector3(0,0,55));
            var pileIds=new int[3];int search=19000;
            for(int n=0;n<3;n++){while(!game.Population.IsAvailable(search)||game.Population.Position(search).y-game.Park.Ground(game.Population.Position(search))>.05f)search++;pileIds[n]=search++;game.Population.Detach(pileIds[n],false);game.Population.Settle(pileIds[n],pile+Vector3.up*(n*.45f),Quaternion.identity);}
            try{Check(game.TickHand(pileIds[0],true),"Pickup is ready again after cooldown");Check(!game.TickHand(pileIds[1],false),"Holding without a new click cannot collect");}catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(.12f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"pickup-cooldown.png"));
            yield return new WaitForSeconds(.9f);
            try{Check(game.Population.IsPhysical(pileIds[1])||game.Population.Position(pileIds[1]).y<pile.y+.4f,"Pile loses support after pickup");Check(tree.interactionCollider!=null&&bush.interactionCollider!=null,"Editable interaction colliders assigned");}catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(2);
            try{Check(game.Population.Position(pileIds[2]).y<pile.y+.6f,"Pile ducks fall down");var image=(UnityEngine.UI.Image)typeof(DuckHUD).GetField("hold",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.HUD);Check(image.type==UnityEngine.UI.Image.Type.Filled&&image.fillMethod==UnityEngine.UI.Image.FillMethod.Radial360,"Circular cooldown indicator");}catch(Exception e){Fail(e);yield break;}
            game.Equip(4);
            game.Player.enabled=false;var camera=game.Player.View.transform;camera.position=installed.transform.position+new Vector3(3,2,-4);camera.LookAt(installed.transform.position+Vector3.up*.5f);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"installed-casket.png"));yield return null;
            camera.position=game.Player.transform.position+new Vector3(4,3,-6);camera.LookAt(game.Player.transform.position+Vector3.forward*.5f+Vector3.up);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"roller-car.png"));yield return null;
            game.Equip(3);camera.position=game.Player.transform.position+Vector3.up*1.7f;camera.rotation=Quaternion.Euler(38,game.Player.transform.eulerAngles.y,0);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"duck-sweeper.png"));yield return null;
            game.Equip(1);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"duck-collector.png"));yield return null;
            camera.position=new Vector3(60,28,22);camera.LookAt(new Vector3(20,0,80));
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"polished-meadow.png"));yield return null;
            camera.position=game.Park.lakeCenter+new Vector3(0,2,-12);camera.rotation=Quaternion.Euler(28,0,0);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"lake-bridges.png"));yield return null;
            camera.position=game.Park.Land(new Vector3(12,0,24))+Vector3.up*1.7f;camera.rotation=Quaternion.Euler(28,50,0);
            yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"dense-grass.png"));yield return null;
            foreach(int roller in new[]{1,4}){
            if(roller==4){game.Player.Teleport(game.Car.transform.position+Vector3.right*2);Check(game.Car.Enter(),"Enter for full car test");}else game.Equip(1);
            game.Player.Teleport(game.Park.Land(new Vector3(0,0,45)));game.Player.transform.rotation=Quaternion.identity;
            int overflowId=22000;while(!game.Population.IsAvailable(overflowId))overflowId++;
            game.Physics.Release(overflowId);game.Population.Detach(overflowId,false);game.Population.Settle(overflowId,game.Player.transform.position+new Vector3(0,.03f,roller==4?2.3f:1.7f),Quaternion.identity);
            int oldCarried=game.Progress.Data.carried;game.Progress.Data.carried=game.Progress.Capacity;Use(game,true);game.SweepFloor(Vector3.forward*.1f);yield return new WaitForSeconds(.15f);
            try{Check(game.CanDrive&&game.Progress.Data.carried==game.Progress.Capacity&&game.Population.IsPhysical(overflowId)&&game.Physics.PushedCount>0,"Full roller still drives and pushes uncollected overflow: "+roller);}catch(Exception e){Fail(e);yield break;}
            Use(game,false);game.Physics.FlushFront();game.Progress.Data.carried=oldCarried;if(game.RidingCar)Check(game.Car.Exit(),"Exit full-bag test car");
            }
            game.Player.Teleport(game.Car.transform.position+Vector3.right*2);
            try{Check(game.Car.Enter(),"Ride car for steering checks");float yaw=game.Player.transform.eulerAngles.y;game.Player.ApplyLook(new Vector2(35,0));Check(Mathf.Abs(Mathf.DeltaAngle(yaw,game.Player.transform.eulerAngles.y))<.01f,"Mouse look does not steer car");Use(game,true);game.Car.TickDrive(1,.6f);game.Player.SteerCar(1,.3f);Check(Mathf.Abs(Mathf.DeltaAngle(yaw,game.Player.transform.eulerAngles.y))>10,"Car steering changes heading");Use(game,false);Check(game.Car.Exit(),"Exit car safely");Check(!game.RidingCar&&game.Car.parkedCollider.enabled,"Car remains parked with collision");}catch(Exception e){Fail(e);yield break;}
            camera.position=game.Car.transform.position+new Vector3(4,3,-6);camera.LookAt(game.Car.transform.position+Vector3.up);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"parked-roller-car.png"));yield return null;
            var effectStation=game.Deposits.Stations[0];int eventValue=0;effectStation.onDuckDeposited.AddListener((id,value)=>eventValue=value);effectStation.Arrived(12345,5);
            try{Check(effectStation.LastDuckValue==5&&eventValue==5&&effectStation.depositEffectPrefab!=null,"Deposit effect event exposes variant value");}catch(Exception e){Fail(e);yield break;}
            camera.position=effectStation.transform.position+new Vector3(3,2,-4);camera.LookAt(effectStation.landing.position);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"deposit-effect.png"));yield return null;
            game.Equip(1);game.Player.Teleport(game.Park.Land(new Vector3(0,0,55)));game.Player.transform.rotation=Quaternion.identity;
            var contactIds=new int[12];int searchContact=25000;for(int n=0;n<contactIds.Length;n++){while(!game.Population.IsAvailable(searchContact))searchContact++;int id=contactIds[n]=searchContact++;game.Population.Detach(id,false);game.Population.Settle(id,game.Player.transform.position+new Vector3((n%3-1)*.25f,.03f,n<6?0:1.5f),Quaternion.identity);}
            int beforeContact=game.Progress.Data.carried,completedBefore=game.feedbackComponent.RollerCompletions;Use(game,true);game.SweepFloor(Vector3.forward*.5f);
            try{foreach(int id in contactIds)Check(!game.Population.IsAvailable(id)&&!game.Population.IsPhysical(id),"Roller collects every contact, including underfoot, beyond batch size");Check(game.Progress.Data.carried>=beforeContact+12,"Dense roller contacts reach bag");Check(game.feedbackComponent.RollerCompletions==completedBefore,"Completion audio waits for pull-in");}catch(Exception e){Fail(e);yield break;}
            Use(game,false);game.Physics.FlushFront();foreach(var visual in game.Physics.GetComponentsInChildren<Rigidbody>())if(visual.isKinematic)Check(!visual.detectCollisions,"Collected pull-in visuals cannot push the player or car");yield return new WaitForSeconds(1.2f);
            try{Check(game.feedbackComponent.RollerCompletions==completedBefore+1,"One completion sound after entire roller batch");game.Player.Teleport(game.Car.transform.position+Vector3.right*2);Check(game.Car.Enter(),"Enter for drivetrain checks");game.Car.Stop();float firstSpeed=game.Car.TickDrive(1,.2f);Check(firstSpeed>0&&firstSpeed<game.Progress.DriveSpeed,"Acceleration ramp");float coast=game.Car.TickDrive(0,.1f);Check(coast>=0&&coast<firstSpeed,"Release decelerates");for(int i=0;i<20;i++)game.Car.TickDrive(-1,.1f);Check(game.Car.CurrentSpeed<0&&game.Car.CurrentSpeed>=-game.Car.reverseSpeed,"S brakes then reverses with a capped reverse speed");game.Car.Stop();Check(game.Car.Exit(),"Exit after reverse test");}catch(Exception e){Fail(e);yield break;}
            int rollingId=27000;while(!game.Population.IsAvailable(rollingId))rollingId++;
            var rollingStart=game.Park.Land(new Vector3(0,0,65));
            Check(game.Physics.Launch(rollingId,rollingStart,Vector3.forward*6),"Launch settling test duck");
            Rigidbody rollingBody=null;foreach(var rb in game.Physics.GetComponentsInChildren<Rigidbody>())if(Vector3.Distance(rb.position,rollingStart+Vector3.up*.23f)<.1f)rollingBody=rb;
            Check(rollingBody!=null,"Find physical duck for pose check");rollingBody.transform.rotation=Quaternion.Euler(35,20,70);UnityEngine.Physics.SyncTransforms();rollingBody.linearVelocity=rollingBody.angularVelocity=Vector3.zero;rollingBody.Sleep();
            var frozenPivot=rollingBody.transform.TransformPoint(new Vector3(0,-.19f,0));var frozenRotation=rollingBody.transform.rotation;
            typeof(DuckPhysics).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game.Physics,null);
            try{Check(game.Population.IsAvailable(rollingId)&&!game.Population.IsPhysical(rollingId),"Sleeping duck returns to instancing");Check(Vector3.Distance(game.Population.Position(rollingId),frozenPivot)<.001f&&Quaternion.Angle(game.Population.Rotation(rollingId),frozenRotation)<.01f,"Settling preserves exact visible position and rotation");}catch(Exception e){Fail(e);yield break;}
            Check(game.Physics.Launch(rollingId,rollingStart+Vector3.up*3,Vector3.forward*10+Vector3.up*2),"Launch airborne throw regression");
            yield return new WaitForSeconds(.4f);
            try{Check(game.Population.Position(rollingId).z>rollingStart.z+2,"Low airborne damping preserves throw travel");}catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(5.5f);
            try{Check(!game.Population.IsPhysical(rollingId),"Duck stops after landing and rolling");}catch(Exception e){Fail(e);yield break;}
            int pileBase=28000;while(!game.Population.IsAvailable(pileBase)||!game.Population.IsAvailable(pileBase+1))pileBase+=2;var crowdedPile=game.Park.Land(new Vector3(0,0,58));for(int n=0;n<2;n++){game.Population.Detach(pileBase+n,false);game.Population.Settle(pileBase+n,crowdedPile+Vector3.up*n,Quaternion.identity);}
            int fillId=29000,fillIndex=0;while(game.Physics.ActiveCount<96&&fillId<35000){if(game.Population.IsAvailable(fillId)){var point=game.Park.Land(new Vector3(75+(fillIndex%12),0,210+fillIndex/12));if(game.Physics.Launch(fillId,point+Vector3.up*10,Vector3.zero))fillIndex++;}fillId++;}
            try{Check(game.Physics.ActiveCount==96,"Saturate rigidbody pool");Check(game.CollectId(pileBase)&&game.Physics.FallingCount>0,"Pile starts collapsing with no free Rigidbody");}catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(.45f);
            try{Check(game.Population.Position(pileBase+1).y<crowdedPile.y+.5f,"Instanced pile actually falls despite pool saturation");Check(game.feedbackComponent.rollerCollectionAudio!=null&&game.particles!=null,"Authored sound and particle hook components");}catch(Exception e){Fail(e);yield break;}
            game.SetMenu(true,true);
            try{Check(game.authoredHUD!=null&&game.HUD==game.authoredHUD&&game.HUD.HasAuthoredLayout,"Authored UI prefab is used");int level=game.Progress.Data.levels[0];foreach(var button in game.HUD.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(button.name=="Pickup Amount buy button")button.onClick.Invoke();Check(game.Progress.Data.levels[0]==level+1,"Serialized prefab buy button is bound");foreach(var outline in game.HUD.GetComponentsInChildren<UnityEngine.UI.Outline>(true))Check(Vector4.Distance(outline.effectColor,(Color)new Color32(249,192,1,255))<.01f,"Yellow UI outlines");}
            catch(Exception e){Fail(e);yield break;}
            var tabs=(UnityEngine.UI.Button[])typeof(DuckHUD).GetField("tabs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.HUD);
            var buys=(UnityEngine.UI.Button[])typeof(DuckHUD).GetField("buys",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.HUD);
            var titles=(UnityEngine.UI.Text[])typeof(DuckHUD).GetField("titles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.HUD);
            try{Check(!titles[4].transform.parent.gameObject.activeSelf,"Upgrades contains only player upgrades");tabs[1].onClick.Invoke();int equipped=game.Progress.Data.currentTool,level=game.Progress.Data.toolLevels[3];buys[0].onClick.Invoke();Check(game.Progress.Data.toolLevels[3]==level+1&&game.Progress.Data.currentTool==equipped,"Tools row upgrades its own purchased tool");Check(game.Player.View.clearFlags==CameraClearFlags.Skybox&&RenderSettings.skybox!=null,"Camera displays the scene skybox");Check(game.physicsComponent.rollingDuckPrefab!=null&&game.feedbackComponent.pickupAudio.clip!=null,"Authored physics and audio prefabs");}catch(Exception e){Fail(e);yield break;}
            var animated=buys[0];var initialScale=animated.transform.localScale;var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(animated.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);yield return new WaitForSecondsRealtime(.25f);
            try{Check(animated.transform.localScale.x>initialScale.x,"UI hover tween enlarges button");}catch(Exception e){Fail(e);yield break;}
            UnityEngine.EventSystems.ExecuteEvents.Execute(animated.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.25f);
            try{Check(animated.transform.localScale.x<initialScale.x,"UI press tween compresses button");}catch(Exception e){Fail(e);yield break;}
            UnityEngine.EventSystems.ExecuteEvents.Execute(animated.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);UnityEngine.EventSystems.ExecuteEvents.Execute(animated.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.25f);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"tool-shop.png"));yield return new WaitForSeconds(.5f);
            if(File.Exists(Path.Combine(output,"errors.txt"))){Application.Quit(3);yield break;}
            File.WriteAllText(Path.Combine(output,"PASS.txt"),"PASS: immediate click pickup and cooldown, incremental arrival credit, in-flight save invariants, per-duck bounce, repeated casket purchases, placement rejection, multiple stations, casket transfers, physical intake, sweeper contact/width, collector contact/speed, roller car capacity/auto-drive and final conservation; jumping/no double jump, shared capacity, four-sided physical intake on both stations, roller bag collection with front visuals and release pull-in, grouped deposits, breakable bushes, tree shaking, dense grass, floating ducks, bridge traversal, deposit and throw acceleration.");Application.Quit(0);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n");}
    }
}
