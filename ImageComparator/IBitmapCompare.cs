using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageComparator
{
    using System.Drawing;

    /// <summary>
    /// Bitmap Compare Interface
    /// </summary>
    public interface IBitmapCompare
    {
        /// <summary>
        /// Gets the similarity.
        /// </summary>
        /// <param name="a">bitmap A.</param>
        /// <param name="b">bitmap B.</param>
        /// <returns></returns>
        double GetSimilarity(Bitmap a, Bitmap b);
    }
}
