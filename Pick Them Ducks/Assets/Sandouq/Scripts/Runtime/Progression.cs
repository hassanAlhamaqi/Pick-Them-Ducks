using System;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    [Serializable] public struct DuckPose { public int id; public Vector3 position; public Quaternion rotation; }
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public int[] inventory = Array.Empty<int>();
        public int[] casketDucks = Array.Empty<int>();
        public bool casketOwned;
        public bool casketCarried, hasPlayerPose;
        public Vector3 playerPosition;
        public float playerYaw;
        public Vector3 casketPosition = new Vector3(7,0,-4);
        public DuckPose[] poses = Array.Empty<DuckPose>();
        public int total;
        public int seed;
        public int money;
        public int carried;
        public int deposited;
        public int currentTool;
        public bool[] owned = { true, false, false, false, false };
        public int[] levels = new int[4];
        public int[] collected = Array.Empty<int>();
    }

    // The inventory, economy and shop share one transaction state. A duck can be either
    // in the world, carried, or deposited; only Deposit is allowed to mint money.
    public sealed class Progression
    {
        public readonly SaveData Data;
        public readonly PrototypeSettings Settings;
        public event Action Changed;
        public bool Dirty { get; private set; }
        public Progression(PrototypeSettings settings, SaveData data) { Settings = settings; Data = data;
            Array.Resize(ref Data.owned, settings.tools.Length); Array.Resize(ref Data.levels, settings.upgrades.Length);
            if(Data.inventory==null || Data.inventory.Length!=Data.carried) { Data.inventory=new int[Data.carried]; Array.Copy(Data.collected,Math.Max(0,Data.collected.Length-Data.carried),Data.inventory,0,Data.carried); }
            if(Data.casketDucks==null)Data.casketDucks=Array.Empty<int>(); Data.version=2; }
        public DuckTool Tool => (DuckTool)Data.currentTool;
        public ToolDefinition Equipment => Settings.tools[Data.currentTool];
        public int Capacity => Equipment.capacity + Mathf.RoundToInt(Data.levels[0] * Settings.upgrades[0].amount);
        public int FreeSpace => Mathf.Max(0, Capacity - Data.carried);
        public float Range => Equipment.range + ((Tool == DuckTool.Vacuum || Tool==DuckTool.IndustrialVacuum) ? Data.levels[2] * Settings.upgrades[2].amount : 0);
        public float Interval => Equipment.interval / (1 + Data.levels[(Tool == DuckTool.Vacuum || Tool==DuckTool.IndustrialVacuum) ? 3 : 1] * Settings.upgrades[(Tool == DuckTool.Vacuum || Tool==DuckTool.IndustrialVacuum) ? 3 : 1].amount);
        public bool Complete => Data.deposited == Data.total;
        public void MarkSaved() => Dirty = false;
        void Notify() { Dirty = true; Changed?.Invoke(); }
        public int CasketCapacity => 1000;
        public void Touch() => Notify();
        public bool BuyCasket() { if(Data.casketOwned || Data.money<450)return false; Data.money-=450; Data.casketOwned=true; Notify(); return true; }
        public int Stash() { int n=Mathf.Min(Data.carried,CasketCapacity-Data.casketDucks.Length); var bag=new System.Collections.Generic.List<int>(Data.inventory); var box=new System.Collections.Generic.List<int>(Data.casketDucks); box.AddRange(bag.GetRange(bag.Count-n,n)); bag.RemoveRange(bag.Count-n,n); Data.inventory=bag.ToArray(); Data.casketDucks=box.ToArray(); Data.carried-=n; Notify(); return n; }
        public void RecordPickup(int id) { var ids=new System.Collections.Generic.List<int>(Data.inventory); ids.Add(id); Data.inventory=ids.ToArray(); }
        public int ThrowId => Data.inventory.Length==0 ? -1 : Data.inventory[Data.inventory.Length-1];
        public void Thrown() { Array.Resize(ref Data.inventory,Data.inventory.Length-1); Data.carried--; Notify(); }
        public bool PickUp() { if (FreeSpace == 0 || Data.carried + Data.deposited + Data.casketDucks.Length >= Data.total) return false; Data.carried++; Notify(); return true; }
        public int Deposit()
        {
            int amount = Data.carried;
            if (amount == 0) return 0;
            Data.carried = 0; Data.inventory=Array.Empty<int>(); Data.deposited += amount;
            Data.money += amount * Settings.moneyPerDuck; Notify(); return amount;
        }
        public bool Equip(int index)
        {
            if (index < 0 || index >= Data.owned.Length || !Data.owned[index]) return false;
            // Never discard ducks when switching down to a smaller tool.
            if (Data.carried > Settings.tools[index].capacity + Data.levels[0] * Settings.upgrades[0].amount) return false;
            Data.currentTool = index; Notify(); return true;
        }
        public bool BuyTool(int index)
        {
            if (index < 1 || index >= Settings.tools.Length || Data.owned[index] || Data.money < Settings.tools[index].cost) return false;
            Data.money -= Settings.tools[index].cost; Data.owned[index] = true; if(Data.carried<=Settings.tools[index].capacity+Data.levels[0]*Settings.upgrades[0].amount) Data.currentTool = index; Notify(); return true;
        }
        public bool BuyUpgrade(int index)
        {
            if (index < 0 || index >= Settings.upgrades.Length || (index >= 2 && !Data.owned[2] && !Data.owned[4])) return false;
            var def = Settings.upgrades[index]; int level = Data.levels[index]; int cost = def.Cost(level);
            if (level >= def.maxLevel || Data.money < cost) return false;
            Data.money -= cost; Data.levels[index]++; Notify(); return true;
        }
    }

    public static class DuckSaveSystem
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "duck-stage-v1.json");
        public static string LastError { get; private set; }
        public static SaveData Load(PrototypeSettings settings, string path = null)
        {
            path = path ?? DefaultPath; LastError = null;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));
                    if(data!=null && data.version==1 && data.owned!=null && data.levels!=null && data.collected!=null && data.carried>=0 && data.carried<=data.collected.Length) { var migration=new Progression(settings,data); }
                    if (!Valid(data, settings)) throw new InvalidDataException("Invalid or incompatible duck save.");
                    return data;
                }
                catch (Exception e) { LastError = e.Message; Debug.LogWarning("Duck save: " + e.Message); }
            }
            return new SaveData { total = settings.totalDucks, seed = settings.seed };
        }
        public static bool Valid(SaveData d, PrototypeSettings settings)
        {
            if (d == null || (d.version != 1 && d.version != 2) || d.total < 1 || d.total > 1000000 || d.money < 0 || d.carried < 0 || d.deposited < 0 ||
                d.owned == null || d.owned.Length != settings.tools.Length || !d.owned[0] ||
                d.levels == null || d.levels.Length != settings.upgrades.Length || d.currentTool < 0 || d.currentTool >= d.owned.Length || !d.owned[d.currentTool] ||
                d.collected == null || (long)d.carried + d.deposited + (d.casketDucks?.Length ?? 0) != d.collected.Length || d.collected.Length > d.total) return false;
            for (int i = 0; i < d.levels.Length; i++) if (d.levels[i] < 0 || d.levels[i] > settings.upgrades[i].maxLevel) return false;
            if (d.carried > settings.tools[d.currentTool].capacity + d.levels[0] * settings.upgrades[0].amount) return false;
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (int id in d.collected) if (id < 0 || id >= d.total || !seen.Add(id)) return false;
            if(d.version==2) {
                if(d.inventory==null || d.inventory.Length!=d.carried || d.casketDucks==null || d.casketDucks.Length>1000 || (!d.casketOwned && d.casketDucks.Length>0))return false;
                var held=new System.Collections.Generic.HashSet<int>();
                foreach(var id in d.inventory)if(!seen.Contains(id)||!held.Add(id))return false;
                foreach(var id in d.casketDucks)if(!seen.Contains(id)||!held.Add(id))return false;
                if(d.casketCarried && !d.casketOwned)return false;
                if(!Finite(d.casketPosition) || (d.hasPlayerPose && (!Finite(d.playerPosition)||!float.IsFinite(d.playerYaw))))return false;
                var poseIds=new System.Collections.Generic.HashSet<int>();
                if(d.poses!=null)foreach(var pose in d.poses) {
                    var q=pose.rotation;
                    if(pose.id<0||pose.id>=d.total||seen.Contains(pose.id)||!poseIds.Add(pose.id)||!Finite(pose.position)||!float.IsFinite(q.x)||!float.IsFinite(q.y)||!float.IsFinite(q.z)||!float.IsFinite(q.w)||q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w<.5f)return false;
                }
            }
            return true;
        }
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&Mathf.Abs(p.x)<10000&&Mathf.Abs(p.y)<10000&&Mathf.Abs(p.z)<10000;
        public static bool Save(SaveData data, string path = null)
        {
            path = path ?? DefaultPath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                LastError = null; return true;
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("Could not save ducks: " + e.Message); return false; }
        }
    }
}
