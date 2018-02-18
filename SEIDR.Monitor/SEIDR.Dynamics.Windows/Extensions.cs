using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using SEIDR.Dynamics.Configurations;
using System.ComponentModel;

namespace SEIDR.Dynamics
{
    /// <summary>
    /// Untested dynamic extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Clone via XML serialization. Public properties that can be written to and are not marked with XML Ignore only.
        /// <para>For use until DClone is figured out for deep cloning</para>
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RT XClone<RT>(this RT source)
        {
            return source.SerializeToXML().DeserializeXML<RT>();            
        }
        
        
        private static object d_clone(this object source)
        {
            Type t = source.GetType();
            object rv;
            try
            {
                rv = Activator.CreateInstance(t);
            }
            catch
            {
                return null; //No default constructor....
            }
            if (t.IsPrimitive || t.IsEnum || t == typeof(string))
                return source;
            if (t.IsArray)
            {
                Type t2 = t.GetElementType();
                Array sArr = source as Array;
                Array rvArr = Array.CreateInstance(t2, sArr.Length);
                for(int i = 0; i < rvArr.Length; i++)
                {
                    rvArr.SetValue(d_clone(sArr.GetValue(i)), i);
                }
                return rvArr;
            }            
            if(t.IsClass || t.IsValueType)
            {
                var props = t.GetProperties();
                foreach(var prop in props)
                {
                    if (!prop.CanWrite || !prop.CanRead)
                        continue;
                    object check = prop.GetValue(source);
                    if (check == null)
                        continue; //don't need to set property if it's going to go to null anyway
                    prop.SetValue(rv, d_clone(check));
                }
                var fields = t.GetFields();
                foreach(var field in fields)
                {
                    if (field.IsLiteral || field.IsStatic)
                        continue;
                    object check = field.GetValue(source);
                    if (check == null)
                        continue;
                    field.SetValue(rv, d_clone(check));
                }
            }
            
            return rv;
        }
        /// <summary>
        /// Clones the source to populate a new, separate instance variable. Properties/fields for classes without default constructors may be null.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RT DClone<RT>(this RT source)
        {
            Type t = source.GetType();
            RT rv = (RT)Activator.CreateInstance(t);
            var props = t.GetProperties();
            foreach(var prop in props)
            {
                if (prop.CanWrite && prop.CanWrite)
                {
                    var check = prop.GetValue(source);
                    if (check == null)
                        continue;
                    prop.SetValue(rv, d_clone(check));
                    /*
                    if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                        prop.SetValue(rv, prop.GetValue(source));
                    else
                        prop.SetValue(rv, prop.GetValue(source).DClone());
                        */
                }
            }
            var fields = t.GetFields();
            foreach(var field in fields)
            {
                if (field.IsLiteral || field.IsStatic)
                    continue;
                //if (field.IsInitOnly)
                //    continue;                     
                var check = field.GetValue(source);
                if (check == null)
                    continue;
                field.SetValue(rv, d_clone(check));
                /*
                if (field.FieldType.IsValueType ||field.FieldType == typeof(string))
                    field.SetValue(rv, field.GetValue(source));
                else
                    field.SetValue(rv, field.GetValue(source).DClone()); //If not a value type, d Clone.

                */
            }
            return rv;
        }
        /// <summary>
        /// Performs an action inside of a try catch and ignores the error. Should not be used unless repeatedly doing some function where it does
        /// not matter if it fails or not.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="o"></param>
        /// <param name="functionToRunWithoutErrorCatching"></param>
        public static void WithoutErrorHandling<RT>(this RT o, Action<RT> functionToRunWithoutErrorCatching)
        {
            try
            {
                functionToRunWithoutErrorCatching(o);
            }
            catch { }
        }
        /// <summary>
        /// Performs an action inside of a try catch and ignores the error. Should not be used unless repeatedly doing some function where it does
        /// not matter if it fails or not.
        /// </summary>
        /// <param name="functionWithoutCatch"></param>
        public static void WithoutErrorHandling(this Action functionWithoutCatch)
        {
            try { functionWithoutCatch(); }
            catch { }
        }
        /// <summary>
        /// Performs an action inside of a try catch and ignores the error. Should not be used unless repeatedly doing some function where it does
        /// not matter if it fails or not.
        /// </summary>
        /// <param name="functionWithoutCatch"></param>
        /// <param name="param"></param>
        public static void WithoutErrorHandling<T>(this Action<T> functionWithoutCatch, T param)
        {
            try
            {
                functionWithoutCatch(param);
            }
            catch { }
        }
        /// <summary>
        /// Performs an action inside of a try catch and ignores the error. Should not be used unless repeatedly doing some function where it does
        /// not matter if it fails or not.
        /// </summary>
        /// <param name="funcWithoutCatch"></param> <param name="param1"></param> <param name="param2"></param>
        public static void WithoutErrorHandling<T, A>(this Action<T, A> funcWithoutCatch, T param1, A param2)
        {
            try
            {
                funcWithoutCatch(param1, param2);
            }
            catch { }
        }
        /// <summary>
        /// Performs an action inside of a try catch and ignores the error. Should not be used unless repeatedly doing some function where it does
        /// not matter if it fails or not.
        /// </summary>
        /// /// <param name="funcWithoutCatch"></param> <param name="param1"></param> <param name="param2"></param><param name="param3"></param>
        public static void WithoutErrorHandling<T, A, B>(this Action<T, A, B> funcWithoutCatch, T param1, A param2, B param3)
        {
            try
            {
                funcWithoutCatch(param1, param2, param3);
            }
            catch { }
        }
        /// <summary>
        /// Note: Do not both currying extension methods in general.... unless you'd be using the same object a bunch of times...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="A"></typeparam>
        /// <param name="func"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Func<A> Curry<T, A>(this Func<T, A> func, T param)
        {
            return (() => func(param));
        }
        public static Func<A, B> Curry<T, A, B>(this Func<T, A, B> func, T param)
        {
            return ((A v) => func(param, v));
        }
        public static Func<A> Curry<T1, T2, A>(this Func<T1, T2, A> func, T1 p1, T2 p2)
        {
            return (() => func(p1, p2));
        }
        public static Func<A> Curry<T1, T2, T3, A, B>(this Func<T1, T2, T3, A> func, T1 p1, T2 p2, T3 p3)
        {
            return (() => func(p1, p2, p3));
        }
        public static Func<A, B> Curry<T1, T2, A, B>(this Func<T1, T2, A, B> func, T1 p1, T2 p2)
        {
            return ((A v1) => func(p1, p2, v1));
        }
        public static Func<A, B, C> Curry<T, A, B, C>(this Func<T, A, B, C> func, T param)
        {
            return ((A v, B v2) => func(param, v, v2));
        }
        public static Action<A, B, C> Curry<T, A, B, C>(this Action<T, A, B, C> func, T param)
        {
            return ((A v1, B v2, C v3) => func(param, v1, v2, v3));
        }
        public static Action<A, B> Curry<T, A, B>(this Action<T, A, B> func, T param)
        {
            return ((A v1, B v2) => func(param, v1, v2));
        }
        public static Action<A> Curry<T, A>(this Action<T, A> func, T param)
        {
            return ((A v1) => func(param, v1));
        }
        /// <summary>
        /// Converts the object to a Dynamic Expando object
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static dynamic ToExpando(this object o)
        {
            dynamic ret = new ExpandoObject();
            var d = ret as IDictionary<string, object>;
            var props = o.GetType().GetProperties();
            foreach(var prop in props)
            {
                d[prop.Name] = prop.GetValue(o);
            }
            return d;
        }
        /// <summary>
        /// Creates a new instance of RT from an Expando object. Only sets properties.
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static RT FromExpando<RT>(this ExpandoObject source) where RT :new()
        {
            IDictionary<string, object> dict = source as IDictionary<string, object>;
            RT ret = new RT();
            var props = ret.GetType().GetProperties();
            foreach(var prop in props)
            {
                if (dict.ContainsKey(prop.Name))
                    prop.SetValue(ret, dict[prop.Name]);
                else
                { 
                    Type t = prop.GetType();
                    bool CanSet = !t.IsValueType || (Nullable.GetUnderlyingType(t) != null);
                    if (CanSet)
                        prop.SetValue(ret, null);
                }
            }
            return ret;
        }
        /// <summary>
        /// Convert an array of objects of type 'S' into a datatable
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="List"></param>
        /// <param name="ColumnIgnore">Columns that are left out of the new DataTable</param>
        /// <returns></returns>
        public static DataTable ToDataTable<S>(this IEnumerable<S> List, params string[] ColumnIgnore)
        {
            var properties = typeof(S).GetProperties();
            DataTable dt = new DataTable();
            foreach(var prop in properties)
            {
                if (ColumnIgnore.Contains(prop.Name))
                    continue;
                var c = dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                //if reference type or if given a nullable<T>, allow DBNull
                if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null) 
                    c.AllowDBNull = true;
            }
            foreach(S item in List)
            {
                var r = dt.NewRow();
                foreach(var prop in properties)
                {
                    if (ColumnIgnore.Contains(prop.Name))
                        continue;
                    object temp = prop.GetValue(item, null);
                    if (temp == null)
                        temp = DBNull.Value;
                    r[prop.Name] = temp;
                }
                dt.Rows.Add(r);
            }
            return dt;
        }

        public static DataTable ToDataTableLimited<S>(this IEnumerable<S> List, params string[] ColumnsInclude)
        {
            string[] propertiesIgnore = typeof(S)
                .GetProperties()
                .Exclude(p => p.Name.In(ColumnsInclude))
                .Select(p => p.Name).ToArray();
            var dt = ToDataTable(List, propertiesIgnore);
            ColumnsInclude.ForEachIndex((c, i) => { dt.Columns[c].SetOrdinal(i); });
            return dt;            
        }
        /// <summary>
        /// Gets the description of the specified MemberInfo
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string GetDescription(this MemberInfo val)
        {            
            var atts = val.GetCustomAttributes(typeof(DescriptionAttribute), true);
            if(atts != null && atts.Length == 1)
            {
                var att = atts[0] as DescriptionAttribute;
                return att?.Description ?? val.Name;
            }
            return val.Name;
        }
        /// <summary>
        /// Tries to get the enum's description, or the name of the value if no description specified
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum value)
        {
            Type t = value.GetType();
            var mem = t.GetMember(nameof(value));
            if(mem != null && mem.Length == 1)
            {
                return mem[0].GetDescription();
            }
            return nameof(value);
        }
        /// <summary>
        /// Requires that RT has an empty constructor. Converts the Datatable into an array of objects of type 'RT'
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static RT[] GetObjectList<RT>(this DataTable dt) where RT :new()
        {
            var properties = typeof(RT).GetProperties();
            RT[] rv = new RT[dt.Rows.Count];
            Func<string, bool> checkTable = (col) =>
            {
                return dt.Columns.Contains(col);
            };
            int idx = 0;
            foreach (DataRow r in dt.Rows)
            {
                RT temp = new RT();
                foreach (var prop in properties)
                {
                    if (!checkTable(prop.Name))
                        continue;
                    object o = r[prop.Name];
                    if (o == DBNull.Value)
                        o = null;
                    prop.SetValue(temp, r[prop.Name]);
                }
                rv[idx++] = temp;
            }
            return rv;
        }

        
    }
}
