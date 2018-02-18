using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.WindowMonitor.Dashboards
{
    public static class ChartHelper
    {
        public static bool CanUseColumn(Type ColumnType)
        {
            return ColumnType == typeof(int)
                || ColumnType == typeof(short)
                || ColumnType == typeof(ushort)
                || ColumnType == typeof(uint);
        }
        public static string FriendifyProcedureName(string procName)
        {
            string s = procName.Substring(procName.LastIndexOf('.') + 1);
            if (s.ToUpper().StartsWith("USP"))
                s = s.Substring(3);
            while (s[0] == '_')
            {
                s = s.Substring(1);
            }
            s = SEIDR.Dynamics.Windows.EditableObjectHelper.FriendifyLabel(s);
            return s;
        }
        public static string GetFriendlyName(string s) => SEIDR.Dynamics.Windows.EditableObjectHelper.FriendifyLabel(s);

        /// <summary>
        /// Gets aggregate data about the rows.
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="Column"></param>
        /// <param name="subColumn">If provided, will count the frequency of value per category.<para> 
        /// Otherwise, just aggregates the value of each column.</para>
        /// </param>
        /// <returns></returns>
        public static Dictionary<string, ObservableCollection<ChartDisplayData>> GetAggregateData(
            IEnumerable<DataRowView> rows, 
            string Column, 
            IEnumerable<string> Excludes,
            string subColumn = null)
        {
            if (!rows.HasMinimumCount(1) || Column == null)
                return null;
            if (Excludes?.UnderMaximumCount(1) ?? true)
                Excludes = new string[] { nameof(SessionWindow.SWID) };
            else if (!Excludes.Contains(nameof(SessionWindow.SWID)))
                Excludes = Excludes.Include(nameof(SessionWindow.SWID));
            DataColumnCollection cols = rows.First().DataView.Table.Columns;
            Dictionary<string, Dictionary<string, int>> temp = new Dictionary<string, Dictionary<string, int>>();
            foreach( var r in rows)
            {
                string k = r[Column]?.ToString();
                if (k == null)
                    k = "_NULL_";                
                else
                {
                    if (k.ToUpper().StartsWith("HDN"))
                        continue;
                    if (k.ToUpper().StartsWith("DTL_"))
                        k = k.Substring(4);
                    k = GetFriendlyName(k);
                }
                if (!temp.ContainsKey(k))
                    temp[k] = new Dictionary<string, int>();
                if(subColumn != null)
                {
                    string sk = r[subColumn]?.ToString();
                    if (sk == null)
                        sk = "_NULL_";
                    else
                    {
                        if (sk.ToUpper().StartsWith("DTL_"))
                            sk = sk.Substring(4);
                        sk = GetFriendlyName(sk);
                    }
                    if (!temp[k].ContainsKey(sk))
                        temp[k][sk] = 1;
                    else
                        temp[k][sk]++;
                }
                else
                {
                    foreach(DataColumn col in cols)
                    {
                        if (!CanUseColumn(col.DataType))
                            continue;
                        string sk = col.ColumnName;
                        string osk = sk;
                        if(sk == "LMUID" || Excludes.Contains(sk))                        
                            continue;
                        
                        if (sk.ToUpper().StartsWith("HDN"))
                            continue;
                        if (sk.ToUpper().StartsWith("DTL_"))
                            sk = sk.Substring(4);
                        sk = GetFriendlyName(sk);

                        if (!temp[k].ContainsKey(sk))
                            temp[k][sk] = 0;

                        if (r[osk] == DBNull.Value || r[osk] == null)
                            continue;
                        else
                            temp[k][sk] += Convert.ToInt32(r[osk]);
                    }
                }
                
            }
            Dictionary<string, ObservableCollection<ChartDisplayData>> ret = new Dictionary<string, ObservableCollection<ChartDisplayData>>();            
            foreach(var kv in temp)
            {
                ret[kv.Key] = new ObservableCollection<ChartDisplayData>();
                foreach (var kkv in kv.Value)
                {                    
                    ret[kv.Key].Add(new ChartDisplayData { Description = kkv.Key, value = kkv.Value });
                }
            }
            return ret;
        }
        
    }
    public class ChartDisplayData: INotifyPropertyChanged
    {
        string _Description = "";
        int _Value = 0;

        public string Description { get { return _Description; }
            set
            {
                if(_Description != value)
                {
                    _Description = value;
                    NotifyPropertyChanged();
                }                
            }
        }
        public int value { get { return _Value; } set
            {
                if(_Value != value)
                {
                    _Value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propName = "")
        {            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
    public class PreAggChartViewModel
    {
        public ObservableCollection<ChartDisplayData> dataCollection { get; private set; }
        public PreAggChartViewModel(DataRowView r)
        {
            dataCollection = new ObservableCollection<ChartDisplayData>();
            foreach (DataColumn c in r.DataView.Table.Columns)
            {
                if (c.ColumnName.ToUpper().StartsWith("HDN") || c.ColumnName.ToUpper() == "LMUID")
                    continue;
                //if (c.DataType == typeof(int) || c.DataType == typeof(short) || c.DataType == typeof(ushort) || c.DataType == typeof(uint))
                if (ChartHelper.CanUseColumn(c.DataType))
                {
                    int val = 0;
                    string FriendlyName = c.ColumnName;
                    if (FriendlyName.ToUpper().StartsWith("DTL_"))
                        FriendlyName = FriendlyName.Substring(4);
                    FriendlyName = ChartHelper.GetFriendlyName(FriendlyName);
                    if (r[c.ColumnName] != DBNull.Value)
                    {
                        val = Convert.ToInt32(r[c.ColumnName]);
                    }
                    dataCollection.Add(new ChartDisplayData { Description = FriendlyName, value = val });
                }

            }
        }
        private object selectedItem = null;
        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                // selected item has changed
                selectedItem = value;
            }
        }
    }
}
