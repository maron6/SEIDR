using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Data;
//using Excel = Microsoft.Office.Interop.Excel;

using IWshRuntimeLibrary;

namespace SEIDR.Dynamics.Windows
{
    public static class FileSaveHelper
    {
        public static string GetSaveFile(string filter, string ext = null)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = filter;
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (ext != null)
            {
                sfd.DefaultExt = ext;
                sfd.AddExtension = true;
            }
            sfd.CheckPathExists = true;
            sfd.CreatePrompt = true;
            bool? b = sfd.ShowDialog();
            if (!b.HasValue || !b.Value)
                return null;
            return sfd.FileName;
        }


        
        public static void CreateShortCut(string shortcutAddress, string target, string Description = null){
            try
            {
                if (!shortcutAddress.EndsWith(".lnk"))
                    shortcutAddress += ".lnk";
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                if (Description != null)
                    shortcut.Description = Description;
                shortcut.TargetPath = target;                
                shortcut.Save();
            }
            catch { }
        }
        public static void Convert(string FileName, string delimiter, System.Data.DataTable dt )
        {
            string result = "";
            var cols = from DataColumn col in dt.Columns
                       select col.ColumnName;            
            result = string.Join(delimiter, cols) + System.Environment.NewLine;
            foreach (DataRow r in dt.Rows)
            {
                result += string.Join(delimiter, r.ItemArray) + System.Environment.NewLine;                                
            }
            using (var sw = new System.IO.StreamWriter(FileName, false))
            {
                sw.Write(result);
            }
        }
        [Obsolete("Not really obsolete, but needs the Dynamics solution to be built with Microsoft.Office.Core and Microsoft.Office.Interop.Excel COM (Microsoft Office Object Library, Microsoft  Excel Object Library)")]
        public static void WriteExcelFile(string ExcelFileName, System.Data.DataTable dt)
        {
            /*
            if(dt == null || dt.Columns.Count ==0)
            {
                new Alert("Empty Data Set - Nothing to export", Choice: false).ShowDialog();
                return;
            }
            Excel.Application xls = new Excel.Application();
            xls.Workbooks.Add();

            Excel._Worksheet ws = xls.ActiveSheet;
            string color = null;
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                string x = dt.Columns[i].ColumnName;
                if (x.ToUpper() == "COLOR")
                {
                    color = x;
                    continue;
                }
                ws.Cells[1, (i + 1)] = x;
            }
            string[] colors = new string[dt.Rows.Count];
            // rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {                
                if (color != null)
                    colors[i] = dt.Rows[i][color] as string;                
                // to do: format datetime values before printing
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (dt.Columns[j].ColumnName == color)
                        continue;                 
                    ws.Cells[(i + 2), (j + 1)] = dt.Rows[i][j];
                    
                }
            }
            int colorCount = -1;
            foreach(Excel.Range row in ws.UsedRange.Rows)
            {
                if (color == null || colorCount > colors.Length)
                    break;
                if(colorCount < 0)
                {
                    colorCount++; //Skip header row
                    continue;
                }
                string tempColor = colors[colorCount++];
                if(tempColor == null)
                    continue;
                Excel.XlRgbColor applyColor;
                if (Enum.TryParse("rgb" + tempColor, true, out applyColor))
                {
                    row.Interior.Color = applyColor;
                }
                /*
                row.Interior.Color = Enum.Parse(typeof(Excel.XlRgbColor), "rgb" + tempColor);
                //* /
                    //colors[colorCount++];                
            }
            // check fielpath
            if (!string.IsNullOrWhiteSpace(ExcelFileName))
            {
                try
                {
                    ws.SaveAs(ExcelFileName);
                    xls.Quit();                                        
                }
                catch
                {
                    new Alert("Could not save Excel file", Choice:false).ShowDialog();
                }
            }
            else
            {
                xls.Visible = true; //Allow user to just open Excel if no file path provided
            }                
            */
        }

         
    }
}
