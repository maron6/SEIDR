using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public class DocValueRandom
    {
        Random r = new Random();

        /// <summary>
        /// Uses <see cref="Random"/> to provide a bool value of true <paramref name="percent"/>% of the time, and false the rest.
        /// </summary>
        /// <param name="percent">Integer value between 0 and 100 (inclusive). 0 will always be false, and 100 will always be true.</param>
        /// <returns></returns>
        public bool PercentCheck(int percent)
        {
            if (percent.Between(0, 100))
                return r.Next(0, 100) < percent;
            throw new ArgumentOutOfRangeException(nameof(percent));
        }
        /// <summary>
        /// Uses <see cref="Random"/> to provide a bool value of true <paramref name="percent"/>% of the time, and false the rest.
        /// </summary>
        /// <param name="percent">Decimal value between 0 and 100 (inclusive). 0 will always be false, and 100 will always be true.</param>
        /// <returns></returns>
        public bool PercentCheck(double percent)
        {
            if (percent < 0 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent));
            int limit = 100;
            while(percent != (int)percent)
            {
                limit *= 10;
                percent *= 10;
            }
            return r.Next(0, limit) < percent;
        }
        /// <summary>
        /// Gets a random DateTime? object, which will be null <paramref name="PercentNull"/>% of the time.
        /// </summary>
        /// <param name="PercentNull"></param>
        /// <param name="yearFrom"></param>
        /// <param name="monthFrom"></param>
        /// <param name="DayFrom"></param>
        /// <param name="yearThrough"></param>
        /// <param name="monthThrough"></param>
        /// <param name="DayThrough"></param>
        /// <returns></returns>
        public DateTime? GetDateTimeNullable(int PercentNull, int yearFrom, int monthFrom = 1, int DayFrom = 1, int yearThrough = 1, int monthThrough = 12, int DayThrough = 31)
        {
            if (PercentCheck(PercentNull))
                return null;
            return GetDateTime(yearFrom, monthFrom, DayFrom, yearThrough, monthThrough, DayThrough);
        }
        /// <summary>
        /// Gets a random date in the specified range.
        /// <para> If the yearThrough is smaller than yearFrom, will be treated as X years after yearFrom instead.</para>
        /// </summary>
        /// <param name="yearFrom"></param>
        /// <param name="monthFrom">Only used when the out year is the same as the <paramref name="yearFrom"/></param>
        /// <param name="DayFrom"></param>
        /// <param name="yearThrough"></param>
        /// <param name="monthThrough">Only used when out year is the same as the <paramref name="yearThrough"/></param>
        /// <param name="DayThrough"></param>
        /// <returns></returns>
        public DateTime GetDateTime(int yearFrom, int monthFrom = 1, int DayFrom = 1, int yearThrough = 1, int monthThrough = 12, int DayThrough = 31)
        {
            var MaxDays = new int[] { 0, 31, 28 /* Ignore leap years*/, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            if (yearThrough < yearFrom)
                yearThrough = yearFrom + yearThrough;
            int year = r.Next(yearFrom, yearThrough);
            if (!monthFrom.Between(1, 12))
                throw new ArgumentOutOfRangeException(nameof(monthFrom));
            if (!monthFrom.Between(1, 12))
                throw new ArgumentOutOfRangeException(nameof(monthThrough));
            int month, day;
            if(year == yearThrough)
            {
                if (year == yearFrom)
                {
                    month = r.Next(monthFrom, monthThrough + 1);
                    if (DayThrough > MaxDays[month])
                        DayThrough = MaxDays[month];
                    if (month == monthThrough)
                    {
                        if (month == monthFrom)
                            day = r.Next(DayFrom, DayThrough);
                        else
                            day = r.Next(1, DayThrough);
                    }
                    else if(month == monthFrom)
                    {
                        day = r.Next(DayFrom, MaxDays[month]);
                    }
                    else
                    {
                        day = r.Next(1, MaxDays[month]);
                    }
                }
                else
                {
                    month = r.Next(1, monthThrough + 1);
                    if (DayThrough > MaxDays[month])
                        DayThrough = MaxDays[month];
                    if (month == monthThrough)
                    {
                        day = r.Next(1, DayThrough);
                    }
                    else
                    {
                        day = r.Next(1, MaxDays[month]);
                    }
                }
            }
            else if(year == yearFrom)
            {
                month = r.Next(monthFrom, monthThrough + 1);
                if (DayThrough > MaxDays[month])
                    DayThrough = MaxDays[month];                
                if (month == monthFrom)
                {
                    day = r.Next(DayFrom, MaxDays[month]);
                }
                else
                {
                    day = r.Next(1, MaxDays[month]);
                }
            }
            else
            {
                month = r.Next(1, monthThrough + 1);
                if (DayThrough > MaxDays[month])
                    DayThrough = MaxDays[month];
                if (month == monthThrough)
                {
                    day = r.Next(1, DayThrough);
                }
                else
                {
                    day = r.Next(1, MaxDays[month]);
                }
            }
            if (month > 12)
                month = 1 + (month % 12);
            if (day > MaxDays[month])
                day = 1 + (day % MaxDays[month]);

            return new DateTime(year, month, day);


        }

        public decimal? GetMoney(int percentNull, int dollarMin, int dollarMax)
        {
            if (PercentCheck(percentNull))
                return null;
            return GetMoney(dollarMin, dollarMax);
        }
        /// <summary>
        /// Gets a random money amount (decimal rounded to 2)
        /// </summary>
        /// <param name="dollarMin"></param>
        /// <param name="DollarMax"></param>
        /// <returns></returns>
        public decimal GetMoney(int dollarMin, int DollarMax)
        {
            return GetDecimal(dollarMin, DollarMax, 2);
        }
        public decimal? GetDecimal(int percentNull, decimal min, decimal max, int round = 8)
        {
            if (PercentCheck(percentNull))
                return null;
            return GetDecimal(min, max, round);
        }
        /// <summary>
        /// Returns a random decimal between min and max. If max is less than min, will treat max as X values after min. (E.g. if max is 50 and min is 100, then treat as 100 through 150)
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public decimal GetDecimal(decimal min, decimal max, int round = 8)
        {
            if (max < min)
                max += min;
            decimal x = r.Next((int)min, (int)max);
            if (x > min || x < (max - 1) ) 
                x += (decimal)r.NextDouble();
            return decimal.Round(x, round);
        }
        /// <summary>
        /// Gets a random string from the provided list, or null <paramref name="percentNull"/>% of the time.
        /// </summary>
        /// <param name="percentNull"></param>
        /// <param name="possibleValueList"></param>
        /// <returns></returns>
        public string GetString(int percentNull, params string[] possibleValueList)
        {
            if (PercentCheck(percentNull) || possibleValueList.Length == 0)
                return null;
            return possibleValueList[r.Next(0, possibleValueList.Length)];
        }
        /// <summary>
        /// Gets a random string based on regex derived rules. <para>E.g. '@[1-2]{1,2}' can return one of the following values: 
        /// @1, @2, @11, @12, @21, @22</para>
        /// <para>'@[1-24]{2}' could return one of the following: @11, @12, @14, @21, @22, @24, @41, @42, @44</para>
        /// <para>'@[1-2]{2}-1 could return one of the following: @11-1, @12-1, @21-1, @22-1</para>
        /// <para>'@[1-2]\{2} COuld return one of the following: @1{2}, @2{2}</para>
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="percentNull"></param>
        /// <returns></returns>
        public string GetString(string pattern, int percentNull = 0)
        {
            if (PercentCheck(percentNull))
                return null;            
            string temp = "", parse = "";
            int from = -1, through;
            bool inSet = false;
            bool wasSet = false;
            bool inQuant = false;
            StringBuilder sb = new StringBuilder();
            for(int i= 0; i < pattern.Length; i++)
            {
                if (inSet)
                {
                    if(pattern[i] == ']')
                    {
                        inSet = false;
                        wasSet = true;
                        continue;
                    }
                    else if(pattern[i] == '\\')
                    {                        
                        temp += pattern[++i]; //Skip to following.
                    }
                    else if(pattern[i] == '-')
                    {
                        int fromRange = temp[temp.Length - 1];
                        int throughRange = pattern[++i];
                        for(int j = fromRange + 1; j <= throughRange; j++)
                        {
                            temp += (char)j;
                        }
                    }
                    else
                        temp += pattern[i];
                    continue;
                }                
                if (pattern[i] == '[')
                {
                    if (inQuant)
                        throw new ArgumentException("UnExpected '[' in pattern at position " + i + ".");

                    if (temp != "")
                    {
                        if (wasSet)
                        {
                            sb.Append(temp[r.Next(0, temp.Length)]);
                            wasSet = false;
                        }
                        else
                        {
                            sb.Append(temp);
                        }
                        temp = "";
                    }
                    inSet = true;
                    continue;
                }
                if (inQuant)
                {
                    if(pattern[i] == ',')
                    {
                        if (parse == "" && from == -1)
                            throw new ArgumentException("Unexpected ',' in pattern at position " + i + ".");
                        from = int.Parse(parse);
                        parse = "";
                        continue;
                    }
                    if(pattern[i] == '}')
                    {
                        if(parse != "")
                        {
                            if (from >= 0)
                                through = int.Parse(parse);
                            else
                            {
                                if (from < 0)
                                    from = int.Parse(parse);
                                through = from;
                            }
                            if (through < from)
                                through += from;
                            int x = r.Next(from, through);
                            for(int count = 0; count < x; count++)
                            {
                                sb.Append(temp[r.Next(0, temp.Length)]);
                            }
                            from = through = -1;
                            temp = parse = "";                            
                            inQuant = false;
                            continue;
                        }
                    }
                    parse += pattern[i];
                    continue;
                }
                if(pattern[i] == '\\')
                {
                    if(temp != "")
                    {
                        if (wasSet)
                        {
                            sb.Append(temp[r.Next(0, temp.Length)]);
                            wasSet = false;
                        }
                        else
                        {
                            sb.Append(temp);
                        }
                    }
                    temp = pattern[++i].ToString();
                    continue;
                }
                if(pattern[i] == '{')
                {
                    inQuant = true;
                    continue;
                }
                if(pattern[i] == '[')
                {
                    inSet = true;
                    continue;
                }
                if (temp != "")
                {
                    if (wasSet)
                    {
                        sb.Append(temp[r.Next(0, temp.Length)]);
                        wasSet = false;
                    }
                    else
                    {
                        sb.Append(temp);
                    }
                }
                temp = pattern[i].ToString();
            }
            if (temp != "")
            {
                if (wasSet)                
                    sb.Append(temp[r.Next(0, temp.Length)]);                 
                else                
                    sb.Append(temp);                
            }
            return sb.ToString();
        }
    }
}
