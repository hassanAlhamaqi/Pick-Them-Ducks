using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Sandouq.Ducks.Editor
{
    // Run with -executeMethod in an isolated project copy, never against a user's open scene.
    public static class DuckScenePreviewChecks
    {
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in an isolated batch-mode project.");
            Directory.CreateDirectory("Logs/DuckPreviewValidation");
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var config = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath);
                var game = new GameObject("Duck preview test").AddComponent<DuckGame>(); game.Settings = config;
                DuckScenePreview.RefreshNow();
                var population = DuckScenePreview.CurrentPreview;
                Check(population != null && population.Total == config.totalDucks, "Preview was not generated");
                Check(game.Progress == null && game.Population == null, "Preview started gameplay");
                Check(population.GetComponentsInChildren<Transform>(true).Length < 10, "Preview expanded to per-duck GameObjects");
                var camera = new GameObject("Scene camera test").AddComponent<Camera>();
                camera.cameraType = CameraType.SceneView;
                camera.transform.position = new Vector3(0, population.Width * .7f, -20);
                camera.transform.LookAt(new Vector3(0, 0, population.Length * .45f));
                camera.farClipPlane = 1000; camera.aspect = 4f / 3;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.cyan;
                var light = new GameObject("Preview test light").AddComponent<Light>();
                light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45, -30, 0);
                light.intensity = 1.2f;
                var target = new RenderTexture(1024, 768, 24); target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var before = RenderTexture.active; RenderTexture.active = target;
                var capture = new Texture2D(1024, 768, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0); capture.Apply(); RenderTexture.active = before;
                int yellow = 0;
                foreach (var color in capture.GetPixels32()) if (color.r > 40 && color.g > color.b * 1.4f && color.r > color.b * 1.4f) yellow++;
                File.WriteAllBytes("Logs/DuckPreviewValidation/scene-preview.png", capture.EncodeToPNG());
                Object.DestroyImmediate(capture); target.Release(); Object.DestroyImmediate(target);
                Check(yellow > 1000, "Scene camera did not render the duck field; yellow pixels: " + yellow);
                population.RenderForCamera(camera, true);
                int playerCount = population.VisibleInstances, playerCalls = population.DrawCalls;
                camera.transform.rotation = Quaternion.Euler(-90, 0, 0);
                population.RenderForCamera(camera);
                Check(population.VisibleInstances == playerCount && population.DrawCalls == playerCalls, "Scene camera overwrote player statistics");
                Check(population.Remove(0) && population.Remaining == config.totalDucks - 1, "Live population removal failed");
                DuckScenePreview.DisposePreview();
                Check(population == null, "Preview scene did not clean up");
                File.WriteAllText("Logs/DuckPreviewValidation/PASS.txt", "PASS: Scene-camera GPU rendering, compact preview, no gameplay startup, independent camera statistics, live removal and preview cleanup. Yellow pixels: " + yellow);
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/DuckPreviewValidation/FAIL.txt", error.ToString()); throw;
            }
        }
        static void Check(bool passed, string message) { if (!passed) throw new Exception(message); }
    }
}
