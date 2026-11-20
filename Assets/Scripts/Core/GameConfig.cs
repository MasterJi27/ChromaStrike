using System;
using UnityEngine;
using ChromaStrike.Spheres;

namespace ChromaStrike.Core
{
    /// <summary>
    /// ScriptableObject that holds every tunable number of the game.
    /// Spawn counts, timings and player settings are all edited from the Inspector
    /// instead of being typed into scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Chroma Strike/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Round")]
        [Min(1f)] public float roundDurationSeconds = 120f;

        [Header("Initial batch (15 + 10 + 5 = 30 spheres)")]
        [Min(0)] public int initialRed = 15;
        [Min(0)] public int initialGreen = 10;
        [Min(0)] public int initialBlue = 5;

        [Header("Spawn rules")]
        [Tooltip("X and Z are drawn from [-extent, +extent]. Default: 10.")]
        [Min(1f)] public float spawnAreaHalfExtent = 10f;
        [Tooltip("Y is fixed. Default: 1.")]
        public float spawnHeight = 1f;
        [Min(0.05f)] public float sphereRadius = 0.5f;
        [Tooltip("Extra empty space kept around every sphere so two spheres never touch.")]
        [Min(0f)] public float spawnPadding = 0.2f;
        [Tooltip("Spheres never spawn closer than this to the player.")]
        [Min(0f)] public float playerClearance = 2.5f;
        [Min(1)] public int spawnAttemptsPerSphere = 60;
        [Tooltip("One extra random sphere appears every N seconds.")]
        [Min(0.1f)] public float periodicSpawnInterval = 5f;
        [Tooltip("After this many destroyed spheres, a new batch with the same colour ratio is spawned.")]
        [Min(1)] public int replacementBatchSize = 10;
        [Tooltip("Layers that a new sphere must not overlap (walls, props, player).")]
        public LayerMask spawnBlockers;

        [Header("Shooting")]
        [Tooltip("Layers the centre ray can hit. Walls are included so props block shots.")]
        public LayerMask shootMask;
        [Min(1f)] public float shootDistance = 80f;

        [Header("Player")]
        [Min(0.1f)] public float walkSpeed = 5f;
        [Min(0.1f)] public float sprintSpeed = 8f;
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [Min(0.001f)] public float lookSensitivity = 0.09f;
        [Range(40f, 89f)] public float maxLookAngle = 85f;
        public float gravity = -20f;

        [Header("Palette")]
        public SphereColorDefinition[] palette = new SphereColorDefinition[3];

        /// <summary>Looks up the visual definition (name, colour, prefab) for a colour.</summary>
        public SphereColorDefinition GetDefinition(SphereColor color)
        {
            for (int i = 0; i < palette.Length; i++)
            {
                if (palette[i].id == color) return palette[i];
            }
            throw new InvalidOperationException($"No palette entry for {color}. Assign it in the GameConfig asset.");
        }

        /// <summary>The 15 / 10 / 5 starting distribution as a tally.</summary>
        public ColorTally InitialTally => new ColorTally(initialRed, initialGreen, initialBlue);
    }

    /// <summary>Inspector friendly description of one sphere colour.</summary>
    [Serializable]
    public struct SphereColorDefinition
    {
        public SphereColor id;
        public string displayName;
        public Color color;
        public Sphere prefab;
    }
}
