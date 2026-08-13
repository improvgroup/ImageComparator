namespace ImageComparator;

using System.Drawing;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static void Main(string[] args)
    {
        var endDate = new DateTime(2010, 4, 2, 20, 30, 0);

        string? goodDirectory;
        string? badDirectory;
        string fileType;

        if (args.Length == 4)
        {
            goodDirectory = string.IsNullOrEmpty(args[1]) ? string.Empty : args[1];
            badDirectory = string.IsNullOrEmpty(args[2]) ? string.Empty : args[2];
            fileType = string.IsNullOrEmpty(args[3]) ? "jpg" : args[3];
        }
        else
        {
            Console.WriteLine("Please input the source image directory (good images):");
            goodDirectory = Console.ReadLine();
            Console.WriteLine("Please input the destination image directory (incorrect images):");
            badDirectory = Console.ReadLine();
            fileType = "jpg";
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

        var processed = new List<string>();

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

                    if (!ImageCompare(Path.Combine(goodDirectory, goodFile), Path.Combine(badDirectory, badFile)))
                    {
                        continue;
                    }

                    try
                    {
                        File.Copy(Path.Combine(badDirectory, badFile), Path.Combine(badDirectory, goodFile), true);
                        processed.Add(badFile);
                        Console.WriteLine("\r\n{0} --> {1}", badFile, goodFile);
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

    private static bool ImageCompare(string firstImagePath, string secondImagePath)
    {
        var comparer = new BitmapCompare();
        using var comImage = new Bitmap(firstImagePath);
        using var fileBitmap = new Bitmap(
            ThumbnailGenerator.GetThumbnailFromFile(secondImagePath, comImage.Width, comImage.Height, true, true));
        var sim = comparer.GetSimilarity(comImage, fileBitmap);
        return Math.Round(sim, 3) > 0.75;
    }

    private static bool DateCompare(string firstImagePath, string secondImagePath)
    {
        var creationDate1 = File.GetCreationTime(firstImagePath);
        var creationDate2 = File.GetCreationTime(secondImagePath);
        var creationDate2Min = creationDate2.Subtract(TimeSpan.FromMinutes(5));
        var creationDate2Max = creationDate2.AddMinutes(5);
        return creationDate1 >= creationDate2Min && creationDate1 <= creationDate2Max;
    }
}
