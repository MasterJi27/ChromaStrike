using System;
using System.Collections.Generic;
using UnityEngine;
using ChromaStrike.Core;
using Random = UnityEngine.Random;

namespace ChromaStrike.Spheres
{
    /// <summary>Why a sphere is being created. Used by feedback (sounds) and by tests.</summary>
    public enum SpawnReason
    {
        Initial,
        Periodic,
        Batch
    }

    /// <summary>
    /// Owns every sphere in the arena and implements the three spawn rules:
    ///   1) the initial 15 / 10 / 5 batch,
    ///   2) one random sphere every 5 seconds,
    ///   3) a replacement batch (same colour ratio) after every 10 destroyed spheres.
    /// It also keeps the live colour counters that the HUD displays.
    /// </summary>
    public sealed class SpawnerManager : MonoBehaviour
    {
        private readonly struct PendingSpawn
        {
            public readonly SphereColor Color;
            public readonly SpawnReason Reason;
            public PendingSpawn(SphereColor color, SpawnReason reason) { Color = color; Reason = reason; }
        }

        [SerializeField] private GameConfig config;
        [SerializeField] private Transform player;
        [SerializeField] private Transform sphereContainer;

        private readonly List<Sphere> liveSpheres = new List<Sphere>();
        private readonly List<Vector3> occupiedPositions = new List<Vector3>();
        // If the arena is too crowded to place a sphere right now, the request waits here and is
        // retried on the next frame. Nothing is ever silently dropped.
        private readonly Queue<PendingSpawn> pending = new Queue<PendingSpawn>();

        private SpawnPointFinder finder;
        private BatchTracker batchTracker;
        private RepeatingTimer periodicTimer;
        private ColorTally liveCounts;
        private ColorTally destroyedCounts;

        public ColorTally LiveCounts => liveCounts;
        public ColorTally DestroyedCounts => destroyedCounts;
        public int TotalDestroyed => destroyedCounts.Total;
        public int BatchesSpawned { get; private set; }
        public int PendingCount => pending.Count;
        public IReadOnlyList<Sphere> LiveSpheres => liveSpheres;

        /// <summary>Live colour counts changed (spawn or destroy).</summary>
        public event Action<ColorTally> LiveCountsChanged;
        /// <summary>(destroyedSinceLastBatch, batchSize)</summary>
        public event Action<int, int> BatchProgressChanged;
        /// <summary>A sphere was shot and is about to be removed.</summary>
        public event Action<Sphere> SphereDestroyed;
        /// <summary>A sphere was created (after any grow animation starts).</summary>
        public event Action<Sphere, SpawnReason> SphereSpawned;
        /// <summary>A replacement batch was queued, with its colour mix.</summary>
        public event Action<ColorTally> BatchSpawned;

        private void Awake()
        {
            finder = new SpawnPointFinder(config, player);
            batchTracker = new BatchTracker(config.replacementBatchSize);
            periodicTimer = new RepeatingTimer(config.periodicSpawnInterval);
            batchTracker.ProgressChanged += (done, size) => BatchProgressChanged?.Invoke(done, size);
        }

        private void Update()
        {
            if (pending.Count > 0) FlushPending();
        }

        /// <summary>Removes every sphere and zeroes all counters. Called before each round.</summary>
        public void ResetAll()
        {
            foreach (Sphere sphere in liveSpheres)
            {
                if (sphere == null) continue;
                sphere.Destroyed -= HandleSphereDestroyed;
                Destroy(sphere.gameObject);
            }

            liveSpheres.Clear();
            occupiedPositions.Clear();
            pending.Clear();
            liveCounts = default;
            destroyedCounts = default;
            BatchesSpawned = 0;
            batchTracker.Reset();
            periodicTimer.Reset();
            LiveCountsChanged?.Invoke(liveCounts);
        }

        /// <summary>Rule 1: 15 red + 10 green + 5 blue.</summary>
        public void SpawnInitialBatch()
        {
            Enqueue(config.InitialTally, SpawnReason.Initial);
            FlushPending();
        }

        /// <summary>Rule 2: called every frame while playing; spawns one random sphere per interval.</summary>
        public void Tick(float deltaTime)
        {
            int due = periodicTimer.Advance(deltaTime);
            for (int i = 0; i < due; i++)
            {
                SphereColor random = (SphereColor)Random.Range(0, SphereColors.All.Length);
                pending.Enqueue(new PendingSpawn(random, SpawnReason.Periodic));
            }

            if (pending.Count > 0) FlushPending();
        }

        private void HandleSphereDestroyed(Sphere sphere)
        {
            sphere.Destroyed -= HandleSphereDestroyed;
            int index = liveSpheres.IndexOf(sphere);
            if (index >= 0)
            {
                liveSpheres.RemoveAt(index);
                occupiedPositions.RemoveAt(index);
            }

            liveCounts = liveCounts.With(sphere.Color, -1);
            destroyedCounts = destroyedCounts.With(sphere.Color);
            LiveCountsChanged?.Invoke(liveCounts);
            SphereDestroyed?.Invoke(sphere);

            // Rule 3: ten destroyed spheres -> spawn ten new ones with the same colour mix.
            if (batchTracker.TryRegister(sphere.Color, out ColorTally batch))
            {
                BatchesSpawned++;
                Enqueue(batch, SpawnReason.Batch);
                FlushPending();
                BatchSpawned?.Invoke(batch);
            }
        }

        private void Enqueue(ColorTally tally, SpawnReason reason)
        {
            foreach (SphereColor color in SphereColors.All)
            {
                for (int i = 0; i < tally[color]; i++) pending.Enqueue(new PendingSpawn(color, reason));
            }
        }

        private void FlushPending()
        {
            while (pending.Count > 0)
            {
                if (!finder.TryFindPoint(occupiedPositions, out Vector3 point)) return; // retry next frame
                PendingSpawn request = pending.Dequeue();
                CreateSphere(request.Color, point, request.Reason);
            }
        }

        private void CreateSphere(SphereColor color, Vector3 position, SpawnReason reason)
        {
            SphereColorDefinition definition = config.GetDefinition(color);
            Sphere sphere = Instantiate(definition.prefab, position, Quaternion.identity, sphereContainer);
            sphere.Destroyed += HandleSphereDestroyed;
            liveSpheres.Add(sphere);
            occupiedPositions.Add(position);
            liveCounts = liveCounts.With(color);
            LiveCountsChanged?.Invoke(liveCounts);
            SphereSpawned?.Invoke(sphere, reason);
        }
    }
}
