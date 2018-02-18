using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SEIDR.Dynamics.Windows
{
    public class ComboDisplay
    {
        public string Name { get; private set; }
        public List<ComboDisplayItem> Items { get; private set; }
        public int SelectedIndex { get; private set; }
        public ComboDisplay(string name, ComboDisplayItem[] records, int Index = -1)
        {
            Name = name;
            Items = new List<ComboDisplayItem>( records);
            SelectedIndex = Index;
        }
        public ComboBox AsComboBox()
        {
            ComboBox cb = new ComboBox
            {
                Tag= Name,
                Name = "CB_" + Name.Replace(' ', '_'),
                IsReadOnly = true
            };
            foreach(var i in Items)
            {
                cb.Items.Add(new ComboBoxItem
                {
                    Content = i
                });
            }
            cb.SelectedIndex = SelectedIndex;
            return cb;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            ComboDisplay c = obj as ComboDisplay;
            if (c == null)
                return false;
            return c.Name == this.Name;
        }
        /// <summary>
        /// DataTable should consist of following: PropertyName (ComboDisplay.Name), 
        /// DisplayName (ComboDisplayItem.Description), DisplayValue (ComboDisplayItem.Value)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<ComboDisplay> Build(DataTable dt)
        {
            List<ComboDisplay> ret = new List<ComboDisplay>();
            DataView v = dt.AsDataView();
            if (v.Count == 0)
                return null;
            v.Sort = "PropertyName ASC, DisplayName ASC, DisplayValue ASC";
            string Last = null;
            List<ComboDisplayItem> working = new List<ComboDisplayItem>();
            for(int i = 0; i < v.Count; i++)
            {
                string prop = v[i]["PropertyName"].ToString();
                if (Last != null && Last != prop)
                {
                    ret.Add(new ComboDisplay(prop, working.ToArray()));
                    Last = prop;
                    working.Clear();
                }
                else if (Last == null)
                    Last = prop;
                working.Add(new ComboDisplayItem(v[i]["DisplayName"].ToString(), v[i]["DisplayValue"]));
                if(i == v.Count - 1)
                {
                    //Last record.
                    ret.Add(new ComboDisplay(prop, working.ToArray()));
                    working = null;
                    break; //done anyway, can make sure it ends though
                }
            }
            return ret;
        }
    }
    public class ComboDisplayItem
    {        
        public string description { get; private set; }
        public object value { get; private set; }
        public ComboDisplayItem(string Description, object Value)
        {
            description = Description;
            value = Value;
        }
        public override string ToString()
        {
            return description;
        }
    }
}
