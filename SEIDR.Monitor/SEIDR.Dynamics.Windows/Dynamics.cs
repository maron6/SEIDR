using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SEIDR.Dynamics
{
    /// <summary>
    /// Helper code for dynamic datarow manipulation
    /// </summary>
    public static class Dynamics
    {
        /// <summary>
        /// Converts the datarow into an instance of type RT
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RT CreateInstance<RT>(DataRow source) where RT : new()
        {
            Func<string, bool> check = (n => source.Table.Columns.Contains(n));
            RT rv = new RT();
            Type t = rv.GetType();
            foreach (var prop in t.GetProperties())
            {
                if (prop.CanWrite && check(prop.Name))
                {
                    prop.SetValue(rv, source[prop.Name]);
                }
            }
            return rv;
        }
        /// <summary>
        /// Creates a datarow from the given record
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="Record"></param>
        /// <returns></returns>
        public static DataRow CreateDataRow<RT>(RT Record)
        {
            return CreateDataRowView(Record).Row;
        }
        /// <summary>
        /// Creates a DataRowView from the given record
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="Record"></param>
        /// <returns></returns>
        public static DataRowView CreateDataRowView<RT>(RT Record)
        {
            DataTable dt = new DataTable();
            var props = Record.GetType().GetProperties();
            foreach (var prop in props)
            {
                DataColumn c = new DataColumn(prop.Name, prop.PropertyType);
                try
                {
                    c.DefaultValue = prop.GetValue(Record, null);
                }
                finally
                {
                    dt.Columns.Add(c);
                }
            }
            dt.NewRow();
            return dt.AsDataView()[0];
        }
    }
}
