namespace ImageComparator;

using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.Versioning;

/// <summary>Compares two bitmaps using a configurable image-comparison strategy.</summary>
[SupportedOSPlatform("windows")]
public class BitmapCompare : IBitmapCompare
{
    /// <summary>Initializes a comparer with automatic strategy selection.</summary>
    public BitmapCompare()
        : this(ComparisonStrategy.Auto)
    {
    }

    /// <summary>Initializes a comparer with a fixed strategy.</summary>
    public BitmapCompare(ComparisonStrategy strategy)
    {
        Strategy = strategy;
    }

    /// <summary>Gets the configured comparison strategy.</summary>
    public ComparisonStrategy Strategy { get; }

    /// <summary>Gets the strategy used in the most recent comparison.</summary>
    public ComparisonStrategy LastStrategyUsed { get; private set; }

    /// <inheritdoc/>
    public double GetSimilarity(Bitmap a, Bitmap b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var chosenStrategy = Strategy == ComparisonStrategy.Auto
            ? ComparisonStrategySelector.SelectBest(a, b)
            : Strategy;

        LastStrategyUsed = chosenStrategy;

        return chosenStrategy switch
        {
            ComparisonStrategy.LegacyDominantChannel => GetLegacySimilarity(a, b),
            ComparisonStrategy.MeanAbsoluteDifference => GetMeanAbsoluteDifferenceSimilarity(a, b),
            ComparisonStrategy.DifferenceHash => GetDifferenceHashSimilarity(a, b),
            _ => throw new InvalidOperationException($"Unsupported strategy '{chosenStrategy}'."),
        };
    }

    /// <summary>Returns whether a pair is similar using strategy-specific default thresholds.</summary>
    public bool IsSimilar(Bitmap a, Bitmap b, out double similarity, double? threshold = null)
    {
        similarity = GetSimilarity(a, b);
        var strategyThreshold = threshold ?? GetDefaultThreshold(LastStrategyUsed);
        return similarity > strategyThreshold;
    }

    /// <summary>Gets default threshold by strategy.</summary>
    public static double GetDefaultThreshold(ComparisonStrategy strategy) => strategy switch
    {
        ComparisonStrategy.LegacyDominantChannel => 0.75,
        ComparisonStrategy.MeanAbsoluteDifference => 0.90,
        ComparisonStrategy.DifferenceHash => 0.82,
        ComparisonStrategy.Auto => 0.90,
        _ => 0.90,
    };

    private static double GetLegacySimilarity(Bitmap a, Bitmap b)
    {
        using var normalizedA = Ensure24Bpp(a, a.Width, a.Height);
        using var normalizedB = Ensure24Bpp(b, b.Width, b.Height);

        var dataA = ProcessBitmap(normalizedA);
        var dataB = ProcessBitmap(normalizedB);

        var maxA = (normalizedA.Width * 3) * normalizedA.Height;
        var maxB = (normalizedB.Width * 3) * normalizedB.Height;

        double result = dataA.GetLargest() switch
        {
            1 => (Math.Abs((double)dataA.R / maxA) - Math.Abs((double)dataB.R / maxB)) / 2,
            2 => (Math.Abs((double)dataA.G / maxA) - Math.Abs((double)dataB.G / maxB)) / 2,
            _ => (Math.Abs((double)dataA.B / maxA) - Math.Abs((double)dataB.B / maxB)) / 2,
        };

        result = Math.Abs((result + 100) / 100);

        if (result > 1.0)
        {
            result -= 1.0;
        }

        return result;
    }

    private static double GetMeanAbsoluteDifferenceSimilarity(Bitmap a, Bitmap b)
    {
        const int targetWidth = 64;
        const int targetHeight = 64;

        using var normalizedA = Ensure24Bpp(a, targetWidth, targetHeight);
        using var normalizedB = Ensure24Bpp(b, targetWidth, targetHeight);

        double totalDiff = 0;
        const double maxDiffPerPixel = 255 * 3;

        var dataA = normalizedA.LockBits(
            new Rectangle(0, 0, targetWidth, targetHeight),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        var dataB = normalizedB.LockBits(
            new Rectangle(0, 0, targetWidth, targetHeight),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            unsafe
            {
                var ptrA = (byte*)(void*)dataA.Scan0;
                var ptrB = (byte*)(void*)dataB.Scan0;
                var rowLength = targetWidth * 3;
                var offsetA = dataA.Stride - rowLength;
                var offsetB = dataB.Stride - rowLength;

                for (var y = 0; y < targetHeight; y++)
                {
                    for (var x = 0; x < rowLength; x++)
                    {
                        totalDiff += Math.Abs(ptrA[0] - ptrB[0]);
                        ptrA++;
                        ptrB++;
                    }

                    ptrA += offsetA;
                    ptrB += offsetB;
                }
            }
        }
        finally
        {
            normalizedA.UnlockBits(dataA);
            normalizedB.UnlockBits(dataB);
        }

        var averageDiff = totalDiff / (targetWidth * targetHeight);
        return Math.Clamp(1.0 - (averageDiff / maxDiffPerPixel), 0.0, 1.0);
    }

    private static double GetDifferenceHashSimilarity(Bitmap a, Bitmap b)
    {
        var hashA = ComputeDifferenceHash(a);
        var hashB = ComputeDifferenceHash(b);
        var bitDistance = BitOperations.PopCount(hashA ^ hashB);
        return 1.0 - (bitDistance / 64.0);
    }

    private static ulong ComputeDifferenceHash(Bitmap source)
    {
        const int width = 9;
        const int height = 8;

        using var normalized = Ensure24Bpp(source, width, height);
        ulong hash = 0;
        var bitIndex = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width - 1; x++)
            {
                var left = normalized.GetPixel(x, y);
                var right = normalized.GetPixel(x + 1, y);

                var leftLuma = (left.R * 299) + (left.G * 587) + (left.B * 114);
                var rightLuma = (right.R * 299) + (right.G * 587) + (right.B * 114);

                if (leftLuma > rightLuma)
                {
                    hash |= 1UL << bitIndex;
                }

                bitIndex++;
            }
        }

        return hash;
    }

    private static Bitmap Ensure24Bpp(Bitmap source, int width, int height)
    {
        var normalized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(normalized);
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return normalized;
    }

    private static RGBData ProcessBitmap(Bitmap source)
    {
        var bmpData = source.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        var ptr = bmpData.Scan0;
        var data = new RGBData();

        try
        {
            unsafe
            {
                var p = (byte*)(void*)ptr;
                var width = source.Width * 3;
                var offset = bmpData.Stride - width;

                for (var y = 0; y < source.Height; ++y)
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
        }
        finally
        {
            source.UnlockBits(bmpData);
        }

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
