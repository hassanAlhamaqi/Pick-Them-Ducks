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
                Check(game.SweepFloor(Vector3.forward*.2f)>0,"Roller collector picks up floor contact");
                game.BuyTool(4);Check(game.Progress.Capacity>=600,"Roller car biggest bag");
                game.Player.Teleport(game.Park.Land(new Vector3(0,0,70))+Vector3.up*.05f);game.SetMenu(false);Use(game,true);
            }
            catch(Exception e){Fail(e);yield break;}
            Vector3 start=game.Player.transform.position;yield return new WaitForSeconds(.3f);Use(game,false);
            try{Check((game.Player.transform.position-start).magnitude>1,"Vehicle auto-forward movement");game.Progress.Data.collected=game.Population.CollectedIds();game.Progress.Data.poses=game.Population.Poses();Check(DuckSaveSystem.Valid(game.Progress.Data,game.Settings),"Final conservation");}
            catch(Exception e){Fail(e);yield break;}
            game.Player.enabled=false;var camera=game.Player.View.transform;camera.position=installed.transform.position+new Vector3(3,2,-4);camera.LookAt(installed.transform.position+Vector3.up*.5f);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"installed-casket.png"));yield return null;
            camera.position=game.Player.transform.position+new Vector3(4,3,-6);camera.LookAt(game.Player.transform.position+Vector3.forward*.5f+Vector3.up);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"roller-car.png"));yield return null;
            game.Equip(3);camera.position=game.Player.transform.position+Vector3.up*1.7f;camera.rotation=Quaternion.Euler(38,game.Player.transform.eulerAngles.y,0);
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"duck-sweeper.png"));yield return null;
            game.Equip(1);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"duck-collector.png"));yield return null;
            game.SetMenu(true,true);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"tool-shop.png"));yield return new WaitForSeconds(.5f);
            if(File.Exists(Path.Combine(output,"errors.txt"))){Application.Quit(3);yield break;}
            File.WriteAllText(Path.Combine(output,"PASS.txt"),"PASS: timed hold/release, incremental arrival credit, in-flight save invariants, per-duck bounce, repeated casket purchases, placement rejection, multiple stations, casket transfers, physical intake, sweeper contact/width, collector contact/speed, roller car capacity/auto-drive and final conservation.");Application.Quit(0);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n");}
    }
}
