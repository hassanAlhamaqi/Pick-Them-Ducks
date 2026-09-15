using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Sandouq.Ducks.Editor
{
    public static class DuckPlacementChecks
    {
        public static void Run()
        {
            var settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath));
            settings.totalDucks = 1000;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DuckPlacementWindow.PrefabPath);
            Check(prefab != null && prefab.GetComponent<DuckPlacement>() != null, "Authored placement prefab");
            var marker = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var root = new GameObject("Placement population check");
            try
            {
                var p = marker.GetComponent<DuckPlacement>(); p.duckId = 7;
                p.variant = AssetDatabase.LoadAssetAtPath<DuckVariant>("Assets/Sandouq/Settings/Golden Duck Variant.asset");
                marker.transform.SetPositionAndRotation(new Vector3(23, 4, 52), Quaternion.Euler(10, 70, 5));
                settings.SetPlacements(new[] { p });
                Check(p.variant != null && settings.VariantFor(7) == p.variant && settings.ValueFor(7) == p.variant.coinValue, "Exact-ID variant override");
                var population = root.AddComponent<DuckPopulationManager>();
                population.Initialize(settings, new SaveData { total = 1000, seed = settings.seed }, null, new[] { p });
                Check(population.Total == 1000 && population.Remaining == 1000, "Placements conserve finite population");
                Check(Vector3.Distance(population.Position(7), marker.transform.position) < .001f && Quaternion.Angle(population.Rotation(7), marker.transform.rotation) < .1f, "Placement position and rotation reach population");
                Check(population.Poses().Length == 0, "Initial placements do not masquerade as saved player moves");
                UnityEngine.Object.DestroyImmediate(population);
                population = root.AddComponent<DuckPopulationManager>();
                var savedPosition = new Vector3(20, 1, 30);
                population.Initialize(settings, new SaveData { total = 1000, seed = settings.seed, poses = new[] { new DuckPose { id = 7, position = savedPosition, rotation = Quaternion.identity } }, collected = new[] { 8 } }, null, new[] { p });
                Check(population.Position(7) == savedPosition && !population.IsAvailable(8), "Saved moves and collected IDs remain respected");
                settings.SetPlacements(Array.Empty<DuckPlacement>());
                Check(!settings.HasPlacement(7), "Runtime overrides clear between layouts");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/DuckPlacementChecks.txt", "PASS: prefab, explicit pose, variant value, finite population, saved move/depletion precedence, and override cleanup.");
            }
            finally { UnityEngine.Object.DestroyImmediate(marker); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(settings); }
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
