namespace ImageComparator;

using System.Drawing;
using System.Runtime.Versioning;

/// <summary>Bitmap Compare Interface</summary>
[SupportedOSPlatform("windows")]
public interface IBitmapCompare
{
    /// <summary>Gets the similarity between two bitmaps.</summary>
    /// <param name="a">Bitmap A.</param>
    /// <param name="b">Bitmap B.</param>
    /// <returns>A similarity score between 0.0 and 1.0.</returns>
    double GetSimilarity(Bitmap a, Bitmap b);
}
