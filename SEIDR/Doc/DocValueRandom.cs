using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Object to help with creating random values for a test/dummy file.
    /// </summary>
    public class DocValueRandom
    {
        Random r;
        /// <summary>
        /// Object to help with creating random values for a test/dummy file.
        /// </summary>
        public DocValueRandom()
        {
            r = new Random();
        }
        /// <summary>
        /// Object to help with creating random values for a test/dummy file.
        /// </summary>
        /// <param name="rand"></param>
        public DocValueRandom(Random rand)
        {
            r = rand;
        }

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
                    if (monthThrough < monthFrom)
                        monthThrough = 1 + ((monthFrom + monthThrough) % 12);
                    month = r.Next(monthFrom, monthThrough + 1);
                    if (DayThrough > MaxDays[month] || DayThrough < 1)
                        DayThrough = MaxDays[month];
                    if (month == monthThrough)
                    {
                        if (month == monthFrom)
                        {
                            if (DayThrough < DayFrom)
                            {
                                DayThrough = DayThrough + DayFrom;
                                if(DayThrough > MaxDays[month])
                                    DayThrough = MaxDays[month];
                            }
                            day = r.Next(DayFrom, DayThrough);
                        }
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
                    if (DayThrough > MaxDays[month] || DayThrough < 1)
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
                if (DayThrough > MaxDays[month] || DayThrough < 1)
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
                if (DayThrough > MaxDays[month] || DayThrough < 1)
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

        public string GetText(int minLength, int maxLength = -1)
        {
            int len = minLength;
            if(maxLength > minLength)
                len = r.Next(minLength, maxLength + 1);
            const string LoremIpsumSource = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec id elit posuere, aliquam neque sit amet, laoreet enim. Nulla vitae erat eget orci scelerisque eleifend. Nulla id erat id mi dictum mollis. Suspendisse pretium ligula vitae mauris bibendum consequat. Morbi mollis congue justo. Cras vel eros nec dui egestas pretium. Suspendisse vulputate rhoncus mi in imperdiet. Praesent dictum ornare purus, imperdiet porttitor nisl tincidunt quis. Morbi maximus, eros vitae scelerisque vestibulum, ipsum lacus lacinia dolor, eu auctor erat lorem id turpis. Integer dapibus porttitor orci vel eleifend. Duis eleifend non lorem eu mattis. Quisque vulputate, massa id facilisis sollicitudin, enim lectus interdum felis, et efficitur risus sapien in metus.
Etiam ut magna mauris. Phasellus mauris dolor, aliquet a ligula nec, aliquet malesuada sem. Donec enim tellus, lobortis sit amet sollicitudin eu, congue ut purus. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Cras et ultricies magna, eget dignissim sapien. Sed cursus sem eget hendrerit bibendum. Vivamus efficitur ante nec eleifend vehicula. Vestibulum luctus pharetra quam, quis consectetur leo. Donec non dolor varius, faucibus enim ultricies, tempor lectus. Maecenas eu orci lobortis, tempus nibh id, mollis lacus. Nunc tincidunt leo ut placerat eleifend. Mauris eros metus, faucibus a nisi nec, cursus lacinia mi.Duis sed nisl sed lectus tristique mollis. Suspendisse scelerisque magna non tincidunt lacinia. Suspendisse potenti. 
Etiam non risus efficitur, vestibulum erat ac, luctus leo. Sed laoreet pellentesque lacus, at blandit nisi consectetur id. Suspendisse in turpis non tellus pellentesque malesuada posuere id ligula. Aenean blandit ex at dui sagittis, sit amet viverra velit sollicitudin. Sed nec sagittis dui. Sed elementum dictum ante, in mollis diam molestie in. In interdum neque a mauris blandit rutrum. Pellentesque suscipit, augue in finibus blandit, mauris mi suscipit dolor, ut molestie odio lorem vitae odio. Ut urna dui, ornare et ex nec, malesuada ornare sapien. Curabitur sed leo id nunc viverra tincidunt. Integer ante risus, tincidunt nec ullamcorper a, pretium at nisl. Curabitur molestie vitae erat nec rhoncus. Aliquam suscipit odio eget odio convallis, in efficitur ante interdum. Aliquam porttitor neque dui, sit amet porttitor justo maximus quis. Suspendisse potenti. Vestibulum vel cursus urna, quis finibus urna. Nunc posuere turpis diam, et pulvinar velit hendrerit ac. Maecenas dignissim commodo sem, ut venenatis felis fermentum id. Donec metus massa, ultrices vel aliquet quis, lobortis quis libero. Fusce molestie velit nibh. Fusce a massa sit amet lacus ultrices commodo quis a metus. Aliquam sagittis consectetur mi, eget pharetra metus efficitur eget. Etiam ullamcorper venenatis purus ornare tincidunt. Curabitur fermentum erat magna, ac consequat ligula aliquet nec. Pellentesque gravida tellus id nulla fermentum, non mattis quam ultrices. Phasellus quis mi ipsum. Aliquam bibendum dolor velit, ut euismod sem suscipit vel. Aliquam erat volutpat. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Ut lobortis suscipit nunc, at interdum eros elementum sed. Maecenas posuere velit nec lacus ultricies imperdiet. Duis ultricies bibendum neque eget pulvinar. Suspendisse ornare pretium urna, eu pharetra lacus auctor ut. 
Maecenas tellus erat, vestibulum et ante ac, molestie fermentum quam.Fusce tincidunt ante ut leo tempus, id consectetur tellus accumsan. Nam malesuada nibh quis urna consectetur, ut scelerisque odio bibendum. Mauris tincidunt odio eu commodo dictum. Sed ac aliquam nunc. Donec gravida finibus tortor, nec mollis ligula rhoncus in. Aenean at lacinia nulla. Nunc ut iaculis mauris. Mauris varius commodo massa, nec pellentesque diam mattis nec. Curabitur a est in dui auctor facilisis ac eget neque. Proin id est facilisis, pellentesque orci vitae, dapibus erat.
Quisque erat neque, rhoncus id tempus vitae, faucibus sit amet erat. Nunc nibh lacus, egestas imperdiet tempus euismod, varius non sapien. Ut et orci fringilla, mattis dolor sed, fermentum orci. Vestibulum vehicula condimentum erat a aliquam. Proin mattis massa nec sodales semper. Nunc libero metus, rutrum ac metus malesuada, accumsan ultricies nunc. Etiam maximus sem vel odio rhoncus bibendum. Quisque a quam massa. In molestie posuere ornare. Aenean efficitur sapien euismod, ornare erat nec, pretium enim. Aliquam erat volutpat.
Nullam ipsum est, pretium in porttitor a, efficitur in tellus. Curabitur a tortor justo. Fusce dignissim diam auctor, fringilla neque id, posuere nisl. Vestibulum aliquam quam mauris, mollis pretium ligula ultrices sed. Donec molestie blandit ipsum eu sagittis. Integer dolor massa, malesuada et ex nec, ullamcorper mollis enim. Nulla est enim, scelerisque eu justo quis, convallis fermentum magna. Morbi id felis ornare, egestas magna ac, pulvinar felis. Duis posuere ante eu consequat fringilla. Nam ut ipsum a nibh condimentum viverra. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Nulla maximus quis dui a venenatis. Vestibulum imperdiet urna vitae consequat dictum. Aenean urna elit, pulvinar ac urna et, faucibus porta magna. Nulla finibus tempus nulla et sollicitudin. Nullam sit amet lacus posuere, egestas lorem eu, efficitur leo. Nullam vestibulum mollis sagittis. Nam efficitur, tortor nec tempor vulputate, erat enim congue justo, nec elementum massa lectus vitae magna. Maecenas nibh sem, porttitor nec neque eget, sagittis iaculis velit. Maecenas sagittis in lorem vitae ullamcorper. Quisque molestie, est in tempus accumsan, neque enim pharetra nunc, ut malesuada nisi velit et ligula. Quisque mattis vehicula dolor vel vulputate.
Maecenas efficitur lorem turpis, non egestas tortor tristique a. Suspendisse potenti. Sed eu lobortis sem. Praesent hendrerit arcu sit amet elit malesuada dictum. Curabitur in nunc ac sapien placerat pellentesque vitae sed libero. Nunc nec consequat neque, eu pretium tellus. Maecenas a ex auctor, semper purus ut, eleifend eros. Cras maximus dignissim dui. Nulla vitae fringilla libero, nec tempor quam. Pellentesque vel urna posuere, varius diam in, porttitor mauris. Nulla tristique libero vel velit finibus ullamcorper sed a velit. Suspendisse vel velit dictum, finibus neque et, eleifend ante.
Vivamus eu pulvinar quam. Sed rutrum fermentum purus at tempus. Aliquam at est turpis. Suspendisse dictum sem at nulla gravida interdum. Donec mattis sit amet nibh in lacinia. Sed nibh nunc, accumsan ut erat id, maximus finibus metus. Sed facilisis quam ut consectetur efficitur. Donec nunc lorem, consectetur ac elementum vitae, consequat sit amet quam. Ut convallis quis lectus ut consectetur. Suspendisse porttitor vulputate leo, at imperdiet enim ullamcorper id. Morbi tincidunt mattis orci. Aenean tincidunt at dolor nec ullamcorper. Phasellus semper nunc ut feugiat egestas. Fusce malesuada at metus a auctor. Quisque dui urna, mattis at lacinia id, porta ac ipsum. Mauris vel aliquam arcu.";
            StringBuilder sb = new StringBuilder(len);
            while(len > LoremIpsumSource.Length)
            {
                int st = r.Next(0, LoremIpsumSource.Length);
                sb.Append(LoremIpsumSource.Substring(st));
                len -= LoremIpsumSource.Length - st;
            }
            int start = r.Next(0, LoremIpsumSource.Length - len);  //Random starting position          
            sb.Append(LoremIpsumSource.Substring(start, len));
            return sb.ToString();
        }
        /// <summary>
        /// Gets a set of text from lorem ipsum
        /// </summary>
        /// <param name="percentNull"></param>
        /// <param name="minLength"></param>
        /// <param name="maxLength">Maximum length. If less than minLength, will treat max as equal to min.</param>
        /// <returns></returns>
        public string GetText(double percentNull, int minLength, int maxLength = -1)
        {
            if (PercentCheck(percentNull))
                return null;
            return GetText(minLength, maxLength);


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
