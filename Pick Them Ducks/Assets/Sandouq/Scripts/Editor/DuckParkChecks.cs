using UnityEditor;
namespace Sandouq.Ducks.Editor
{
    public static class DuckParkChecks
    {
        public static void Run()=>DuckPrototypeChecks.Run(AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath));
        public static void ValidateAndBuild(){Run();DuckPrototypeBuilder.BuildValidationPlayer();}
    }
}
