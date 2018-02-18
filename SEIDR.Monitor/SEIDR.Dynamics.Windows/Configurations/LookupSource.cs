using SEIDR.Dynamics.Configurations.UserConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Dynamics.Configurations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LookupSourceAttribute: Attribute
    {
        public override object TypeId
        {
            get
            {
                return InstanceID;
            }
        }
        private Guid InstanceID;
        [Obsolete]        
        public string ScopeProperty { get; private set; }
        public WindowConfigurationScope LookupScope { get; private set; }
        public bool ForCloning { get; private set; }
        public LookupSourceAttribute(WindowConfigurationScope scope, string scopeProperty = null, bool forCloning = true)
        {
            LookupScope = scope;
            InstanceID = Guid.NewGuid();
            ScopeProperty = scopeProperty?.Trim();
            ForCloning = forCloning;
        }
    }
    [AttributeUsage(AttributeTargets.Property| AttributeTargets.Enum | AttributeTargets.Field, 
        AllowMultiple =false, Inherited =true)]
    public sealed class WindowConfigurationEditorIgnoreAttribute :Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, 
        AllowMultiple =true, Inherited = true)]
    public sealed class WindowConfigurationEditorElementInfoAttribute :Attribute
    {
        public override object TypeId
        {
            get
            {
                return base.TypeId.ToString() + string.Format("_{0}_{1}", TabNumber, Label);
            }
        }
        public string Label { get; private set; }
        public short TabNumber { get; private set; }
        /// <summary>
        /// Should populate a dictionary of objects with required set, 
        /// <para>then the configuration editor should make sure that they are all populated/ not null
        /// </para>
        /// <para>Update dictionary using RequiredEventArgs
        /// </para>
        /// </summary>
        public bool Required { get; private set; } 
        public bool IsColor { get; private set; }
        /// <summary>
        /// Attribute for formatting in configuration editor
        /// </summary>
        /// <param name="Tab">Choose tab from the Tabs attribute on class.<para>
        /// If out of range (mainly negative), will be in the general Section</para></param>
        /// <param name="required">If true, will have a visual indicator when no selection or content. Only required within tab</param>
        public WindowConfigurationEditorElementInfoAttribute(string label, 
            short Tab = -1, bool required = false, bool isColor = false)
        {
            Label = label;
            TabNumber = Tab;
            Required = required;
            IsColor = isColor;
        }
    }
    public class RequiredEventArgs: EventArgs
    {
        public object Value { get; private set; }
        public bool HasValue { get { return Value != null && Value.ToString() != string.Empty; } }
        public RequiredEventArgs(object update)
        {
            Value = update;
        }
        public RequiredEventArgs() { Value = null; }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false, Inherited =false)]
    public sealed class WindowConfigurationEditorTabsAttribute: Attribute
    {
        public string[] Tabs { get; }
        /// <summary>
        /// Required permissions to use tab.
        /// </summary>
        public Dictionary<string, BasicUserPermissions> TabPermissions { get; set; }
        public WindowConfigurationEditorTabsAttribute(params string[] TabList)
        {
            Tabs = TabList;
            TabPermissions = new Dictionary<string, BasicUserPermissions>();
        }
        public WindowConfigurationEditorTabsAttribute(string[] TabList, params BasicUserPermissions[] RequiredPermissions)
        {
            Tabs = TabList;
            TabPermissions = new Dictionary<string, BasicUserPermissions>();
            RequiredPermissions.ForEachIndex(
                (p, i) => { TabPermissions[TabList[i]] = p;  }, 
                TabList.Length
                );
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple =false, Inherited = true)]
    public sealed class CloneLookupSourceRequiredAttribute:Attribute
    {        
    }
}
