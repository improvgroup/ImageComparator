namespace ImageComparator
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    /// <summary>
    /// Bitmap Compare
    /// </summary>
    public class BitmapCompare : IBitmapCompare
    {
        #region IBitmapCompare Members

        /// <summary>
        /// Gets the similarity.
        /// </summary>
        /// <param name="a">bitmap A.</param>
        /// <param name="b">bitmap B.</param>
        /// <returns></returns>
        public double GetSimilarity(Bitmap a, Bitmap b)
        {
            var dataA = ProcessBitmap(a);
            var dataB = ProcessBitmap(b);
            double result = 0;
            int averageA;
            int averageB;

            var maxA = ((a.Width * 3) * a.Height);
            var maxB = ((b.Width * 3) * b.Height);

            switch (dataA.GetLargest()) //Find dominant color to compare
            {
                case 1:
                    {
                        averageA = Math.Abs(dataA.R / maxA);
                        averageB = Math.Abs(dataB.R / maxB);
                        result = (averageA - averageB) / 2;
                        break;
                    }
                case 2:
                    {
                        averageA = Math.Abs(dataA.G / maxA);
                        averageB = Math.Abs(dataB.G / maxB);
                        result = (averageA - averageB) / 2;
                        break;
                    }
                case 3:
                    {
                        averageA = Math.Abs(dataA.B / maxA);
                        averageB = Math.Abs(dataB.B / maxB);
                        result = (averageA - averageB) / 2;
                        break;
                    }
            }

            result = Math.Abs((result + 100) / 100);

            if (result > 1.0)
            {
                result -= 1.0;
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Processes the bitmap.
        /// </summary>
        /// <param name="a">A.</param>
        /// <returns></returns>
        private static RGBData ProcessBitmap(Bitmap a)
        {
            var bmpData = a.LockBits(new Rectangle(0, 0, a.Width, a.Height), ImageLockMode.ReadOnly,
                                     PixelFormat.Format24bppRgb);
            var ptr = bmpData.Scan0;
            var data = new RGBData();

            unsafe
            {
                var p = (byte*)(void*)ptr;
                var offset = bmpData.Stride - a.Width * 3;
                var width = a.Width * 3;

                for (var y = 0; y < a.Height; ++y)
                {
                    for (var x = 0; x < width; ++x)
                    {
                        data.R += p[0]; //gets red values
                        data.G += p[1]; //gets green values
                        data.B += p[2]; //gets blue values
                        ++p;
                    }
                    p += offset;
                }
            }
            a.UnlockBits(bmpData);
            return data;
        }

        #region Nested type: RGBData

        /// <summary>
        /// Red/Green/Blue Data
        /// </summary>
        public struct RGBData
        {
            /// <summary>
            /// Blue
            /// </summary>
            public int B { get; set; }

            /// <summary>
            /// Green
            /// </summary>
            public int G { get; set; }

            /// <summary>
            /// Red
            /// </summary>
            public int R { get; set; }

            /// <summary>
            /// Gets the largest.
            /// </summary>
            /// <returns></returns>
            public int GetLargest()
            {
                return this.R > this.B ? (this.R > this.G ? 1 : 2) : 3;
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
            /// <returns>
            /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
            /// </returns>
            public override bool Equals(object obj)
            {
                if (obj is RGBData)
                {
                    var other = (RGBData)obj;
                    return this.R == other.R && this.G == other.G && this.B == other.B;
                }

                return false;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        #endregion
    }
}