using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Sandouq.Ducks.Editor
{
 public static class DuckPrototypeBuilder
 {
  public const string ScenePath="Assets/Sandouq/Scenes/DuckPrototype.unity";
  public const string SettingsPath="Assets/Sandouq/Settings/DuckPrototypeSettings.asset";
  [MenuItem("Sandouq/Ducks/Open playable prototype")]
  public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
  [MenuItem("Sandouq/Ducks/Run logic and population checks")]
  public static void Validate()=>DuckPrototypeChecks.Run(AssetDatabase.LoadAssetAtPath<PrototypeSettings>(SettingsPath));
  public static void BatchValidate()=>Validate();
  [MenuItem("Sandouq/Ducks/Build authored Windows player")]
  public static void BuildValidationPlayer()
  {
   if(!File.Exists(ScenePath))throw new FileNotFoundException("The authored scene is missing",ScenePath);
   PlayerSettings.enableFrameTimingStats=true;Directory.CreateDirectory("Build/DuckPrototype");
   var result=BuildPipeline.BuildPlayer(new[]{ScenePath},"Build/DuckPrototype/PickThemDucks.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
   if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Player build failed: "+result.summary.result);
  }
 }
}
