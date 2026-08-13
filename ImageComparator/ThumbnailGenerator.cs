namespace ImageComparator;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>Generates and saves image thumbnails.</summary>
[SupportedOSPlatform("windows")]
public static class ThumbnailGenerator
{
    /// <summary>Creates a thumbnail stream from an input image stream.</summary>
    /// <param name="input">The source image stream.</param>
    /// <param name="width">Target width (0 = source width).</param>
    /// <param name="height">Target height (0 = source height).</param>
    /// <param name="preserveAspectRatio">Whether to preserve the source aspect ratio.</param>
    /// <param name="uniformToFill">Whether to crop to fill the target dimensions.</param>
    /// <param name="quality">JPEG/encoder quality (0–100).</param>
    /// <param name="format">Output image format (defaults to PNG).</param>
    /// <returns>A <see cref="Stream"/> containing the encoded thumbnail.</returns>
    public static Stream CreateThumbnail(
        Stream input,
        int width = 0,
        int height = 0,
        bool preserveAspectRatio = true,
        bool uniformToFill = false,
        long quality = 100L,
        ImageFormat? format = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        var output = new MemoryStream();
        var encoder = GetEncoder(format ?? ImageFormat.Png);
        using var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

        using var source = Image.FromStream(input);
        int sourceWidthBeforeResize = source.Width;
        int sourceHeightBeforeResize = source.Height;

        if (width == 0) width = sourceWidthBeforeResize;
        if (height == 0) height = sourceHeightBeforeResize;

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

        using var thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        thumbnail.SetResolution(source.HorizontalResolution, source.VerticalResolution);

        using var graphic = Graphics.FromImage(thumbnail);
        graphic.Clear(Color.Transparent);
        graphic.CompositingMode = CompositingMode.SourceCopy;
        graphic.CompositingQuality = CompositingQuality.HighQuality;
        graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphic.SmoothingMode = SmoothingMode.HighQuality;
        graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var destination = new Rectangle(0, 0, width, height);

        using var imageAttributes = new ImageAttributes();
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

        thumbnail.Save(output, encoder, encoderParameters);
        output.Seek(0, SeekOrigin.Begin);
        return output;
    }

    /// <summary>Saves a thumbnail of an image file to disk.</summary>
    /// <param name="originalPath">Path to the source image.</param>
    /// <param name="thumbnailPath">Path where the thumbnail will be written.</param>
    /// <param name="width">Target width (0 = source width).</param>
    /// <param name="height">Target height (0 = source height).</param>
    /// <param name="preserveAspectRatio">Whether to preserve the source aspect ratio.</param>
    /// <param name="uniformToFill">Whether to crop to fill the target dimensions.</param>
    /// <param name="quality">Encoder quality (0–100).</param>
    /// <param name="format">Output image format (defaults to PNG).</param>
    public static void SaveThumbnail(
        string originalPath,
        string thumbnailPath,
        int width = 0,
        int height = 0,
        bool preserveAspectRatio = true,
        bool uniformToFill = false,
        long quality = 100L,
        ImageFormat? format = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath, nameof(originalPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbnailPath, nameof(thumbnailPath));

        using var fileStream = File.OpenRead(originalPath);
        using var thumbnailStream = CreateThumbnail(fileStream, width, height, preserveAspectRatio, uniformToFill, quality, format);
        using var output = File.OpenWrite(thumbnailPath);
        thumbnailStream.CopyTo(output);
    }

    /// <summary>Returns a thumbnail stream generated from an image file.</summary>
    /// <param name="originalPath">Path to the source image file.</param>
    /// <param name="width">Target width (0 = source width).</param>
    /// <param name="height">Target height (0 = source height).</param>
    /// <param name="preserveAspectRatio">Whether to preserve the source aspect ratio.</param>
    /// <param name="uniformToFill">Whether to crop to fill the target dimensions.</param>
    /// <param name="quality">Encoder quality (0–100).</param>
    /// <param name="format">Output image format (defaults to PNG).</param>
    /// <returns>A <see cref="Stream"/> containing the encoded thumbnail.</returns>
    public static Stream GetThumbnailFromFile(
        string originalPath,
        int width = 0,
        int height = 0,
        bool preserveAspectRatio = true,
        bool uniformToFill = false,
        long quality = 100L,
        ImageFormat? format = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalPath, nameof(originalPath));

        try
        {
            using var fileStream = File.OpenRead(originalPath);
            return CreateThumbnail(fileStream, width, height, preserveAspectRatio, uniformToFill, quality, format);
        }
        catch (OutOfMemoryException ex)
        {
            Trace.TraceError(ex.Message);
            throw;
        }
        catch (FileNotFoundException ex)
        {
            Trace.TraceError(ex.Message);
            throw;
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid)
            ?? throw new InvalidOperationException($"No encoder found for image format {format}.");
    }
}
