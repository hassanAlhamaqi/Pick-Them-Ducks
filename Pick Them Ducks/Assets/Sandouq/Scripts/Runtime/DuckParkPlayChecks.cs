using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    // Explicit opt-in test player; never opens the player's save.
    public sealed class DuckParkPlayChecks : MonoBehaviour
    {
        public static bool Running { get; private set; }
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"--duck-park-check");
            if(index<0 || index+1>=args.Length)return;
            Running=true;var runner=new GameObject("Park play checks").AddComponent<DuckParkPlayChecks>();runner.output=args[index+1];DontDestroyOnLoad(runner);
            Application.runInBackground=true;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            yield return null;yield return null;
            var game=FindAnyObjectByType<DuckGame>();
            try
            {
                Check(game!=null && game.Progress!=null && game.Park!=null,"Authored game initialization");
                var p=game.Progress;var pop=game.Population;
                Check(game.Stage.Shop!=null && game.Stage.ToolModels.Length==5 && game.Park.terrain!=null,"Scene references");
                Check(game.CollectId(0),"Pickup");Check(p.Data.carried==1 && pop.Remaining==p.Data.total-1,"Pickup conservation");
                Check(game.ThrowDuck(),"Throw inventory");Check(p.Data.carried==0 && pop.Remaining==p.Data.total && game.Physics.ActiveCount==1,"Throw conservation");
                Check(!game.ThrowDuck(),"Empty throw must fail");
                p.Data.money=450;game.BuyCasket();Check(p.Data.casketOwned && p.Data.money==0,"Casket purchase");
                for(int i=1;i<=10;i++)Check(game.CollectId(i),"Fill hand inventory");
                Check(p.Stash()==10 && p.Data.carried==0 && p.Data.casketDucks.Length==10,"Stash transaction");
                Check(game.Deposit()==0 && p.Data.casketDucks.Length==10,"Remote casket deposit blocked");
                game.Player.Teleport(game.Park.casket.position+Vector3.back);game.ToggleCasket();Check(game.CarryingCasket,"Physical casket pickup");
                game.Player.Teleport(game.Stage.BoxPosition+Vector3.back*2);Check(game.Deposit()==10 && p.Data.money==10,"Carried casket deposit");Check(game.Deposit()==0,"Repeated deposit");game.ToggleCasket();
                p.Data.money=5000;Check(p.BuyTool(3) && p.Tool==DuckTool.Fork,"Fork purchase");Check(p.BuyTool(4) && p.Capacity==250,"Industrial vacuum purchase");
                p.Data.collected=pop.CollectedIds();p.Data.poses=pop.Poses();Check(DuckSaveSystem.Valid(p.Data,game.Settings),"Save invariants after throw and casket");
                string save=Path.Combine(output,"test-save.json");Check(DuckSaveSystem.Save(p.Data,save),"Save write");var restored=DuckSaveSystem.Load(game.Settings,save);Check(restored.deposited==10 && restored.casketOwned,"Save roundtrip");
                var legacy=new SaveData{version=1,total=1000,seed=1731,carried=2,deposited=1,collected=new[]{4,5,6},owned=new[]{true,false,false},levels=new int[4]};
                Check(DuckSaveSystem.Save(legacy,save),"Legacy fixture");restored=DuckSaveSystem.Load(game.Settings,save);Check(restored.version==2 && restored.carried==2 && restored.inventory[0]==5 && restored.owned.Length==5,"Legacy save migration");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(13);
            try
            {
                Check(game.Physics.ActiveCount==0 && game.Population.IsAvailable(0),"Physics returns to instancing after settling");
                var location=game.Population.Position(0);Check(game.Population.Query(location+Vector3.up*1.5f,Vector3.down,2,.9f,.3f)==0,"Thrown duck is collectible at its new position");
                int before=game.Population.Remaining;var target=game.Population.Position(1000);Check(game.Physics.Push(target+Vector3.back,Vector3.forward,3,12,6)>0,"Fork push activates bodies");Check(game.Population.Remaining==before,"Push must not collect ducks");
                for(int id=0;id<game.Population.Total;id++)if(game.Population.IsPhysical(id))Check(game.CollectId(id),"Collect moving duck");
                Check(game.Physics.ActiveCount==0,"Moving pickup releases proxy");
                for(int i=0;i<96;i++)Check(game.Physics.Launch(10000+i,new Vector3(40+i*.6f,1,20),Vector3.forward),"Fill physics pool");
                int held=game.Progress.Data.carried;Check(!game.ThrowDuck() && game.Progress.Data.carried==held,"Full pool must preserve thrown inventory");
                for(int i=0;i<96;i++)Check(game.CollectId(10000+i),"Release saturated pool");
                Check(game.Physics.ActiveCount==0,"Pool reusable after saturation");
            }
            catch(Exception e){Fail(e);yield break;}
            yield return new WaitForSeconds(13);
            game.SetMenu(false);game.Player.enabled=false;
            var camera=game.Player.View.transform;
            camera.position=new Vector3(80,110,-70);camera.LookAt(new Vector3(-15,0,110));
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"park-overview.png"));yield return null;
            camera.position=new Vector3(-12,2,-12);camera.LookAt(new Vector3(1,1,-4));
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"park-hub.png"));yield return null;
            camera.position=new Vector3(-22,5,47);camera.LookAt(new Vector3(-64,0,105));
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"park-lake.png"));yield return null;
            game.SetMenu(true,true);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"park-shop.png"));yield return new WaitForSeconds(1);
            File.WriteAllText(Path.Combine(output,"PASS.txt"),"PASS: authored references; pickup/throw conservation; empty throw; casket purchase, stash, physical transport, deposit; duplicate deposit; fork and industrial tool purchases; v2 save roundtrip; v1 migration; rigidbody retirement and relocated query; fork conservation; moving pickup; pool saturation and reuse. Render captures saved.");Application.Quit(0);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error || type==LogType.Exception)File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+trace+"\n");}
        void Fail(Exception e){File.WriteAllText(Path.Combine(output,"FAILED.txt"),e.ToString());Application.Quit(2);}
    }
}
