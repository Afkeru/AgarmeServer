using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AgarmeServer.Others
{
    public struct Boost
    {
        public float dx;
        public float dy;
        public float d;
    }
    public static class Misc
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern int memcmp(byte* b1, byte* b2, long count);
        public unsafe class BinaryComparer<T> : IEqualityComparer<T> where T : unmanaged
        {
            private BinaryComparer() { }
            public static readonly BinaryComparer<T> Instance = new BinaryComparer<T>();
            public bool Equals(T x, T y)
            {

                return memcmp((byte*)&x, (byte*)&y, sizeof(T)) == 0;
            }

            public int GetHashCode([DisallowNull] T obj)
            {
                if(sizeof(T) <= 4)
                {
                    return (int)&obj;
                }
                return obj.GetHashCode();
            }
        }
        public const string version = "1.3.6";
        private static readonly Random randomMath = new Random();
        public static double RandomDouble() => randomMath.NextDouble();
        public static void ThrowIfBadNumber(params double[] numbers)
        {
            for (int i = 0; i < numbers.Length; i++)
                if (numbers[i] == double.NaN || double.IsInfinity(numbers[i]))
                    throw new Exception($"Bad number ({numbers[i]}, index {i}");
        }
        public static void ThrowIfBadOrNegativeNumber(params double[] numbers)
        {
            for (int i = 0; i < numbers.Length; i++)
                if (numbers[i] == double.NaN || double.IsInfinity(numbers[i]) || numbers[i] < 0)
                    throw new Exception($"Bad or negative number ({numbers[i]}, index {i}");
        }
        public static bool Intersects(RectangleF a, RectangleF b)
        {
            return a.X - a.Width < b.X + b.Width &&
            a.X + a.Width > b.X - b.Width &&
            a.Y - a.Height < b.Y + b.Height &&
            a.Y + a.Height > b.Y - b.Height;
        }
        public static bool FullyIntersects(RectangleF a, RectangleF b)
        {
            return a.X - a.Width >= b.X + b.Width &&
               a.X + a.Width <= b.X - b.Width &&
               a.Y - a.Height >= b.Y + b.Height &&
               a.Y + a.Height <= b.Y - b.Height;
        }
        public static (bool t, bool b, bool l, bool r) GetQuadFullIntersect(RectangleF a, RectangleF b) 
        {
            return (
                t: a.Y - a.Height < b.Y && a.Y + a.Height < b.Y,
                b: a.Y - a.Height > b.Y && a.Y + a.Height > b.Y,
                l: a.X - a.Width < b.X && a.X + a.Width < b.X,
                r: a.X - a.Width > b.X && a.X + a.Width > b.X
                );
        }
        public static (bool t, bool b, bool l, bool r) GetQuadIntersect(RectangleF a, RectangleF b)
        {
            return (
                t: a.Y - a.Height < b.Y || a.Y + a.Height < b.Y,
                b: a.Y - a.Height > b.Y || a.Y + a.Height > b.Y,
                l: a.X - a.Width < b.X || a.X + a.Width < b.X,
                r: a.X - a.Width > b.X || a.X + a.Width > b.X
            );
        }
    }
}
