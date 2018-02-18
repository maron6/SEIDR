using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Sets property info... Note that description can be set on most properties in the EditableObjectWindow by using System.ComponentModel.DescriptionAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EditableObjectInfoAttribute : Attribute
    {
        public bool CanUpdate { get; private set; } = true;
        public EditableObjectInfoAttribute(bool canUpdate)
        {
            CanUpdate = canUpdate;
        }
        public int? MinSize { get; private set; } = null;
        public int? MaxSize { get; private set; } = null;
        public EditableObjectInfoAttribute(int MinSize, int MaxSize)
            : this(true)
        {
            this.MinSize = MinSize;
            this.MaxSize = MaxSize;
        }
        public EditableObjectInfoAttribute(int MinSize) : this(true)
        {
            this.MinSize = MinSize;
        }
        public DateTime? MinDate { get; private set; } = null;
        public DateTime? MaxDate { get; private set; } = null;
        public EditableObjectInfoAttribute(DateTime MinDate, DateTime MaxDate) : this(true)
        {
            this.MinDate = MinDate;
            this.MaxDate = MaxDate;
        }
        public EditableObjectInfoAttribute(DateTime MinDate) : this(true)
        {
            this.MinDate = MinDate;
        }

    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple =true, Inherited =true)]
    public sealed class EditableObjectMethodAttribute:Attribute
    {
        public override object TypeId
        {
            get
            {
                return base.TypeId.ToString() + ButtonName;
            }
        }
        public bool RefreshAfter { get; private set; }
        public object[] MethodParameters { get; private set; }
        public string ButtonName { get; private set; }
        /// <summary>
        /// Sets up a method as a button in an Editable Object display window.
        /// </summary>
        /// <param name="refreshAfter">If true, refreshes the display after returning</param>
        /// <param name="parameters">Parameters to pass to Invoke</param>
        public EditableObjectMethodAttribute(string buttonName, bool refreshAfter,  params object[] parameters)
        {            
            ButtonName = buttonName;
            RefreshAfter = refreshAfter;
            MethodParameters = parameters;
        }
        public EditableObjectMethodAttribute(string buttonName, params object[] parameters)
            :this(buttonName, refreshAfter:false, parameters:parameters)
        {

        }
    }
    /// <summary>
    /// Hides an enum value in Editable object display's combo box...
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple =false, Inherited =false)]
    public class EditableObjectHiddenEnumValueAttribute : Attribute
    {

    }
    public static class EditableObjectHelper
    {
        public static string FriendifyLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;
            string temp = label[0].ToString().ToUpper();
            if (label.Length > 1)
                temp += label.Substring(1);
            else return temp;
            temp = temp.Replace('_', ' ');
            return SpacebeforeCap(temp);
            //Regex capitalLetterMatch = new Regex(@"\B[A-Z]+", RegexOptions.Compiled);
            //return capitalLetterMatch.Replace(temp, " $&");
        }        
        private static string SpacebeforeCap(string temp)
        {            
            if (string.IsNullOrWhiteSpace(temp))
                return string.Empty;
            while (temp[0] == ' ')
                temp = temp.Substring(1);
            if (temp == string.Empty)
                return temp;
            StringBuilder result = new StringBuilder();                        
            result.Append(char.ToUpper(temp[0]));
            bool lastUpper = true;
            bool lastSpace = false;
            for(int i = 1; i < temp.Length; i++)
            {
                if(temp[i] == ' ')
                {
                    if (lastSpace)
                        continue;
                    lastSpace = true;
                    result.Append(' ');
                    continue;
                }
                lastSpace = false;
                if (char.IsUpper(temp[i]))
                {
                    if (lastUpper)
                    {
                        result.Append(temp[i]);
                    }
                    else
                    {
                        result.Append((lastSpace? "": " ") + temp[i]);
                        lastUpper = true;
                    }
                }
                else
                {
                    result.Append(temp[i]);   
                    lastUpper = false;                    
                }
            }
            return result.ToString();
        }
        public static string GET_WPF_NAME(string name)
        {
            return Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        }
    }


    public interface EditableObjectValidator
    {
        bool CheckValid(object o);
    }
}
