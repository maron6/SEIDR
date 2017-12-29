using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SEIDR.Test
{
    [TestClass]
    public class StringExtensionTest
    {
        [TestMethod]
        public void SplitWithQualifiers()
        {
            string[] Expected = new[]
            {
                "Tes", "Forward,Value", "Value|x", "x"
            };
            string test1 = "Tes,{Forward,Value},Value|x,x";
            string test2 = "Tes,'Forward,Value',Value|x,x";
            string test3 = "Tes|Forward,Value|{Value|x}|x";
            string test4 = "Tes|Forward,Value|'Value|x'|x";
            List<string> value;
            Action<string, string, string, string> action = (s, d, l, r) =>
                {
                    value = s.SplitByQualifier(d, l, r);
                    Assert.AreEqual(value.Count, Expected.Length);
                    for (int i = 0; i < Expected.Length; i++)
                    {
                        Assert.AreEqual( Expected[i], value[i]);
                    }
                };
            action(test1, ",", "{", "}");
            action(test2, ",", "'", "'");
            action(test3, "|", "{", "}");
            action(test4, "|", "'", "'");

            Expected = new[] { "Database", "test;" };
            string test = "Database={test;}";
            action(test, "=", "{", "}");
        }
        [TestMethod]
        public void SplitOnString()
        {
            string[] Expected = new[]
            {
                "Test", "Forward", "Value", "x"
            };
            string test1 = "Test_#Forward_#Value_#x";
            string test2 = "TestKMNForwardKMNValueKMNx";
            string test3 = "Test\0Forward\0Value\0x";
            Action<string, string> act = (val, key) =>
            {
                var l = val.SplitOnString(key);
                Assert.AreEqual(Expected.Length, l.Count);
                l.ForEachIndex((s, i) =>
                {
                    Assert.AreEqual(Expected[i], s);
                });
            };
            act(test1, "_#");
            act(test2, "KMN");
            act(test3, '\0'.ToString());
            /*
            var l = test1.SplitOnString("_#");
            Assert.AreEqual(Expected.Length, l.Count);
            for(int i = 0; i < Expected.Length; i++)
            {
                Assert.AreEqual(Expected[i], l[i]);
            }            
            l = test2.SplitOnString("KMN");
            Assert.AreEqual(Expected.Length, l.Count);
            for(int i= 0; i < Expected.Length; i++)
            {
                Assert.AreEqual(Expected[i], l[i]);
            }
            l = test3.SplitOnString('\0'.ToString());
            Assert.AreEqual(Expected.Length, l.Count);
            for(int i = 0; i < Expected.Length; i++)
            {
                Assert.AreEqual(Expected[i], l[i]);
            }*/
        }

        [TestMethod]
        public void SplitOnKeywords()
        {
            string[] expected = new[]
            {
                "Test", "Value", "Something"
            };
            string[] Keywords = new[]
            {
                "Woop", "Var"
            };
            string[] ExpectedInclude = new[]
            {
                "Test", "Woop", "Value", "Var", "Something"
            };
            string Test = "TestWoopValueVarSomething";
            var match = Test.SplitByKeyword(Keywords);
            match.ForEachIndex((s, i) =>
            {
                Assert.AreEqual(expected[i], s);
            });
            match = Test.SplitByKeyword(Keywords, true);
            match.ForEachIndex((s, i) =>
            {
                Assert.AreEqual(ExpectedInclude[i], s);
            });
        }

        [TestMethod]
        public void SplitOnRelatedKeywords()
        {
            string[] expected = new[]
            {
                "Something", "!=", "Something else"
            };
            string[] kw = new[]
            {
                "!=", "="
            };
            string[] kwReverse = new[]
            {
                "=", "!="
            };
            string test = "Something!=Something else";
            var match = test.SplitByKeyword(kw, true);
            match.ForEachIndex((s, i) =>
            {
                Assert.AreEqual(expected[i], s);
            });
            match = test.SplitByKeyword(kwReverse, true);
            match.ForEachIndex((s, i) =>
            {
                Assert.AreEqual(expected[i], s);
            });
            match = test.SplitByKeyword("!=", true);
            match.ForEachIndex((s, i) =>
            {
                Assert.AreEqual(expected[i], s);
            });
        }
    }
}
