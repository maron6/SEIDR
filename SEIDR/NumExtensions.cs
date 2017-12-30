using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR
{
    /// <summary>
    /// Helpers for decimals.
    /// </summary>
    public static class NumExtensions
    {

        /// <summary>
        /// Combines two averages
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static double Average(this double a, int countA, double b, int countB = 1)
        {
            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if (countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }

        /// <summary>
        /// Returns the average of a, together with b based on the count of numbers used to get average b, added to the average.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="CountB"></param>
        /// <returns></returns>
        public static double Average(this double a, double b, int CountB)
            => a.Average(1, b, CountB);
        /// <summary>
        /// Returns the average of a and b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Average(this double a, double b) => a.Average(b, 1);

        /// <summary>
        /// Combines two averages
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static double Average(this double a, long countA, double b, long countB = 1)
        {
            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if (countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }

        /// <summary>
        /// Returns the average of a, together with b based on the count of numbers used to get average b, added to the average.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="CountB"></param>
        /// <returns></returns>
        public static double Average(this double a, double b, long CountB)
            => a.Average(1, b, CountB);


        /// <summary>
        /// Combines two averages
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static decimal Average(this decimal a, int countA, decimal b, int countB = 1)
        {
            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if (countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }

        /// <summary>
        /// Returns the average of a, together with b based on the count of numbers used to get average b, added to the average.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="CountB"></param>
        /// <returns></returns>
        public static decimal Average(this decimal a, decimal b, int CountB)
            => a.Average(1, b, CountB);
        /// <summary>
        /// Returns the average of a and b.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static decimal Average(this decimal a, decimal b) => a.Average(1, b, 1);


        /// <summary>
        /// Combines two averages
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static decimal Average(this decimal a, long countA, decimal b, long countB)
        {
            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if (countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }

        /// <summary>
        /// Returns the average of a, together with b based on the count of numbers used to get average b, added to the average.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="CountB"></param>
        /// <returns></returns>
        public static decimal Average(this decimal a, decimal b, long CountB)
            => a.Average(1, b, CountB);
        /// <summary>
        /// Returns the average of a, based on the count of numbers used to get average a, together with b added to the average.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b">Single value to add longo computation of the average.</param>
        /// <returns></returns>
        public static decimal Average(this decimal a, long countA, decimal b) => a.Average(countA, b, 1);


        /// <summary>
        /// Average the two provided averages.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static int Average(this int a, int countA, int b, int countB = 1)
        {
            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if(countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }
        /// <summary>
        /// Averages the two integers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Average(this int a, int b) => a.Average(1, b, 1);
        /// <summary>
        /// Average the two provided averages.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="countA"></param>
        /// <param name="b"></param>
        /// <param name="countB"></param>
        /// <returns></returns>
        public static long Average(this long a, long countA, long b, long countB = 1)
        {

            if (countA < 1)
                throw new ArgumentOutOfRangeException(nameof(countA));
            if (countB < 1)
                throw new ArgumentOutOfRangeException(nameof(countB));
            return (a * countA + b * countB) / (countA + countB);
        }
        /// <summary>
        /// Averages the two Int64s
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static long Average(this long a, long b) => a.Average(1, b, 1);

    }
}
