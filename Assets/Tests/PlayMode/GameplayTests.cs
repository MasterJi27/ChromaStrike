using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ChromaStrike.Core;
using ChromaStrike.Player;
using ChromaStrike.Spheres;

namespace ChromaStrike.Tests.PlayMode
{
    /// <summary>
    /// Loads the real Game scene and checks every gameplay rule of the assignment end to end.
    /// GameManager.Advance(seconds) lets the tests fast forward the 120 second round.
    /// </summary>
    public class GameplayTests
    {
        private GameManager game;
        private SpawnerManager spawner;
        private ClickHandler clicker;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            spawner = Object.FindAnyObjectByType<SpawnerManager>();
            clicker = Object.FindAnyObjectByType<ClickHandler>();
            Assert.IsNotNull(game);
            Assert.IsNotNull(spawner);
            game.StartGame();
            yield return null;
        }

        [TearDown]
        public void Cleanup() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator InitialBatch_Is30Spheres_15Red_10Green_5Blue_AtLegalPositions()
        {
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(new ColorTally(15, 10, 5), spawner.LiveCounts);

            Sphere[] spheres = Object.FindObjectsByType<Sphere>();
            Assert.AreEqual(30, spheres.Length);

            foreach (Sphere sphere in spheres)
            {
                Vector3 p = sphere.transform.position;
                Assert.That(p.x, Is.InRange(-10f, 10f));
                Assert.That(p.z, Is.InRange(-10f, 10f));
                Assert.AreEqual(1f, p.y, 0.0001f);
            }

            for (int a = 0; a < spheres.Length; a++)
                for (int b = a + 1; b < spheres.Length; b++)
                    Assert.GreaterOrEqual(Vector3.Distance(spheres[a].transform.position, spheres[b].transform.position), 1f, "No overlapping spheres");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingTen_SpawnsTenWithTheSameRatio()
        {
            ColorTally spawnedBatch = default;
            spawner.BatchSpawned += batch => spawnedBatch = batch;

            // Destroy 5 red, 2 green, 3 blue (the example from the assignment).
            var plan = new Dictionary<SphereColor, int> { { SphereColor.Red, 5 }, { SphereColor.Green, 2 }, { SphereColor.Blue, 3 } };
            foreach (var entry in plan)
            {
                for (int i = 0; i < entry.Value; i++)
                {
                    Sphere target = null;
                    foreach (Sphere s in spawner.LiveSpheres) if (s.Color == entry.Key) { target = s; break; }
                    Assert.IsNotNull(target);
                    target.Pop();
                }
            }
            yield return null;

            Assert.AreEqual(10, spawner.TotalDestroyed);
            Assert.AreEqual(new ColorTally(5, 2, 3), spawnedBatch, "New batch uses the destroyed 5-2-3 ratio");
            Assert.AreEqual(new ColorTally(15, 10, 5), spawner.LiveCounts, "30 destroyed-and-replaced spheres keep the live mix");
            Assert.AreEqual(1, spawner.BatchesSpawned);
        }

        [UnityTest]
        public IEnumerator EveryFiveSeconds_OneRandomSphereIsAdded()
        {
            int before = spawner.LiveCounts.Total;
            game.Advance(4.9f);
            Assert.AreEqual(before, spawner.LiveCounts.Total, "Nothing before 5 seconds");
            game.Advance(0.2f);
            Assert.AreEqual(before + 1, spawner.LiveCounts.Total);
            game.Advance(5f);
            Assert.AreEqual(before + 2, spawner.LiveCounts.Total);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoundEndsAfterExactlyTwoMinutes_AndCanRestart()
        {
            game.Advance(119.9f);
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.Greater(game.RemainingSeconds, 0f);

            game.Advance(0.2f);
            Assert.AreEqual(GameState.Ended, game.State);
            Assert.AreEqual(0f, Time.timeScale, "Gameplay is frozen");
            Assert.IsFalse(clicker.IsActive, "Shooting is disabled after the end");
            Assert.IsFalse(spawner.enabled, "Pending spawn retries are disabled after the end");

            ColorTally frozen = spawner.LiveCounts;
            game.Advance(30f);
            Assert.AreEqual(frozen, spawner.LiveCounts, "Nothing spawns after the end");

            game.RestartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(120f, game.RemainingSeconds, 0.5f);
            Assert.AreEqual(new ColorTally(15, 10, 5), spawner.LiveCounts, "Restart gives a fresh 15-10-5 batch");
        }

        [UnityTest]
        public IEnumerator CentreRaycast_DestroysTheSphereUnderTheCrosshair()
        {
            Camera cam = clicker.GetComponentInChildren<Camera>();
            bool fired = false;
            foreach (Sphere sphere in new List<Sphere>(spawner.LiveSpheres))
            {
                foreach (Vector3 direction in new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right })
                {
                    cam.transform.position = sphere.transform.position + direction * 2.5f;
                    cam.transform.LookAt(sphere.transform.position);
                    if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 10f)
                        && hit.collider.gameObject == sphere.gameObject)
                    {
                        int before = spawner.TotalDestroyed;
                        Assert.IsTrue(clicker.TryShoot());
                        Assert.AreEqual(before + 1, spawner.TotalDestroyed);
                        fired = true;
                        break;
                    }
                }
                if (fired) break;
            }
            Assert.IsTrue(fired, "At least one sphere must be shootable from a clear line");

            // Aiming at the empty sky must not destroy anything.
            cam.transform.position = new Vector3(0, 1.65f, 0);
            cam.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
            int destroyed = spawner.TotalDestroyed;
            Assert.IsFalse(clicker.TryShoot());
            Assert.AreEqual(destroyed, spawner.TotalDestroyed);
            yield return null;
        }
    }
}
