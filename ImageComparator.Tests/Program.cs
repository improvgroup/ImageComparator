namespace ImageComparator.Tests;

using System.Drawing;
using System.Runtime.Versioning;
using ImageComparator;
using TUnit.Core;

public class BitmapCompareTests
{
    [Test]
    [SupportedOSPlatform("windows")]
    public async Task StrategiesRankIdenticalAboveDifferent()
    {
        if (!IsWindows())
        {
            Skip.Test("System.Drawing comparisons are Windows-only.");
            return;
        }

        using var imageA = CreateSolidBitmap(Color.Red);
        using var imageB = CreateSolidBitmap(Color.Red);
        using var imageC = CreateSolidBitmap(Color.Blue);

        var strategies = new[]
        {
            ComparisonStrategy.LegacyDominantChannel,
            ComparisonStrategy.MeanAbsoluteDifference,
            ComparisonStrategy.DifferenceHash,
        };

        foreach (var strategy in strategies)
        {
            var comparer = new BitmapCompare(strategy);
            var sameSimilarity = comparer.GetSimilarity(imageA, imageB);
            var differentSimilarity = comparer.GetSimilarity(imageA, imageC);

            await Assert.That(sameSimilarity > differentSimilarity).IsTrue();
        }
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task AutoStrategyPicksDifferenceHashForLargeImages()
    {
        if (!IsWindows())
        {
            Skip.Test("System.Drawing comparisons are Windows-only.");
            return;
        }
        using var imageA = CreateSolidBitmap(Color.Green, width: 2400, height: 1600);
        using var imageB = CreateSolidBitmap(Color.Green, width: 2400, height: 1600);
        var comparer = new BitmapCompare(ComparisonStrategy.Auto);

        _ = comparer.GetSimilarity(imageA, imageB);

        await Assert.That(comparer.LastStrategyUsed).IsEqualTo(ComparisonStrategy.DifferenceHash);
    }

    [Test]
    [SupportedOSPlatform("windows")]
    public async Task BenchmarkReturnsConcreteStrategies()
    {
        if (!IsWindows())
        {
            Skip.Test("System.Drawing comparisons are Windows-only.");
            return;
        }

        using var imageA = CreateSolidBitmap(Color.White);
        using var imageB = CreateSolidBitmap(Color.Black);
        var results = ComparisonBenchmark.Run(imageA, imageB, iterations: 3);

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results.All(result => result.Strategy != ComparisonStrategy.Auto)).IsTrue();
    }

    [SupportedOSPlatform("windows")]
    private static Bitmap CreateSolidBitmap(Color color, int width = 64, int height = 64)
    {
        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    [SupportedOSPlatformGuard("windows")]
    private static bool IsWindows() => OperatingSystem.IsWindows();
}
