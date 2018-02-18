using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Class for building up menu items that can be added to a Context Menu or other WPF Menu
    /// </summary>
    public static class MenuItemBuilder
    {
        /// <summary>
        /// Builds the initial MenuItem that can be added to a Context Menu or other WPF menu
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="click"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static MenuItem BuildInitial(string Header, Action click, string Name = null)
        {
            Action<object, RoutedEventArgs> t = (o, r) => { click(); r.Handled = true; };
            return BuildInitial(Header, t, Name);
        }
        /// <summary>
        /// Adds child menuItems to the passed menuItem
        /// </summary>
        /// <param name="item"></param>
        /// <param name="ClickInfo"></param>
        /// <returns></returns>
        public static MenuItem Build(MenuItem item, Dictionary<string, Action> ClickInfo)
        {
            foreach(var kv in ClickInfo)
            {
                item.Items.Add(BuildInitial(kv.Key, kv.Value, Name: item.Name + "_" + CleanName(kv.Key)));
            }
            return item;
        }
        /// <summary>
        /// Build an initial MenuItem
        /// </summary>
        /// <param name="Header"></param>
        /// <param name="click"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static MenuItem BuildInitial(string Header, Action<object, RoutedEventArgs> click = null, string Name = null)
        {
            string _Name = Name ?? "MENU_ITEM_" + Header; 
            MenuItem m = new MenuItem
            {
                Name = CleanName(_Name),
                Header = Header
            };
            m.Click += new RoutedEventHandler(click);
            return m;
        }
        /// <summary>
        /// Build menu Items that get added to the passed MenuItem as children
        /// </summary>
        /// <param name="item"></param>
        /// <param name="clickInfo"></param>
        /// <returns></returns>
        public static MenuItem Build(MenuItem item, Dictionary<string, Action<object, RoutedEventArgs>> clickInfo)
        {
            foreach(var kv in clickInfo)
            {
                item.Items.Add(BuildInitial(kv.Key, click: kv.Value, Name: item.Name + "_" + CleanName(kv.Key)));
            }
            return item;
        }
        /// <summary>
        /// Cleans up the string for use as a name for WPF objects
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static string CleanName(string Name)
        {
            return System.Text.RegularExpressions.Regex.Replace(Name, @"[^a-zA-Z\d_]", "");
        }
    }
}
