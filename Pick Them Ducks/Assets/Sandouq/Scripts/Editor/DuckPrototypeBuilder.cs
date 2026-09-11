using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandouq.Ducks.Editor
{
    [InitializeOnLoad]
    public static class DuckPrototypeBuilder
    {
        public const string ScenePath = "Assets/Sandouq/Scenes/DuckPrototype.unity";
        public const string SettingsPath = "Assets/Sandouq/Settings/DuckPrototypeSettings.asset";
        static DuckPrototypeBuilder() { EditorApplication.delayCall += AutoSetup; }
        static void AutoSetup()
        {
            // One-shot setup requested by the workspace task. Never replaces the user's open scene.
            if (!File.Exists("Temp/DuckValidation/setup-request")) return;
            File.Delete("Temp/DuckValidation/setup-request");
            try { Build(); File.WriteAllText("Temp/DuckValidation/setup-result.txt", "Scene and settings generated successfully."); }
            catch (Exception e) { File.WriteAllText("Temp/DuckValidation/setup-result.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Sandouq/Ducks/Create or refresh prototype scene")]
        public static void Build()
        {
            DuckParkBuilder.Build(); DuckToolsBuilder.UpgradeScene();
        }
        [MenuItem("Sandouq/Ducks/Open playable prototype")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Sandouq/Ducks/Run logic and population checks")]
        public static void Validate()
        {
            Directory.CreateDirectory("Temp/DuckValidation");
            try
            {
                var settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(SettingsPath);
                DuckPrototypeChecks.Run(settings);
                File.WriteAllText("Temp/DuckValidation/checks.txt", "PASS: economy, inventory, upgrades, save validation, recovery and spatial populations.");
            }
            catch (Exception e) { File.WriteAllText("Temp/DuckValidation/checks.txt", "FAIL: " + e); throw; }
        }
        public static void BatchValidate() { Build(); Validate(); }
        public static void BuildValidationPlayer()
        {
            if(!File.Exists(ScenePath))Build();
            UnityEditor.PlayerSettings.enableFrameTimingStats = true;
            Directory.CreateDirectory("Build/DuckPrototype");
            var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Build/DuckPrototype/PickThemDucks.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Player build failed: " + result.summary.result);
        }
    }
}
