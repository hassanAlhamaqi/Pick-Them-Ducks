using UnityEditor;
using UnityEngine;

namespace Sandouq.Ducks.Editor
{
    [CustomEditor(typeof(DuckPile))]
    public sealed class DuckPileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var pile = (DuckPile)target;
            var game = DuckPlacementWindow.Game();
            int seed = game != null && game.Settings != null ? game.Settings.seed : 1731;
            int slots = pile.GetComponentsInChildren<DuckPlacement>().Length;
            EditorGUILayout.HelpBox($"{pile.SelectedPlacements(seed).Length} ducks selected from {slots} authored slots. Counts are inclusive and stable for the layout seed, so reloading does not respawn ducks. Change Count Seed to reroll. Bottom layers fill first.", MessageType.Info);
            if (pile.maximumDucks > slots) EditorGUILayout.HelpBox("Maximum exceeds the authored slots. Duplicate placement children and assign unique free IDs before using that maximum.", MessageType.Warning);
            if (GUILayout.Button("Pack existing slots into solid layers"))
            {
                var children = pile.GetComponentsInChildren<DuckPlacement>();
                var offsets = DuckPile.SolidOffsets(children.Length);
                for (int i = 0; i < children.Length; i++)
                {
                    Undo.RecordObject(children[i].transform, "Pack solid duck pile");
                    children[i].transform.position = pile.transform.TransformPoint(offsets[i]);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(children[i].transform);
                }
                DuckScenePreview.DisposePreview(); DuckScenePreview.RefreshNow();
            }
        }
    }
}
