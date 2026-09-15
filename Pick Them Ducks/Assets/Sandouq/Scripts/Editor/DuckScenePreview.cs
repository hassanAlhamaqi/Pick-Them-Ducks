using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Sandouq.Ducks.Editor
{
    // Editor-only, non-serialized preview. Never starts DuckGame or reads player saves.
    [InitializeOnLoad]
    public static class DuckScenePreview
    {
        const string MenuPath = "Sandouq/Ducks/Show ducks in Scene view";
        const string Preference = "Sandouq.Ducks.ScenePreview";
        static Scene previewScene;
        static DuckPopulationManager preview;
        static DuckGame owner;
        static PrototypeSettings settings;
        static GameObject prefab;
        static Shader outline;
        static int count, seed, cellWidth;
        static float spacing, size;
        static double nextRefresh;
        public static bool Enabled => EditorPrefs.GetBool(Preference, true);
        internal static DuckPopulationManager CurrentPreview => preview;
        internal static void RefreshNow() { nextRefresh = 0; Refresh(); }
        internal static void DisposePreview() => Clear();

        static DuckScenePreview()
        {
            EditorApplication.update += Refresh;
            RenderPipelineManager.beginCameraRendering += Render;
            SceneView.duringSceneGui += DrawGuide;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
            EditorApplication.playModeStateChanged += _ => Clear();
            EditorApplication.projectChanged += Clear;
            Undo.undoRedoPerformed += Clear;
            TerrainCallbacks.heightmapChanged += (terrain, region, synced) => Clear();
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            EditorPrefs.SetBool(Preference, !Enabled); Clear(); nextRefresh = 0; SceneView.RepaintAll();
        }
        [MenuItem(MenuPath, true)]
        static bool ValidateToggle() { Menu.SetChecked(MenuPath, Enabled); return true; }

        static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!Enabled) { if (preview != null) Clear(); return; }
            if (EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + .25;
            var game = FindGame();
            if (game == null || game.Settings == null || game.Settings.duckPrefab == null) { if (preview != null) Clear(); return; }
            var config = game.Settings;
            if (preview != null && owner == game && settings == config && prefab == config.duckPrefab && outline == config.outlineShader &&
                count == config.totalDucks && seed == config.seed && cellWidth == config.cellWidth && spacing == config.spacing && size == config.duckSize) return;
            Clear();
            if (config.totalDucks < 1 || config.spacing <= 0 || config.duckSize <= 0) return;
            owner = game; settings = config; prefab = config.duckPrefab; outline = config.outlineShader;
            count = config.totalDucks; seed = config.seed; cellWidth = config.cellWidth; spacing = config.spacing; size = config.duckSize;
            previewScene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Duck field editor preview") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(root, previewScene);
            preview = root.AddComponent<DuckPopulationManager>();
            preview.Initialize(config, new SaveData { total = count, seed = seed }, null, DuckPlacement.InScene(game.gameObject.scene));
            SceneView.RepaintAll();
        }

        static DuckGame FindGame()
        {
            // Prefer the active scene, and never preview a prefab stage or hidden preview scene.
            var active = SceneManager.GetActiveScene();
            DuckGame fallback = null;
            foreach (var game in Object.FindObjectsByType<DuckGame>(FindObjectsSortMode.None))
            {
                if (!game.isActiveAndEnabled || !game.gameObject.scene.IsValid() || EditorSceneManager.IsPreviewScene(game.gameObject.scene)) continue;
                if (game.gameObject.scene == active) return game;
                fallback = game;
            }
            return fallback;
        }

        static void Render(ScriptableRenderContext context, Camera camera)
        {
            if (!Enabled || camera.cameraType != CameraType.SceneView) return;
            // In Play mode, use the actual depleted population rather than rebuilding it.
            var population = Application.isPlaying ? FindGame()?.Population : preview;
            if (population != null) population.RenderForCamera(camera);
        }

        static void DrawGuide(SceneView sceneView)
        {
            if (!Enabled || Application.isPlaying || preview == null) return;
            var previous = Handles.color;
            Handles.color = new Color(.2f, .8f, .75f, .7f);
            Handles.DrawWireCube(new Vector3(0, 0, preview.Length * .5f - 5), new Vector3(preview.Width + 8, .1f, preview.Length + 22));
            Handles.Label(new Vector3(-4, 1.5f, -4), "COLLECTION BOX");
            Handles.Label(new Vector3(4, 1.5f, -4), "TOOL SHOP");
            Handles.Label(new Vector3(0, 1.7f, -2), "PLAYER SPAWN");
            Handles.color = previous;
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12, 12, 320, 72), GUI.skin.box);
            GUILayout.Label($"{count:N0} ducks — editor preview");
            GUILayout.Label("Full field shown; saved progress loads on Play.");
            if (GUILayout.Button("Frame duck field")) Frame();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        [MenuItem("Sandouq/Ducks/Frame duck field")]
        public static void Frame()
        {
            var game = FindGame();
            var config = game != null ? game.Settings : null;
            var sceneView = SceneView.lastActiveSceneView;
            if (config == null || sceneView == null) return;
            int total = Application.isPlaying && game.Population != null ? game.Population.Total : config.totalDucks;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(total));
            float length = Mathf.CeilToInt((float)total / columns) * config.spacing;
            sceneView.LookAt(new Vector3(0, 0, length * .45f), Quaternion.Euler(45, 0, 0), Mathf.Max(10, length * .65f));
        }

        static void Clear()
        {
            preview = null; owner = null; settings = null;
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            previewScene = default;
        }
    }

    [CustomEditor(typeof(DuckGame))]
    public sealed class DuckGameInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Ducks are instanced, so they do not appear individually in the hierarchy. Scene view previews the full field before Play and the live remaining population during Play. Terrain, shop, depot, tools and player are saved scene objects and prefab instances.", MessageType.Info);
            if (GUILayout.Button("Frame duck field in Scene view")) DuckScenePreview.Frame();
        }
    }
}
