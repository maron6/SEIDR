namespace SEIDR
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.ComponentModel;

    public static class BaseExtensions
    {
        #region enum extend
        /// <summary>
        /// Gets the description of the specified MemberInfo, the name of the MemberInfo if no Description attribute has been specified.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string GetDescription(this MemberInfo val)
        {
            var atts = val.GetCustomAttributes(typeof(DescriptionAttribute), true);
            if (atts != null && atts.Length >= 1)
            {
                var att = atts[0] as DescriptionAttribute;
                return att?.Description ?? val.Name;
            }
            return val.Name;
        }
        /// <summary>
        /// Tries to get the enum's description, or the name of the value if no description specified
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            Type t = value.GetType();
            var mem = t.GetMember(value.ToString());
            if (mem != null && mem.Length == 1)
            {
                return mem[0].GetDescription();
            }
            return value.ToString();
        }
        #endregion

        public static int GetDateSerial(this DateTime value)
        {
            return Convert.ToInt32(value.ToOADate());
        }
        public static DateTime GetDateFromSerial(this int value)
        {
            return DateTime.FromOADate(value);
        }
        public static T GetLoopedIndex<T>(this IList<T> source, int index)
        {
            if (source.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(source), "Source does not haver any indexes to loop.");
            return source[index % source.Count];
        }
        /// <summary>
        /// Map properties of map to the inheriting class instance, IT
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <typeparam name="IT">Inheriting type</typeparam>
        /// <param name="inheritor">Object to map properties onto. If null and <paramref name="map"/> is not null, a new object will  be created and returned.</param>
        /// <param name="map">Object to map properties from. If null, <paramref name="inheritor"/> will be returned as-is.</param>
        /// <param name="cache">Cache the property info if the mapping for these class types are going to be done often.</param>
        /// <returns>REturns the inheritor for method chaining</returns>
        public static IT MapInheritance<RT, IT>(this IT inheritor, RT map, bool cache = true) where IT: RT, new()
        {
            if (map == null)
                return inheritor;
            if (inheritor == null)
                inheritor = new IT();
            Type iInfo = typeof(IT);
            Type rInfo = typeof(RT);
            List < PropertyInfo > md;
            Dictionary<string, PropertyInfo> td;
            lock (((System.Collections.ICollection)mapCache).SyncRoot)
            {
                if (!cache || !mapCache.TryGetValue(rInfo, out md))
                {
                    md = rInfo.GetProperties().Where(p => p.CanRead).ToList(); //Cache this in a limited dictionary of <Type, Dictionary<string, PropertyInfo>> ?
                    if (cache)
                        mapCache[rInfo] = md; //don't worry about cached values becoming innacurate, since the TypeInfo isn't going to change after compilation.
                                              //Even dynamic class matching is based on definition matching
                }
            }
            lock (((System.Collections.ICollection)mapWriteCache).SyncRoot)
            {
                if (!cache || !mapWriteCache.TryGetValue(iInfo, out td))
                {
                    td = iInfo.GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name, p => p); //Cache this in a limited dictionary of <Type, Dictionary<string, PropertyInfo>> ?
                    if (cache)
                        mapWriteCache[iInfo] = td;
                }
            }
            Map(inheritor, map, td,  md);
            return inheritor;
        }
        /// <summary>
        /// Maps properties from the map to target. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="map"></param>
        /// <param name="propertiesToWrite"></param>
        /// <param name="propertiesToMap"></param>
        public static void Map(this object target, object map, 
            Dictionary<string, PropertyInfo> propertiesToWrite, List<PropertyInfo> propertiesToMap)
        {
            foreach (var mapping in propertiesToMap)
            {
                PropertyInfo p;
                if (propertiesToWrite.TryGetValue(mapping.Name, out p))
                {
                    object nValue = mapping.GetValue(map);
                    if (nValue == null || nValue == DBNull.Value)
                    {
                        if(!p.PropertyType.IsClass)
                            continue; //Don't try to set a struct value to null.                    
                    }
                    else
                    {
                        Type underType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        if (underType.IsEnum)
                        {
                            nValue = Enum.Parse(underType, nValue.ToString(), true);
                        }
                        else if (underType.IsArray) //ToDo.. map array properties
                            continue; 
                        /*else
                        {
                            nValue = p.GetValue(map);
                        }*/
                    }
                    p.SetValue(target, nValue);
                }
            }
        }
        static Dictionary<Type, List<PropertyInfo>> mapCache = new Dictionary<Type, List<PropertyInfo>>();
        static Dictionary<Type, Dictionary<string, PropertyInfo>> mapWriteCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        #region string Extensions
        /// <summary>
        /// For any string properties on the object, set them to null if they're white space or empty
        /// </summary>
        /// <param name="j"></param>
        public static void NullifyStringProperties(this object j)
        {

            var props = j.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(string) && prop.CanWrite && prop.CanRead)
                {
                    string x = prop.GetValue(j) as string;
                    if (x == null)
                        continue;
                    if (x.Trim() == "")
                        prop.SetValue(j, null);
                }
            }
        }
        /// <summary>
        /// For any readable and writable string properties on the object, check if they're null. If so, set them to be an empty string instead
        /// </summary>
        /// <param name="j"></param>
        public static void DeNullifyStrings(this object j)
        {
            var props = j.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(string) && prop.CanWrite && prop.CanRead)
                {
                    string x = prop.GetValue(j) as string;
                    if (x == null)
                        prop.SetValue(j, string.Empty);
                }
            }
        }
        /// <summary>
        /// Returns the length of the string, or -1 if null.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public static int nLength(this string check)
        {
            if (check == null)
                return -1;
            return check.Length;
        }
        /// <summary>
        /// Splits the string on the split string instead of
        /// </summary>
        /// <param name="check"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static List<string> SplitOnString(this string check, string split)
        {
            List<string> ret = new List<string>();
            if (string.IsNullOrEmpty(check))
                return ret;
            return check.Split(new[] { split }, StringSplitOptions.None).ToList();
        }
        /// <summary>
        /// Compares each sequence of list left with the corresponding entry in right using <see cref="object.Equals(object)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool Matches<T>(this IList<T> left, IList<T> right)
        {
            if (left.Count != right.Count)
                return false;
            for(int i = 0; i< left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Checks that every item of the sublist is contained in the main list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="subList"></param>
        /// <returns></returns>
        public static bool IsSuperSet<T>(this IEnumerable<T> list, IEnumerable<T> subList)
        {
            foreach(var s in subList)
            {
                if (!list.Contains(s))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Checks if the list is a super set of sublist
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subList"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsSubset<T>(this IEnumerable<T> subList, IEnumerable<T> list)
            => list.IsSuperSet(subList);
        /// <summary>
        /// Checks if every item in list is contained by list2 and if every item contained by list 2 is contained by list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool IsSetEquivalent<T>(this IEnumerable<T> list, IEnumerable<T> list2)
        {
            if (!list.IsSuperSet(list2))
                return false;
            return list2.IsSuperSet(list);
        }
        public static List<string> SplitOnString(this string check, int SplitLength)
        {
            List<string> ret = new List<string>();
            if (string.IsNullOrEmpty(check))
                return ret;
            int Position = 0;
            while(Position < check.Length)
            {
                int tl = SplitLength;
                if (Position + tl > check.Length)
                    tl = check.Length - Position;
                ret.Add(check.Substring(Position, tl));
                Position += tl;
            }
            return ret;
        }
        /// <summary>
        /// Unions the enumerables and returns a List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="distinct">If true, will only add items from right to left if they are not already contained in left</param>
        /// <returns></returns>
        public static List<T> Union<T>(this IEnumerable<T> left, IEnumerable<T> right, bool distinct)
        {
            List<T> rl = new List<T>(left);
            if(!distinct)
            {
                rl.AddRange(right);
                return rl;
            }
            foreach(var r in right)
            {
                if (!left.Contains(r))
                    rl.Add(r);
            }
            return rl;
        }
        /// <summary>
        /// Inserts a value at the specified index by calling <see cref="List{T}.Insert(int, T)"/>. If the list does not have enough records, it will have the missing indexes filled by <paramref name="filler"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toFill"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="filler"></param>
        public static void InsertWithExpansion<T>(this List<T> toFill, int index, T value, T filler = default(T))
        {
            if (index > toFill.Count)
            {
                if (filler == null || filler.Equals(default(T)))
                {

                    toFill.AddRange(new T[index - toFill.Count]);
                }
                else
                {
                    while (index > toFill.Count)
		            {
		                toFill.Add(filler);
		            }
                }
            }
            toFill.Insert(index, value);
        }
        /// <summary>
        /// Sets the value at the specified index by calling the indexer. If the list does not have enough records, it will have the missing indexes filled by <paramref name="filler"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toFill"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="filler"></param>
        public static void SetWithExpansion<T>(this List<T> toFill, int index, T value, T filler = default(T))
        {
            if (index >= toFill.Count)
            { 
                if (filler == null || filler.Equals(default(T)))
                {
                
                        toFill.AddRange(new T[index + 1 - toFill.Count]);
                }
                else
                {
		            while (index >= toFill.Count)
		            {
		                toFill.Add(filler);
		            }
                }
            }
            toFill[index] = value;
        }
 
        /// <summary>
        /// Returns the length of the trimmed string, or -1 if null
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public static int ntLength(this string check)
        {
            if (check == null)
                return -1;
            return check.Trim().Length;
        }
        /// <summary>
        /// Null safe version of Trim.
        /// <para>
        /// Returns an empty string if null.
        /// </para>
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public static string nTrim(this string check)
        {
            if (check == null)
                return "";
            return check.Trim();
        }
        /// <summary>
        /// Null safe version of Trim.
        /// <para>
        /// If nullify is true, returns null instead of an empty string.
        /// </para>
        /// Else acts the same as nTrim with no boolean
        /// </summary>
        /// <param name="check"></param>
        /// <param name="nullify"></param>
        /// <returns></returns>
        public static string nTrim(this string check, bool nullify)
        {
            if (!nullify)
                return check.nTrim();
            if (check.ntLength() <= 0)
                return null;
            return check.Trim();
        }
        /// <summary>
        /// Returns null if the trimmed string is empty.
        /// <para>
        /// Otherwise, returns the string
        /// </para>
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public static string nString(this string check)
        {
            if (check.ntLength() < 1)
                return null;
            return check;
        }
        /// <summary>
        /// Check if string a is like string b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="escapeRegularExpressions">If set to false, will allow making use of regular expressions included in string a or b for doing the comparison..</param>
        /// <returns></returns>
        public static bool Like(this string a, string b, bool escapeRegularExpressions = true)
        {
            LikeExpressions le = new LikeExpressions();
            le.AllowRegex = !escapeRegularExpressions;
            return le.Compare(a, b);
        }
        /// <summary>
        /// Returns a new IEnumerable including the new record(s) via a union
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="toInclude"></param>
        /// <returns></returns>
        public static IEnumerable<T> Include<T>(this IEnumerable<T> source, params T[] toInclude) => source.Union(toInclude);
        
        /*
        /// <summary>
        /// Gets the first substring of the provided string that matches the 'LIKE'
        /// </summary>
        /// <param name="a"></param>
        /// <param name="LIKE"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        public static string LikeMatch(this string a, string LIKE, bool caseSensitive = false)
        {
            LikeExpressions le = new LikeExpressions();
            if (caseSensitive)
                return le.SearchStringWithCase(a, LIKE);
            return le.SearchString(a, LIKE);
        }*/
        /// <summary>
        /// Gets the first match from the list that matches the LIKE expression
        /// </summary>
        /// <param name="list"></param>
        /// <param name="LIKE"></param>
        /// <param name="EscapeRegex"></param>
        /// <returns></returns>
        public static string FirstMatch(this IEnumerable<string> list, string LIKE, bool EscapeRegex = true)
        {
            LikeExpressions le = new LikeExpressions();
            le.AllowRegex = !EscapeRegex;            
            return le.Compare(list, LIKE);            
        }
        /// <summary>
        /// Returns the last match from the list that matches the LIKE expression
        /// </summary>
        /// <param name="list"></param>
        /// <param name="LIKE"></param>
        /// <param name="escapeRegularExpressions"></param>
        /// <returns></returns>
        public static string LastMatch(this IEnumerable<string> list, string LIKE, bool escapeRegularExpressions = true)
        {
            LikeExpressions le = new LikeExpressions();
            le.AllowRegex = !escapeRegularExpressions;            ;
            return le.ReverseCompare(list, LIKE);            
        }
        /// <summary>
        /// Alias for <see cref="Like(IEnumerable{string}, string, bool)"/>
        /// </summary>
        /// <param name="list"></param>
        /// <param name="LIKE"></param>
        /// <param name="escapeRegularExpressions"></param>
        /// <returns></returns>
        public static IEnumerable<string> AllMatches(this IEnumerable<string> list, string LIKE, bool escapeRegularExpressions = true) => list.Like(LIKE, escapeRegularExpressions);        
        #endregion
        ///<summary>
        ///Returns an IEnumerable containing only the strings that are 'Like' b
        /// </summary>              
        public static IEnumerable<string> Like(this IEnumerable<string> aList, string b, bool escapeRegularExpressions = true)
        {
            LikeExpressions le = new LikeExpressions();
            le.AllowRegex = !escapeRegularExpressions;
            return le.GetMatches(aList, b);
            //return aList.Where(a => a.Like(b, escapeRegularExpressions));
        }
        /// <summary>
        /// Takes a subset of the ordered list and returns a new IList containing the values that match the selector
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IList<T> TakeSubset<T>(this IList<T> list, Predicate<T> selector)
        {
            List<T> ret = new List<T>();
            for(int i = 0; i < list.Count; i++)
            {
                if (selector(list[i]))
                    ret.Add(list[i]);
            }
            return ret;
        }
        /// <summary>
        /// Returns the minimal value of the two parameters.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static int MinOfComparison(this int left, int compare)
        {
            if (left < compare)
                return left;
            return compare;
        }
        /// <summary>
        /// Gets the minimal value of all the parameters.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static int MinOfComparison(this int left, params int[] compare)
        {
            return left.MinOfComparison(compare.Min());
        }
        /// <summary>
        /// Returns the maximum value of the parameters
        /// </summary>
        /// <param name="left"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static int MaxOfComparison(this int left, int compare)
        {
            if (left > compare)
                return left;
            return compare;            
        }
        /// <summary>
        /// Returns the maximum value of all the parameters
        /// </summary>
        /// <param name="left"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static int MaxOfComparison(this int left, params int[] compare)
        {
            return left.MaxOfComparison(compare.Max());
        }
        
        /// <summary>
        /// Adds the range to the list. 
        /// <para>If the range would increase the count above the limit, then will only insert part of the range
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="aList"></param>
        /// <param name="range"></param>
        /// <param name="limit"></param>
        public static void AddRangeLimited<T>(this List<T> aList, IEnumerable<T> range, int limit)
        {
            if (aList.Count >= limit)
                return;
            if(aList.Count + range.Count() <= limit)
            {
                aList.AddRange(range);
                return;
            }
            foreach(T r in range)
            {                
                aList.Add(r);
                if (aList.Count >= limit)
                    return;
            }
        }
        /// <summary>
        /// Add list of T objects to the end of List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="toAdd"></param>
        public static void AddRange<T>(this List<T> list, params T[] toAdd)
        {            
            list.AddRange(collection: toAdd);
        }

        public static bool In<T>(this T obj, IEnumerable<T> list) => list.Contains(obj);
        public static bool In<T>(this T obj, params T[] list) => list.Contains(obj);
        /// <summary>
        /// Short circuit after finding a match to return false. Opposite of In
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="list"></param>
        /// <returns>True if the list does not contain obj. False if obj is found in the list.</returns>
        public static bool NotIn<T>(this T obj, params T[] list)
        {
            foreach(T l in list)
            {
                if (l.Equals(obj))
                    return false;
            }
            return true;
        }
    
        public static bool Exists<T>(this IEnumerable<T> list, Predicate<T> check)
        {
            foreach(T l in list)
            {
                if (check(l))
                    return true;
            }
            return false;
        }        
        public static bool ContainsAnySubstring(this string value, IEnumerable<string> matches)
        {
            foreach(var m in matches)
            {
                if (value.IndexOf(m) >= 0)
                    return true;
            }
            return false;
        }
        public static List<string> SplitByQualifier(this string value, string delimiter, string leftQualifier, string rightQualifier)
        {
            if (value == null)
                return null;
            if(string.IsNullOrWhiteSpace(rightQualifier))
                throw new ArgumentNullException(nameof(rightQualifier));
            if (string.IsNullOrWhiteSpace(leftQualifier))
                throw new ArgumentNullException(nameof(leftQualifier));
            if (string.IsNullOrEmpty(delimiter))
                throw new ArgumentNullException(nameof(delimiter));
            if (leftQualifier == rightQualifier)
                return value.SplitByQualifier(delimiter, leftQualifier);
            if (delimiter.In(leftQualifier, rightQualifier))
                throw new ArgumentException("Delimiter must not match qualifiers", nameof(delimiter));            
            List<string> list = new List<string>();            
            int i = value.IndexOf(leftQualifier);
            int j = value.IndexOf(delimiter);
            int k = value.IndexOf(rightQualifier, i + leftQualifier.Length);  //-1;
            if( j < 0)
            {
                list.Add(value);
                return list;
            }
            Action<bool> work = (add)=>
            {
                if(add)
                    list.Add(value.Substring(0, j));
                int idx = j + delimiter.Length;
                if (idx <= value.Length)
                    value = value.Substring(j + delimiter.Length);
                else if (value.Length == j)
                {
                    value = null;
                    i = -1;
                    j = -1;
                    k = -1;
                    return;
                }
                i = value.IndexOf(leftQualifier);
                j = value.IndexOf(delimiter);
                k = value.IndexOf(rightQualifier, i + leftQualifier.Length);
            };
            while(j >= 0)
            {
                if(i < 0)
                {
                    list.AddRange(value.SplitByKeyword(delimiter));
                    break;
                }
                if(j < i || k < 0) // (k = value.IndexOf(rightQualifier, i + leftQualifier.Length))< 0) //.Between(0, value.IndexOf(delimiter, i)) == false)
                {
                    work(true);
                    if (j >= 0 || i != 0)
                    {
                        if (j < 0)
                            list.Add(value); //last block of the delimiting, just add it. i should be > 0, so we're basically doing the same thing as below (j >= i && k >= 0)
                        /*
                        {                            
                            j = k + rightQualifier.Length;
                            work(true);
                            // && i == 0) //no more delimiters, but the leftQualifier that got us into the block hasn't been processed. 
                            //list.Add(value); //Note: i should always > 0 in this block, so we want the rest of the value
                        }*/
                        continue;
                    }
                    //j < 0 and i == 0, we're in the last segment and need the text qualified handling.
                }
                if(i == 0)
                    list.Add(value.Substring(i + leftQualifier.Length, k - leftQualifier.Length));
                else                
                    list.Add(value.Substring(0, k + rightQualifier.Length));                
                j = k + rightQualifier.Length;
                work(false);
                if (j < 0 && value != null)
                    list.Add(value);
            }            
            return list;

        }
        public static List<string> SplitByQualifier(this string value, string delimiter, string qualifier, bool keepQualifier = false)
        {

            if (value == null)
                return null;
            if (qualifier == null)
                throw new ArgumentNullException(nameof(qualifier));            
            if (delimiter == null)
                throw new ArgumentNullException(nameof(delimiter));
            if (delimiter == qualifier)
                throw new ArgumentException("Delimiter must not match qualifier", nameof(delimiter));
            List<string> list = new List<string>();
            string work = string.Empty;
            int i = value.IndexOf(qualifier);
            int j = value.IndexOf(delimiter);
            if (j < 0)
            {
                list.Add(value);
                return list;
            }
            var s = value.SplitByKeyword(qualifier).ToList();
            int c = s.Count;
            if (c == 1)
                return value.SplitByKeyword(delimiter).ToList();
            if (c % 2 == 0)
                throw new Exception($"Missing Qualifier in string value: '{qualifier}'");
            for(int a = 0; a < c; a++)
            {
                if (a % 2 == 0)
                {
                    var s2 = (s[a]).SplitByKeyword(delimiter).ToList();
                    var s2l = s2.Count - 1;
                    s2[0] = work + s2[0];
                    if (s2l == 0)
                    {
                        work = s2[0];
                        continue;
                    }
                    work = s2[s2l];
                    s2.RemoveAt(s2l);
                    list.AddRange(s2);
                }
                else
                {
                    if(keepQualifier)
                        work = $"{work}{qualifier}{s[a]}{qualifier}";
                    else
                        work = work + s[a];
                }
            }
            //if (work != string.Empty)
                list.Add(work);
            

            return list;
        }
        /// <summary>
        /// Splits the string by a single keyword
        /// </summary>
        /// <param name="value"></param>
        /// <param name="KeyWord"></param>
        /// <param name="includeKeyword">Incldue the keyword in the resulting IEnumerable</param>
        /// <param name="IgnoreCase">If true, ignores the cases of value and KeyWord</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitByKeyword(this string value, string KeyWord, bool includeKeyword = false, bool IgnoreCase = false)
            => value.SplitByKeyword(new string[] { KeyWord }, includeKeyword, IgnoreCase);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="KeyWords"></param>
        /// <param name="IncludeKeywords">If true, include the keywords in the Enumerable. If False, leave them out.</param>
        /// <param name="IgnoreCase">If true, ignores the cases of value and KeyWord</param>
        /// <returns></returns>
        public static IEnumerable<string> SplitByKeyword(this string value, IEnumerable<string> KeyWords, bool IncludeKeywords = false, bool IgnoreCase = false)
        {
            int work = 0;
            while(work <= value.Length)
            {
                string kw;
                int i = value.IndexOfAny(KeyWords, work, out kw, IgnoreCase);
                if (i >= 0)
                {
                    yield return value.Substring(work, i - work);
                    if (IncludeKeywords)
                        yield return kw;
                    work = i + kw.Length;
                }
                else
                {
                    yield return value.Substring(work);
                    yield break;
                }
            }
        }        
        public static int IndexOfAny(this string value, IEnumerable<string> words, int start= 0, bool ignoreCase = false)
        {
            string c;
            return value.IndexOfAny(words, start, out c, ignoreCase);
        }
        /// <summary>
        /// First index of any of the keywords that occurs at or after start.
        /// <para>Returns -1 if no match found</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="words"></param>
        /// <param name="start"></param>
        /// <param name="Chosen">The keyword that found the index</param>
        /// <param name="CaseInsensitive">Ignore case when doing comparison</param>
        /// <returns></returns>
        public static int IndexOfAny(this string value, IEnumerable<string> words, int start, out string Chosen, bool CaseInsensitive)
        {
            Chosen = null;
            
            int i = value.Length + 1;
            foreach(var w in words)
            {
                int wi = -1;
                if (CaseInsensitive)
                    wi = value.IndexOf(w, start, StringComparison.OrdinalIgnoreCase);
                else
                    wi = value.IndexOf(w, start, StringComparison.Ordinal);                
                if (wi < 0)
                    continue;
                if (wi < i)
                {
                    i = wi;
                    Chosen = w;
                }
            }
            if (i > value.Length)
                return -1;
            return i;
        }        
        /// <summary>
        /// Lazy check to make sure that an IEnumerable has at least <paramref name="minimum"/> records.
        /// <para>Empty IEnumerables will always return false.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="minimum">Must be at least 1 to be useful - null/empty IEnumerables will always return false</param>
        /// <returns></returns>
        public static bool HasMinimumCount<T>(this IEnumerable<T> list, int minimum)
        {
            if (list == null)
                return false;
            //if (minimum < 1)
            //    throw new ArgumentException("Value must be greater than 0", "minimum");
            int count = 0;
            foreach(T l in list)
            {
                count++;
                if (count >= minimum)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Lazy check to make sure that an IEnumerable has at least <paramref name="minimum"/> records.
        /// <para>Empty IEnumerables will always return false.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="condition"></param>
        /// <param name="minimum">Must be at least 1 to be useful - null/empty IEnumerables will always return false</param>
        /// <returns></returns>
        public static bool HasMinimumCount<T>(this IEnumerable<T> list, Predicate<T> condition, int minimum)
        {
            if (list == null)
                return false;
            //if (minimum < 1)
            //    throw new ArgumentException("Value must be greater than 0", "minimum");
            int count = 0;
            foreach (T l in list)
            {
                if (!condition(l))
                    continue;
                count++;
                if (count >= minimum)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Maps the dictionary to an ordered enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="orderSelection">Determines order that values are grabbed from the dictionary.<para>
        /// If a record does not exist as a key, the default of <typeparamref name="T"/> will be returned</para></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderedMap<K,T>(this IDictionary<K, T> dictionary, IEnumerable<K> orderSelection)
        {
            foreach(var key in orderSelection)
            {
                T value = default(T);
                if (dictionary.TryGetValue(key, out value))
                    yield return value;
                yield return default(T);
            }
        }
        /// <summary>
        /// Maps the values into the dictionary to populate the value of the corresponding key by ordinal position
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="Keys"></param>
        /// <param name="values"></param>
        /// <param name="destination"></param>
        public static void MapInto<K, T>(this IList<T> values, IDictionary<K, T> destination, IList<K> Keys)
        {            
            for(int i = 0; i < Keys.Count; i++)
            {
                destination[Keys[i]] = values[i];
            }
        }
        /// <summary>
        /// Lazy check to make sure that an IENumerable has less than <paramref name="maximum"/> records. (Returns immediately after confirming that we're above the count)
        /// <para>Empty IEnumerables will always return true.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public static bool UnderMaximumCount<T>(this IEnumerable<T> list, int maximum)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            int count = 0;
            foreach(T l in list)
            {
                count++;
                if (count >= maximum)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Lazy check to make sure that an IENumerable has less than <paramref name="maximum"/> records. (Returns immediately after confirming that we're above the count)
        /// <para>Empty IEnumerables will always return true.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="condition">Condition to filter options that contribute to the count being compared against maximum</param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public static bool UnderMaximumCount<T>(this IEnumerable<T> list, Predicate<T> condition, int maximum)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            int count = 0;
            foreach (T l in list)
            {
                if (!condition(l))
                    continue;
                count++;
                if (count >= maximum)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Reverse from exists, but exits early on finding a match.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        public static bool NotExists<T>(this IEnumerable<T> list, Predicate<T> check)
        {
            foreach(T l in list)
            {
                if (check(l))
                    return false;
            }
            return true;
        }
        public static bool And(this bool a, bool b) => a && b;
        public static bool Or(this bool a, bool b) => a || b;

        /// <summary>
        /// returns true if a and not b. False A or true b will result in false
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool AndNot(this bool a, bool b) => a && !b;
        
        public static List<T> Exclude<T>(this IEnumerable<T> list, Predicate<T> Check, out int ExcludedCount)
        {
            ExcludedCount = 0;
            List<T> rt = new List<T>();
            foreach(T l in list)
            {
                if (Check(l))
                    ExcludedCount++;
                else
                    rt.Add(l);
            }
            return rt;
        }
        public static IEnumerable<T> Exclude<T>(this IEnumerable<T> list, Predicate<T> check)
        {
            return list.Where(l => !check(l));
        }
        
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> myUpdateAction)
        {
            foreach(T l in list)
            {
                myUpdateAction(l);
            }
        }
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> Update, int limit)
        {
            if (limit < 1)
                return;
            int i = 0;
            foreach (T l in list)
            {
                Update(l);
                i++;
                if (i >= limit)
                    break;
            }
        }
        /// <summary>
        /// Transforms each record of the enumerable and returns it as a new enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static IEnumerable<T> TransformEach<T>(this IEnumerable<T> list, Func<T, T> func)
        {
            foreach(var ti in list)
            {
                yield return func(ti);
            }
        }
        public static void ForEachIndexLimited<T>(this IEnumerable<T> list, Action<T, int> Update, int limit)
        {
            if (limit < 1)
                return;
            int i = 0;
            foreach(T l in list)
            {
                Update(l, i++);
                if (i >= limit)
                    break;
            }
        }
        /// <summary>
        /// Apply <paramref name="Update"/> to each item in <paramref name="list"/>. 
        /// <para>Passes an integer value starting at <paramref name="startIndex"/> to each record in the list. Value is incremented by <paramref name="Interval"/>each time <paramref name="Update"/> is called.
        /// </para> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="Update"></param>
        /// <param name="startIndex">Starting value for the indexes passed to the procedure</param>
        /// <param name="Interval">Interval that index value increases</param>
        public static void ForEachIndex<T>(this IEnumerable<T> list, Action<T, int> Update, int startIndex = 0, int Interval = 1)
        {
            if (Interval == 0)
                throw new ArgumentException("Interval is Zero", "Interval");
            int i = startIndex;
            foreach (T l in list)
            {
                if(l != null)
                    Update(l, i );
                i += Interval;
            }
        }
        /// <summary>
        /// Takes an IEnumerable and applies method 'Apply' to every item that fits the predicate. Other items are excluded from the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="Predicate"></param>
        /// <param name="Apply"></param>
        /// <returns></returns>
        public static IEnumerable<T> CrossApply<T>(this IEnumerable<T> list, Func<T, bool> Predicate, Action<T> Apply)
        {
            IEnumerable<T> temp = list.Where(t => Predicate(t));
            temp.ForEach(t => Apply(t));
            return temp;
        }        
        /// <summary>
        /// Cross apply with an action that can return more records.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="Predicate"></param>
        /// <param name="Apply"></param>
        /// <returns>Returns results of calling apply on the list members that match Predicate</returns>
        public static IEnumerable<T> CrossApply<T>(this IEnumerable<T> list, Func<T, bool> Predicate, Func<T, IEnumerable<T>> Apply)
        {
            IEnumerable<T> temp = list.Where(t => Predicate(t));
            int l = temp.Count();
            List<T> temp2 = new List<T>();
            temp.ForEach(t => temp2.Concat(Apply(t)));
            //return temp;
            return temp2;
        }
        /// <summary>
        /// Takes an IEnumerable and applies method 'Apply' to every item that fits the predicate. Other items are left 'As-Is', but included in the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="Predicate"></param>
        /// <param name="Apply"></param>
        /// <returns></returns>
        public static IEnumerable<T> OuterApply<T>(this IEnumerable<T> list, Func<T, bool> Predicate, Action<T> Apply)
        {
            IEnumerable<T> inner = list.Where(t => Predicate(t));
            IEnumerable<T> outer = list.Except(inner);
            inner.ForEach(t => Apply(t));
            return inner.Union(outer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="Predicate"></param>
        /// <param name="Apply"></param>
        /// <returns>List Members that don't match predicate combined with result of applying function to members that do match</returns>
        public static IEnumerable<T> OuterApply<T>(this IEnumerable<T> list, Func<T, bool> Predicate, Func<T, IEnumerable<T>> Apply)
        {
            IEnumerable<T> inner = list.Where(t => Predicate(t));
            IEnumerable<T> outer = list.Except(inner);            
            inner.ForEach(t => outer.Concat(Apply(t)));  //Apply, add result to outer to leave inner alone
            //return inner.Union(outer); //Original inner + Result of apply + original outer
            return outer; //Return outer + results of applying on inner
        }
                
    }
}
