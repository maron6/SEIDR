﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SEIDR.Doc
{
    /// <summary>
    /// Extensions related to documents/files and reading/writing/manipulating content
    /// </summary>
    public static class DocExtensions
    {
        #region SERIALIZATION extensions
        /// <summary>
        /// Overwrite the filepath with the xml content of the object (Basic XML serializer)
        /// </summary>
        /// <param name="toFile"></param>
        /// <param name="FilePath"></param>
        public static void SerializeToFile(this object toFile, string FilePath)
        {
            XmlSerializer xsr = new XmlSerializer(toFile.GetType());
            using (StreamWriter sw = new StreamWriter(FilePath, false))
            {
                xsr.Serialize(sw, toFile);
            }
        }
        /// <summary>
        /// Deserialize the file's content into an instance fo type RT (Basic XML serializer)
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public static RT DeSerializeFile<RT>(string FilePath) //where RT:new()
        {
            XmlSerializer xsr = new XmlSerializer(typeof(RT));
            RT x;
            using (StreamReader sr = new StreamReader(FilePath))
            {
                x = (RT)xsr.Deserialize(sr);
            }
            return x;
        }
        /// <summary>
        /// Serialize the object to an XML string and return it (Basic XML serializer)
        /// </summary>
        /// <param name="toString"></param>
        /// <returns></returns>
        public static string SerializeToXML(this object toString)
        {
            XmlSerializer xsr = new XmlSerializer(toString.GetType());
            using (StringWriter sw = new StringWriter())
            {
                xsr.Serialize(sw, toString);
                return sw.ToString();
            }
        }
        /// <summary>
        /// Attempt to deserialize the XML into an object of type RT. Does not catch exceptions
        /// <para>Uses Basic XMLSerializer</para>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="XML"></param>
        /// <returns></returns>
        public static RT DeserializeXML<RT>(this string XML)
        {
            RT x;
            XmlSerializer xsr = new XmlSerializer(typeof(RT));
            using (StringReader sr = new StringReader(XML))
            {
                x = (RT)xsr.Deserialize(sr);
            }
            return x;
        }
        #endregion
        /// <summary>
        /// Tries to guess the delimiter of a line string from the following characters: |,\t;:
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static char GuessDelimiter(this string line)
        {
            string Delimiter = "|" + ",\t;:";
            char current = '\0';
            foreach (char i in Delimiter)
            {
                if (line.Split(i).Length > 1)
                {
                    current = i;
                    break;
                }
            }
            return current;
        }
        /// <summary>
        /// Uses md5 to create a hash of the file content. Returns null if the file does not exist.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetFileHash(this FileInfo file)
        {
            if (!file.Exists)
                return null;
            return GetFileHash(file.FullName);
        }
        /// <summary>
        /// Uses md5 to create a hash of the file's content
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        public static string GetFileHash(this string fullFilePath)
        {

            using (FileStream fs = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read))
            {
                var m = System.Security.Cryptography.MD5.Create();
                return m.ComputeHash(fs).ToString();
            }
        }
        /// <summary>
        /// Add listed attributes to the File and refreshes the FilInfo. Does nothing if the File doesn't exist.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="AttributesToAdd"></param>
        public static void AddAttributes(this FileInfo f, params FileAttributes[] AttributesToAdd)
        {
            if (!f.Exists)
                return;
            FAttModder.AddAttribute(f.FullName, AttributesToAdd);
            f.Refresh();
        }
        /// <summary>
        /// Remove listed attributes from the File and refreshes the FilInfo. Does nothing if the File doesn't exist.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="attributesToRemove"></param>
        public static void RemoveAttributes(this FileInfo f, params FileAttributes[] attributesToRemove)
        {
            if (!f.Exists)
                return;
            FAttModder.RemoveAttribute(f.FullName, attributesToRemove);
            f.Refresh();
        }
        public static long? GetFileSize(this string FilePath)
        {
            FileInfo f = new FileInfo(FilePath);
            if (!f.Exists)
                return null;
            return f.Length;
        }
        /// <summary>
        /// Generates a new file name using the dateFormat and passed FileDate. Multiple date offsets can be used by offsetting with an alias.
        /// <para>E.g., &lt;a:0YYYY0MM-1D>test_&lt;a:YY>_&lt;a:MM>_&lt;a:DD>_&lt;DD>.txt for date 2017/12/2 should lead to test_17_12_01_02.txt</para>
        /// </summary>
        /// <param name="dateFormat"></param>
        /// <param name="fileDate"></param>
        /// <returns></returns>
        public static string GetFileName(this string dateFormat, DateTime fileDate)
        {
            string offsetPattern = @"[<][a-zA-Z]+[:](\+|-)?\d+(YYYY|YY)(\+|-)?\d+M{1,2}(\+|-)?\d+D{1,2}[>]";
            var r = new Regex(offsetPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            var m = r.Matches(dateFormat); //get formats to use.
            DateTime td = fileDate;
            var maps = new System.Collections.Generic.Dictionary<string, DateTime>();
            foreach (Match dateMatch in m)
            {
                dateFormat = dateFormat.Replace(dateMatch.Value, ""); //remove the offset value.
                string s = dateMatch.Value;
                s = s.Substring(1, s.Length - 2); //Remove starting '<' and '>'
                if (s.Contains(":"))
                {
                    var s2 = s.Split(':');
                    string k = s2[0];
                    s = s2[1].ToUpper();
                    int yOffset = 4;
                    int yy = s.IndexOf("YYYY");
                    if (yy < 0)
                    {
                        yy = s.IndexOf("YY");
                        yOffset = 2;
                    }
                    int mOffset = 2;
                    int mm = s.IndexOf("MM");
                    if (mm < 0)
                    {
                        mm = s.IndexOf("M");
                        mOffset = 1;
                    }
                    int dOffset = 2;
                    int dd = s.IndexOf("DD");
                    if (dd < 0)
                    {
                        dd = s.IndexOf("D");
                        dOffset = 1;
                    }
                    int year = Int32.Parse(s.Substring(0, yy));
                    int month = Int32.Parse(s.Substring(yy + yOffset, mm - yy - yOffset));
                    int day = Int32.Parse(s.Substring(mm + mOffset, dd - mm - mOffset));
                    DateTime d;
                    if (!maps.TryGetValue(k, out d))
                        d = fileDate;
                    d = d.AddDays(day);
                    d = d.AddMonths(month);
                    d = d.AddYears(year);
                    maps[k] = d;
                }
                else
                {
                    int yOffset = 4;
                    int yy = s.IndexOf("YYYY");
                    if (yy < 0)
                    {
                        yy = s.IndexOf("YY");
                        yOffset = 2;
                    }
                    int mOffset = 2;
                    int mm = s.IndexOf("MM");
                    if (mm < 0)
                    {
                        mm = s.IndexOf("M");
                        mOffset = 1;
                    }
                    int dOffset = 2;
                    int dd = s.IndexOf("DD");
                    if (dd < 0)
                    {
                        dd = s.IndexOf("D");
                        dOffset = 1;
                    }
                    int year = Int32.Parse(s.Substring(0, yy));
                    int month = Int32.Parse(s.Substring(yy + yOffset, mm - yy - yOffset));
                    int day = Int32.Parse(s.Substring(mm + mOffset, dd - mm - mOffset));

                    td.AddDays(day);
                    td.AddMonths(month);
                    td.AddYears(year);
                }
            }            
            foreach(var kv in maps)
            {
                dateFormat = dateFormat
                                .Replace("<" + kv.Key + ":YYYY>", kv.Value.Year.ToString())
                                .Replace("<" + kv.Key + ":YY>", kv.Value.Year.ToString().Substring(2))
                                .Replace("<" + kv.Key + ":CC>", kv.Value.Year.ToString().Substring(0, 2))
                                .Replace("<" + kv.Key + ":MM>", kv.Value.Month.ToString().PadLeft(2, '0'))
                                .Replace("<" + kv.Key + ":M>", kv.Value.Month.ToString())
                                .Replace("<" + kv.Key + ":DD>", kv.Value.Day.ToString().PadLeft(2, '0'))
                                .Replace("<" + kv.Key + ":D>", kv.Value.Day.ToString())
                                ;
            }
            return dateFormat
                                .Replace("<YYYY>", td.Year.ToString())
                                .Replace("<YY>", td.Year.ToString().Substring(2))
                                .Replace("<CC>", td.Year.ToString().Substring(0, 2))
                                .Replace("<MM>", td.Month.ToString().PadLeft(2, '0'))
                                .Replace("<M>", td.Month.ToString())
                                .Replace("<DD>", td.Day.ToString().PadLeft(2, '0'))
                                .Replace("<D>", td.Day.ToString())
                                ;            
        }
        /// <summary>
        /// Parses the file date out of a file.
        /// <para>Example: file_&lt;YYYY>_&lt;MM>_&lt;DD>_*&lt;0YYYY0MM1DD>.txt might be used for file_2017_12_01_through_2017_12_04.txt to get 2017/12/02</para>
        /// <para>Another example for the same fileName: file_*_through_&lt;YYYY>_&lt;MM>_&lt;DD>_&lt;0YYYY0MM-2DD>.txt could be used for file_2017_12_01_through_2017_12_04.txt to get 2017/12/02</para>
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dateFormat"></param>
        /// <param name="fileDate">File date from parsing the fileName with dateFormat. Will be unchanged if the method returns false. 
        /// <para>Note: if the format has an offset for Month/Year, this could potentially lead to some unexpected behavior due to months having different numbers of days, or leap years.
        /// </para>
        /// </param>
        /// <returns></returns>
        public static bool ParseDateRegex(this string fileName, string dateFormat, ref DateTime fileDate)
        {
            fileDate = fileDate.Date;
            //System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"<[+-\dDMY]?>");
            string offsetPattern = @"[<](\+|-)?\d+(YYYY|YY)(\+|-)?\d+M{1,2}(\+|-)?\d+D{1,2}[>]";
            var r = new Regex(offsetPattern, RegexOptions.IgnoreCase|RegexOptions.IgnorePatternWhitespace);
            var m = r.Match(dateFormat); //get formats to use.
            //int offset = 0;
            int year = 0, month = 0, day = 0;
            if(m.Success)
            {
                string s = m.Value;
                s = s.Substring(1, s.Length - 2); //Remove starting '<' and '>'
                dateFormat = dateFormat.Replace(m.Value, "");
                DateTime td = fileDate;
                int yOffset = 4;
                int yy = s.IndexOf("YYYY");
                if (yy < 0)
                {
                    yy = s.IndexOf("YY");
                    yOffset = 2;
                }
                int mOffset = 2;
                int mm = s.IndexOf("MM");
                if (mm < 0)
                {
                    mm = s.IndexOf("M");
                    mOffset = 1;
                }
                int dd = s.IndexOf("DD");
                if (dd < 0)
                {
                    dd = s.IndexOf("D");
                }
                year = Int32.Parse(s.Substring(0, yy));
                month = Int32.Parse(s.Substring(yy + yOffset, mm - yy - yOffset));
                day = Int32.Parse(s.Substring(mm + mOffset, dd - mm - mOffset));
            }
            bool x;
            if (x = fileName.ParseDate(dateFormat, out fileDate))
                fileDate = fileDate.AddDays(day).AddMonths(month).AddYears(year);
            
            return x;
        }        
        /// <summary>
        /// Gets the file date
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="dateFormat"></param>
        /// <returns></returns>
        public static DateTime GetFileDate(this DocMetaData metaData, string dateFormat)
        {
            FileInfo fi = new FileInfo(metaData.FilePath);
            if (!fi.Exists)
                throw new FileNotFoundException("Could not find file at specified path: " + metaData.FilePath);
            DateTime date = fi.CreationTime.Date;
            fi.Name.ParseDateRegex(dateFormat, ref date);
            return date;
        }
        /// <summary>
        /// Parses the filename to determine a date that should be associated with it. 
        /// <para>If a date can be determined, the FileDate out parameter will be set and usable</para>
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dateFormat">* For skipping a variable number of misc characters, &lt;MM> for Finding a month, &lt;DD> for finding a day, &lt;YY> for finding year without century, &lt;YYYY> for finding year with century.
        /// <para>E.g., 'example01_2016_File_12_30' would match 'example01_*&lt;YYYY>_File_&lt;MM>_&lt;DD>' or '*&lt;YYYY>*&lt;MM>_&lt;DD>'. Other numbers in between may cause issues, though. </para> </param>
        /// <param name="FileDate"></param>
        /// <returns>True if able to parse a date from the file name using specified format.</returns>
        public static bool ParseDate(this string fileName, string dateFormat, out DateTime FileDate)
        {
            //ToDo: Regex approach: <[+-]\d+YY>, <[+-]\d+MM>offset months, <[+-]\d+DD> offset days, <YYYY>, <MM> padded month, <YY>, <CC>, <CCYY>, <DD> padded day, <M>, <D> 
            FileDate = new DateTime();
            char[] fDel = new char[] { '<', '>' };
            if (dateFormat.IndexOfAny(fDel) < 0)
                return false;
            string[] sl = dateFormat.Split('*');
            //int lastIndex = -1;
            string year = null;
            string month = null;
            string day = null;
            foreach (string s in sl)
            {
                if (s == "")
                    continue;
                if (s.IndexOfAny(fDel) < 0)
                {
                    int x = fileName.IndexOf(s);
                    if (x < 0 || x + s.Length > fileName.Length)
                        return false; //No match. Don't trust the rest
                    fileName = fileName.Substring(x + s.Length);
                    continue;
                }

                string[] tokens = s.Split('<');
                if (tokens[0] != "")
                {
                    int x = fileName.IndexOf(tokens[0]);
                    if (x < 0)
                        return false;
                    fileName = fileName.Substring(x + tokens[0].Length);
                }
                for (int i = 1; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    int check = token.IndexOf('>');
                    if (check < 0)
                        return false;
                    string search = token.Substring(0, check).ToUpper();
                    check = fileName.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                    if (check < 0)
                        return false;
                    fileName = fileName.Substring(check);
                    if (search == "YYYY" || year == "CCYY")
                    {
                        year = fileName.Substring(0, 4);
                        fileName = fileName.Substring(4);
                    }
                    else if(search == "CC")
                    {
                        year = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "YY")
                    {
                        if (year == null)
                            year = DateTime.Today.Year.ToString().Substring(0, 2);
                        year += fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "MM")
                    {
                        month = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if(search == "M")
                    {
                        month = fileName.Substring(0, 1).PadLeft(2, '0');
                        fileName = fileName.Substring(1);
                    }
                    else if (search == "DD")
                    {
                        day = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if(search == "D")
                    {
                        day = fileName.Substring(0, 1).PadLeft(2, '0');
                        fileName = fileName.Substring(1);
                    }
                    else
                        return false;
                    //int x = fileName.IndexOf(token);                    

                    if (year != null && month != null && day != null)
                    {
                        try
                        {                            
                            FileDate = DateTime.ParseExact(year + month + day, "yyyyMMdd", new System.Globalization.CultureInfo("EN-US"));
                            return true;
                        }
                        catch { return false; }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Removes invisible characters from a line of text. Also replaces u0092 with single quote.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string CleanLine(this string line)
        {
            string fastInvalids = @"[\u0000-\u0008\u000B-\u0019\u00A0]";
            line = System.Text.RegularExpressions.Regex.Replace(line, fastInvalids, "");
            line = System.Text.RegularExpressions.Regex.Replace(line, @"[\u0092]", "'");
            return line;
        }
        /// <summary>
        /// Splits a string so that delimiters inside quotes do not create extra fields.
        /// </summary>
        /// <remarks>Assumes that there are no NULL characters in the string.</remarks>
        /// <param name="line">Line to be split</param>
        /// <param name="delimiter">Delimiter to split the file</param>
        /// <param name="TextQual">Text qualifier. Delimiters between text qualifiers will be kept. Default to '"'</param>
        /// <returns>Array of strings split by delimiter except where the delimiter is between text qualifiers</returns>
        public static string[] SplitOutsideQuotes(this string line, char delimiter, char TextQual = '"')
        {
            string[] switcher = line.Split(TextQual);
            for (int i = 0; i < switcher.Length; i += 2)
            {
                switcher[i] = switcher[i].Replace(delimiter, (char)0);
            }
            line = string.Join("" + TextQual, switcher);
            return line.Split((char)0);
        }
    }
}
