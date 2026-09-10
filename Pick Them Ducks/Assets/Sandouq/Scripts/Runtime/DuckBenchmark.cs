using System;
using System.Collections;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sandouq.Ducks
{
    // Opt-in standalone benchmark; never touches the player's save. Run the development
    // build with --duck-benchmark <output-directory>. Normal play never creates this object.
    public sealed class DuckBenchmark : MonoBehaviour
    {
        public static bool Running { get; private set; }
        public static int PopulationOverride { get; private set; }
        static readonly int[] Counts = { 1000, 10000, 50000, 100000 };
        string output;
        readonly StringBuilder report = new StringBuilder("count,view,fps,median_ms,p95_ms,main_thread_ms,gpu_ms,gc_bytes_per_frame,total_allocated_mb,managed_mb,gameobjects,colliders,rigidbodies,visible,batches\n");
        readonly FrameTiming[] timings = new FrameTiming[1];
        readonly float[] frames = new float[600];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            string[] args = Environment.GetCommandLineArgs(); int arg = Array.IndexOf(args, "--duck-benchmark");
            if (arg < 0 || arg + 1 >= args.Length) return;
            Running = true; PopulationOverride = Counts[0];
            var runner = new GameObject("Isolated benchmark").AddComponent<DuckBenchmark>(); runner.output = args[arg + 1]; DontDestroyOnLoad(runner);
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            Application.logMessageReceived += Log;
            for (int step = 0; step < Counts.Length; step++)
            {
                PopulationOverride = Counts[step];
                if (step > 0) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                yield return null; yield return null;
                var game = FindAnyObjectByType<DuckGame>();
                if (game == null || game.Progress == null) { File.WriteAllText(Path.Combine(output, "FAILED.txt"), "Game initialization failed"); Application.Quit(1); yield break; }
                try { GameplayCheck(game); }
                catch (Exception e) { File.WriteAllText(Path.Combine(output, "FAILED.txt"), e.ToString()); Application.Quit(2); yield break; }
                game.Population.HoverId = Counts[step] / 2;
                for (int view = 0; view < 2; view++)
                {
                    var camera = game.Player.View.transform;
                    if (view == 0) { game.Player.Teleport(new Vector3(0, .05f, -2)); camera.localPosition = Vector3.up * 1.7f; camera.rotation = Quaternion.Euler(18, 0, 0); }
                    else { camera.position = new Vector3(0, game.Population.Width * .55f, -game.Population.Length * .25f); camera.LookAt(new Vector3(0, 0, game.Population.Length * .48f)); }
                    float warmUntil = Time.realtimeSinceStartup + (step == 0 && view == 0 ? 5 : 2);
                    while (Time.realtimeSinceStartup < warmUntil) yield return null;
                    double mainSum = 0, gpuSum = 0, gcSum = 0; int gpuSamples = 0;
                    using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1))
                    using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1))
                    {
                        for (int frame = 0; frame < frames.Length; frame++)
                        {
                            FrameTimingManager.CaptureFrameTimings(); yield return null;
                            frames[frame] = Time.unscaledDeltaTime * 1000;
                            if (main.Valid) mainSum += main.LastValue / 1000000.0;
                            if (gc.Valid) gcSum += gc.LastValue;
                            if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) { gpuSum += timings[0].gpuFrameTime; gpuSamples++; }
                        }
                        float total = 0; foreach (float ms in frames) total += ms; Array.Sort(frames);
                        int gos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                        int colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
                        int bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;
                        report.AppendLine(string.Join(",", Counts[step], view == 0 ? "ground" : "overview", (frames.Length * 1000 / total).ToString("F1"), frames[frames.Length / 2].ToString("F2"), frames[(int)(frames.Length * .95f)].ToString("F2"),
                            main.Valid ? (mainSum / frames.Length).ToString("F2") : "unavailable", gpuSamples > 0 ? (gpuSum / gpuSamples).ToString("F2") : "unavailable", gc.Valid ? (gcSum / frames.Length).ToString("F1") : "unavailable",
                            (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f).ToString("F1"), (GC.GetTotalMemory(false) / 1048576f).ToString("F1"), gos, colliders, bodies, game.Population.VisibleInstances, game.Population.DrawCalls));
                    }
                    File.WriteAllText(Path.Combine(output, "render-benchmark.csv"), report.ToString());
                    if (Counts[step] == 50000)
                    {
                        yield return CaptureChecked(view == 0 ? "prototype-ground.png" : "prototype-overview.png");
                    }
                }
                if (Counts[step] == 50000)
                {
                    var camera = game.Player.View.transform;
                    game.Population.HoverId = 1000;
                    var hover = game.Population.Position(1000);
                    camera.position = hover + new Vector3(0, 1.2f, -1.5f); camera.LookAt(hover + Vector3.up * .2f);
                    yield return null; yield return null;
                    yield return CaptureChecked("prototype-hover.png");
                    game.Population.HoverId = -1;
                    camera.position = new Vector3(0, 2.7f, -10); camera.LookAt(new Vector3(0, 1, -3));
                    yield return null; yield return null;
                    yield return CaptureChecked("prototype-hub.png");
                    game.SetMenu(true, true); yield return null;
                    yield return CaptureChecked("prototype-shop.png");
                }
            }
            File.WriteAllText(Path.Combine(output, "PASS.txt"), "All four populations rendered and runtime gameplay checks passed. No player save used.");
            Application.Quit(0);
        }
        IEnumerator CaptureChecked(string name)
        {
            yield return new WaitForEndOfFrame();
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            var pixels = capture.GetPixels32(); bool visible = false;
            for (int i = 0; i < pixels.Length; i += 997) if (pixels[i].r > 8 || pixels[i].g > 8 || pixels[i].b > 8) { visible = true; break; }
            if (!visible) { File.WriteAllText(Path.Combine(output, "FAILED.txt"), "Black capture: the player must run in a visible window for valid graphics measurements."); Destroy(capture); Application.Quit(3); yield break; }
            File.WriteAllBytes(Path.Combine(output, name), capture.EncodeToPNG()); Destroy(capture);
        }
        static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void GameplayCheck(DuckGame game)
        {
            var p = game.Progress; int first = game.Population.Remaining;
            for (int i = 0; i < 10; i++) Assert(game.CollectId(i), "Runtime pickup");
            Assert(!game.CollectId(10) && p.Data.money == 0, "Runtime capacity / money");
            Assert(game.Deposit() == 10 && p.Data.money == 10 * game.Settings.moneyPerDuck, "Runtime deposit");
            Assert(game.Deposit() == 0 && !game.CollectId(0), "Runtime double collection/deposit");
            for (int i = 10; i < 20; i++) Assert(game.CollectId(i), "Second trip"); game.Deposit(); game.BuyTool(1);
            Assert(p.Tool == DuckTool.Basket && p.Capacity == 35, "Basket progression");
            Assert(game.Population.Remaining == first - 20 && p.Data.deposited == 20 && p.Data.carried == 0, "Duck conservation");
            var camera = game.Player.View.transform;
            camera.position = game.Population.Position(100) + Vector3.up * 2; camera.rotation = Quaternion.Euler(90, 0, 0);
            Assert(game.CollectAimed() == game.Settings.tools[1].batch, "Aimed basket scoop");
            game.Deposit();
            int candidate = 200;
            while (p.Data.money < game.Settings.tools[2].cost)
            {
                while (p.FreeSpace > 0) { if (game.Population.IsAvailable(candidate)) game.CollectId(candidate); candidate++; }
                game.Deposit();
            }
            game.BuyTool(2); Assert(p.Tool == DuckTool.Vacuum, "Runtime vacuum purchase");
            camera.position = game.Population.Position(600) + Vector3.up * 4; camera.rotation = Quaternion.Euler(90, 0, 0);
            Assert(game.CollectAimed() == game.Settings.tools[2].batch, "Vacuum cone batch at distance");
            camera.position = Vector3.up * 100; Assert(game.CollectAimed() == 0, "Out-of-range pickup");
            game.Deposit();
            Assert(game.Population.Remaining + p.Data.carried + p.Data.deposited == first, "Final conservation");
        }
        void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) File.AppendAllText(Path.Combine(output, "errors.txt"), message + "\n" + stack + "\n"); }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
