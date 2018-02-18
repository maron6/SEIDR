using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System.ComponentModel.Composition;
using SEIDR.DataBase;
using System.Windows;
using System.Windows.Controls;

namespace RowColumnProcessor
{
    [ Export(typeof(SEIDR_WindowAddOn)),  
        ExportMetadata("AddonName", "RowColumnProcessor"),
        ExportMetadata("Description", "Attempts to open the column data of the selected row"),
        //ExportMetadata("IDValueParameterName", null),
        //ExportMetadata("IDValueParameterTooltip", null),
        ExportMetadata("NeedParameterMapping", true), //Default value
        //ExportMetadata("IDNameParameterName", "Column Name to open"),
        //ExportMetadata("IDNameParameterTooltip", "Will try to open/start the process referenced\r\n by the data in the column of the selected row."),
        ExportMetadata("MultiSelect", false)]
    public class RowColumnProcessor : SEIDR_WindowAddOn
    {
        public BasicUser Caller
        {
            get; set;
        }

        public DatabaseConnection Connection
        {
            get; set;
        }
        /*
        public string IDName
        {
            get; set;
        }

        public int? IDValue
        {
            get; set;
        } */

        public string Execute(DataRowView selectedRow)
        {
            string message = null;
            try {
                //System.Diagnostics.Process.Start(selectedRow[""].ToString());                
                selectedRow["TEST"] = "Updated!";
            }
            catch(Exception e)
            {
                message = e.Message;
            }
            return message;
        } 

        public string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters)
        {
            string column = (string)Parameters["ColumnToOpen"];
            bool hidden = (bool)Parameters["hiddenReader"];            
            string message = null;
            try
            {
                if (hidden)
                {
                    var a = (new SEIDR.Dynamics.Windows.Alert(selectedRow[column].ToString(), Choice: false));
                    a.ShowDialog();
                }
                else
                    System.Diagnostics.Process.Start(selectedRow[column].ToString());
                //selectedRow["TEST"] = "Updated!";
            }
            catch (Exception e)
            {
                message = e.Message;
            }
            return message;
        }

        public string Execute(IEnumerable<DataRowView> selectedRows, Dictionary<string, object> Parameters)
        {
            throw new NotImplementedException();
        }

        public DataRowView GetParameterInfo()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn
            {
                DataType = typeof(string),
                ColumnName = "ColumnToOpen",
                Caption = "Column to Open content",
                DefaultValue = string.Empty
            });
            dt.Columns.Add(new DataColumn
            {
                DataType = typeof(bool),
                ColumnName="hiddenReader",
                Caption="Open display of a hidden column",
                DefaultValue = false
            });
            dt.Rows.Add();
            return dt.AsDataView()[0];
        }
    }


}
