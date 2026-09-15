using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckMountainPlayChecks : MonoBehaviour
    {
        public static bool Running => Array.IndexOf(Environment.GetCommandLineArgs(), "--duck-mountain-check") >= 0;
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (Running) DontDestroyOnLoad(new GameObject("Mountain play checks").AddComponent<DuckMountainPlayChecks>()); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--duck-mountain-check");
            output = args[index + 1]; Directory.CreateDirectory(output);
            yield return new WaitForSeconds(1);
            var game = FindAnyObjectByType<DuckGame>();
            var area = game.Park.mountain;
            try
            {
                Check(area != null && area.trailWaypoints.Length > 20, "Authored mountain and winding route");
                var summit=area.summitPile.GetComponent<DuckPile>();Check(summit!=null&&summit.SelectedPlacements(game.Settings.seed).Length>=summit.minimumDucks&&summit.SelectedPlacements(game.Settings.seed).Length<=summit.maximumDucks,"Summit pile count range");
                var cave=area.cavePile.GetComponent<DuckPile>();var rare = cave.SelectedPlacements(game.Settings.seed);
                Check(rare.Length>=cave.minimumDucks&&rare.Length<=cave.maximumDucks,"Cave pile count range");
                foreach (var p in rare) Check(game.Settings.ValueFor(p.duckId) == 5 && game.Population.Position(p.duckId) == p.transform.position, "Rare cave placement and value");
                Check(game.Park.lakeRadius == new Vector2(28, 35), "Smaller lake bounds");
                foreach (var point in area.trailWaypoints) Check(Mathf.Abs(game.Park.Ground(point.position) - point.position.y) < .15f, "Trail supports ducks at " + point.name);
            }
            catch (Exception e) { Fail(e); yield break; }
            game.SetMenu(false); game.Player.enabled = false;
            var controller = game.Player.GetComponent<CharacterController>(); controller.radius = .3f;
            game.Player.Teleport(area.trailWaypoints[0].position + Vector3.up * .08f);
            for (int i = 1; i < area.trailWaypoints.Length; i++)
            {
                try
                {
                    Walk(controller, area.trailWaypoints[i].position);
                    if (i == 30)
                    {
                        Walk(controller, area.caveEntrance.position);
                        Walk(controller, area.cavePile.position + area.cavePile.forward * 2.8f);
                        Walk(controller, area.caveEntrance.position);
                        Walk(controller, area.trailWaypoints[i].position);
                    }
                }
                catch (Exception e) { Fail(e); yield break; }
                yield return null;
            }
            var camera = game.Player.View; camera.transform.SetParent(null); camera.farClipPlane = 1000; camera.fieldOfView = 58;
            camera.transform.position = area.transform.position + new Vector3(-68, 53, -72);
            camera.transform.LookAt(area.transform.position + new Vector3(0, 7, -12));
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "mountain-overview.png")); yield return null;
            camera.transform.position = area.caveEntrance.position + area.caveEntrance.forward * 3.3f + Vector3.up * 2.4f;
            camera.transform.LookAt(area.cavePile.position + Vector3.up * 1);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "rare-duck-cave.png")); yield return null;
            camera.transform.position = area.transform.position + new Vector3(12, 24, -14);
            camera.transform.LookAt(area.summitPile.position + Vector3.up * .8f);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "summit-pile.png")); yield return null;
            camera.transform.position = game.Park.lakeCenter + new Vector3(50, 48, -60);
            camera.transform.LookAt(game.Park.lakeCenter + Vector3.forward * 15);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "smaller-lake.png")); yield return null;
            File.WriteAllText(Path.Combine(output, "PASS.txt"), "PASS: winding route traversed uphill with the CharacterController (no jumps), cave entry/exit, elevated duck support, ranged summit/rare cave placements, and smaller lake bounds.");
            Application.Quit(0);
        }
        static void Walk(CharacterController controller, Vector3 destination)
        {
            for (int step = 0; step < 180; step++)
            {
                Vector3 d = destination - controller.transform.position; d.y = 0;
                if (d.magnitude < .12f) break;
                controller.Move(d.normalized * Mathf.Min(.24f, d.magnitude) + Vector3.down * .12f);
            }
            var difference = controller.transform.position - destination;
            Check(new Vector2(difference.x, difference.z).magnitude < .35f && Mathf.Abs(difference.y) < .45f,
                "Walkable route: target " + destination + ", reached " + controller.transform.position);
        }
        static void Check(bool passed, string message) { if (!passed) throw new Exception(message); }
        void Fail(Exception e) { File.WriteAllText(Path.Combine(output, "FAILED.txt"), e.ToString()); Application.Quit(1); }
    }
}
