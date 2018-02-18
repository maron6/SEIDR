using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Ryan_UtilityCode.Dynamics.Windows;
using System.ComponentModel.Composition;
using Microsoft.Win32;

namespace TemplateDTSBuilder
{
    [TemplateDTSBuilderMetaData]
    [Export(typeof(SEIDR_WindowMenuAddOn))]
    [ExportMetadata("Name", "Template DTS Builder")]
    [ExportMetadata("HasParameterMapping", false)]
    [ExportMetadata("RequirePermission", false)]
    [ExportMetadata("Team", "ETL")]
    public class TemplateDTSBuilder : SEIDR_WindowMenuAddOn
    {
        public SEIDR_Window callerWindow
        {
            get;set;
        }

        public Dictionary<string, Type> GetParameterInfo()
        {
            throw new NotImplementedException();
        }

        public MenuItem Setup(Ryan_UtilityCode.Dynamics.Configurations.BasicUser User, Ryan_UtilityCode.Processing.Data.DBObjects.DatabaseConnection Connection, string internalName, Dictionary<string, object> setParameters)
        {
            mySetup.setParams = setParameters;
            Action r = () => 
            {
                var dr = mySetup.GetRowSetup();
                EditableDashboardDisplay edd = new EditableDashboardDisplay(dr, "Template DTS Builder");
                var ok = edd.ShowDialog();
                if(ok ?? false)
                {
                    dr = edd.myDataRowView;
                    Evaluate(dr);
                }
            };
            return MenuItemBuilder.BuildInitial("Template DTS Builder", r);            
        }

        private void Evaluate(DataRowView dr)
        {
            DtsPackageBuilder b = new DtsPackageBuilder();

            short count = (short)dr[mySetup.NUMBER_DB_CONNS];
            for (int i = 0; i < count; i++)
            {
                b.AddConnection(DtsPackageBuilder.ConnectionType.OleDB, "");
            }
            count = (short)dr[mySetup.NUMBER_FLAT_FILES];
            for (int i = 0; i < count; i++)
            {
                b.AddConnection(DtsPackageBuilder.ConnectionType.FlatFile, "");
            }

            var typeList = new List<DtsPackageBuilder.DataFlowType>();
            var mapping = new Dictionary<string, object>();
            if ((bool)dr[mySetup.USE_SCRIPT])
                typeList.Add(DtsPackageBuilder.DataFlowType.Script);
            if ((bool)dr[mySetup.LOAD_FL_DB])
                typeList.Add(DtsPackageBuilder.DataFlowType.File_DB);
            if ((bool)dr[mySetup.LOAD_DB_DB])
                typeList.Add(DtsPackageBuilder.DataFlowType.DB_DB);
            if ((bool)dr[mySetup.LOAD_DB_FL])
            {
                typeList.Add(DtsPackageBuilder.DataFlowType.DB_File);
                mapping.Add("OutputFolder", string.Empty);
            }
            b.AddChainedDataFlowTasks(typeList.ToArray());

            if ((bool)dr[mySetup.FACILITYID])
                mapping.Add("FacilityID", 0);
            mapping.Add("LoadBatchID", 0);
            mapping.Add("InputFile", string.Empty);
            if ((bool)dr[mySetup.INPUTFILE_DATE])
                mapping.Add("InputFileDate", new DateTime(1, 1, 1));

            b.AddVariableCollection(mapping);

            SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = "Package.dtsx",
                ValidateNames = true,
                AddExtension = true,
                DefaultExt = ".dtsx",
                Filter = "DTS Packages |*.dtsx"
            };
            var res = sfd.ShowDialog();
            if (res ?? false)
                b.SavePackage(sfd.FileName);
        }
    }
    public class TemplateDTSBuilderMetaData : ExportAttribute, SEIDR_WindowMenuAddOn_MetaData
    {
        public bool HasParameterMapping
        {
            get
            {
                return false;
            }
        }

        public string Name
        {
            get { return "Template DTS Builder"; }
        }

        public Dictionary<string, Type> parameterInfo
        {
            get
            {
                return new Dictionary<string, Type>();
            }
        }

        public bool RequirePermission
        {
            get
            {
                return false;
            }
        }

        public bool RequireSessionCache
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Singleton
        {
            get
            {
                return true;
            }
        }

        public string Team
        {
            get
            {
                return "ETL";
            }
        }

        public bool UsesCaller
        {
            get
            {
                return false;
            }
        }
    }

    public static class mySetup
    {
        public const string NUMBER_DB_CONNS = "Number of DB Connections";
        public const string NUMBER_FLAT_FILES = "Number of Flat File Connections";
        public const string INPUTFILE_DATE = "Include InputFileDate";


        public const string FACILITYID = "Include FacilityID";
        public const string USE_SCRIPT = "Use Script Task"   ;
        public const string LOAD_FL_DB = "Load File to DB"   ;
        public const string LOAD_DB_DB = "Load DB to DB"     ;
        public const string LOAD_DB_FL = "Load DB to File";



        public static Dictionary<string, object> setParams { get; internal set; }        
        public static DataRowView GetRowSetup()
        {
            Type b = typeof(bool);
            DataTable dt = new DataTable();
            dt.Columns.Add(NUMBER_DB_CONNS, typeof(short));
            dt.Columns.Add(NUMBER_FLAT_FILES, typeof(short));
            
            dt.Columns.Add(INPUTFILE_DATE, b);
            dt.Columns.Add(FACILITYID , b);            
            dt.Columns.Add(USE_SCRIPT , b);
            dt.Columns.Add(LOAD_FL_DB , b);
            dt.Columns.Add(LOAD_DB_DB , b);
            dt.Columns.Add(LOAD_DB_FL, b);

            dt.NewRow();
            return dt.AsDataView()[0];
        }
    }
}
