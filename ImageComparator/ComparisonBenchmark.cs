namespace ImageComparator;

using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;

/// <summary>Benchmark helpers for image comparison strategies.</summary>
[SupportedOSPlatform("windows")]
public static class ComparisonBenchmark
{
    /// <summary>Benchmarks all concrete strategies for a file pair.</summary>
    public static IReadOnlyList<ComparisonBenchmarkResult> RunForFiles(
        string firstImagePath,
        string secondImagePath,
        int iterations = 25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstImagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondImagePath);

        using var a = new Bitmap(firstImagePath);
        using var b = new Bitmap(secondImagePath);
        return Run(a, b, iterations);
    }

    /// <summary>Benchmarks all concrete strategies for a bitmap pair.</summary>
    public static IReadOnlyList<ComparisonBenchmarkResult> Run(Bitmap a, Bitmap b, int iterations = 25)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be greater than zero.");
        }

        var strategies = new[]
        {
            ComparisonStrategy.LegacyDominantChannel,
            ComparisonStrategy.MeanAbsoluteDifference,
            ComparisonStrategy.DifferenceHash,
        };

        var results = new List<ComparisonBenchmarkResult>(strategies.Length);

        foreach (var strategy in strategies)
        {
            var comparer = new BitmapCompare(strategy);
            double similarityTotal = 0;

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                similarityTotal += comparer.GetSimilarity(a, b);
            }

            sw.Stop();
            results.Add(new ComparisonBenchmarkResult(
                strategy,
                similarityTotal / iterations,
                sw.Elapsed.TotalMilliseconds / iterations));
        }

        return results
            .OrderBy(result => result.AverageMillisecondsPerComparison)
            .ToList();
    }
}

/// <summary>Strategy benchmark output.</summary>
/// <param name="Strategy">The benchmarked strategy.</param>
/// <param name="AverageSimilarity">Average similarity produced during the benchmark.</param>
/// <param name="AverageMillisecondsPerComparison">Average elapsed milliseconds per comparison.</param>
public sealed record ComparisonBenchmarkResult(
    ComparisonStrategy Strategy,
    double AverageSimilarity,
    double AverageMillisecondsPerComparison);

