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

namespace SEIDR_Window.AddonTest
{
    [Export,
        ExportMetadata("AddonName", "RowProcessor"),
        ExportMetadata("Description", "Sample addon for processing"),
        ExportMetadata("IDValueParameterName", "TestID"),
        ExportMetadata("IDNameParameterName", null),
        ExportMetadata("IDNameParameterTooltip", null),
        ExportMetadata("IDValueParameterTooltip", "Test Value"),
        ExportMetadata("MultiSelect", false)]
    public class RowProcessor : SEIDR_WindowAddOn
    {
        public BasicUser Caller { get; set; }

        public DatabaseConnection Connection { get; set; }
       
        public string IDName { get; set; }
        

        public int? IDValue { get; set; }

        public DataRowView GetParameterInfo()
        {
            return null;
        }

        public string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters)
        {
            int x = IDValue ?? 0; //Start value.
            var cols = selectedRow.DataView.Table.Columns;
            int c = 0;
            foreach (DataColumn col in cols)
            {
                if(col.DataType == typeof(int))
                {
                    IDValue += (selectedRow[col.ColumnName] as int?) ?? 0;
                    c++;
                    selectedRow[col.ColumnName] = c;
                }
            }
            if (x == IDValue && c == 0)
                return "No Change - result is " + x;
            else if (x == IDValue)
                return "No change, but there were " + c + " columns being used to attempt to change it.";
            else
                return string.Format("Value has changed from {0} to {1}. Success!", IDValue, x);

        }
        public string Execute(IEnumerable<DataRowView> selectedRows, Dictionary<string, object> Parameters)
        {
            throw new NotImplementedException();
        }
    }
}
