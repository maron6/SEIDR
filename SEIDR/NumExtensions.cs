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
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this int value, int a, int b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {
            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this double value, double a, double b, bool inclusiveLeft = true, bool inclusiveRight =true)
        {

            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this Single value, Single a, Single b, bool inclusiveLeft= true, bool inclusiveRight = true)
        {
            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this long value, long a, long b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {

            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this short value, short a, short b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {

            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this decimal value, decimal a, decimal b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {

            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this byte value, byte a, byte b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {

            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value < a)
                return false;
            if (value > b)
                return false;
            return true;
        }
        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between(this byte value, int a, int b, bool inclusiveLeft = true, bool inclusiveRight = true)
        {
            if (value < a)
                return false;
            if (!inclusiveLeft && a == value)
                return false;
            if (!inclusiveRight && b == value)
                return false;
            if (value > b)
                return false;            
            return true;
        }

        /// <summary>
        /// Checks that <paramref name="value"/> is between <paramref name="a"/> and <paramref name="b"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="inclusiveLeft">If true, can return true when <paramref name="a"/> == <paramref name="value"/></param>
        /// <param name="inclusiveRight">If true, can return true when <paramref name="b"/> == <paramref name="value"/></param>
        /// <returns></returns>
        public static bool Between<ET, N>(this ET value, N a, N b, bool inclusiveLeft = true, bool inclusiveRight = true) 
        {
            Type t = typeof(ET);
            if (!t.IsEnum)
                throw new ArgumentException("Not an enum", nameof(value));
            Type n = typeof(N);
            if (n.IsEnum)
            {
                n = n.GetEnumUnderlyingType();
            }
            
            if(n == typeof(int))
            {
                return Convert.ToInt32(value).Between(Convert.ToInt32(a), Convert.ToInt32(b), inclusiveLeft, inclusiveRight);
            }
            if (n == typeof(long))
            {
                return Convert.ToInt64(value).Between(Convert.ToInt64(a), Convert.ToInt64(b), inclusiveLeft, inclusiveRight);
            }
            if (n == typeof(double))
            {
                return Convert.ToDouble(value).Between(Convert.ToDouble(a), Convert.ToDouble(b), inclusiveLeft, inclusiveRight);
            }
            if (n == typeof(short))
            {
                return Convert.ToInt16(value).Between(Convert.ToInt16(a), Convert.ToInt16(b), inclusiveLeft, inclusiveRight);
            }

            if (n == typeof(byte))
            {
                return Convert.ToInt32(value).Between(Convert.ToInt32(a), Convert.ToInt32(b), inclusiveLeft, inclusiveRight);
            }

            if (n == typeof(Single) || n == typeof(float)) //should be the same..
            {
                return Convert.ToSingle(value).Between(Convert.ToSingle(a), Convert.ToSingle(b), inclusiveLeft, inclusiveRight);
            }

            if (n == typeof(decimal))
            {
                return Convert.ToDecimal(value).Between(Convert.ToDecimal(a), Convert.ToDecimal(b), inclusiveLeft, inclusiveRight);
            }

            throw new ArgumentException("Invalid comparison type", nameof(N));            
        }


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
