
namespace  Evergreen.Lib.Git.Models
{
    public class Color
    {
        private struct Rgb
        {
            public ushort R;
            public ushort G;
            public ushort B;

            public static implicit operator Rgb(ushort[] values) => new ()
            {
                R = values[0],
                G = values[1],
                B = values[2],
            };
        }

        private static readonly Rgb[] Palette =
        {
            new ushort[] { 196, 160, 0   },
            new ushort[] { 78,  154, 6   },
            new ushort[] { 206, 92,  0   },
            new ushort[] { 32,  74,  135 },
            new ushort[] { 108, 53,  102 },
            new ushort[] { 164, 0,   0   },
            new ushort[] { 138, 226, 52  },
            new ushort[] { 252, 175, 62  },
            new ushort[] { 114, 159, 207 },
            new ushort[] { 252, 233, 79  },
            new ushort[] { 136, 138, 133 },
            new ushort[] { 173, 127, 168 },
            new ushort[] { 233, 185, 110 },
            new ushort[] { 239, 41,  41  }
        };

        private static uint currentIndex;

        public uint Idx;

        public static void Reset()
        {
            currentIndex = 0;
        }

        public double R => Palette[Idx].R / 255.0;

        public double G => Palette[Idx].G / 255.0;

        public double B => Palette[Idx].B / 255.0;

        public void Components(out double r, out double g, out double b)
        {
            r = R;
            g = G;
            b = B;
        }

        private static uint IncIndex()
        {
            var next = currentIndex++;

            if (currentIndex == Palette.Length)
            {
                currentIndex = 0;
            }

            return next;
        }

        public static Color Next()
        {
            return new Color
            {
                Idx = IncIndex()
            };
        }

        public Color NextIndex()
        {
            Idx = IncIndex();
            return this;
        }

        public Color Copy()
        {
            return new Color
            {
                Idx = Idx
            };
        }
    }
}
