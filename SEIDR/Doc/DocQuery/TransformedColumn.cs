using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc.DocQuery
{   
    public class TransformedColumn
    {        
        public static bool? Evaluate(TransformedColumn left, TransformedColumn right, ConditionType comparison)
        {
            if (left.Content.IsNull || right.Content.IsNull)
                return null;
            switch (comparison)
            {
                case ConditionType.EQUAL:
                    return left.Content.Equal(right.Content);
            }
            return null;
        }
        public TransformedColumn(TransformedColumnMetaData metaData, TransformedData data)
        {
            MetaData = 
                metaData 
                ?? new TransformedColumnMetaData
            {
                    Type = DataType.DBNULL
            };
            Content = data ?? TCNull.Value;
        }
        public TransformedColumnMetaData MetaData { get; private set; }
        public TransformedData Content { get; private set; }
        /// <summary>
        /// Check if the column is valid for comparisons.
        /// </summary>
        public bool ValidContent
        {
            get { return Content.IsNull || Content.Type == MetaData.Type; }
        }
        /// <summary>
        /// Check if the column is null for the content.
        /// </summary>
        public bool IsNull => Content.IsNull;
        
    }
    class TCNull : TransformedData
    {
        public static readonly TCNull Value = new TCNull();
        #region TCNULL shell
        private TCNull(DataType d) : base(DataType.DBNULL)
        {

        }
        private TCNull() : base(DataType.DBNULL) { }

        public override bool? Equal(TransformedData b)
        {
            return null;
        }

        public override bool? Greater(TransformedData b)
        {
            return null;
        }

        public override bool? GreaterEqual(TransformedData b)
        {
            return null;
        }

        public override bool? Less(TransformedData b)
        {
            return null;
        }

        public override bool? LessEqual(TransformedData b)
        {
            return null;
        }

        public override bool? NotEqual(TransformedData b)
        {
            return null;
        }
        #endregion
    }
    public abstract class TransformedData
    {        
        public DataType Type { get; protected set; } = DataType.DBNULL;
        public TransformedData(DataType d) { Type = d; }        
        public bool IsNull { get { return Type == DataType.DBNULL; } }
        public abstract bool? Equal(TransformedData b);
        public abstract bool? NotEqual(TransformedData b);
        public abstract bool? Greater(TransformedData b);
        public abstract bool? GreaterEqual(TransformedData b);
        public abstract bool? Less(TransformedData b);
        public abstract bool? LessEqual(TransformedData b);
        protected static bool NiX(TransformedData a, TransformedData b)
        {
            if (a.IsNull || b.IsNull)
                return false;
            return a.Type == b.Type;
        }
    }
    public class TransformedDate : TransformedData
    {
        #region Basic Transformations
        public static TransformedData BasicTransform(string content)
        {
            DateTime x;
            if (!DateTime.TryParse(content, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out x))
                return new TransformedDate(null);
            return new TransformedDate(x);
        }
        public static TransformedDate BasicTransform(string content, string Format)
        {
            DateTime x;
            if (!DateTime.TryParseExact(content, Format, null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out x))
                return new TransformedDate(null);
            return new TransformedDate(x);
        }
        public static TransformedDate BasicTransformAddDays(string content, string Format, int days)
        {
            var c = BasicTransform(content, Format);
            if (!c.IsNull)
                c.dt = c.dt.AddDays(days);
            return c;
        }
        public static TransformedDate BasicTransformAddMinutes(string content, string Format, int minutes)
        {
            var c = BasicTransform(content, Format);
            if (!c.IsNull)
                c.dt = c.dt.AddMinutes(minutes);
            return c;
        }
        public static TransformedDate BasicTransformAddWeeks(string content, string Format, int weeks)
        {
            var c = BasicTransform(content, Format);
            if (!c.IsNull)
                c.dt = c.dt.AddDays(weeks * 7);
            return c;
        }
        public static TransformedDate BasicTransformAddMonths(string content, string Format, int months)
        {
            var c = BasicTransform(content, Format);
            if (!c.IsNull)
                c.dt = c.dt.AddMonths(months);
            return c;
        }
        public static TransformedDate BasicTransformAddYears(string content, string Format, int Years)
        {
            var c = BasicTransform(content, Format);
            if (!c.IsNull)
                c.dt = c.dt.AddYears(Years);
            return c;
        }
        #endregion
        DateTime dt;    
        public TransformedDate(DateTime? content) : base(content.HasValue? DataType.DATE : DataType.DBNULL)
        {
            if (!IsNull)
                dt = content.Value;
        }
        public DateTime? Content
        {
            get
            {
                if (IsNull)
                    return null;
                return new DateTime(dt.Year, dt.Month, dt.Day);
            }
        }
        public override bool? Equal(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt == (b as TransformedDate)?.dt;
        }

        public override bool? Greater(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt > (b as TransformedDate)?.dt;
        }

        public override bool? GreaterEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt >= (b as TransformedDate)?.dt;
        }

        public override bool? Less(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt < (b as TransformedDate)?.dt;
        }

        public override bool? LessEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt <= (b as TransformedDate)?.dt;
        }

        public override bool? NotEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return dt != (b as TransformedDate)?.dt;
        }
    }
    /// <summary>
    /// Decimal content
    /// </summary>
    public class TransformedMoney: TransformedData
    {
        #region basic Transformations
        public static TransformedMoney BasicAdd(string content, decimal constant)
        {
            decimal x;
            if (!decimal.TryParse(content, out x))
                return new TransformedMoney(null);
            return new TransformedMoney(x + constant);
        }
        public static TransformedMoney BasicTransform(string content) => BasicAdd(content, 0);
        public static TransformedMoney BasicMultiply(string content, int constant)
        {
            decimal x;
            if (!decimal.TryParse(content, out x))
                return new TransformedMoney(null);
            return new TransformedMoney(x * constant);
        }
        public static TransformedMoney BasicDivide(string content, int constant)
        {
            decimal x;
            if (!decimal.TryParse(content, out x))
                return new TransformedMoney(null);
            return new TransformedMoney(x / constant);
        }
        #endregion

        decimal content;
        public TransformedMoney(decimal? content) 
            :base(content.HasValue ? DataType.MONEY : DataType.DBNULL)
        {
            if (!IsNull)
                content = content.Value;
        }
        public decimal? Content => IsNull ? (decimal?) null : content;
        public override bool? Equal(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content == (b as TransformedMoney)?.content;
        }

        public override bool? Greater(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content > (b as TransformedMoney)?.content;
        }

        public override bool? GreaterEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content >= (b as TransformedMoney)?.content;
        }

        public override bool? Less(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content < (b as TransformedMoney)?.content;
        }

        public override bool? LessEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content <= (b as TransformedMoney)?.content;
        }

        public override bool? NotEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content != (b as TransformedMoney)?.content;
        }
    }
    /// <summary>
    /// int 64 content
    /// </summary>
    public class TransformedNum : TransformedData
    {
        #region basic transform
        public static TransformedNum BasicTransform(string content)
        {
            long x;
            if (!long.TryParse(content, out x))
                return new TransformedNum(null);
            return new TransformedNum(x);
        }
        public static TransformedNum BasicTransformAdd(string content, long value)
        {
            TransformedNum n = BasicTransform(content);
            if (n.IsNull)
                return n;
            n.content += value;
            return n;
        }
        public static TransformedNum BasicTransformMultiply(string content, int mult)
        {
            TransformedNum n = BasicTransform(content);
            if (n.IsNull)
                return n;
            n.content *= mult;
            return n;
        }
        public static TransformedNum BasicTransformDivide(string content, int div)
        {
            TransformedNum n = BasicTransform(content);
            if (n.IsNull)
                return n;
            n.content /= div;
            return n;
        }
        #endregion
        long content;
        public TransformedNum(long? content) : base(content.HasValue? DataType.NUMBER : DataType.DBNULL)
        {
            if (!IsNull)
                this.content = content.Value;
        }
        public override bool? Equal(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content == (b as TransformedNum)?.content;
        }
        public long? Content
        {
            get
            {
                if (IsNull)
                    return null;
                return content;
            }
        }
        public override bool? Greater(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content > (b as TransformedNum)?.content;
        }

        public override bool? GreaterEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content >= (b as TransformedNum)?.content;
        }

        public override bool? Less(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content < (b as TransformedNum)?.content;
        }

        public override bool? LessEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content <= (b as TransformedNum)?.content;
        }

        public override bool? NotEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return content != (b as TransformedNum)?.content;
        }
    }

    public class TransformedVarchar : TransformedData
    {
        public static TransformedVarchar BasicTransform(string content)
            => BasicTransform(content, false);
        public static TransformedVarchar BasicTransform(string content, bool Trim)
        {
            if (Trim)
                content = content?.Trim();
            return new TransformedVarchar(content);
        }
        public string Content { get; private set; }
        public TransformedVarchar(string content) 
            : base(!string.IsNullOrEmpty(content) ? DataType.VARCHAR : DataType.DBNULL)
        {
            if (!IsNull)
                Content = content;
        }
        public override bool? Equal(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return Content == (b as TransformedVarchar)?.Content;
        }

        public override bool? Greater(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return CheckChars(Content) > CheckChars((b as TransformedVarchar)?.Content);
        }
        long CheckChars(string x)
        {
            long value = long.MinValue;
            if (x == null)
                return value;
            foreach (char a in x)
                value += a;
            return value;
        }
        public override bool? GreaterEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return CheckChars(Content) >= CheckChars((b as TransformedVarchar)?.Content);
        }

        public override bool? Less(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return CheckChars(Content) < CheckChars((b as TransformedVarchar)?.Content);
        }

        public override bool? LessEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return CheckChars(Content) <= CheckChars((b as TransformedVarchar)?.Content);
        }

        public override bool? NotEqual(TransformedData b)
        {
            if (NiX(this, b))
                return null;
            return Content != (b as TransformedVarchar)?.Content;
        }
    }
}
