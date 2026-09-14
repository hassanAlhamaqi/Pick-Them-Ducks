using System;
using System.IO;
using UnityEngine;
namespace Sandouq.Ducks.Editor
{
    public static class DuckPrototypeChecks
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(PrototypeSettings settings)
        {
            Directory.CreateDirectory("Logs");
            var data=new SaveData{total=1000,seed=1731};var p=new Progression(settings,data);
            for(int i=0;i<10;i++){Check(p.PickUp(),"Pickup capacity");p.RecordPickup(i);}Check(!p.PickUp(),"Full bag");
            Check(p.DepositInventory(0)&&data.carried==9&&data.deposited==1&&data.money==1,"One-duck deposit transaction");Check(!p.DepositInventory(0),"Duplicate landing");
            data.collected=new[]{0,1,2,3,4,5,6,7,8,9};Check(DuckSaveSystem.Valid(data,settings),"In-flight inventory snapshot");
            data.money=10000;for(int i=0;i<4;i++)Check(p.BuyUpgrade(i),"Each global upgrade is unlocked");Check(p.PickupAmount==2&&p.Interval<.65f&&p.Capacity==35&&p.MovementMultiplier>1,"Upgrade effects");
            Check(p.BuyCasket()&&p.BuyCasket()&&data.casketKits==2,"Repeat casket purchases");Check(p.InstallCasket(new Vector3(0,0,18),0)&&p.InstallCasket(new Vector3(0,0,30),45)&&!p.InstallCasket(Vector3.zero,0),"Installation consumes kits once");
            Check(p.BuyTool(3),"Sweeper purchase");float width=p.WorkingWidth;Check(p.BuyToolUpgrade()&&p.WorkingWidth>width,"Sweeper width upgrade");
            Check(p.BuyTool(1),"Collector purchase");float speed=p.DriveSpeed;width=p.WorkingWidth;Check(p.BuyToolUpgrade()&&p.DriveSpeed>speed&&p.WorkingWidth>width,"Collector width/speed upgrade");
            Check(p.BuyTool(4)&&p.Capacity>settings.tools[1].capacity,"Vehicle capacity");Check(DuckSaveSystem.Valid(data,settings),"Upgraded station save");
            int equipped=data.currentTool, coins=data.money, cost=p.UpgradeCost(3), prior=data.toolLevels[3];Check(p.BuyToolUpgrade(3)&&data.toolLevels[3]==prior+1&&data.currentTool==equipped&&data.money==coins-cost,"Upgrade an owned tool without equipping it");Check(!p.BuyToolUpgrade(2)&&!p.BuyToolUpgrade(-1),"Reject unsupported or invalid tool upgrades");
            var valueSettings=UnityEngine.Object.Instantiate(settings);var variant=ScriptableObject.CreateInstance<DuckVariant>();variant.coinValue=7;valueSettings.variantRanges=new[]{new DuckVariantRange{firstDuckId=17,count=1,variant=variant}};var valuedData=new SaveData{total=100};var valued=new Progression(valueSettings,valuedData);valued.PickUp();valued.RecordPickup(17);Check(valued.DepositInventory(17)&&valuedData.deposited==1&&valuedData.money==7&&!valued.DepositInventory(17),"Variant value credits once without changing duck count");valuedData.casketDucks=new[]{17};Check(valued.DepositLegacy(17)&&valuedData.money==14,"Legacy deposits use variant value");valued.Credit(17);Check(valuedData.money==21&&valuedData.deposited==3,"World deposits use variant value");UnityEngine.Object.DestroyImmediate(valueSettings);UnityEngine.Object.DestroyImmediate(variant);
            data.hasCarPose=true;data.carPosition=new Vector3(6,0,18);data.carYaw=45;
            string path=Path.GetFullPath("Logs/tool-save-test.json");Check(DuckSaveSystem.Save(data,path),"Save");var reloaded=DuckSaveSystem.Load(settings,path);Check(reloaded.stations.Length==2,"Multiple stations reload");Check(reloaded.hasCarPose&&reloaded.carPosition==data.carPosition&&reloaded.carYaw==45,"Parked car pose reloads");
            var legacy=new SaveData{version=2,total=1000,carried=2,deposited=1,inventory=new[]{1,2},collected=new[]{0,1,2,3,4},casketDucks=new[]{3,4},casketOwned=true,owned=new[]{true,true,true,true,true},levels=new[]{2,1,1,1}};
            DuckSaveSystem.Migrate(legacy,settings);Check(legacy.version==3&&legacy.stations.Length==1&&legacy.casketDucks.Length==2&&legacy.levels[2]==2&&legacy.money==180&&DuckSaveSystem.Valid(legacy,settings),"Legacy casket and upgrades migration");
            var root=new GameObject("Tool spatial validation");
            try{var pop=root.AddComponent<DuckPopulationManager>();pop.Initialize(settings,new SaveData{total=1000,seed=1731},null);Check(pop.Reserve(0)&&!pop.IsAvailable(0)&&!pop.IsPhysical(0)&&pop.Remaining==1000,"World flight reservation");Check(pop.Remove(0)&&!pop.Remove(0)&&pop.Remaining==999,"World landing once");}
            finally{UnityEngine.Object.DestroyImmediate(root);}
            File.WriteAllText("Logs/tools-logic-PASS.txt","PASS: single-duck deposit, duplicate landing guard, four upgrades, repeat casket kits/installation, tool upgrades, save/load, legacy migration and world-flight reservations.");
        }
    }
}
