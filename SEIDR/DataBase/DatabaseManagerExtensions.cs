using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;

namespace SEIDR.DataBase
{
    public static class DatabaseManagerExtensions
    {
        /// <summary>
        /// Flag Parameter direction to determine if the value needs to be checked after execution
        /// </summary>
        public const ParameterDirection CheckOutput = ParameterDirection.Output | ParameterDirection.ReturnValue;
        /// <summary>
        /// Returns the mapped name (by FieldMapping attribute), or the property's actual name if there is no populated mapping.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static string GetMappedName(this PropertyInfo prop)
        {
            var att = prop.GetCustomAttribute(typeof(DatabaseManagerFieldMappingAttribute));
            if (att != null)
                return ((DatabaseManagerFieldMappingAttribute)att).MappedName ?? prop.Name;
            return prop.Name;
        }
        /// <summary>
        /// Returns a dictionary of the mapped property names and their getMethods
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Dictionary<string, MethodInfo> GetGetters(this Type t)
        {
            return t
                .GetProperties()
                .Where(p => p.CanRead && !p.MappingIgnored(false))
                .ToDictionary(pn => pn.GetMappedName(), pn => pn.GetMethod);
        }
        /// <summary>
        /// Checks if the property should be ignored by the DatabaseManager when working with SqlParameters
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="forOutParameter">Ignores the mapping only when checking Out Parameters for their updated values</param>
        /// <returns></returns>
        public static bool MappingIgnored(this PropertyInfo prop, bool forOutParameter)
        {
            DatabaseManagerIgnoreMappingAttribute att = prop.GetCustomAttribute(typeof(DatabaseManagerIgnoreMappingAttribute)) as DatabaseManagerIgnoreMappingAttribute;
            if (att == null)
                return false;
            if (forOutParameter)
                return att.ignoreReadOut;

            return att.ignoreSet;
        }
    }
    /// <summary>
    /// Tells the DatabaseManager to ignore a mapping. Should only be used if there's a default value
    /// </summary>
    public class DatabaseManagerIgnoreMappingAttribute :Attribute
    {
        /// <summary>
        /// Ignore setting the value in parameters when populating the SqlCommand's parameters if the direction to ignore is either <see cref="ParameterDirection.Input"/> or <see cref="ParameterDirection.InputOutput"/>
        /// </summary>
        public bool ignoreSet { get; private set; } = false;
        /// <summary>
        /// Ignore reading the output parameters of the SqlCommand after execution if the direction to ignore is <see cref="ParameterDirection.InputOutput"/>, <see cref="ParameterDirection.Output"/>, or <see cref="ParameterDirection.ReturnValue"/>
        /// </summary>
        public bool ignoreReadOut { get; private set; } = false;
        /// <summary>
        /// Tells DatabaseManager to ignore the parameter for the specified direction
        /// </summary>
        /// <param name="directionToIgnore"></param>
        public DatabaseManagerIgnoreMappingAttribute(ParameterDirection directionToIgnore)
        {
            if((directionToIgnore & DatabaseManagerExtensions.CheckOutput) != 0)
            {
                ignoreReadOut = true;
            }
            if((directionToIgnore 
                & (ParameterDirection.Input | ParameterDirection.InputOutput))
                != 0)
            {
                ignoreSet = true;
            }
        }
    }

    public class DatabaseManagerIgnoreOutParameterAttribute :Attribute { }

    /// <summary>
    /// Changes the name for properties by the DatabaseManager when doing any mappings from/to objects
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple =false, Inherited = false)]
    public class DatabaseManagerFieldMappingAttribute:Attribute
    {
        /// <summary>
        /// Name to be used by DatabaseManagers
        /// </summary>
        public readonly string MappedName;
        /// <summary>
        /// Maps the property to a different name for use in DatabaseManager mappings
        /// </summary>
        /// <param name="Map">Name to use. Note: This value will be trimmed and set to null if empty. If a property should be ignored in mapping, use the DatabaseManagerIgnoreMapping attribute</param>
        public DatabaseManagerFieldMappingAttribute(string Map)
        {
            MappedName = Map.nTrim(true);
        }
    }
}
