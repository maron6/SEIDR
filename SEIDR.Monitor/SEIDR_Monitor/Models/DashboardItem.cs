using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.WindowMonitor.Models
{
    public class DashboardItem
    {
        public string Color { get; set; }
        public int? ProgressMin { get; set; }
        public int? ProgressMax { get; set; }
        public int? Progress { get; set; }
        public string value;
        public string ItemName = null;
        public DashboardItem(string pseudoJson)
        {
            Color = "SteelBlue";
            Progress = null;
            ProgressMax = null;
            ProgressMin = null;
            value = null;
            try
            {
                string[] items = pseudoJson.Split(',');
                foreach (string item in items)
                {
                    var kv = item.Split(':');
                    string setVal = kv[1].Trim();
                    switch (kv[0].ToUpper().Trim())
                    {
                        case "COLOR":
                            {
                                Color = setVal;
                                break;
                            }
                        case "MIN":
                            {
                                ProgressMin = Convert.ToInt32(setVal);
                                break;
                            }
                        case "MAX":
                            {
                                ProgressMax = Convert.ToInt32(setVal);
                                break;
                            }
                        case "VALUE":
                            {
                                value = setVal;
                                try
                                {
                                    Progress = Convert.ToInt32(setVal);
                                }
                                catch { }
                                break;
                            }
                    }
                }
            }
            catch
            {
                throw new Exception("Badly formatted DashboardItem.");
            }
        }
    }
}
