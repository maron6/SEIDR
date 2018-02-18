using System;
using System.Collections.Generic;
using System.Data;

namespace SEIDR.WindowMonitor.Models
{
    public class DashboardPage
    {
        public string DashboardTitle { get; set; }
        int _Page;
        public int Page { get { return _Page ;} set { if (value <= 0) throw new Exception("Invalid page number"); _Page = value; } }
        public List<DashboardItem> MyItems = new List<DashboardItem>();
                
        public DashboardPage(DataRow source, int page = 1)
        {
            Page = page;
            
            foreach (DataColumn c in source.Table.Columns)
            {
                string name = c.ColumnName.ToUpper();
                if (name == "TITLE")
                {
                    DashboardTitle = source[c.ColumnName].ToString();
                    continue;
                }
                MyItems.Add(new DashboardItem(source[c.ColumnName].ToString()) { ItemName = c.ColumnName });
            }
        }
        public DashboardItem this[int index]
        {
            get 
            {
                return MyItems[index];
            }
        }
    }       
}
