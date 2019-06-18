using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Helper for Dates from Doc data
    /// </summary>
    public static class DateConverter
    {
        /// <summary>
        /// Gets a DateTime object for the specified column.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="columnInfo"></param>
        /// <returns></returns>
        public static DateTime? GetDateTime(this IRecord r, DocRecordColumnInfo columnInfo)
        {
            var dt = r[columnInfo];
            if (string.IsNullOrEmpty(dt))
                return null;
            if(columnInfo.Format == null)
            {
                string fmt;
                if (columnInfo.DataType == DocRecordColumnType.DateTime)
                {
                    if (!GuessFormatDateTime(dt, out fmt))
                    {
                        throw new Exception("Unable to guess DateTime format.");
                    }
                }
                else
                {
                    if(!GuessFormatDate(dt, out fmt))
                    {
                        throw new Exception("Unable to guess Date format.");
                    }
                }
                columnInfo.Format = fmt;
            }
            return DateTime.ParseExact(dt, columnInfo.Format, System.Globalization.CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Guess the formats of any date columns in the DocReader
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static int GuessDateTimeFormats(this DocReader doc)
        {
            int rc = 0;
            var dmd = doc.MetaData as DocMetaData;
            if(dmd != null)
            {
                rc = GuessFormats(doc, doc);
            }
            else //Multi Record
            {
                foreach(var record in doc)
                {
                    rc = GuessFormats(metaData:doc.MetaData, record); //Return: Number of date/datetime columns that don't have a format.
                    if (rc == 0)
                        break; 
                }
            }
            return rc;
        }
        /// <summary>
        /// Check Date/DateTime formats for a single record, given the metadata for the file.
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="record"></param>
        /// <returns>Number of DateColumns remaining with an undetermined format.</returns>
        public static int GuessFormats(MetaDataBase metaData, IRecord record)
        {
            var col = metaData.GetRecordColumnInfos(record);
            var rc = GuessFormats(col, record);
            if (metaData is DocMetaData)
                return rc;
            if(metaData is MultiRecordDocMetaData)
            {
                var mmd = metaData as MultiRecordDocMetaData;
                foreach(var kv in mmd.ColumnSets)
                {
                    if (kv.Value == col)
                        continue;
                    rc = kv.Value.Count(c => c.DataType.In(DocRecordColumnType.Date, DocRecordColumnType.DateTime)
                                            && string.IsNullOrEmpty(c.Format));
                }
                return rc;
            }
            return rc;
        }
        /// <summary>
        /// Check Date/DateTime Formats for a single record
        /// </summary>
        /// <param name="columnSet"></param>
        /// <param name="record"></param>        
        /// <returns>Number of DateColumns remaining with an undetermined format.</returns>
        public static int GuessFormats(DocRecordColumnCollection columnSet, IRecord record)
        {
            var dateCols = columnSet.Columns
               .Where(c => c.DataType.In(DocRecordColumnType.Date, DocRecordColumnType.DateTime)
                               && string.IsNullOrEmpty(c.Format))
               .ToList();
            var colCount = dateCols.Count;
            if (colCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("No Date columns to check.");
            }
            foreach (var col in dateCols)
            {
                var dv = record[col];
                if (!string.IsNullOrEmpty(dv))
                {
                    string f;
                    if (col.DataType == DocRecordColumnType.DateTime)
                    {
                        if (GuessFormatDateTime(dv, out f))
                        {
                            col.Format = f;
                        }
                    }
                    else if (GuessFormatDate(dv, out f))
                    {
                        col.Format = f;
                    }
                }
            }
            colCount -= dateCols.RemoveAll(c => !string.IsNullOrEmpty(c.Format));
            return colCount;
        }
        /// <summary>
        /// Attempts to use <see cref="GuessFormatDateTime(string, out string)"/> to populate the formats of the columns.
        /// <para>Note: will only guess for values that are populated and does not already have a Format - 
        /// if a column is never populated in the data, then it will still have a null Format after this call.</para>
        /// </summary>
        /// <param name="columnSet"></param>
        /// <param name="records">Set of records to check. Note: You should be able to pass an entire DocReader to this method because it implements IEnumerable for <see cref="DocRecord"/></param>
        /// <returns>Number of DateColumns remaining with an undetermined format.</returns>
        public static int GuessFormats(DocRecordColumnCollection columnSet, IEnumerable<IRecord> records)
        {
            var dateCols = columnSet.Columns
                .Where(c => c.DataType.In(DocRecordColumnType.Date, DocRecordColumnType.DateTime) 
                                && string.IsNullOrEmpty(c.Format))
                .ToList();
            var colCount = dateCols.Count;
            if(colCount == 0)
            {
                System.Diagnostics.Debug.WriteLine("No Date columns to check.");
            }
            foreach(var record in records)
            {
                foreach(var col in dateCols)
                {
                    var dv = record[col];
                    if(!string.IsNullOrEmpty(dv))
                    {
                        string f;
                        if (col.DataType == DocRecordColumnType.DateTime)
                        {
                            if (GuessFormatDateTime(dv, out f))
                            {
                                col.Format = f;
                            }
                        }
                        else if(GuessFormatDate(dv, out f))
                        {
                            col.Format = f;
                        }
                    }
                }
                colCount -= dateCols.RemoveAll(c => !string.IsNullOrEmpty(c.Format));
                if (colCount == 0)
                    break;
            }
            return colCount;
        }
        const string YEAR = "(1[4-9]||[2-9][0-9])[0-9][0-9]";
        const string YEAR_NO_CENTURY = "[0-9][0-9]";
        const string MONTH = "(0[1-9]|1[0-2])";
        const string DAY = "(0[1-9]|[1-2][0-9]|3[0-1])";
        const string DASH = "-";
        const string SLASH = @"\/";
        const string SPACE = " ";
        const string HOUR = "([0-1][0-9]|2[0-3])";
        const string SMALL_HOUR = "(0[0-9]|1[0-2])"; //12 hour clock
        const string AM_PM = SPACE + "([AP]M)";
        const string MINUTE = "[0-5][0-9]";
        const string SECOND = MINUTE;
        const string H_M = HOUR + ":" + MINUTE;
        const string H_M_S = H_M + ":" + SECOND;
        const string Hs_M = SMALL_HOUR + ":" + MINUTE + AM_PM; //single minute shouldn't really be common...
        const string Hs_M_S = SMALL_HOUR + ":" + MINUTE + ":" + SECOND + AM_PM;
        const string H1 = "([0-9])";
        const string H1_M = H1 + ":" + MINUTE;
        const string H1s_M = H1 + ":" + MINUTE + AM_PM;
        const string H1_M_S = H1 + ":" + MINUTE + ":" + SECOND;
        const string H1s_M_S = H1 + ":" + MINUTE + ":" + SECOND + AM_PM;


        

        /// <summary>
        /// Guesses the format of a date string.
        /// </summary>
        /// <param name="DateString"></param>
        /// <param name="Format"></param>
        /// <returns></returns>
        public static bool GuessFormatDateTime(string DateString, out string Format)
        {
            if (GuessFormatDate(DateString, out Format))
                return true;
            Format = null;
            Dictionary<string, string> patternSet = new Dictionary<string, string>
            {
                
                //Year, but no century
                //{ YEAR_NO_CENTURY + MONTH + DAY, "yyMMdd"},
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H_M, "yyMMdd HH:mm" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H_M_S, "yyMMdd HH:mm:ss" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H1_M, "yyMMdd H:mm" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H1_M_S, "yyMMdd H:mm:ss" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + Hs_M, "yyMMdd hh:mm tt" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + Hs_M_S, "yyMMdd hh:mm:ss tt" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H1s_M, "yyMMdd h:mm tt" },
                { YEAR_NO_CENTURY + MONTH + DAY + SPACE + H1s_M_S, "yyMMdd h:mm:ss tt" },    
                //No space separator only makes sense if no other separators
                //{ YEAR_NO_CENTURY + MONTH + DAY + HOUR + MINUTE, "yyMMddHHmm" },
                { YEAR_NO_CENTURY + MONTH + DAY + HOUR + MINUTE + SECOND, "yyMMddHHmmss" },
                //Separators:
                //{ YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY , "yy-MM-dd"},
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H_M, "yy-MM-dd HH:mm" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H_M_S, "yy-MM-dd HH:mm:ss" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H1_M, "yy-MM-dd H:mm" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H1_M_S, "yy-MM-dd H:mm:ss" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + Hs_M, "yy-MM-dd hh:mm tt" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + Hs_M_S, "yy-MM-dd hh:mm:ss tt" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H1s_M, "yy-MM-dd h:mm tt" },
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY + SPACE + H1s_M_S, "yy-MM-dd h:mm:ss tt" },
                //{ YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY , "yy/MM/dd"},
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H_M, @"yy/MM/dd HH:mm" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H_M_S, @"yy/MM/dd HH:mm:ss" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H1_M, @"yy/MM/dd H:mm" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H1_M_S, @"yy/MM/dd H:mm:ss" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + Hs_M, @"yy/MM/dd hh:mm tt" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + Hs_M_S, @"yy/MM/dd hh:mm:ss tt" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H1s_M, @"yy/MM/dd h:mm tt" },
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY + SPACE + H1s_M_S, @"yy/MM/dd h:mm:ss tt" },
                //{ MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY , "MM/dd/yy"},
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H_M, @"MM/dd/yy HH:mm" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H_M_S, @"MM/dd/yy HH:mm:ss" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H1_M, @"MM/dd/yy H:mm" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H1_M_S, @"MM/dd/yy H:mm:ss" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + Hs_M, @"MM/dd/yy hh:mm tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + Hs_M_S, @"MM/dd/yy hh:mm:ss tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H1s_M, @"MM/dd/yy h:mm tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY + SPACE + H1s_M_S, @"MM/dd/yy h:mm:ss tt" },

                //Full Year
                //{ YEAR + MONTH + DAY, "yyyyMMdd"},
                { YEAR + MONTH + DAY + SPACE + H_M, "yyyyMMdd HH:mm" },
                { YEAR + MONTH + DAY + SPACE + H_M_S, "yyyyMMdd HH:mm:ss" },
                { YEAR + MONTH + DAY + SPACE + H1_M, "yyyyMMdd H:mm" },
                { YEAR + MONTH + DAY + SPACE + H1_M_S, "yyyyMMdd H:mm:ss" },
                { YEAR + MONTH + DAY + SPACE + Hs_M, "yyyyMMdd hh:mm tt" },
                { YEAR + MONTH + DAY + SPACE + Hs_M_S, "yyyyMMdd hh:mm:ss tt" },
                { YEAR + MONTH + DAY + SPACE + H1s_M, "yyyyMMdd h:mm tt" },
                { YEAR + MONTH + DAY + SPACE + H1s_M_S, "yyyyMMdd h:mm:ss tt" },
                //No space separator only makes sense if no other separators
                { YEAR + MONTH + DAY + HOUR + MINUTE, "yyyyMMddHHmm" },
                { YEAR + MONTH + DAY + HOUR + MINUTE + SECOND, "yyyyMMddHHmmss" },


                //{ YEAR + DASH + MONTH + DASH + DAY , "yyyy-MM-dd"},
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H_M, "yyyy-MM-dd HH:mm" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H_M_S, "yyyy-MM-dd HH:mm:ss" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H1_M, "yyyy-MM-dd H:mm" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H1_M_S, "yyyy-MM-dd H:mm:ss" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + Hs_M, "yyyy-MM-dd hh:mm tt" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + Hs_M_S, "yyyy-MM-dd hh:mm:ss tt" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H1s_M, "yyyy-MM-dd h:mm tt" },
                { YEAR + DASH + MONTH + DASH + DAY + SPACE + H1s_M_S, "yyyy-MM-dd h:mm:ss tt" },

                //{ YEAR + SLASH + MONTH + SLASH + DAY , "yyyy/MM/dd"},
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H_M, @"yyyy/MM/dd HH:mm" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H_M_S, @"yyyy/MM/dd HH:mm:ss" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H1_M, @"yyyy/MM/dd H:mm" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H1_M_S, @"yyyy/MM/dd H:mm:ss" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + Hs_M, @"yyyy/MM/dd hh:mm tt" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + Hs_M_S, @"yyyy/MM/dd hh:mm:ss tt" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H1s_M, @"yyyy/MM/dd h:mm tt" },
                { YEAR + SLASH + MONTH + SLASH + DAY + SPACE + H1s_M_S, @"yyyy/MM/dd h:mm:ss tt" },

                //{ MONTH + SLASH + DAY + SLASH + YEAR , "MM/dd/yyyy"},
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H_M, @"MM/dd/yyyy HH:mm" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H_M_S, @"MM/dd/yyyy HH:mm:ss" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H1_M, @"MM/dd/yyyy H:mm" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H1_M_S, @"MM/dd/yyyy H:mm:ss" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + Hs_M, @"MM/dd/yyyy hh:mm tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + Hs_M_S, @"MM/dd/yyyy hh:mm:ss tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H1s_M, @"MM/dd/yyyy h:mm tt" },
                { MONTH + SLASH + DAY + SLASH + YEAR + SPACE + H1s_M_S, @"MM/dd/yyyy h:mm:ss tt" },

            };
            foreach (var kv in patternSet)
            {
                Regex r = new Regex($"^{kv.Key}$");
                if (r.IsMatch(DateString))
                {
                    Format = kv.Value;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Guess the format of a date string (No time component)
        /// </summary>
        /// <param name="DateString"></param>
        /// <param name="Format"></param>
        /// <returns></returns>
        public static bool GuessFormatDate(string DateString, out string Format)
        {
            Format = null;
            Dictionary<string, string> patternSet = new Dictionary<string, string>
            {
                //Year, but no century
                { YEAR_NO_CENTURY + MONTH + DAY, "yyMMdd"},
                { YEAR_NO_CENTURY + DASH + MONTH + DASH + DAY , "yy-MM-dd"},
                { YEAR_NO_CENTURY + SLASH + MONTH + SLASH + DAY , "yy/MM/dd"},
                { MONTH + SLASH + DAY + SLASH + YEAR_NO_CENTURY , "MM/dd/yy"},

                //Full Year
                { YEAR + MONTH + DAY, "yyyyMMdd"},
                { YEAR + DASH + MONTH + DASH + DAY , "yyyy-MM-dd"},
                { YEAR + SLASH + MONTH + SLASH + DAY , "yyyy/MM/dd"},
                { MONTH + SLASH + DAY + SLASH + YEAR , "MM/dd/yyyy"},

            };
            foreach (var kv in patternSet)
            {
                Regex r = new Regex($"^{kv.Key}$");
                if (r.IsMatch(DateString))
                {
                    Format = kv.Value;
                    return true;
                }
            }
            return false;
        }
    }
}
