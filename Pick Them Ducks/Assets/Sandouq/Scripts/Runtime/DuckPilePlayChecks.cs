using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckPileContactCounter : MonoBehaviour
    {
        public static int Contacts;
        void OnCollisionEnter(Collision other)
        {
            if (other.rigidbody != null && other.transform.IsChildOf(transform.parent)) Contacts++;
        }
    }
    public sealed class DuckPilePlayChecks : MonoBehaviour
    {
        public static bool Running => Array.IndexOf(Environment.GetCommandLineArgs(), "--duck-pile-check") >= 0;
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (Running) DontDestroyOnLoad(new GameObject("Pile play checks").AddComponent<DuckPilePlayChecks>()); }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); output = args[Array.IndexOf(args, "--duck-pile-check") + 1]; Directory.CreateDirectory(output);
            yield return new WaitForSeconds(1);
            var game = FindAnyObjectByType<DuckGame>(); game.SetMenu(false); game.Player.enabled = false;
            game.Player.Teleport(game.Park.Land(new Vector3(0, 0, 40)));
            var pile = game.Park.mountain.summitPile.GetComponent<DuckPile>();
            var selected = pile.SelectedPlacements(game.Settings.seed);
            try
            {
                Check(selected.Length >= pile.minimumDucks && selected.Length <= pile.maximumDucks, "Inclusive pile range");
                Check(selected.Length == pile.SelectedPlacements(game.Settings.seed).Length, "Stable count when reloading");
                var points = DuckPile.SolidOffsets(240); int inner = 0, elevatedInner = 0;
                foreach (var p in points) if (new Vector2(p.x, p.z).magnitude < .8f) { inner++; if (p.y > .3f) elevatedInner++; }
                Check(inner > 15 && elevatedInner > 10, "Solid interior on multiple layers");
                foreach (var p in selected) Check(game.Population.IsAvailable(p.duckId), "Selected pile slots spawn");
            }
            catch (Exception e) { Fail(e); yield break; }
            // Two ordinary-sized duck bodies must exchange momentum, not just overlap or animate past one another.
            int first = 27500; while (!game.Population.IsAvailable(first) || !game.Population.IsAvailable(first + 1)) first += 2;
            var start = game.Park.Land(new Vector3(0, 0, 62)) + Vector3.up * .03f;
            game.Physics.Launch(first, start, Vector3.forward * 6, false, true);
            game.Physics.Launch(first + 1, start + Vector3.forward * .8f, Vector3.zero, false, true);
            DuckPileContactCounter.Contacts = 0;
            foreach (var body in game.Physics.GetComponentsInChildren<Rigidbody>()) body.gameObject.AddComponent<DuckPileContactCounter>();
            yield return new WaitForSeconds(.5f);
            try { Check(DuckPileContactCounter.Contacts > 0, "Duck-to-duck collision callbacks"); Check(game.Population.Position(first + 1).z > start.z + .92f, "Collision pushes the resting duck"); }
            catch (Exception e) { Fail(e); yield break; }
            yield return new WaitForSeconds(5);
            try { Check(!game.Population.IsPhysical(first) && !game.Population.IsPhysical(first + 1), "Quiet collision group returns to instancing"); }
            catch (Exception e) { Fail(e); yield break; }
            var camera = game.Player.View; camera.transform.SetParent(null); camera.fieldOfView = 65;
            camera.transform.position = pile.transform.position + new Vector3(7, 4, -8); camera.transform.LookAt(pile.transform.position + Vector3.up);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "solid-pile-before.png")); yield return null;
            int top = selected[0].duckId; float topHeight = float.NegativeInfinity;
            foreach (var p in selected) if (p.transform.position.y > topHeight) { topHeight = p.transform.position.y; top = p.duckId; }
            game.CollectId(selected[0].duckId);
            try { Check(game.Physics.ActivePileBodies > 96, "Large pile has simultaneous colliding bodies beyond the regular pool"); Check(game.Physics.ActivePileBodies <= game.Physics.maximumPileBodies, "Pile physics remains bounded"); }
            catch (Exception e) { Fail(e); yield break; }
            yield return new WaitForSeconds(1.2f);
            try { Check(game.Population.Position(top).y < topHeight - .08f, "Upper pile falls onto other physical ducks"); }
            catch (Exception e) { Fail(e); yield break; }
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "colliding-pile.png")); yield return null;
            yield return new WaitForSeconds(7);
            try
            {
                Check(game.Physics.ActivePileBodies == 0, "Pile group finishes settling");
                game.Progress.Data.collected = game.Population.CollectedIds(); game.Progress.Data.poses = game.Population.Poses();
                Check(DuckSaveSystem.Valid(game.Progress.Data, game.Settings), "Ranged and collapsed piles conserve duck identities");
            }
            catch (Exception e) { Fail(e); yield break; }
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "pile-settled.png")); yield return null;
            File.WriteAllText(Path.Combine(output, "PASS.txt"), "PASS: solid interior layers, stable inclusive counts, real duck-to-duck contacts and momentum transfer, large simultaneous physical collapse, bounded pool, settled group cleanup and save conservation. Summit count: " + selected.Length);
            Application.Quit(0);
        }
        static void Check(bool passed, string message) { if (!passed) throw new Exception(message); }
        void Fail(Exception e) { File.WriteAllText(Path.Combine(output, "FAILED.txt"), e.ToString()); Application.Quit(1); }
    }
}
