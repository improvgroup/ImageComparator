namespace ImageComparator
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WebApp.Services;

    internal class Program
    {
        private static BitmapCompare _simpleCompare;

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
                                continue;

                            if (!ImageCompare(Path.Combine(goodDirectory, goodFile), Path.Combine(badDirectory, badFile)))
                                continue;

                            try
                            {
                                File.Copy(Path.Combine(badDirectory, badFile),
                                          Path.Combine(badDirectory, goodFile), true);

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
                            //File.Delete(Path.Combine(badDirectory, badFile));

                            if (badFiles.Contains(badFile))
                            {
                                badFiles.Remove(badFile);
                            }

                            //Console.WriteLine("Removed {0}", badFile);
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

        private static List<string> GetFiles(string directory, string filetype)
        {
            var di = new DirectoryInfo(directory);
            FileInfo[] rgFiles;
            try
            {
                rgFiles = di.GetFiles(string.Format("*.{0}", filetype));

                return rgFiles.Select(fi => fi.Name).ToList();
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Bad directory specified");
                return null;
            }
        }

        private static bool ImageCompare(string imgPath1, string imgPath2)
        {
            _simpleCompare = new BitmapCompare();
            double sim;
            using (var comImage = new Bitmap(imgPath1))
            using (var fBmap = new Bitmap(ThumbnailGenerator.gGetThumbnail(imgPath2, comImage.Width, comImage.Height, true, true)))
            {
                sim = _simpleCompare.GetSimilarity(comImage, fBmap);
            }

            return Math.Round(sim, 3) > 0.75;
        }

        private static bool DateCompare(string imgPath1, string imgPath2)
        {
            var creationDate1 = File.GetCreationTime(imgPath1);
            var creationDate2 = File.GetCreationTime(imgPath2);
            var creationDate2Min = creationDate2.Subtract(TimeSpan.FromMinutes(5));
            var creationDate2Max = creationDate2.AddMinutes(5);

            return creationDate1 >= creationDate2Min && creationDate1 <= creationDate2Max;
        }
    }
}