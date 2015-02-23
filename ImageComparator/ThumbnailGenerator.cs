// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThumbnailGenerator.cs" company="">
//   
// </copyright>
// <summary>
//   Thumbnail Generator
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebApp.Services
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    /// <summary>
    ///     Thumbnail Generator
    /// </summary>
    public class ThumbnailGenerator
    {
        /// <summary>
        /// Creates the thumbnail.
        /// </summary>
        /// <param name="input">The <paramref name="input" /> .</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="preserveAspectRatio">if set to <see langword="true" /> [preserve aspect ratio].</param>
        /// <param name="uniformToFill">if set to <see langword="true" /> [uniform to fill].</param>
        /// <param name="quality">The quality.</param>
        /// <param name="format">The format.</param>
        /// <returns>The <see cref="System.IO.Stream" /> .</returns>
        /// <exception cref="System.ArgumentNullException">The value of 'input' cannot be null.</exception>
        /// <exception cref="Exception">The operation failed.</exception>
        /// <exception cref="ExternalException">The image was saved with the wrong image format.</exception>
        /// <exception cref="IOException">Seeking is attempted before the beginning of the stream.</exception>
        /// <exception cref="ObjectDisposedException">The current stream instance is closed.</exception>
        /// <exception cref="ArgumentException">There is an invalid <see cref="T:System.IO.SeekOrigin" />. -or- offset caused an arithmetic overflow.</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is greater than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="ExternalException">The image was saved with the wrong image format.</exception>
        public static Stream CreateThumbnail(
            Stream input,
            int width = 0,
            int height = 0,
            bool preserveAspectRatio = true,
            bool uniformToFill = false,
            long quality = 100L,
            ImageFormat format = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            var output = new MemoryStream();
            var encoder = GetEncoder(format ?? ImageFormat.Png);
            var encoderParameters = new EncoderParameters(1)
            {
                Param =
                                                {
                                                    [0] =
                                                        new EncoderParameter(
                                                        Encoder.Quality,
                                                        quality)
                                                }
            };

            using (var source = Image.FromStream(input))
            {
                int sourceWidthBeforeResize = source.Width;
                int sourceHeightBeforeResize = source.Height;
                if (width == 0)
                {
                    width = sourceWidthBeforeResize;
                }

                if (height == 0)
                {
                    height = sourceHeightBeforeResize;
                }

                var currentRatio = source.Width / (double)source.Height;
                var desiredRatio = preserveAspectRatio ? currentRatio : width / (double)height;
                if (currentRatio < desiredRatio)
                {
                    sourceWidthBeforeResize = source.Width;
                    sourceHeightBeforeResize = Convert.ToInt32(sourceWidthBeforeResize / desiredRatio);
                }
                else if (currentRatio > desiredRatio)
                {
                    sourceHeightBeforeResize = source.Height;
                    sourceWidthBeforeResize = Convert.ToInt32(sourceHeightBeforeResize * desiredRatio);
                }

                float topLeftX = uniformToFill ? Convert.ToInt32((source.Width - sourceWidthBeforeResize) / 2) : 0;
                float topLeftY = uniformToFill ? Convert.ToInt32((source.Height - sourceHeightBeforeResize) / 2) : 0;

                using (var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    thumbnail.SetResolution(source.HorizontalResolution, source.VerticalResolution);
                    using (var graphic = Graphics.FromImage(thumbnail))
                    {
                        graphic.Clear(Color.Transparent);
                        graphic.CompositingMode = CompositingMode.SourceCopy;
                        graphic.CompositingQuality = CompositingQuality.HighQuality;
                        graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphic.SmoothingMode = SmoothingMode.HighQuality;
                        graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        var destination = new Rectangle(0, 0, width, height);

                        using (var imageAttributes = new ImageAttributes())
                        {
                            imageAttributes.SetWrapMode(WrapMode.TileFlipXY);

                            graphic.DrawImage(
                                source,
                                destination,
                                topLeftX,
                                topLeftY,
                                sourceWidthBeforeResize,
                                sourceHeightBeforeResize,
                                GraphicsUnit.Pixel,
                                imageAttributes);
                        }

                        thumbnail.Save(output, encoder, encoderParameters);
                    }
                }

                output.Seek(0, SeekOrigin.Begin);
                return output;
            }
        }

        /// <summary>
        /// Saves the thumbnail.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <param name="thumbnailPath">The thumbnail path.</param>
        /// <param name="width">The <paramref name="width" />.</param>
        /// <param name="height">The <paramref name="height" />.</param>
        /// <param name="preserveAspectRatio">if set to <c>true</c> [preserve aspect ratio].</param>
        /// <param name="uniformToFill">if set to <c>true</c> [uniform to fill].</param>
        /// <param name="quality">The quality.</param>
        /// <param name="format">The format.</param>
        /// <exception cref="System.ArgumentNullException">The value of ' <paramref name="originalPath" /> ' cannot be null.</exception>
        /// <exception cref="OutOfMemoryException">The file does not have a valid image format.-or-GDI+ does not support the pixel format of the file.</exception>
        /// <exception cref="FileNotFoundException">The specified file does not exist.</exception>
        /// <exception cref="ExternalException">The image was saved with the wrong image format</exception>
        /// <exception cref="NotSupportedException">path is in an invalid format.</exception>
        /// <exception cref="UnauthorizedAccessException">path specified a directory.-or- The caller does not have the required permission.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by GetInvalidPathChars.</exception>
        /// <exception cref="Exception">The operation failed.</exception>
        /// <exception cref="IOException">Seeking is attempted before the beginning of the stream.</exception>
        /// <exception cref="ObjectDisposedException">The current stream instance is closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is greater than <see cref="F:System.Int32.MaxValue" />.</exception>
        public static void SaveThumbnail(
            string originalPath,
            string thumbnailPath,
            int width = 0,
            int height = 0,
            bool preserveAspectRatio = true,
            bool uniformToFill = false,
            long quality = 100L,
            ImageFormat format = null)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                throw new ArgumentNullException("originalPath");
            }

            if (string.IsNullOrWhiteSpace(thumbnailPath))
            {
                throw new ArgumentNullException("thumbnailPath");
            }

            using (var fileStream = File.OpenRead(originalPath))
            {
                var thumbnail = CreateThumbnail(
                    fileStream,
                    width,
                    height,
                    preserveAspectRatio,
                    uniformToFill,
                    quality,
                    format);
                var output = File.OpenWrite(thumbnailPath);
                thumbnail.CopyTo(output);
            }
        }

        /// <summary>
        /// Gets a thumbnail as a stream from a file at the specified path.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <param name="width">The <paramref name="width" /> .</param>
        /// <param name="height">The <paramref name="height" /> .</param>
        /// <param name="preserveAspectRatio">The preserve aspect ratio.</param>
        /// <param name="uniformToFill">The uniform to fill.</param>
        /// <param name="quality">The <paramref name="quality" /> .</param>
        /// <param name="format">The format.</param>
        /// <returns>The <see cref="System.IO.Stream" /> .</returns>
        /// <exception cref="System.OutOfMemoryException">Out of memory while loading image from file.</exception>
        /// <exception cref="System.IO.FileNotFoundException">File not found.</exception>
        /// <exception cref="System.ArgumentNullException">The value of 'originalPath' cannot be null.</exception>
        /// <exception cref="Exception">The operation failed.</exception>
        /// <exception cref="ExternalException">The image was saved with the wrong image format.</exception>
        /// <exception cref="IOException">Seeking is attempted before the beginning of the stream.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters. </exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive). </exception>
        /// <exception cref="UnauthorizedAccessException">path specified a directory.-or- The caller does not have the required permission. </exception>
        /// <exception cref="NotSupportedException">path is in an invalid format. </exception>
        /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by GetInvalidPathChars.</exception>
        /// <exception cref="ObjectDisposedException">The current stream instance is closed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is greater than <see cref="F:System.Int32.MaxValue" />.</exception>
        public static Stream GetThumbnailFromFile(
            string originalPath,
            int width = 0,
            int height = 0,
            bool preserveAspectRatio = true,
            bool uniformToFill = false,
            long quality = 100L,
            ImageFormat format = null)
        {
            if (string.IsNullOrEmpty(originalPath))
            {
                throw new ArgumentNullException("originalPath");
            }

            try
            {
                var fileStream = File.OpenRead(originalPath);
                return CreateThumbnail(fileStream, width, height, preserveAspectRatio, uniformToFill, quality, format);
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                Trace.TraceError(outOfMemoryException.Message);
                throw;
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                Trace.TraceError(fileNotFoundException.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the encoder.
        /// </summary>
        /// <param name="format">
        /// The <paramref name="format"/> .
        /// </param>
        /// <returns>
        /// The <see cref="ImageCodecInfo" />.
        /// </returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}