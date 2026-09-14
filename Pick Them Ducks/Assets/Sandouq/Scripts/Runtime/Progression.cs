using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    [Serializable] public struct DuckPose { public int id; public Vector3 position; public Quaternion rotation; }
    [Serializable] public struct CasketPlacement { public Vector3 position; public float yaw; }
    [Serializable] public class SaveData
    {
        public int version=3, total, seed, money, carried, deposited, currentTool;
        public int[] inventory=Array.Empty<int>(), collected=Array.Empty<int>();
        public bool[] owned={true,false,false,false,false};
        public int[] levels=new int[4], toolLevels=new int[5];
        public int casketKits;
        public string[] brokenBushes=Array.Empty<string>();
        public CasketPlacement[] stations=Array.Empty<CasketPlacement>();
        public DuckPose[] poses=Array.Empty<DuckPose>();
        public bool hasPlayerPose;
        public bool hasCarPose;public Vector3 carPosition;public float carYaw;
        public Vector3 playerPosition;
        public float playerYaw;
        // Retained only to migrate the old portable container. Contents drain visibly on load.
        public int[] casketDucks=Array.Empty<int>();
        public bool casketOwned, casketCarried;
        public Vector3 casketPosition=new Vector3(7,0,-4);
    }

    public sealed class Progression
    {
        public readonly SaveData Data;
        public readonly PrototypeSettings Settings;
        public event Action Changed;
        public bool Dirty { get; private set; }
        public Progression(PrototypeSettings settings,SaveData data)
        {
            Settings=settings; Data=data; DuckSaveSystem.Migrate(data,settings);
        }
        public DuckTool Tool=>(DuckTool)Data.currentTool;
        public ToolDefinition Equipment=>Settings.tools[Data.currentTool];
        public static int SharedBaseCapacity(SaveData data,PrototypeSettings settings)
        {int capacity=settings.tools[0].capacity;for(int i=0;i<data.owned.Length;i++)if(data.owned[i])capacity=Mathf.Max(capacity,settings.tools[i].capacity);return capacity;}
        public int CapacityFor(int index)=>SharedBaseCapacity(Data,Settings)+Mathf.RoundToInt(Data.levels[2]*Settings.upgrades[2].amount);
        public int Capacity=>CapacityFor(Data.currentTool);
        public int FreeSpace=>Mathf.Max(0,Capacity-Data.carried);
        public int PickupAmount=>Equipment.batch+Mathf.RoundToInt(Data.levels[0]*Settings.upgrades[0].amount);
        public float Interval=>Equipment.interval/(1+Data.levels[1]*Settings.upgrades[1].amount);
        public float Range=>Equipment.range;
        public float MovementMultiplier=>1+Data.levels[3]*Settings.upgrades[3].amount;
        public int ToolLevel=>Data.toolLevels[Data.currentTool];
        public bool CanUpgradeTool=>Tool==DuckTool.Sweeper||Tool==DuckTool.Collector||Tool==DuckTool.RollerCar;
        public int ToolUpgradeCost=>UpgradeCost(Data.currentTool);
        public bool CanUpgrade(int index)=>index>=0&&index<Data.owned.Length&&Data.owned[index]&&(index==1||index==3||index==4);
        public int UpgradeCost(int index)=>index>=0&&index<Data.toolLevels.Length?Mathf.CeilToInt((index==4?250:75)*Mathf.Pow(1.6f,Data.toolLevels[index])):int.MaxValue;
        public float WorkingWidth=>Equipment.range*(1+ToolLevel*.18f);
        public float DriveSpeed=>(Tool==DuckTool.RollerCar?20:8)*(1+ToolLevel*.12f)*MovementMultiplier;
        public bool Complete=>Data.deposited==Data.total;
        public void MarkSaved()=>Dirty=false;
        public void Touch(){Dirty=true;Changed?.Invoke();}
        public bool PickUp(){if(FreeSpace==0||Data.carried+Data.deposited+Data.casketDucks.Length>=Data.total)return false;Data.carried++;Touch();return true;}
        public void RecordPickup(int id){int n=Data.inventory.Length;Array.Resize(ref Data.inventory,n+1);Data.inventory[n]=id;}
        public int ThrowId=>Data.inventory.Length==0?-1:Data.inventory[Data.inventory.Length-1];
        public void Thrown(){Array.Resize(ref Data.inventory,Data.inventory.Length-1);Data.carried--;Touch();}
        public bool DepositInventory(int id)
        {
            int at=Array.IndexOf(Data.inventory,id);if(at<0)return false;
            var ids=new List<int>(Data.inventory);ids.RemoveAt(at);Data.inventory=ids.ToArray();Data.carried--;Credit(id);return true;
        }
        public bool DepositLegacy(int id)
        {
            int at=Array.IndexOf(Data.casketDucks,id);if(at<0)return false;
            var ids=new List<int>(Data.casketDucks);ids.RemoveAt(at);Data.casketDucks=ids.ToArray();Credit(id);return true;
        }
        public void Credit(int id=-1){Data.deposited++;Data.money=(int)Math.Min(int.MaxValue,(long)Data.money+Settings.ValueFor(id));Touch();}
        public bool BuyCasket(){if(Data.money<Settings.casketCost)return false;Data.money-=Settings.casketCost;Data.casketKits++;Touch();return true;}
        public bool InstallCasket(Vector3 position,float yaw)
        {
            if(Data.casketKits<=0)return false;
            int n=Data.stations.Length;Array.Resize(ref Data.stations,n+1);Data.stations[n]=new CasketPlacement{position=position,yaw=yaw};Data.casketKits--;Touch();return true;
        }
        public bool Equip(int index)
        {
            if(index<0||index>=Data.owned.Length||!Data.owned[index]||Data.carried>CapacityFor(index))return false;
            Data.currentTool=index;Touch();return true;
        }
        public bool BuyTool(int index)
        {
            if(index<1||index>=Settings.tools.Length||Data.owned[index]||Data.money<Settings.tools[index].cost)return false;
            Data.money-=Settings.tools[index].cost;Data.owned[index]=true;if(Data.carried<=CapacityFor(index))Data.currentTool=index;Touch();return true;
        }
        public bool BuyUpgrade(int index)
        {
            if(index<0||index>=Settings.upgrades.Length)return false;
            var def=Settings.upgrades[index];int level=Data.levels[index];if(level>=def.maxLevel||Data.money<def.Cost(level))return false;
            Data.money-=def.Cost(level);Data.levels[index]++;Touch();return true;
        }
        public bool BuyToolUpgrade()=>BuyToolUpgrade(Data.currentTool);
        public bool BuyToolUpgrade(int index)
        {
            if(!CanUpgrade(index)||Data.toolLevels[index]>=6||Data.money<UpgradeCost(index))return false;
            Data.money-=UpgradeCost(index);Data.toolLevels[index]++;Touch();return true;
        }
    }

    public static class DuckSaveSystem
    {
        public static string DefaultPath=>Path.Combine(Application.persistentDataPath,"duck-stage-v1.json");
        public static string LastError {get;private set;}
        public static void Migrate(SaveData d,PrototypeSettings settings)
        {
            if(d.version>=3)return;
            if(d.version<1||d.owned==null||d.levels==null||d.collected==null||d.carried<0||d.carried>d.collected.Length)throw new InvalidDataException("Invalid legacy save");
            if(d.version==1){d.inventory=new int[d.carried];Array.Copy(d.collected,d.collected.Length-d.carried,d.inventory,0,d.carried);}
            d.casketDucks=d.casketDucks??Array.Empty<int>();
            int[] old=d.levels;d.levels=new int[4];d.levels[2]=old[0];d.levels[1]=old[1];
            // Retired vacuum-only upgrades are refunded; shared capacity and hold-speed levels survive.
            for(int index=2;index<Math.Min(4,old.Length);index++)for(int level=0;level<old[index];level++)d.money+=Mathf.CeilToInt((index==2?80:100)*Mathf.Pow(1.5f,level));
            Array.Resize(ref d.owned,5);d.toolLevels=new int[5];
            d.stations=d.casketOwned?new[]{new CasketPlacement{position=d.casketPosition}}:Array.Empty<CasketPlacement>();
            d.casketOwned=d.casketCarried=false;d.casketKits=0;
            if(d.carried>settings.tools[d.currentTool].capacity+d.levels[2]*25) d.levels[2]=Mathf.CeilToInt((d.carried-settings.tools[d.currentTool].capacity)/25f);
            d.version=3;
        }
        public static SaveData Load(PrototypeSettings settings,string path=null)
        {
            path=path??DefaultPath;LastError=null;
            foreach(string candidate in new[]{path,path+".bak"})
            {
                if(!File.Exists(candidate))continue;
                try{var d=JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));Migrate(d,settings);if(!Valid(d,settings))throw new InvalidDataException("Invalid duck save");return d;}
                catch(Exception e){LastError=e.Message;Debug.LogWarning("Duck save: "+e.Message);}
            }
            return new SaveData{total=settings.totalDucks,seed=settings.seed};
        }
        public static bool Valid(SaveData d,PrototypeSettings settings)
        {
            if(d==null||d.version!=3||d.total<1||d.total>1000000||d.money<0||d.carried<0||d.deposited<0||d.casketKits<0||d.stations==null||d.inventory==null||d.casketDucks==null||d.collected==null||d.owned==null||d.owned.Length!=5||!d.owned[0]||d.levels==null||d.levels.Length!=4||d.toolLevels==null||d.toolLevels.Length!=5||d.currentTool<0||d.currentTool>=5||!d.owned[d.currentTool])return false;
            if((long)d.carried+d.deposited+d.casketDucks.Length!=d.collected.Length||d.inventory.Length!=d.carried||d.collected.Length>d.total||d.carried>Progression.SharedBaseCapacity(d,settings)+d.levels[2]*settings.upgrades[2].amount)return false;
            for(int i=0;i<4;i++)if(d.levels[i]<0||d.levels[i]>settings.upgrades[i].maxLevel)return false;
            foreach(int level in d.toolLevels)if(level<0||level>6)return false;
            if(d.hasCarPose&&(!Finite(d.carPosition)||!float.IsFinite(d.carYaw)))return false;
            if(d.hasPlayerPose&&(!Finite(d.playerPosition)||!float.IsFinite(d.playerYaw)))return false;
            foreach(var station in d.stations)if(!Finite(station.position)||!float.IsFinite(station.yaw))return false;
            var seen=new HashSet<int>();foreach(int id in d.collected)if(id<0||id>=d.total||!seen.Add(id))return false;
            var held=new HashSet<int>();foreach(int id in d.inventory)if(!seen.Contains(id)||!held.Add(id))return false;
            foreach(int id in d.casketDucks)if(!seen.Contains(id)||!held.Add(id))return false;
            var moved=new HashSet<int>();if(d.poses!=null)foreach(var pose in d.poses){var q=pose.rotation;if(pose.id<0||pose.id>=d.total||seen.Contains(pose.id)||!moved.Add(pose.id)||!Finite(pose.position)||!float.IsFinite(q.x)||!float.IsFinite(q.y)||!float.IsFinite(q.z)||!float.IsFinite(q.w)||q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w<.5f)return false;}
            return true;
        }
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&p.sqrMagnitude<100000000;
        public static bool Save(SaveData data,string path=null)
        {
            path=path??DefaultPath;
            try{Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(data));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);LastError=null;return true;}
            catch(Exception e){LastError=e.Message;Debug.LogWarning("Could not save ducks: "+e.Message);return false;}
        }
    }
}
