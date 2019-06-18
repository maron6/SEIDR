using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// For some methods in DocRecord, and derived classes, to specify type of data. 
    /// <para>Mainly for formatting purposes when writing, but also some being able to parse to a consistent format based on <see cref="DocRecordColumnInfo.Format"/>
    /// </para>
    /// </summary>
    public enum DocRecordColumnType
    {
        /// <summary>
        /// For BSON. Should not be used for actual column info.
        /// </summary>
        NUL = 0,
        /// <summary>
        /// Default type. Essentially treated as Varchar, but indicates that the type has not been set.
        /// </summary>
        Unknown,
        /// <summary>
        /// Variable character
        /// </summary>
        Varchar,
        /// <summary>
        /// NonVariable characters, indicates length should be constant (padded if needed). Use with <see cref="DocRecordColumnInfo.MaxLength"/>
        /// </summary>
        NVarchar,
        Tinyint,
        Smallint,
        Int,
        Bigint,
        /// <summary>
        /// Parses the string value of a record/column into a DateTime object, using the column's format. 
        /// <para>If a format is not set, then <see cref="DateConverter.GuessFormats(DocRecordColumnCollection, IRecord)"/> will be used and attempt to set the format.</para>
        /// </summary>
        DateTime,
        /// <summary>
        /// Similar to DateTime, but no time component expected
        /// </summary>
        Date,
        /// <summary>
        /// Gets the value of a record/column and parse it to a decimal value.
        /// </summary>
        Decimal,
        Double,
        /// <summary>
        /// Money Type - parse to decimal after removal of all non-numeric values. Will be treated as negative if a '-' occurs anywhere in the value, otherwise positive.
        /// <para>Does not perform any validations - if you want to perform custom validations, get the underlying string value instead of using <see cref="DocRecord.Evaluate{T}(string, string, bool)"/></para>
        /// </summary>
        Money,
        Bool, 
    }
}
