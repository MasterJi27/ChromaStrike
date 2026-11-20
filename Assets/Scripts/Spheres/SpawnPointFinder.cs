using System.Collections.Generic;
using UnityEngine;
using ChromaStrike.Core;

namespace ChromaStrike.Spheres
{
    /// <summary>
    /// Finds a random free position inside the spawn area:
    ///   X, Z random in [-extent, extent], Y fixed,
    ///   never overlapping another sphere, a prop, a wall or the player.
    /// Sphere to sphere distance is checked in plain C# (reliable on the same frame), while walls,
    /// props and the player are checked with Physics.CheckSphere.
    /// </summary>
    public sealed class SpawnPointFinder
    {
        private readonly GameConfig config;
        private readonly Transform player;

        public SpawnPointFinder(GameConfig config, Transform player)
        {
            this.config = config;
            this.player = player;
        }

        public bool TryFindPoint(IReadOnlyCollection<Vector3> occupied, out Vector3 point)
        {
            float extent = config.spawnAreaHalfExtent;
            for (int attempt = 0; attempt < config.spawnAttemptsPerSphere; attempt++)
            {
                var candidate = new Vector3(Random.Range(-extent, extent), config.spawnHeight, Random.Range(-extent, extent));
                if (IsFree(candidate, occupied))
                {
                    point = candidate;
                    return true;
                }
            }

            point = default;
            return false;
        }

        /// <summary>True if a sphere centred at <paramref name="candidate"/> would not overlap anything.</summary>
        public bool IsFree(Vector3 candidate, IReadOnlyCollection<Vector3> occupied)
        {
            Vector3 fromPlayer = candidate - player.position;
            fromPlayer.y = 0f;
            if (fromPlayer.magnitude < config.playerClearance) return false;

            float minDistance = config.sphereRadius * 2f + config.spawnPadding;
            foreach (Vector3 other in occupied)
            {
                if ((other - candidate).sqrMagnitude < minDistance * minDistance) return false;
            }

            float checkRadius = config.sphereRadius + config.spawnPadding * 0.5f;
            return !Physics.CheckSphere(candidate, checkRadius, config.spawnBlockers, QueryTriggerInteraction.Ignore);
        }
    }
}
