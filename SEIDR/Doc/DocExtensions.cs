using System;
using System.IO;
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

        public static string GetFileHash(this FileInfo file)
        {
            using(FileStream fs = new FileStream(file.FullName,FileMode.Open, FileAccess.Read))
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
                    if (search == "YYYY")
                    {
                        year = fileName.Substring(0, 4);
                        fileName = fileName.Substring(4);
                    }
                    else if (search == "YY")
                    {
                        year = "20" + fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "MM")
                    {
                        month = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
                    }
                    else if (search == "DD")
                    {
                        day = fileName.Substring(0, 2);
                        fileName = fileName.Substring(2);
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
