using System;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int total;
        public int seed;
        public int money;
        public int carried;
        public int deposited;
        public int currentTool;
        public bool[] owned = { true, false, false };
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
        public Progression(PrototypeSettings settings, SaveData data) { Settings = settings; Data = data; }
        public DuckTool Tool => (DuckTool)Data.currentTool;
        public ToolDefinition Equipment => Settings.tools[Data.currentTool];
        public int Capacity => Equipment.capacity + Mathf.RoundToInt(Data.levels[0] * Settings.upgrades[0].amount);
        public int FreeSpace => Mathf.Max(0, Capacity - Data.carried);
        public float Range => Equipment.range + (Tool == DuckTool.Vacuum ? Data.levels[2] * Settings.upgrades[2].amount : 0);
        public float Interval => Equipment.interval / (1 + Data.levels[Tool == DuckTool.Vacuum ? 3 : 1] * Settings.upgrades[Tool == DuckTool.Vacuum ? 3 : 1].amount);
        public bool Complete => Data.deposited == Data.total;
        public void MarkSaved() => Dirty = false;
        void Notify() { Dirty = true; Changed?.Invoke(); }
        public bool PickUp() { if (FreeSpace == 0 || Data.carried + Data.deposited >= Data.total) return false; Data.carried++; Notify(); return true; }
        public int Deposit()
        {
            int amount = Data.carried;
            if (amount == 0) return 0;
            Data.carried = 0; Data.deposited += amount;
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
            Data.money -= Settings.tools[index].cost; Data.owned[index] = true; Data.currentTool = index; Notify(); return true;
        }
        public bool BuyUpgrade(int index)
        {
            if (index < 0 || index >= Settings.upgrades.Length || (index >= 2 && !Data.owned[2])) return false;
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
                    if (!Valid(data, settings)) throw new InvalidDataException("Invalid or incompatible duck save.");
                    return data;
                }
                catch (Exception e) { LastError = e.Message; Debug.LogWarning("Duck save: " + e.Message); }
            }
            return new SaveData { total = settings.totalDucks, seed = settings.seed };
        }
        public static bool Valid(SaveData d, PrototypeSettings settings)
        {
            if (d == null || d.version != 1 || d.total < 1 || d.total > 1000000 || d.money < 0 || d.carried < 0 || d.deposited < 0 ||
                d.owned == null || d.owned.Length != settings.tools.Length || !d.owned[0] ||
                d.levels == null || d.levels.Length != settings.upgrades.Length || d.currentTool < 0 || d.currentTool >= d.owned.Length || !d.owned[d.currentTool] ||
                d.collected == null || (long)d.carried + d.deposited != d.collected.Length || d.collected.Length > d.total) return false;
            for (int i = 0; i < d.levels.Length; i++) if (d.levels[i] < 0 || d.levels[i] > settings.upgrades[i].maxLevel) return false;
            if (d.carried > settings.tools[d.currentTool].capacity + d.levels[0] * settings.upgrades[0].amount) return false;
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (int id in d.collected) if (id < 0 || id >= d.total || !seen.Add(id)) return false;
            return true;
        }
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
