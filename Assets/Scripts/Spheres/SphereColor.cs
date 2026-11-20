using System;

namespace ChromaStrike.Spheres
{
    /// <summary>The three colours used in the game. Values double as array indices.</summary>
    public enum SphereColor
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }

    public static class SphereColors
    {
        /// <summary>All colours in a fixed order, for loops.</summary>
        public static readonly SphereColor[] All = (SphereColor[])Enum.GetValues(typeof(SphereColor));
    }
}
