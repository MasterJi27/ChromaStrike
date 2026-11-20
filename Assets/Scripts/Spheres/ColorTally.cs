using System;

namespace ChromaStrike.Spheres
{
    /// <summary>
    /// A small value type that counts spheres per colour. It is used for the live counters,
    /// for the "destroyed since the last batch" ledger, and for the batch that has to be spawned.
    /// </summary>
    [Serializable]
    public struct ColorTally : IEquatable<ColorTally>
    {
        public int red;
        public int green;
        public int blue;

        public ColorTally(int red, int green, int blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        public int this[SphereColor color]
        {
            get
            {
                switch (color)
                {
                    case SphereColor.Red: return red;
                    case SphereColor.Green: return green;
                    default: return blue;
                }
            }
            set
            {
                switch (color)
                {
                    case SphereColor.Red: red = value; break;
                    case SphereColor.Green: green = value; break;
                    default: blue = value; break;
                }
            }
        }

        public int Total => red + green + blue;

        /// <summary>Returns a copy with <paramref name="amount"/> added to one colour.</summary>
        public ColorTally With(SphereColor color, int amount = 1)
        {
            var copy = this;
            copy[color] += amount;
            return copy;
        }

        public bool Equals(ColorTally other) => red == other.red && green == other.green && blue == other.blue;
        public override bool Equals(object obj) => obj is ColorTally other && Equals(other);
        public override int GetHashCode() => (red * 397 ^ green) * 397 ^ blue;
        public override string ToString() => $"R{red} G{green} B{blue}";
    }
}
