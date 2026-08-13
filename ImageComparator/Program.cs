namespace ImageComparator;

using System.Drawing;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static void Main(string[] args)
    {
        var endDate = new DateTime(2010, 4, 2, 20, 30, 0);
        var options = ParseArguments(args);

        string? goodDirectory = options.GoodDirectory;
        string? badDirectory = options.BadDirectory;
        var fileType = options.FileType;

        if (goodDirectory is null || badDirectory is null)
        {
            Console.WriteLine("Please input the source image directory (good images):");
            goodDirectory = Console.ReadLine();
            Console.WriteLine("Please input the destination image directory (incorrect images):");
            badDirectory = Console.ReadLine();
        }

        if (goodDirectory is null || badDirectory is null)
        {
            return;
        }

        var badFiles = GetFiles(badDirectory, fileType);
        if (badFiles is null)
        {
            return;
        }

        var comparer = new BitmapCompare(options.Strategy);
        var processed = new List<string>();
        var benchmarkPrinted = false;

        foreach (var goodFile in GetFiles(goodDirectory, fileType) ?? [])
        {
            Console.Write("{0}", goodFile);

            if (badFiles.Contains(goodFile))
            {
                badFiles.Remove(goodFile);
                Console.WriteLine();
                continue;
            }

            if (File.GetCreationTime(Path.Combine(badDirectory, goodFile)) <= endDate)
            {
                foreach (var badFile in badFiles)
                {
                    Console.Write(".");

                    if (!DateCompare(Path.Combine(goodDirectory, goodFile), Path.Combine(badDirectory, badFile)))
                    {
                        continue;
                    }

                    if (options.Benchmark && !benchmarkPrinted)
                    {
                        PrintBenchmark(
                            Path.Combine(goodDirectory, goodFile),
                            Path.Combine(badDirectory, badFile),
                            options.BenchmarkIterations);
                        benchmarkPrinted = true;
                    }

                    if (!ImageCompare(
                            Path.Combine(goodDirectory, goodFile),
                            Path.Combine(badDirectory, badFile),
                            comparer,
                            out var similarity))
                    {
                        continue;
                    }

                    try
                    {
                        File.Copy(Path.Combine(badDirectory, badFile), Path.Combine(badDirectory, goodFile), true);
                        processed.Add(badFile);
                        Console.WriteLine("\r\n{0} --> {1} ({2:F3} via {3})", badFile, goodFile, similarity, comparer.LastStrategyUsed);
                        break;
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            Console.WriteLine();

            foreach (var badFile in processed)
            {
                if (badFiles.Contains(badFile))
                {
                    badFiles.Remove(badFile);
                }
            }
        }

        try
        {
            using var file = new StreamWriter(Path.Combine(badDirectory, "processed.txt"), append: true);
            foreach (var f in processed)
            {
                file.WriteLine(f);
            }
        }
        catch (IOException)
        {
        }

        Console.WriteLine();
        Console.WriteLine("Complete");
        Console.ReadLine();
    }

    private static CommandLineOptions ParseArguments(string[] args)
    {
        var nonOptionArguments = new List<string>();
        var strategy = ComparisonStrategy.Auto;
        var benchmark = false;
        var benchmarkIterations = 25;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--strategy=", StringComparison.OrdinalIgnoreCase))
            {
                strategy = ParseStrategy(arg.Split('=', 2)[1]);
                continue;
            }

            if (arg.Equals("--strategy", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Missing value for --strategy; using default of auto.");
                    continue;
                }

                strategy = ParseStrategy(args[++i]);
                continue;
            }

            if (arg.Equals("--benchmark", StringComparison.OrdinalIgnoreCase))
            {
                benchmark = true;
                continue;
            }

            if (arg.StartsWith("--benchmark-iterations=", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(arg.Split('=', 2)[1], out var parsed) && parsed > 0)
                {
                    benchmarkIterations = parsed;
                }
                else
                {
                    Console.WriteLine(
                        "Invalid --benchmark-iterations value; using default of {0}.",
                        benchmarkIterations);
                }

                continue;
            }

            nonOptionArguments.Add(arg);
        }

        string? goodDirectory = null;
        string? badDirectory = null;
        var fileType = "jpg";

        if (nonOptionArguments.Count >= 2)
        {
            goodDirectory = nonOptionArguments[0];
            badDirectory = nonOptionArguments[1];
            fileType = nonOptionArguments.Count >= 3 && !string.IsNullOrWhiteSpace(nonOptionArguments[2])
                ? nonOptionArguments[2]
                : "jpg";
        }
        else if (nonOptionArguments.Count == 1)
        {
            goodDirectory = nonOptionArguments[0];
        }

        return new CommandLineOptions(goodDirectory, badDirectory, fileType, strategy, benchmark, benchmarkIterations);
    }

    private static ComparisonStrategy ParseStrategy(string value) => value.ToLowerInvariant() switch
    {
        "legacy" => ComparisonStrategy.LegacyDominantChannel,
        "mad" => ComparisonStrategy.MeanAbsoluteDifference,
        "dhash" => ComparisonStrategy.DifferenceHash,
        "auto" => ComparisonStrategy.Auto,
        _ => ComparisonStrategy.Auto,
    };

    private static void PrintBenchmark(string firstImagePath, string secondImagePath, int iterations)
    {
        try
        {
            var results = ComparisonBenchmark.RunForFiles(firstImagePath, secondImagePath, iterations);
            Console.WriteLine();
            Console.WriteLine("Benchmark results ({0} iterations):", iterations);
            foreach (var result in results)
            {
                Console.WriteLine(
                    "- {0}: {1:F4} ms/comparison, avg similarity {2:F4}",
                    result.Strategy,
                    result.AverageMillisecondsPerComparison,
                    result.AverageSimilarity);
            }

            if (results.Count > 0)
            {
                Console.WriteLine("Fastest strategy: {0}", results[0].Strategy);
            }

            Console.WriteLine();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Console.WriteLine("Benchmark skipped: {0}", ex.Message);
        }
    }

    private static List<string>? GetFiles(string directory, string filetype)
    {
        var di = new DirectoryInfo(directory);
        try
        {
            return di.GetFiles($"*.{filetype}").Select(fi => fi.Name).ToList();
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Bad directory specified");
            return null;
        }
    }

    private static bool ImageCompare(
        string firstImagePath,
        string secondImagePath,
        BitmapCompare comparer,
        out double similarity)
    {
        using var comImage = new Bitmap(firstImagePath);
        using var fileBitmap = new Bitmap(
            ThumbnailGenerator.GetThumbnailFromFile(secondImagePath, comImage.Width, comImage.Height, true, true));
        return comparer.IsSimilar(comImage, fileBitmap, out similarity);
    }

    private static bool DateCompare(string firstImagePath, string secondImagePath)
    {
        var creationDate1 = File.GetCreationTime(firstImagePath);
        var creationDate2 = File.GetCreationTime(secondImagePath);
        var creationDate2Min = creationDate2.Subtract(TimeSpan.FromMinutes(5));
        var creationDate2Max = creationDate2.AddMinutes(5);
        return creationDate1 >= creationDate2Min && creationDate1 <= creationDate2Max;
    }

    private sealed record CommandLineOptions(
        string? GoodDirectory,
        string? BadDirectory,
        string FileType,
        ComparisonStrategy Strategy,
        bool Benchmark,
        int BenchmarkIterations);
}
