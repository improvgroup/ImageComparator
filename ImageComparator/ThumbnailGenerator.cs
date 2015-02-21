namespace WebApp.Services
{
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;

    /// <summary>
    /// Thumbnail Generator
    /// </summary>
    public class ThumbnailGenerator
    {
        /// <summary>
        /// Creates the thumbnail.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="preserveAspectRatio">if set to <c>true</c> [preserve aspect ratio].</param>
        /// <param name="uniformToFill">if set to <c>true</c> [uniform to fill].</param>
        /// <returns></returns>
        public static Stream CreateThumbnail(Stream input, int width, int height, bool preserveAspectRatio,
                                             bool uniformToFill)
        {
            Contract.Requires(input != null);
            var original = new Bitmap(input);

            var newWidth = width;
            var newHeight = height;

            if (preserveAspectRatio)
            {
                if (uniformToFill)
                {
                    newWidth = width;
                    newHeight = width*original.Height/original.Width;

                    if (newHeight < height)
                    {
                        newHeight = height;
                        newWidth = height*original.Width/original.Height;
                    }
                }
                else
                {
                    if (original.Width > original.Height)
                    {
                        newWidth = width;
                        newHeight = width*original.Height/original.Width;
                    }
                    else
                    {
                        newHeight = height;
                        newWidth = height*original.Width/original.Height;
                    }
                }
            }

            var thumbnail = new Bitmap(newWidth, newHeight);
            using (var graphic = Graphics.FromImage(thumbnail))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.AntiAlias;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphic.DrawImage(original, 0, 0, newWidth, newHeight);

                var ms = new MemoryStream();
                thumbnail.Save(ms, ImageFormat.Jpeg);

                ms.Seek(0, SeekOrigin.Begin);

                if (!uniformToFill)
                {
                    return ms;
                }

                var originalImage = Image.FromStream(ms);
                var cropped = new Bitmap(width, height);
                using (var g = Graphics.FromImage(cropped))
                {
                    //var rectDestination = new Rectangle(0, 0, cropped.Width, cropped.Height);
                    var rectCropArea = new Rectangle(0, 0, width, height);

                    g.DrawImageUnscaledAndClipped(originalImage, rectCropArea);
                    //g.DrawImage(originalImage, rectDestination, rectCropArea, GraphicsUnit.Pixel);

                    var cms = new MemoryStream();
                    cropped.Save(cms, ImageFormat.Jpeg);

                    cms.Seek(0, SeekOrigin.Begin);

                    return cms;
                }
            }
        }

        /// <summary>
        /// Saves the thumbnail.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <param name="thumbnailPath">The thumbnail path.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="preserveAspectRatio">if set to <c>true</c> [preserve aspect ratio].</param>
        public static void SaveThumbnail(string originalPath, string thumbnailPath, int width, int height,
                                         bool preserveAspectRatio)
        {
            SaveThumbnail(originalPath, thumbnailPath, width, height, preserveAspectRatio, false);
        }

        /// <summary>
        /// Saves the thumbnail.
        /// </summary>
        /// <param name="originalPath">The original path.</param>
        /// <param name="thumbnailPath">The thumbnail path.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="preserveAspectRatio">if set to <c>true</c> [preserve aspect ratio].</param>
        /// <param name="uniformToFill">if set to <c>true</c> [uniform to fill].</param>
        public static void SaveThumbnail(string originalPath, string thumbnailPath, int width, int height,
                                         bool preserveAspectRatio, bool uniformToFill)
        {
            Contract.Requires(!string.IsNullOrEmpty(originalPath));
            Contract.Requires(!string.IsNullOrEmpty(thumbnailPath));
            var image = Image.FromFile(originalPath);

            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);

            var thumbnail = CreateThumbnail(ms, width, height, preserveAspectRatio, uniformToFill);

            var thumbnailImage = Image.FromStream(thumbnail);

            thumbnailImage.Save(thumbnailPath, ImageFormat.Jpeg);
        }

        public static Stream gGetThumbnail(string originalPath, int width, int height, bool preserveAspectRatio,
                                           bool uniformToFill)
        {
            var image = Image.FromFile(originalPath);

            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);

            var thumbnail = CreateThumbnail(ms, width, height, preserveAspectRatio, uniformToFill);

            return thumbnail;
        }
    }
}