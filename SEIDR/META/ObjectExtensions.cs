using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.META
{
    using System.Data;
    using System.Reflection;

    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts the IEnumerable of objects to a dataTable with the specified name.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="Name"></param>
        /// <param name="propertyList"></param>
        /// <returns></returns>
        public static DataTable ToTable(this IEnumerable<object> obj, string Name, params string[] propertyList)
        {
            Type t = obj.GetType();
            DataTable dt = new DataTable { TableName = Name };
            PropertyInfo[] props = t.GetProperties();
            props = props.Where(p => p.CanRead.AndNot(p.PropertyType.IsArray)).ToArray();
            if (props.Length == 0)
                throw new ArgumentException("Object of type '" + t.Name + "' does not contain any useable properties.");            
            if (propertyList.Length > 0)
                props = props.Where(p => p.Name.In(propertyList).And(p.CanRead)).ToArray();
            if (props.Length == 0)
                throw new ArgumentOutOfRangeException("propertyList limits properties too much - No properties remain to add to the Table");
            Dictionary<string, MethodInfo> sa = new Dictionary<string, MethodInfo>();
            foreach(var prop in props)
            {
                sa.Add(prop.Name, prop.GetMethod);
                Type nbt = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                dt.Columns.Add(prop.Name, nbt);
            }
            foreach(object o in obj)
            {
                DataRow r = dt.NewRow();
                foreach(var kv in sa)
                {
                    r[kv.Key] = kv.Value.Invoke(o, null);
                }
                dt.Rows.Add(r);
            }
            return dt;
        }


        /// <summary>
        /// Converts the datarow into an instance of type RT. Requires a parameterless constructor
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RT CreateInstance<RT>(DataRow source) where RT : new()
        {
            Func<string, bool> check = (n => source.Table.Columns.Contains(n));
            RT rv = new RT();
            Type t = rv.GetType();
            foreach (var prop in t.GetProperties() )
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
        /// Creates a DataRowView from the given record.
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
            dt.Rows.Add(dt.NewRow());
            return dt.AsDataView()[0];
        }
    }
}
