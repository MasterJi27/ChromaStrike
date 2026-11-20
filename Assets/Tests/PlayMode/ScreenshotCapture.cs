using System;
using System.Collections;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ChromaStrike.Core;
using ChromaStrike.Spheres;

namespace ChromaStrike.Tests.PlayMode
{
    /// <summary>
    /// Not a rule check: walks through the real scene and saves the README screenshots to Docs/Screenshots.
    /// Run it with graphics enabled (no -nographics) so the game actually renders.
    /// </summary>
    public class ScreenshotCapture
    {
        private static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Screenshots"));

        /// <summary>
        /// Forces the Editor Game view to a fixed 1920x1080 so screenshots are real 16:9 (done by reflection because
        /// Unity has no public API for it). If an internal name ever changes it only logs a warning.
        /// </summary>
        private static void SetGameViewResolution(int width, int height)
        {
            try
            {
                const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                Type sizesType = Type.GetType("UnityEditor.GameViewSizes, UnityEditor");
                Type singletonType = Type.GetType("UnityEditor.ScriptableSingleton`1, UnityEditor").MakeGenericType(sizesType);
                object sizes = singletonType.GetProperty("instance", all).GetValue(null);
                object group = sizesType.GetMethod("GetGroup", all).Invoke(sizes, new object[] { 0 }); // 0 = Standalone

                Type sizeType = Type.GetType("UnityEditor.GameViewSize, UnityEditor");
                Type sizeKind = Type.GetType("UnityEditor.GameViewSizeType, UnityEditor");
                object size = Activator.CreateInstance(sizeType, Enum.ToObject(sizeKind, 1), width, height, "Screenshot");
                group.GetType().GetMethod("AddCustomSize", all).Invoke(group, new[] { size });
                int index = (int)group.GetType().GetMethod("GetTotalCount", all).Invoke(group, null) - 1;

                Type gameView = Type.GetType("UnityEditor.GameView, UnityEditor");
                Type editorWindow = Type.GetType("UnityEditor.EditorWindow, UnityEditor");
                object window = editorWindow.GetMethod("GetWindow", new[] { typeof(Type) }).Invoke(null, new object[] { gameView });
                gameView.GetProperty("selectedSizeIndex", all).SetValue(window, index);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not set the Game view size: " + e.Message);
            }
        }

        private static IEnumerator Snap(string fileName)
        {
            Directory.CreateDirectory(OutputFolder);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputFolder, fileName));
            for (int i = 0; i < 10; i++) yield return null; // the file is written at the end of the frame
        }

        [UnityTest]
        public IEnumerator CaptureMenuGameplayAndEndScreens()
        {
            Time.timeScale = 1f;
            SetGameViewResolution(1920, 1080);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Game");
            for (int i = 0; i < 20; i++) yield return null;
            yield return Snap("01_start_screen.png");

            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var spawner = UnityEngine.Object.FindAnyObjectByType<SpawnerManager>();
            game.StartGame();
            game.Advance(0.2f);
            for (int i = 0; i < 90; i++) yield return null;
            yield return Snap("02_gameplay_start.png");

            // Pop a few spheres of every colour so the counters, toast and batch bar are all visible.
            int popped = 0;
            foreach (Sphere s in new System.Collections.Generic.List<Sphere>(spawner.LiveSpheres))
            {
                if (popped >= 4) break;
                s.Pop();
                popped++;
            }
            for (int i = 0; i < 20; i++) yield return null;
            yield return Snap("03_gameplay_batch.png");

            game.Advance(100f);
            for (int i = 0; i < 20; i++) yield return null;
            yield return Snap("04_gameplay_low_time.png");

            game.Advance(30f);
            Time.timeScale = 1f; // the end screen fades in with unscaled time, but keep the frame loop running
            for (int i = 0; i < 90; i++) yield return null;
            yield return Snap("05_end_screen.png");
            Assert.AreEqual(GameState.Ended, game.State);
        }
    }
}
