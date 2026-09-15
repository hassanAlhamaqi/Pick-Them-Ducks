using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sandouq.Ducks.Editor
{
    public sealed class DuckPlacementWindow : EditorWindow
    {
        public const string PrefabPath = "Assets/Sandouq/Park/Prefabs/Duck Placement.prefab";
        DuckVariant variant;
        int mode;

        [MenuItem("Sandouq/Ducks/Edit duck placements")]
        public static void Open() => GetWindow<DuckPlacementWindow>("Duck placements");
        void OnEnable() => SceneView.duringSceneGui += SceneGUI;
        void OnDisable() => SceneView.duringSceneGui -= SceneGUI;
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Pick an existing preview duck to turn it into an editable prefab placement. Place relocates an unused meadow duck to the clicked surface; it does not increase the finite population.", MessageType.Info);
            variant = (DuckVariant)EditorGUILayout.ObjectField("Variant for new placements", variant, typeof(DuckVariant), false);
            mode = GUILayout.Toolbar(mode, new[] { "Off", "Pick duck", "Place duck" });
            EditorGUILayout.LabelField("Left click in Scene view. Alt still orbits the camera.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Set Off to use normal move/rotate handles. Choose Variant in each placement's Inspector. Escape exits placement mode.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Refresh duck preview")) { DuckScenePreview.DisposePreview(); DuckScenePreview.RefreshNow(); }
        }

        internal static DuckGame Game() => Object.FindAnyObjectByType<DuckGame>();
        internal static int FreeId(DuckGame game, DuckPlacement ignore = null)
        {
            var used = new HashSet<int>();
            foreach (var p in DuckPlacement.InScene(game.gameObject.scene,true)) if (p != ignore) used.Add(p.duckId);
            if (game.Park != null) foreach (var habitat in game.Park.GetComponentsInChildren<DuckHabitat>())
                for (int n = 0; n < habitat.duckCount; n++) used.Add(game.Settings.totalDucks - 501 - habitat.index * 10 - n);
            for (int id = 0; id < game.Settings.totalDucks - 500; id++) if (!used.Contains(id)) return id;
            return -1;
        }
        internal static DuckPlacement Create(DuckGame game, int id, Vector3 position, Quaternion rotation, DuckVariant type)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new System.InvalidOperationException("Duck Placement.prefab is missing.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, game.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(root, "Place duck");
            root.name = "Duck placement " + id;
            if (game.Park != null) root.transform.SetParent(game.Park.transform, true);
            root.transform.SetPositionAndRotation(position, rotation);
            var placement = root.GetComponent<DuckPlacement>();
            placement.duckId = id; placement.variant = type;
            PrefabUtility.RecordPrefabInstancePropertyModifications(placement);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Selection.activeGameObject = root;
            DuckScenePreview.DisposePreview(); DuckScenePreview.RefreshNow();
            return placement;
        }
        void SceneGUI(SceneView view)
        {
            if (Application.isPlaying || mode == 0) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { mode = 0; e.Use(); Repaint(); return; }
            if (e.alt) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type != EventType.MouseDown || e.button != 0) return;
            var game = Game(); if (game == null || game.Settings == null) return;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (mode == 1)
            {
                DuckScenePreview.RefreshNow(); var population = DuckScenePreview.CurrentPreview;
                if (population == null) return;
                int id = population.Query(ray.origin, ray.direction, 600, 0, .45f, false);
                if (id < 0) return;
                foreach (var p in DuckPlacement.InScene(game.gameObject.scene,true)) if (p.duckId == id) { Selection.activeGameObject = p.gameObject; e.Use(); return; }
                Create(game, id, population.Position(id), population.Rotation(id), variant != null ? variant : game.Settings.VariantFor(id));
            }
            else
            {
                if (!Physics.Raycast(ray, out var hit, 1000, ~0, QueryTriggerInteraction.Ignore)) return;
                int id = FreeId(game); if (id < 0) { ShowNotification(new GUIContent("No unassigned meadow IDs remain.")); return; }
                Create(game, id, hit.point, Quaternion.identity, variant);
            }
            e.Use();
        }
    }

    [CustomEditor(typeof(DuckPlacement)), CanEditMultipleObjects]
    public sealed class DuckPlacementInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Position and rotation come from this Transform; scale does not change duck size. Variant selects value and deposit effect (the existing variants share the rubber-duck visual). Saved moved/collected ducks keep their progress; use a fresh run to review a redesigned starting layout.", MessageType.Info);
            var game = DuckPlacementWindow.Game();
            if (game == null || game.Settings == null) return;
            foreach (var item in targets)
            {
                var p = (DuckPlacement)item; int duplicates = 0;
                foreach (var other in DuckPlacement.InScene(p.gameObject.scene,true)) if (other.duckId == p.duckId) duplicates++;
                if (p.duckId < 0 || p.duckId >= game.Settings.totalDucks || duplicates > 1)
                    EditorGUILayout.HelpBox(p.name + ": invalid or duplicate ID. Assign a free ID below after duplicating a placement.", MessageType.Error);
            }
            if (GUILayout.Button("Assign free IDs to selected placements")) foreach (var item in targets)
            {
                var p = (DuckPlacement)item; int id = DuckPlacementWindow.FreeId(game, p); if (id < 0) break;
                Undo.RecordObject(p, "Assign duck ID"); p.duckId = id;
                PrefabUtility.RecordPrefabInstancePropertyModifications(p); EditorUtility.SetDirty(p);
            }
        }
        void OnSceneGUI()
        {
            var p = (DuckPlacement)target;
            Handles.color = new Color(1, .78f, 0);
            Handles.DrawWireDisc(p.transform.position, Vector3.up, .3f);
            Handles.Label(p.transform.position + Vector3.up * .6f, "Duck " + p.duckId + " · " + (p.variant != null ? p.variant.displayName : "Default"));
        }
    }

    [InitializeOnLoad]
    static class DuckPlacementPreviewRefresh
    {
        static double next; static int previous;
        static DuckPlacementPreviewRefresh() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .25;
            var game = DuckPlacementWindow.Game(); if (game == null) return;
            int hash = 17;
            unchecked { foreach (var p in DuckPlacement.InScene(game.gameObject.scene,true)) { var pile=p.GetComponentInParent<DuckPile>();if(pile!=null){hash=hash*31+pile.minimumDucks;hash=hash*31+pile.maximumDucks;hash=hash*31+pile.countSeed;}hash = hash * 31 + p.duckId; hash = hash * 31 + p.transform.position.GetHashCode(); hash = hash * 31 + p.transform.rotation.GetHashCode(); hash = hash * 31 + (p.variant != null ? p.variant.GetInstanceID() : 0); } }
            if (hash == previous) return; previous = hash;
            DuckScenePreview.DisposePreview(); DuckScenePreview.RefreshNow();
        }
    }
}
