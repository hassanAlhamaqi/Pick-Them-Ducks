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
                Check(!game.TickHand(0,true,game.Progress.Interval*.8f)&&game.Progress.Data.carried==0,"Hand must finish its hold");
                game.TickHand(-1,false,0);Check(!game.TickHand(0,true,game.Progress.Interval*.3f),"Release resets hold");Check(game.TickHand(0,true,game.Progress.Interval)&&game.Progress.Data.carried==1,"Completed hand hold picks up");
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
                Check(game.Physics.Launch(1001,installed.transform.position+Vector3.back*2.4f+Vector3.up*.1f,Vector3.forward*7),"Launch duck into casket");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1.5f);
            try
            {
                Check(game.Progress.Data.deposited==before+1&&installed.Landed==2,"Pushed duck auto-deposits once");
                game.BuyTool(3);float width=game.Progress.WorkingWidth;game.BuyToolUpgrade();Check(game.Progress.WorkingWidth>width,"Sweeper grows");
                game.Player.Teleport(game.Park.Land(new Vector3(30,0,10))+Vector3.up*.05f);game.Player.transform.rotation=Quaternion.identity;
                game.Population.Detach(2000,false);game.Population.Settle(2000,game.Park.Land(game.Player.transform.position+Vector3.forward*1.5f),Quaternion.identity);
                Check(game.SweepFloor(Vector3.forward*.15f)>0,"Floor sweeper pushes contacted ducks");
                game.BuyTool(1);float speed=game.Progress.DriveSpeed;game.BuyToolUpgrade();Check(game.Progress.DriveSpeed>speed,"Collector grows faster");
                game.Population.Detach(3000,false);game.Population.Settle(3000,game.Park.Land(game.Player.transform.position+Vector3.forward*1.5f),Quaternion.identity);
                Check(game.SweepFloor(Vector3.forward*.2f)>0,"Roller pushes floor contact");
                game.BuyTool(4);Check(game.Progress.Capacity>=600,"Roller car biggest bag");
                game.Player.Teleport(game.Park.Land(new Vector3(0,0,70))+Vector3.up*.05f);game.SetMenu(false);Use(game,true);
            }
            catch(Exception e){Fail(e);yield break;}
            Vector3 start=game.Player.transform.position;yield return new WaitForSeconds(.3f);Use(game,false);
            try{Check((game.Player.transform.position-start).magnitude>1,"Vehicle auto-forward movement");game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Final conservation");}
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
                    game.Physics.Launch(id,station.transform.position+outward*2.6f+Vector3.up*.12f,-outward*5);
                }
                yield return new WaitForSeconds(2);
                try{Check(station.Landed==expected,"Physical intake from all four sides: "+station.name);}
                catch(Exception e){Fail(e);yield break;}
            }
            foreach(int tool in new[]{1,4})
            {
                game.Equip(tool);game.Player.Teleport(game.Park.Land(new Vector3(0,0,70)));game.Player.transform.rotation=Quaternion.identity;
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
            for(int i=15000;i<15003;i++)game.CollectId(i);
            game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*3);game.Deposit();
            yield return null;
            try{Check(game.Deposits.InFlight==3,"Pickup amount three launches three simultaneous ducks");}
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(1.6f);game.Progress.Data.levels[0]=0;
            for(int i=14000;i<14200;i++)game.CollectId(i);
            game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*3);game.Deposit();
            int first=game.Progress.Data.deposited;
            yield return new WaitForSeconds(1);int slow=game.Progress.Data.deposited-first;
            yield return new WaitForSeconds(2);first=game.Progress.Data.deposited;
            yield return new WaitForSeconds(1);int fast=game.Progress.Data.deposited-first;
            try{Check(fast>slow*2,"Sustained deposit accelerates");}
            catch(Exception e){Fail(e);yield break;}
            game.Deposit();yield return new WaitForSeconds(.5f);
            game.Player.Teleport(game.Park.Land(new Vector3(0,0,100)));game.Player.View.transform.rotation=Quaternion.Euler(-15,0,0);
            int carried=game.Progress.Data.carried;float elapsed=0;
            while(elapsed<1){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            int slowThrow=carried-game.Progress.Data.carried;elapsed=0;
            while(elapsed<2){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            carried=game.Progress.Data.carried;elapsed=0;
            while(elapsed<1){game.TickThrow(true,Time.deltaTime);elapsed+=Time.deltaTime;yield return null;}
            int fastThrow=carried-game.Progress.Data.carried;game.TickThrow(false,0);
            try{Check(fastThrow>slowThrow*2,"Held throws accelerate");game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Polish conservation");}
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
            game.SetMenu(true,true);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"tool-shop.png"));yield return new WaitForSeconds(.5f);
            if(File.Exists(Path.Combine(output,"errors.txt"))){Application.Quit(3);yield break;}
            File.WriteAllText(Path.Combine(output,"PASS.txt"),"PASS: timed hold/release, incremental arrival credit, in-flight save invariants, per-duck bounce, repeated casket purchases, placement rejection, multiple stations, casket transfers, physical intake, sweeper contact/width, collector contact/speed, roller car capacity/auto-drive and final conservation; jumping/no double jump, shared capacity, four-sided physical intake on both stations, roller bag collection with front visuals and release pull-in, grouped deposits, breakable bushes, tree shaking, dense grass, floating ducks, bridge traversal, deposit and throw acceleration.");Application.Quit(0);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n");}
    }
}
