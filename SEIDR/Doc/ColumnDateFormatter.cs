using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// A class to sugarcoat converting a consistent column to a DateTime. <para>Expected to be useful when you're not sure if a file will keep the same date format,
    /// if the date format changes depending on the column with the file, or if the c# code is expected to be reused for multiple, different data sources.</para>
    /// </summary>
    /// <remarks>It's technically possible for a junk column to be converted to a date. 
    /// <para>Ex: After finding the format for Col A to be mm-dd-yyyy, the string "ash04ketchum142014" should convert to 4/14/2014.</para>
    /// <para>Also to note, null and invisible characters should not impact date transformation AS LONG AS the *ENTIRE* date string can be read.</para>    
    /// </remarks>
    public class ColumnDateFormatter
    {    
        private object _FormatSetLock = new object();
        /// <summary>
        /// Set to true to prevent successful conversion of outstanding dates when calling ParseString. Default: False. 
        /// <para>E.g. 3/24/1825 would have a valid datetime out, but would return false instead of true.</para>
        /// </summary>
        /// <remarks>
        /// The requirements for failing sanity check are that it would be at least 100 years back from runtime, or at least 150 years into the future from runtime.
        /// <para>It's false by default since the usefulness of this really depends on what you're doing.</para>
        /// </remarks>
        public bool checkSanity = false;
        private string[] _formats;
        private string[] _formatNames;
        private int _size;
        /// <summary>
        /// Return the number of columns contained in the ColumnDateFormatter. Columns and the number of columns cannot be changed without recreating the ColumnDateFormatter. 
        /// <para>Columns CAN be renamed using the indexer and their index though.</para><para> E.g., df["pat"] -> 0. df[0] = "pie" -> . df["pat"] -> -1. df["pie"] -> 0</para>
        /// </summary>
        public int size { get { return _size; } }
        private const string invalidDate = "NOTVALID";
        private string years; //initialzed in constructor
        private char[] separators;
        private string vals = "-/-";
        /// <summary>
        /// Creates a new date formatter.
        /// </summary>
        /// <param name="size">This should be the number of date columns you're expecting to exist.</param>
        public ColumnDateFormatter(int size)
        {
            _size = size;
            _formats = new string[size];
            _formatNames = new string[size];
            int cc = DateTime.Today.Year / 100; //only get the century.		
            years = (cc - 1) + "|" + cc + "|" + (cc + 1); //centure -1 | cc | century plus 1. E.g. (19|20|21)
            separators = "".PadLeft(size).ToCharArray();
        }
        /// <summary>
        /// Create a new ColumnDateFormatter using an array of column names.
        /// </summary>
        ///<param name="names">(Params) String array containing ALL names of columns expected to be dates. 
        ///Can be passed as just a number of string arguments if you don't have a lot of them</param>
        public ColumnDateFormatter(params string[] names)
        {
            _formats = new string[names.Length];
            _size = names.Length;
            _formatNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                _formatNames[i] = names[i];
            }
            int cc = DateTime.Today.Year / 100; //only get the century.		
            years = (cc - 1) + "|" + cc + "|" + (cc + 1); //centure -1 | cc | century plus 1. E.g. (19|20|21)
            separators = "".PadLeft(names.Length).ToCharArray();
        }
        /// <summary>
        /// Returns the column index within the ColumnDateFormatter of the given column's name. Not case sensitive
        /// </summary>
        /// <param name="column">Column you want the index of.</param>
        /// <returns>integer index of the column</returns>
        public int this[string column]
        {
            get
            {
                for (int i = 0; i < _formatNames.Length; i++)
                {
                    if (_formatNames[i].ToUpper() == column.ToUpper())
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
        /// <summary>
        /// Get: Return the name of the column at that index. Set: Change the name of a column using it's index.
        /// </summary>
        /// <param name="column">Index</param>
        /// <returns>Empty string if no name has been set, else the name associated with a column</returns>
        public string this[int column]
        {
            get
            {
                return _formatNames[column] == null ? "" : _formatNames[column];
            }
            set
            {
                _formatNames[column] = value;
            }

        }
        private string GetSeparatorRegex()
        {
            string result = "";
            foreach (char s in vals)
            {
                result = result + "|" + s;
            }
            return result;
        }
        /// <summary>
        /// Attempts to return the format for the given column. If unable to find the column using the given name, "Column not found" is returned instead.
        /// </summary>
        /// <param name="columnName">Name of the column</param>
        /// <returns>The format for the column to use when parsing the format.</returns>
        public string getFormat(string columnName)
        {
            for (int i = 0; i < _formatNames.Length; i++)
            {
                if (_formatNames[i].ToUpper() == columnName.ToUpper())
                {
                    return getFormat(i);
                }
            }
            return "Column not found";
        }
        /// <summary>
        /// Attempts to return the format for the given column index. If unable to find the column using the given name, "Column not found" is returned instead.
        /// </summary>
        /// <param name="column">Index of the column in the ColumnDateFormatter. Can be obtained using an indexer if you're not sure.</param>
        /// <returns>Format for the column to use when parsing the format</returns>
        public string getFormat(int column)
        {
            string s = _formats[column];
            if (s == null)
                return invalidDate;
            if (s.IndexOf("y") == 0)
            {
                if (separators[column] != ' ')
                {
                    s = s.Substring(0, 4) + separators[column]
                        + s.Substring(4, 2) + separators[column]
                        + s.Substring(6);
                }
            }
            else
            {
                if (separators[column] != ' ')
                {
                    s = s.Substring(0, 2) + separators[column]
                        + s.Substring(2, 2) + separators[column]
                        + s.Substring(4);
                }
            }
            return s;
        }
        /// <summary>
        /// Takes a string and attempts to find the format to use in ParseString
        /// </summary>
        /// <param name="original">String to parse format from</param>
        /// <param name="column">Column the string is in.</param>
        /// <returns>True if we are able to parse a date from this string. False otherwise</returns>
        public bool ParseFormat(string original, int column)
        {
            _formats[column] = invalidDate;
            if (original.ToUpper() != original.ToLower())
            {
                //contains alphabet characters
                //_formats[column] = invalidDate;
                return false;
            }

            string[] expressions = {
        @"[0-1]?\d("+GetSeparatorRegex()+ @"){1}?[0-3]?\d("+GetSeparatorRegex()+ @"){1}?("+years + @")\d\d", //MM dd yyyy,
		@"("+years + @")\d\d("+GetSeparatorRegex()+ @"){1}?[0-1]?\d("+GetSeparatorRegex()+ @"){1}?[0-3]?\d", // ccyyy MM dd
		@"[0-1]?\d("+GetSeparatorRegex()+ @"){1}?[0-3]?\d("+GetSeparatorRegex()+ @"){1}?\d\d" , // MM dd yy 
		@"("+ years+@")[0-1]{1}?\d[0-3]?\d" //yyyy MM dd
		};
            string[] map = {
        "MMddyyyy",
        "yyyyMMdd",
        "MMddyy",
        "yyyyMMdd"
        };
            original = original.Trim();
            System.Text.RegularExpressions.Match check;
            //System.Windows.Forms.MessageBox.Show("Value being checked:" + original);
            bool success = false;
            for (int i = 0; i < expressions.Length; i++)
            {
                string exp = expressions[i];
                check = System.Text.RegularExpressions.Regex.Match(original, exp);
                //make sure it finds the expression in original and that the whole thing is the expression.
                if (check.Success && check.Value == original)
                {
                    _formats[column] = map[i];
                    success = true;
                    foreach (char s in vals)
                    {
                        if (original.IndexOf(s) >= 0)
                        {
                            separators[column] = s;
                            break;
                        }
                    }
                }
            }

            return success;

        }
        /// <summary>
        /// Calls on ParseString(int column, string value, out DateTime result) after finding the index of the column Name
        /// </summary>
        /// <param name="columnName">Name of column</param>
        /// <param name="value">Value to parse</param>
        /// <param name="result">Value converted to a DateTime</param>
        /// <returns>True if conversion to date succeeded.</returns>
        public bool ParseString(string columnName, string value, out DateTime result)
        {
            int c = this[columnName];
            return ParseString(c, value, out result);
        }
        /// <summary>
        /// Attempts to parse the passed value using the given column's format.
        /// </summary>
        /// <param name="column">Index of column in ColumnDateFormatter</param>
        /// <param name="value">Value to parse</param>
        /// <param name="result">Value converted to a DateTime</param>
        /// <returns>True if conversion to date succeeded. 
        /// <para>If CheckSanity is true, then failing the sanity check will cause this to return false even if the conversion to date succeeded</para>         
        /// </returns>
        public bool ParseString(int column, string value, out DateTime result)
        {

            if (_formats[column] == null)
            {
                lock (_FormatSetLock)
                {
                    if (_formats[column] == null)
                    {
                        ParseFormat(value, column);
                    }
                }
            }
            if (_formats[column] == invalidDate) { result = DateTime.Today; return false; }
            //System.Windows.Forms.MessageBox.Show("Value before replace:" + value);
            string[] padder = value.Split(separators[column]);

            if (padder.Length > 2 && _formats[column][0] == 'y')
            {
                padder[1] = padder[1].PadLeft(2, '0');
                padder[2] = padder[2].PadLeft(2, '0');
            }
            else if (padder.Length > 2)
            {
                padder[0] = padder[0].PadLeft(2, '0');
                padder[1] = padder[1].PadLeft(2, '0');
            }
            value = string.Join("", padder); //use this to make sure that stuff is long enough and to get rid of the separator at the same time instead of regex.
                                                //value = System.Text.RegularExpressions.Regex.Replace(value, @"[^0-9]", "");
                                                //System.Windows.Forms.MessageBox.Show("Value after replace:" + value);
                                                //remove any non numbers. We've already confirmed that they should only appear in between by parseFormat 
                                                //Should consider add continuing checks on the date parseformat just in case it passes by accident
            bool success = DateTime.TryParseExact(value, _formats[column], new System.Globalization.CultureInfo("EN-US"), System.Globalization.DateTimeStyles.None, out result);
            if (checkSanity)
            {
                //reduce work if we just want it converted to datetime no matter what.
                DateTime sanityCheck = new DateTime(DateTime.Today.Year - 150, 1, 1); //YYYY, MM, DD
                DateTime sanityCheck2 = new DateTime(DateTime.Today.Year + 100, 1, 1);
                if (result.CompareTo(sanityCheck) < 0 || result.CompareTo(sanityCheck2) > 0)
                    success = false;
            }
            return success;
        }

        /// <summary>
        /// Creates a local ColumnDateFormatter to run a single check on the date and then be deleted via leaving scope.
        /// </summary>
        /// <param name="value">String we want to parse</param>
        /// <param name="result">Output date</param>
        /// <param name="SanityCheck">If true, includes Sanity check</param>
        /// <returns>True if the result is a valid datetime</returns>
        public static bool ParseOnce(string value, out DateTime result, bool SanityCheck = true)
        {
            ColumnDateFormatter d = new ColumnDateFormatter(1);
            d.checkSanity = SanityCheck;
            return d.ParseString(0, value, out result);
        }
    }
}
