using NUnit.Framework;
using UnityEngine;
using ChromaStrike.Core;
using ChromaStrike.Spheres;

namespace ChromaStrike.Tests.EditMode
{
    /// <summary>Unit tests for the pure C# rule classes. They run in milliseconds without starting the game.</summary>
    public class LogicTests
    {
        [Test]
        public void ColorTally_IndexerAndTotal()
        {
            var tally = new ColorTally(15, 10, 5);
            Assert.AreEqual(15, tally[SphereColor.Red]);
            Assert.AreEqual(10, tally[SphereColor.Green]);
            Assert.AreEqual(5, tally[SphereColor.Blue]);
            Assert.AreEqual(30, tally.Total);
            Assert.AreEqual(new ColorTally(16, 10, 5), tally.With(SphereColor.Red));
        }

        [Test]
        public void GameConfig_DefaultsMatchTheRules()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            Assert.AreEqual(120f, config.roundDurationSeconds);
            Assert.AreEqual(new ColorTally(15, 10, 5), config.InitialTally);
            Assert.AreEqual(30, config.InitialTally.Total);
            Assert.AreEqual(10f, config.spawnAreaHalfExtent);
            Assert.AreEqual(1f, config.spawnHeight);
            Assert.AreEqual(5f, config.periodicSpawnInterval);
            Assert.AreEqual(10, config.replacementBatchSize);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BatchTracker_TenDestroyed_ReturnsSameColourRatio()
        {
            var tracker = new BatchTracker(10);
            SphereColor[] hits =
            {
                SphereColor.Red, SphereColor.Red, SphereColor.Red, SphereColor.Red, SphereColor.Red,
                SphereColor.Green, SphereColor.Green,
                SphereColor.Blue, SphereColor.Blue
            };

            foreach (SphereColor color in hits)
            {
                Assert.IsFalse(tracker.TryRegister(color, out _), "No batch before the 10th sphere");
            }

            Assert.AreEqual(9, tracker.Progress);
            Assert.IsTrue(tracker.TryRegister(SphereColor.Blue, out ColorTally batch));
            Assert.AreEqual(new ColorTally(5, 2, 3), batch, "5 red, 2 green, 3 blue destroyed -> same 5-2-3 batch");
            Assert.AreEqual(10, batch.Total);
            Assert.AreEqual(0, tracker.Progress, "Counter restarts after a batch");
        }

        [Test]
        public void BatchTracker_SecondBatchStartsFresh()
        {
            var tracker = new BatchTracker(10);
            for (int i = 0; i < 10; i++) tracker.TryRegister(SphereColor.Red, out _);
            for (int i = 0; i < 9; i++) Assert.IsFalse(tracker.TryRegister(SphereColor.Green, out _));
            Assert.IsTrue(tracker.TryRegister(SphereColor.Green, out ColorTally second));
            Assert.AreEqual(new ColorTally(0, 10, 0), second);
        }

        [Test]
        public void CountdownTimer_FinishesExactlyOnceAtTwoMinutes()
        {
            var timer = new CountdownTimer(120f);
            Assert.IsFalse(timer.Advance(119f));
            Assert.AreEqual(1f, timer.Remaining, 0.0001f);
            Assert.IsTrue(timer.Advance(5f), "Reports finished on the frame it reaches zero");
            Assert.AreEqual(0f, timer.Remaining);
            Assert.IsFalse(timer.Advance(1f), "Never reports twice");
            timer.Reset();
            Assert.AreEqual(120f, timer.Remaining);
        }

        [Test]
        public void RepeatingTimer_FiresEveryFiveSeconds_EvenAfterALagSpike()
        {
            var timer = new RepeatingTimer(5f);
            Assert.AreEqual(0, timer.Advance(4.9f));
            Assert.AreEqual(1, timer.Advance(0.2f));
            Assert.AreEqual(3, timer.Advance(15.1f), "A long frame reports several ticks");

            timer.Reset();
            int total = 0;
            for (int i = 0; i < 120; i++) total += timer.Advance(1f);
            Assert.AreEqual(24, total, "120 seconds contain 24 five-second intervals");
        }

        [Test]
        public void SpawnPointFinder_ProducesLegalNonOverlappingPositions()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var playerObject = new GameObject("Test Player");
            var finder = new SpawnPointFinder(config, playerObject.transform);
            var placed = new System.Collections.Generic.List<Vector3>();

            for (int i = 0; i < 60; i++)
            {
                Assert.IsTrue(finder.TryFindPoint(placed, out Vector3 point), "Arena has room for 60 spheres");
                Assert.That(point.x, Is.InRange(-10f, 10f));
                Assert.That(point.z, Is.InRange(-10f, 10f));
                Assert.AreEqual(1f, point.y, "Y is fixed at 1");
                Assert.GreaterOrEqual(new Vector2(point.x, point.z).magnitude, config.playerClearance, "Never spawns on the player");
                placed.Add(point);
            }

            float minimum = config.sphereRadius * 2f;
            for (int a = 0; a < placed.Count; a++)
                for (int b = a + 1; b < placed.Count; b++)
                    Assert.GreaterOrEqual(Vector3.Distance(placed[a], placed[b]), minimum, "Spheres never overlap");

            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(config);
        }
    }
}
