using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public class MissingColumnException:Exception
    {
        public int ExpectedCount { get; private set; }
        public int LastColumn { get; private set; }
        public MissingColumnException(int lastColumn, int expectedLastColumn)
            :base("Missing Columns encountered")
        {
            ExpectedCount = expectedLastColumn;
            LastColumn = lastColumn;
        }
    }
    public class ColumnOverflowException :Exception
    {
        public int ExpectedCount { get; private set; }
        public int FoundColumnCount { get; private set; }
        public bool FixedWidth { get; private set; } = false;
        public int RemainingCharacters { get; private set; } = -1;
        public int RecordLength { get; private set; } = -1;
        public ColumnOverflowException(int foundColumnCount, int expectedColumnCount)
            :base("Found too many delimited Columns. Expected " + expectedColumnCount + "; Found " + foundColumnCount)
        {
            ExpectedCount = expectedColumnCount;
            FoundColumnCount = foundColumnCount;
        }
        public ColumnOverflowException(int remainingCharacters, int ExpectedColumnCount, int RecordLength)
            :base("Too many characters in record. Expected " + (RecordLength - remainingCharacters) + " characters, found "
                 + RecordLength + ". Remaining Characters: " + remainingCharacters)
        {
            ExpectedCount = ExpectedColumnCount;
            FoundColumnCount = ExpectedColumnCount + 1;
            RemainingCharacters = remainingCharacters;
            this.RecordLength = RecordLength;
            FixedWidth = true;
        }
    }
}
