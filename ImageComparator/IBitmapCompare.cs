// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBitmapCompare.cs" company="">
//   
// </copyright>
// <summary>
//   Bitmap Compare Interface
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ImageComparator
{
    using System.Drawing;

    /// <summary>
    ///     Bitmap Compare Interface
    /// </summary>
    public interface IBitmapCompare
    {
        /// <summary>
        /// Gets the similarity.
        /// </summary>
        /// <param name="a">
        /// bitmap A.
        /// </param>
        /// <param name="b">
        /// bitmap B.
        /// </param>
        /// <returns>
        /// The similarity.
        /// </returns>
        double GetSimilarity(Bitmap a, Bitmap b);
    }
}