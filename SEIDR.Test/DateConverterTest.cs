using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Test
{
    [TestClass]
    public class DateConverterTest
    {
        [TestMethod]
        public void TestFormats()
        {
            var Tests = new Dictionary<string, string>
            {
                { "09/16/2019", "MM/dd/yyyy" },
                {"20190604 18:01", "yyyyMMdd HH:mm" },
                {"2019-06-04 18:01:20", "yyyy-MM-dd HH:mm:ss" },
                {"19-05-19", "yy-MM-dd" },
                {"19-05-19 04:20", "yy-MM-dd HH:mm" },
                { "2019/06/04 04:21 AM", "yyyy/MM/dd hh:mm tt" },
                { "09/04/23 08:26:01 PM", "yy/MM/dd hh:mm:ss tt" },
                { "09/04/23 8:26:01 AM", "yy/MM/dd h:mm:ss tt" }
            };
            foreach(var testCase in Tests)
            {
                string fmt;
                DateTime dt;
                bool b = SEIDR.Doc.DateConverter.GuessFormatDateTime(testCase.Key, out fmt);
                Assert.IsTrue(b);
                Assert.AreEqual(testCase.Value, fmt);
                Assert.IsTrue(
                    DateTime.TryParseExact(testCase.Key, 
                                            fmt, 
                                            System.Globalization.CultureInfo.InvariantCulture, 
                                            System.Globalization.DateTimeStyles.None, 
                                            out dt));
                Assert.AreNotEqual(default, dt);
            }
        }

    }
}
