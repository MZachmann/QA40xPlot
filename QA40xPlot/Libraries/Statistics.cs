using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Libraries
{
    internal class Statistics
    {
        /// <summary>
        /// Calculates the Pearson correlation coefficient between two arrays of doubles.
        /// </summary>
        /// <param name="valuesx">array 1</param>
        /// <param name="valuesy">array 2</param>
        /// <returns></returns>
        public static double Pearson(double[] valuesx, double[] valuesy)
        {
            int n = valuesx.Length;
            double Exy = 0;
            double Ex = 0;
            double Ey = 0;
            double Exsquare = 0;
            double Eysquare = 0;
            int i = 0;
            foreach (double v in valuesx)
            {
                Exy = Exy + (v * valuesy[i]);
                Ex = Ex + v;
                Ey = Ey + valuesy[i];
                Exsquare = Exsquare + Math.Pow(v, 2);
                Eysquare = Eysquare + Math.Pow(valuesy[i], 2);
                i = i + 1;
            }
            double r = ((n * Exy) - (Ex * Ey)) / Math.Sqrt(((n * Exsquare) - Math.Pow(Ex, 2))
                * ((n * Eysquare) - Math.Pow(Ey, 2)));
            return r;
        }
    }
}
