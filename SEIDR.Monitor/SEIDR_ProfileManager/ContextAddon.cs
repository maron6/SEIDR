using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ryan_UtilityCode.Dynamics.Configurations;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using System.ComponentModel.Composition;

namespace SEIDR_ProfileManager
{
    [Export(typeof(SEIDR_WindowAddOn)),
        ExportMetadata("AddonName", "SEIDR Profile manager - Context"),
        ExportMetadata("Description", "Opens the SEIDR Profile Manager for the LoadProfileID in the selected row.\n Requires a DataServices connection."),
        //ExportMetadata("IDValueParameterName", null),
        //ExportMetadata("IDValueParameterTooltip", null),
        //ExportMetadata("IDNameParameterName", null),
        //ExportMetadata("IDNameParameterTooltip", null),    
        ExportMetadata("NeedParameterMapping", false),
        ExportMetadata("Team", "ETL"),    
        ExportMetadata("RequireSessionCache", true),
        ExportMetadata("MultiSelect", false)]
    public class ContextAddon : SEIDR_WindowAddOn
    {
        public BasicUser Caller
        {
            get;set;
        }

        public DatabaseConnection Connection
        {
            get;set;
        }
        /*
        public string IDName
        {
            get;set;
        }

        public int? IDValue
        {
            get;set;
        } */

        public string Execute(DataRowView selectedRow)
        {
            if (Connection.DefaultCatalog != "DataServices")
                return "Invalid Catalog for Seidr ProfileManager: " + Connection.DefaultCatalog;
            //if (!Caller.BasicValidate(false, null, "ETL"))
            //    return "Not part of ETL Team - Cannot open Seidr ProfileManager."; //Library will handle this 
            if (selectedRow.DataView.Table.Columns.Contains("LoadProfileID"))
            {
                ProfileManager pm;         
                int p = 0;
                if (Int32.TryParse(selectedRow["LoadProfileID"] as string, out p))
                    pm = new ProfileManager(p, db: Connection);
                else
                    pm = new ProfileManager(db: Connection);
                pm.ShowDialog();                
                return null;
            }
            else
                return "No LoadProfileID in selected row. Cannot open the Seidr ProfileManager.";
        }

        public string Execute(DataRowView[] selectedRows)
        {
            return "Multi select not enabled.";
        }

        public string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters)
        {
            return Execute(selectedRow);
        }

        public string Execute(DataRowView[] selectedRows, Dictionary<string, object> Parameters)
        {
            return "Multi Select not enabled";
        }

        public DataRowView GetParameterInfo()
        {
            throw new NotImplementedException();
        }
    }
    
}
