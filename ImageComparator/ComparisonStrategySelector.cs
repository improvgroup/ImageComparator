namespace ImageComparator;

using System.Drawing;
using System.Runtime.Versioning;

/// <summary>Selects an appropriate comparison strategy for a pair of images.</summary>
[SupportedOSPlatform("windows")]
public static class ComparisonStrategySelector
{
    /// <summary>Returns the strategy best suited for the provided images.</summary>
    public static ComparisonStrategy SelectBest(Bitmap a, Bitmap b)
    {
        var maxPixels = Math.Max(a.Width * a.Height, b.Width * b.Height);
        if (maxPixels >= 1920 * 1080)
        {
            return ComparisonStrategy.DifferenceHash;
        }

        var ratioA = a.Width / (double)a.Height;
        var ratioB = b.Width / (double)b.Height;
        var ratioDelta = Math.Abs(ratioA - ratioB);
        if (ratioDelta > 0.20)
        {
            return ComparisonStrategy.DifferenceHash;
        }

        return ComparisonStrategy.MeanAbsoluteDifference;
    }
}

