using System;
using UnityEngine;

namespace Sandouq.Ducks
{
    public enum DuckTool { Hands, Collector, Vacuum, Sweeper, RollerCar }
    public enum UpgradeKind { PickupAmount, PickupSpeed, BagCapacity, MovementSpeed }

    [Serializable]
    public class ToolDefinition
    {
        public string name;
        public int cost;
        public int capacity;
        public float range;
        public float interval;
        public int batch;
        public ToolDefinition(string name, int cost, int capacity, float range, float interval, int batch)
        { this.name = name; this.cost = cost; this.capacity = capacity; this.range = range; this.interval = interval; this.batch = batch; }
    }

    [Serializable]
    public class UpgradeDefinition
    {
        public string name;
        public int baseCost;
        public float costMultiplier = 1.5f;
        public float amount;
        public int maxLevel = 8;
        public UpgradeDefinition(string name, int cost, float amount) { this.name = name; baseCost = cost; this.amount = amount; }
        public int Cost(int level) => Mathf.CeilToInt(baseCost * Mathf.Pow(costMultiplier, level));
    }

    [CreateAssetMenu(menuName = "Sandouq/Duck Prototype Settings")]
    public class PrototypeSettings : ScriptableObject
    {
        public GameObject duckPrefab;
        public Shader outlineShader;
        [Header("One finite stage")]
        [Min(1)] public int totalDucks = 50000;
        public int seed = 1731;
        [Range(.25f, 1f)] public float duckSize = .48f;
        [Min(.5f)] public float spacing = 1.2f;
        [Min(2)] public int cellWidth = 16;
        [Min(1)] public int moneyPerDuck = 1;
        [Header("Duck values: stable ID ranges preserve identity through pickup and throwing")]
        public DuckVariant defaultVariant;
        public DuckVariantRange[] variantRanges=Array.Empty<DuckVariantRange>();
        readonly System.Collections.Generic.HashSet<int> placedIds=new System.Collections.Generic.HashSet<int>();
        public bool HasPlacement(int id)=>placedIds.Contains(id);
        readonly System.Collections.Generic.Dictionary<int,DuckVariant> placedVariants=new System.Collections.Generic.Dictionary<int,DuckVariant>();
        public void SetPlacements(DuckPlacement[] placements){placedVariants.Clear();placedIds.Clear();foreach(var placement in placements)if(placement.duckId>=0&&placement.duckId<totalDucks&&placedIds.Add(placement.duckId)&&placement.variant!=null)placedVariants.Add(placement.duckId,placement.variant);}
        public DuckVariant VariantFor(int id){if(placedVariants.TryGetValue(id,out var placed))return placed;foreach(var range in variantRanges)if(id>=range.firstDuckId&&(long)id<((long)range.firstDuckId+range.count)&&range.variant!=null)return range.variant;return defaultVariant;}
        public int ValueFor(int id){var variant=VariantFor(id);return Mathf.Max(1,variant!=null?variant.coinValue:moneyPerDuck);}
        [Header("Equipment â€” enum order: Hands, Collector, Vacuum, Sweeper, RollerCar")]
        public ToolDefinition[] tools = {
            new ToolDefinition("Hands",0,10,2.8f,.65f,1),
            new ToolDefinition("Duck Collector",120,45,1.4f,.16f,3),
            new ToolDefinition("Duck Vacuum",350,120,5.5f,.12f,4),
            new ToolDefinition("Duck Sweeper",50,120,2.4f,.08f,1),
            new ToolDefinition("Duck Roller Car",2000,600,3.5f,.12f,10)
        };
        [Header("Upgrades: pickup amount, pickup cooldown, bag capacity, movement")]
        public UpgradeDefinition[] upgrades = {
            new UpgradeDefinition("Pickup Amount +1",30,1),
            new UpgradeDefinition("Pickup Speed +20%",40,.2f),
            new UpgradeDefinition("Bag Capacity +25",35,25),
            new UpgradeDefinition("Walk / Sprint Speed +10%",60,.1f)
        };
        public int casketCost=450;
        public float depositInterval=.09f;
        public float depositFlightTime=.32f;
        [Header("Movement / feedback")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 10f;
        public float mouseSensitivity = .10f;
        [Range(8, 64)] public int animationPoolSize = 32;
        [Min(1)] public float autosaveSeconds = 8f;
    }
}
