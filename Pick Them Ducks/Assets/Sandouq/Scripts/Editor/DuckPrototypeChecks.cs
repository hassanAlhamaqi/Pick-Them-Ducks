using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sandouq.Ducks.Editor
{
    public static class DuckPrototypeChecks
    {
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        public static void Run(PrototypeSettings settings)
        {
            Check(settings != null, "Settings missing");
            var d = new SaveData { total = 1000, seed = 1731 }; var p = new Progression(settings, d);
            for (int i = 0; i < 10; i++) Check(p.PickUp(), "Pickup rejected within capacity");
            Check(!p.PickUp() && d.money == 0, "Capacity or money-on-pickup violation");
            Check(p.Deposit() == 10 && d.money == 10 * settings.moneyPerDuck && d.deposited == 10 && d.carried == 0, "Deposit transaction");
            Check(p.Deposit() == 0 && d.money == 10 * settings.moneyPerDuck, "Repeated deposit duplication");
            d.money = settings.tools[1].cost; Check(p.BuyTool(1) && p.Tool == DuckTool.Basket && d.money == 0, "Basket purchase");
            Check(!p.BuyTool(1) && !p.BuyTool(2), "Duplicate/unaffordable purchase");
            for (int i = 0; i < 11; i++) p.PickUp(); Check(!p.Equip(0), "Smaller-tool inventory loss");
            Check(!p.BuyUpgrade(2), "Locked vacuum upgrade");
            d.money = settings.tools[2].cost; Check(p.BuyTool(2), "Vacuum purchase");
            float oldRange = p.Range; d.money = settings.upgrades[2].Cost(0); Check(p.BuyUpgrade(2) && p.Range > oldRange, "Range upgrade");
            d.money = int.MaxValue / 4; for (int i = 0; i < settings.upgrades[0].maxLevel; i++) Check(p.BuyUpgrade(0), "Capacity upgrade");
            Check(!p.BuyUpgrade(0), "Max-level limit");
            d.collected = new int[d.carried + d.deposited]; for (int i = 0; i < d.collected.Length; i++) d.collected[i] = i;
            d.inventory=new int[d.carried];Array.Copy(d.collected,d.deposited,d.inventory,0,d.carried);
            Check(DuckSaveSystem.Valid(d, settings), "Valid save rejected");
            string path = Path.GetFullPath("Temp/DuckValidation/test-save.json");
            Check(DuckSaveSystem.Save(d, path), "Save failed");
            var loaded = DuckSaveSystem.Load(settings, path); Check(loaded.money == d.money && loaded.carried == d.carried && loaded.currentTool == d.currentTool && loaded.collected.Length == d.collected.Length, "Save roundtrip");
            Check(DuckSaveSystem.Save(d, path), "Atomic replacement failed"); File.WriteAllText(path, "invalid");
            loaded = DuckSaveSystem.Load(settings, path); Check(loaded.deposited == d.deposited, "Backup recovery failed");
            var invalid = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d)); invalid.collected[1] = invalid.collected[0]; Check(!DuckSaveSystem.Valid(invalid, settings), "Duplicate collected ID accepted");
            var complete = new Progression(settings, new SaveData { total = 1 }); Check(complete.PickUp() && !complete.Complete && complete.Deposit() == 1 && complete.Complete, "Completion before deposit");

            var report = new StringBuilder("Population structural checks (Editor, not GPU frame benchmarks)\ncount,init_ms,query_1000_ms,managed_delta_bytes,parts,vertices_per_duck\n");
            foreach (int count in new[] { 1000, 10000, 50000, 100000 })
            {
                var go = new GameObject("Validation population"); var cameraGo = new GameObject("Validation camera", typeof(Camera));
                try
                {
                    long before = GC.GetTotalMemory(true); var timer = Stopwatch.StartNew();
                    var pop = go.AddComponent<DuckPopulationManager>(); pop.Initialize(settings, new SaveData { total = count, seed = settings.seed }, cameraGo.GetComponent<Camera>());
                    double init = timer.Elapsed.TotalMilliseconds; long memory = GC.GetTotalMemory(false) - before;
                    Check(pop.Remaining == count, "Population count");
                    Vector3 first = pop.Position(0); int found = pop.Query(first + Vector3.up * 1.5f, Vector3.down, 2, .9f, .3f);
                    Check(found == 0, "Spatial ray target"); Check(pop.Remove(found) && !pop.Remove(found), "Double removal");
                    Check(pop.Remaining == count - 1 && !pop.IsAvailable(found), "Remaining mismatch");
                    int middle = count / 2; Vector3 target = pop.Position(middle);
                    Check(pop.Query(target + Vector3.up * 1.5f, Vector3.down, 2, .9f, .25f) == middle, "Spatial query middle/seam");
                    timer.Restart(); for (int i = 0; i < 1000; i++) pop.Query(target + Vector3.up * 1.5f, Vector3.down, 6, .7f);
                    report.AppendLine($"{count},{init:F2},{timer.Elapsed.TotalMilliseconds:F2},{memory},{pop.PartCount},{pop.VerticesPerDuck}");
                    Check(go.GetComponentsInChildren<Collider>(true).Length == 0, "Duck collider exists");
                    Check(go.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Duck rigidbody exists");
                    // Reconstruct depleted IDs using the exact same seed and validate conservation.
                    var restored = new GameObject("Restored population");
                    try { var r = restored.AddComponent<DuckPopulationManager>(); r.Initialize(settings, new SaveData { total = count, seed = settings.seed, collected = pop.CollectedIds() }, cameraGo.GetComponent<Camera>()); Check(r.Remaining == count - 1 && !r.IsAvailable(0) && r.Position(middle) == target, "Population reload"); }
                    finally { Object.DestroyImmediate(restored); }
                }
                finally { Object.DestroyImmediate(go); Object.DestroyImmediate(cameraGo); }
            }
            File.WriteAllText("Temp/DuckValidation/population.csv", report.ToString());
            UnityEngine.Debug.Log(report.ToString());
        }
    }
}
