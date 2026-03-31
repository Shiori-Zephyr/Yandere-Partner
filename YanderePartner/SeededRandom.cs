namespace YanderePartner;

public class SeededRandom
{
    private uint a, b, c, d;

    public SeededRandom(string seed)
    {
        unchecked
        {
            uint h = 1779033703u ^ (uint)seed.Length;
            for (var i = 0; i < seed.Length; i++)
            {
                h = (h ^ seed[i]) * 3432918353u;
                h = (h << 13) | (h >> 19);
            }

            a = Splitmix(ref h);
            b = Splitmix(ref h);
            c = Splitmix(ref h);
            d = Splitmix(ref h);
        }
    }

    private static uint Splitmix(ref uint state)
    {
        unchecked
        {
            state = (state ^ (state >> 16)) * 2246822507u;
            state = (state ^ (state >> 13)) * 3266489909u;
            return state ^= state >> 16;
        }
    }

    public double Random(double min = 0.0, double max = 1.0) =>
        Next() * (max - min) + min;

    public int RandomInt(int min, int max) =>
        Math.Min((int)(Next() * (max - min + 1)) + min, max);

    private double Next()
    {
        unchecked
        {
            var t = a + b;
            a = b ^ (b >> 9);
            b = c + (c << 3);
            c = (c << 21) | (c >> 11);
            d++;
            t += d;
            c += t;
            return t / 4294967296.0;
        }
    }
}
