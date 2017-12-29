using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SEIDR.FileSystem
{
    partial class FS
    {
		public string ApplyDateMask(string SourceFile, DateTime originalDate)
        {
            //DateTime d = originalDate;
            Match m = Regex.Match(SourceFile.ToUpper(), @"<[\-+]\d+[MDY]>");
            while(m.Success){
                string temp = m.Value;
                SourceFile = SourceFile.Replace(temp, "");
                int offset = 0;
                if (temp.Contains("M"))
                {//Month offset
                    temp = temp.Substring(0, temp.IndexOf("M"));
                    offset = Convert.ToInt32(temp.Substring(2));
                    if (temp[1] == '-')
                    {
                        offset = offset * -1;
                    }
                    originalDate = originalDate.AddMonths(offset);
                }
                else if (temp.Contains('Y'))
                {//Year offset
                    temp = temp.Substring(0, temp.IndexOf("Y"));
                    offset = Convert.ToInt32(temp.Substring(2));
                    if (temp[1] == '-')
                    {
                        offset = offset * -1;
                    }
                    originalDate = originalDate.AddYears(offset);
                }
                else
                {//Day
                    temp = temp.Substring(0, temp.IndexOf("D"));
                    offset = Convert.ToInt32(temp.Substring(2));
                    if (temp[1] == '-')
                    {
                        offset = offset * -1;
                    }
                    originalDate = originalDate.AddDays(offset);
                }
            }            
            SourceFile = SourceFile
                .Replace("<YYYY>", originalDate.Year.ToString())
                .Replace("<YY>", originalDate.Year.ToString().Substring(2, 2))
                .Replace("<MM>", originalDate.Month.ToString().PadLeft(2, '0'))
                .Replace("<M>", originalDate.Month.ToString())
                .Replace("<DD>", originalDate.Day.ToString().PadLeft(2, '0'))
                .Replace("<D>", originalDate.Day.ToString());
            return SourceFile;
        }
    }
}
