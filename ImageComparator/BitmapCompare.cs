namespace ImageComparator;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

/// <summary>Compares two bitmaps by their dominant colour channel average.</summary>
[SupportedOSPlatform("windows")]
public class BitmapCompare : IBitmapCompare
{
    /// <inheritdoc/>
    public double GetSimilarity(Bitmap a, Bitmap b)
    {
        var dataA = ProcessBitmap(a);
        var dataB = ProcessBitmap(b);

        var maxA = (a.Width * 3) * a.Height;
        var maxB = (b.Width * 3) * b.Height;

        double result = dataA.GetLargest() switch
        {
            1 => (double)(Math.Abs(dataA.R / maxA) - Math.Abs(dataB.R / maxB)) / 2,
            2 => (double)(Math.Abs(dataA.G / maxA) - Math.Abs(dataB.G / maxB)) / 2,
            _ => (double)(Math.Abs(dataA.B / maxA) - Math.Abs(dataB.B / maxB)) / 2,
        };

        result = Math.Abs((result + 100) / 100);

        if (result > 1.0)
        {
            result -= 1.0;
        }

        return result;
    }

    private static RGBData ProcessBitmap(Bitmap a)
    {
        var bmpData = a.LockBits(
            new Rectangle(0, 0, a.Width, a.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        var ptr = bmpData.Scan0;
        var data = new RGBData();

        unsafe
        {
            var p = (byte*)(void*)ptr;
            var width = a.Width * 3;
            var offset = bmpData.Stride - width;

            for (var y = 0; y < a.Height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    data.R += p[0];
                    data.G += p[1];
                    data.B += p[2];
                    ++p;
                }

                p += offset;
            }
        }

        a.UnlockBits(bmpData);
        return data;
    }

    /// <summary>Holds summed RGB channel values for a bitmap.</summary>
    public struct RGBData : IEquatable<RGBData>
    {
        /// <summary>Gets or sets the summed red channel value.</summary>
        public int R { get; set; }

        /// <summary>Gets or sets the summed green channel value.</summary>
        public int G { get; set; }

        /// <summary>Gets or sets the summed blue channel value.</summary>
        public int B { get; set; }

        /// <summary>Returns which channel (1=R, 2=G, 3=B) has the largest sum.</summary>
        public readonly int GetLargest() =>
            R > B ? (R > G ? 1 : 2) : 3;

        /// <inheritdoc/>
        public readonly bool Equals(RGBData other) =>
            R == other.R && G == other.G && B == other.B;

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj) =>
            obj is RGBData other && Equals(other);

        /// <inheritdoc/>
        public override readonly int GetHashCode() =>
            HashCode.Combine(R, G, B);

        /// <summary>Equality operator.</summary>
        public static bool operator ==(RGBData left, RGBData right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        public static bool operator !=(RGBData left, RGBData right) => !(left == right);
    }
}
