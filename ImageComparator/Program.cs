// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
//   
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImageComparator
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using WebApp.Services;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The simple compare.
        /// </summary>
        private static BitmapCompare simpleCompare;

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        private static void Main(string[] args)
        {
            var endDate = new DateTime(2010, 4, 2, 20, 30, 0);

            string goodDirectory;
            string badDirectory;
            string fileType;

            if (args.Length == 4)
            {
                goodDirectory = string.IsNullOrEmpty(args[1]) ? string.Empty : args[1];
                badDirectory = string.IsNullOrEmpty(args[2]) ? string.Empty : args[2];
                fileType = string.IsNullOrEmpty(args[3]) ? "jpg" : args[3];
            }
            else
            {
                Console.WriteLine("Please input the source image directory (good images) :");
                goodDirectory = Console.ReadLine();
                Console.WriteLine("Please input the destination image directory (incorrect images) :");
                badDirectory = Console.ReadLine();
                fileType = "jpg";
            }

            if (goodDirectory != null && badDirectory != null)
            {
                var badFiles = GetFiles(badDirectory, fileType);
                var processed = new List<string>();
                foreach (var goodFile in GetFiles(goodDirectory, fileType))
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

                            if (
                                !ImageCompare(
                                    Path.Combine(goodDirectory, goodFile), 
                                    Path.Combine(badDirectory, badFile)))
                            {
                                continue;
                            }

                            try
                            {
                                File.Copy(
                                    Path.Combine(badDirectory, badFile), 
                                    Path.Combine(badDirectory, goodFile), 
                                    true);

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
                        try
                        {
                            // File.Delete(Path.Combine(badDirectory, badFile));
                            if (badFiles.Contains(badFile))
                            {
                                badFiles.Remove(badFile);
                            }

                            // Console.WriteLine("Removed {0}", badFile);
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                        }
                    }
                }

                try
                {
                    using (var file = new StreamWriter(Path.Combine(badDirectory, "processed.txt"), true))
                    {
                        foreach (var f in processed)
                        {
                            file.WriteLine(f);
                        }
                    }
                }
                catch (IOException)
                {
                }
            }

            Console.WriteLine();
            Console.WriteLine("Complete");
            Console.ReadLine();
            Console.ReadLine();
        }

        /// <summary>
        /// The get files.
        /// </summary>
        /// <param name="directory">
        /// The <paramref name="directory"/>.
        /// </param>
        /// <param name="filetype">
        /// The <paramref name="filetype"/>.
        /// </param>
        /// <returns>
        /// The list.
        /// </returns>
        private static List<string> GetFiles(string directory, string filetype)
        {
            var di = new DirectoryInfo(directory);
            try
            {
                var files = di.GetFiles(string.Format("*.{0}", filetype));

                return files.Select(fi => fi.Name).ToList();
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Bad directory specified");
                return null;
            }
        }

        /// <summary>
        /// The image compare.
        /// </summary>
        /// <param name="firstImagePath">The first image path.</param>
        /// <param name="secondImagePath">The second image path.</param>
        /// <returns>The <see cref="bool" />.</returns>
        private static bool ImageCompare(string firstImagePath, string secondImagePath)
        {
            simpleCompare = new BitmapCompare();
            double sim;
            using (var comImage = new Bitmap(firstImagePath))
            using (
                var fileBitmap =
                    new Bitmap(
                        ThumbnailGenerator.GetThumbnailFromFile(secondImagePath, comImage.Width, comImage.Height, true, true)))
            {
                sim = simpleCompare.GetSimilarity(comImage, fileBitmap);
            }

            return Math.Round(sim, 3) > 0.75;
        }

        /// <summary>
        /// The date compare.
        /// </summary>
        /// <param name="firstImagePath">The first image path.</param>
        /// <param name="secondImagePath">The second image path.</param>
        /// <returns>The <see cref="bool" />.</returns>
        private static bool DateCompare(string firstImagePath, string secondImagePath)
        {
            var creationDate1 = File.GetCreationTime(firstImagePath);
            var creationDate2 = File.GetCreationTime(secondImagePath);
            var creationDate2Min = creationDate2.Subtract(TimeSpan.FromMinutes(5));
            var creationDate2Max = creationDate2.AddMinutes(5);

            return creationDate1 >= creationDate2Min && creationDate1 <= creationDate2Max;
        }
    }
}