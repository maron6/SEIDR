using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace SEIDR.Dynamics.Windows
{
    /// <summary>
    /// Stores a read-only, plain text password, which can also be used for setting up a PasswordBox.
    /// </summary>
    public class Password
    {
        public string value { get; private set; }
        public Password(string Value)
        {
            value = Value;
        }
        public static PasswordBox CreatePasswordBox(string initialValue = null)
        {
            PasswordBox pb = new PasswordBox
            {
                Password = initialValue
            };            
            return pb;
        }
        /// <summary>
        /// Creates a password box which will update the password on this object
        /// </summary>
        /// <returns></returns>
        public PasswordBox CreatePasswordBox()
        {
            PasswordBox p = new PasswordBox { Password = value };
            p.PasswordChanged += new RoutedEventHandler((object o, RoutedEventArgs r) => 
            {
                PasswordBox temp = r.Source as PasswordBox;
                if (temp == null)
                    return;
                value = temp.Password;
            });            
            return p;
        }
        
    }
    public static class PasswordExtensions
    {
        public static PasswordBox CreatePasswordBox(this string initialValue)
        {
            PasswordBox pb = new PasswordBox
            {
                Password = initialValue
            };
            return pb;
        }
        public static Password GetPassword(this PasswordBox box)
        {
            return new Password(box.Password);
        }
    }
}
