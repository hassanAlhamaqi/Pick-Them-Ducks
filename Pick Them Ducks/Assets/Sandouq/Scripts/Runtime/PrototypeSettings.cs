using System;
using UnityEngine;

namespace Sandouq.Ducks
{
    public enum DuckTool { Hands, Basket, Vacuum, Fork, IndustrialVacuum }
    public enum UpgradeKind { Capacity, PickupSpeed, VacuumRange, VacuumSpeed }

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
        [Header("Equipment — enum order: Hands, Basket, Vacuum, Fork, IndustrialVacuum")]
        public ToolDefinition[] tools = {
            new ToolDefinition("HANDS", 0, 10, 2.8f, .24f, 1),
            new ToolDefinition("BASKET", 25, 45, 3.2f, .28f, 4),
            new ToolDefinition("DUCK VACUUM", 220, 120, 5.5f, .085f, 6),
            new ToolDefinition("BIG FORK", 80, 120, 3.3f, .3f, 12),
            new ToolDefinition("INDUSTRIAL VAC", 1500, 250, 9, .055f, 12)
        };
        [Header("Upgrades — enum order: Capacity, Pickup, Range, Vacuum speed")]
        public UpgradeDefinition[] upgrades = {
            new UpgradeDefinition("Carry capacity  +25", 35, 25),
            new UpgradeDefinition("Pickup speed  +20%", 40, .2f),
            new UpgradeDefinition("Vacuum range  +1m", 80, 1),
            new UpgradeDefinition("Vacuum speed  +25%", 100, .25f)
        };
        [Header("Movement / feedback")]
        public float walkSpeed = 6f;
        public float sprintSpeed = 10f;
        public float mouseSensitivity = .10f;
        [Range(8, 64)] public int animationPoolSize = 32;
        [Min(1)] public float autosaveSeconds = 8f;
    }
}
