namespace ImageComparator.Tests;

using System.Drawing;
using System.Runtime.Versioning;
using ImageComparator;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static int Main()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("Tests skipped: System.Drawing comparisons are Windows-only.");
            return 0;
        }

        var tests = new (string Name, Action Execute)[]
        {
            ("Strategies rank identical above different", StrategiesRankIdenticalAboveDifferent),
            ("Auto strategy picks dHash for very large images", AutoStrategyPicksDifferenceHashForLargeImages),
            ("Benchmark returns concrete strategies", BenchmarkReturnsConcreteStrategies),
        };

        var failures = new List<string>();

        foreach (var (name, execute) in tests)
        {
            try
            {
                execute();
                Console.WriteLine($"PASS: {name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"FAIL: {name}");
            }
        }

        if (failures.Count == 0)
        {
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("Failures:");
        foreach (var failure in failures)
        {
            Console.WriteLine($"- {failure}");
        }

        return 1;
    }

    private static void StrategiesRankIdenticalAboveDifferent()
    {
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

            AssertTrue(
                sameSimilarity > differentSimilarity,
                $"{strategy} expected sameSimilarity > differentSimilarity but was {sameSimilarity:F4} <= {differentSimilarity:F4}");
        }
    }

    private static void AutoStrategyPicksDifferenceHashForLargeImages()
    {
        using var imageA = CreateSolidBitmap(Color.Green, width: 2400, height: 1600);
        using var imageB = CreateSolidBitmap(Color.Green, width: 2400, height: 1600);
        var comparer = new BitmapCompare(ComparisonStrategy.Auto);

        _ = comparer.GetSimilarity(imageA, imageB);

        AssertTrue(
            comparer.LastStrategyUsed == ComparisonStrategy.DifferenceHash,
            $"Expected {ComparisonStrategy.DifferenceHash} but got {comparer.LastStrategyUsed}");
    }

    private static void BenchmarkReturnsConcreteStrategies()
    {
        using var imageA = CreateSolidBitmap(Color.White);
        using var imageB = CreateSolidBitmap(Color.Black);
        var results = ComparisonBenchmark.Run(imageA, imageB, iterations: 3);

        AssertTrue(results.Count == 3, $"Expected 3 benchmark rows, got {results.Count}");
        AssertTrue(
            results.All(result => result.Strategy != ComparisonStrategy.Auto),
            "Benchmark should return only concrete strategies.");
    }

    private static Bitmap CreateSolidBitmap(Color color, int width = 64, int height = 64)
    {
        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        return bitmap;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
