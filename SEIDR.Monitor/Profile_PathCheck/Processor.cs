using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Windows;
using SEIDR.DataBase;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Profile_PathCheck
{

    [ExportMetadata("AddonName", "Profile Processor"),
        ExportMetadata("Description", "Check that Profile Folders exist. Input and Output folders should be mapped to nonempty if they need to be checked."),
        ExportMetadata("MultiSelect", false)]
    public class Processor : SEIDR_WindowAddOn
    {
        public BasicUser Caller { get; set; }

        public DatabaseConnection Connection { get; set; }

        public string IDName { get; set; }

        public int? IDValue { get; set; }       

        public string Execute(DataRowView selectedRow, Dictionary<string, object> Parameters)
        {
            string ip = Parameters["InputFileColumn"].ToString();
            string op = Parameters["OutputFileColumn"].ToString();
            var cols = selectedRow.Row.Table.Columns;
            if (ip != null && !cols.Contains(ip))
                ip = null;
            if (op != null && !cols.Contains(op))
                op = null;
            if (ip == null && op == null)
                return "No suitable columns found to check.";
            string idir = null;
            string inputDev = null;
            string MainDir = null;
            if (ip != null)
            {
                idir = selectedRow[ip].ToString();
                if (idir != null)
                {
                    System.IO.Directory.CreateDirectory(idir);
                    inputDev = System.IO.Path.Combine(idir, "DEV");
                    System.IO.Directory.CreateDirectory(inputDev);
                    var d = new System.IO.DirectoryInfo(idir);
                    MainDir = d.Parent.FullName;
                    FileSaveHelper.CreateShortCut(MainDir, inputDev, "Dev Input");
                }

            }
            if (op != null)
            {
                string odir = null;
                odir = selectedRow[op].ToString();
                if (odir != null)
                {
                    System.IO.Directory.CreateDirectory(odir);
                    string dev = System.IO.Path.Combine(odir, "DEV");
                    System.IO.Directory.CreateDirectory(dev);
                    var d = new System.IO.DirectoryInfo(odir);
                    FileSaveHelper.CreateShortCut(d.Parent.FullName, dev, "Dev Output");
                    if (inputDev != null)
                    {
                        FileSaveHelper.CreateShortCut(inputDev, dev, "Dev Output");
                        FileSaveHelper.CreateShortCut(dev, inputDev, "Dev Input");
                    }

                }
            }
            return null;
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
                ColumnName="InputFileColumn",
                DataType= typeof(string),
                DefaultValue=string.Empty
            });
            dt.Columns.Add(new DataColumn
            {
                ColumnName = "OutputFileColumn",
                DataType = typeof(string),
                DefaultValue = string.Empty
            });
            dt.Rows.Add(dt.NewRow());
            return dt.AsDataView()[0];
        }
    }

    [ExportMetadata("Name", "Profile DTS Package Manager"),
        ExportMetadata("UsesCaller", true),
        ExportMetadata("Team", "ETL")]
    public class DTSProcessor : SEIDR_WindowMenuAddOn
    {
        SEIDR_Window _caller;
        public SEIDR_Window callerWindow
        {
            set
            {
                _caller = value;
            }
        }

        const string INTEGRATION_SERVICES = "Integration Services Server";        
        const string PROFILE_SELECT = "Profile Selection Procedure";
        const string PACKAGE = "Results Package Path Column";        
        public Dictionary<string, Type> GetParameterInfo()
        {
            return new Dictionary<string, Type>
            {
                {INTEGRATION_SERVICES, typeof(string) },                
                {PROFILE_SELECT, typeof(string) },                
                {PACKAGE, typeof(string) }
            };
        }
        string _DOWNLOAD;
        string _UPLOAD;        
        public MenuItem Setup(BasicUser User, DatabaseConnection Connection, int internalID, Dictionary<string, object> setParameters)
        {
            #region context
            ContextMenu cm = new ContextMenu();
            _DOWNLOAD = $"DTS_W_{internalID}_DTS_SAVE";
            _UPLOAD = $"DTS_W_{internalID}_DTS_SET";
            MenuItem profile = new MenuItem
            {
                Header = "Save Package"
                ,
                Name = _DOWNLOAD
            };
            profile.Click += (sender, args) =>
            {
                MenuItem im = sender as MenuItem;
                if (im == null)
                    return;
                ContextMenu icm = im.Parent as ContextMenu;
                DataGrid dg = icm?.PlacementTarget as DataGrid;
                DataRowView selected = dg?.SelectedItem as DataRowView;
                if (selected == null)
                    return;
                string pkg = selected[PACKAGE].ToString();
                OpenFileDialog ofd = new OpenFileDialog
                {
                    AddExtension = false,
                    CheckFileExists = true,
                    DefaultExt = "dtsx",
                    Multiselect = false
                };
                var b = ofd.ShowDialog() ?? false;
                if (!b)
                    return;
                dg.Cursor = System.Windows.Input.Cursors.Wait;
                try
                {
                    DTS_SaveHelper.UploadDTSPackage(pkg, ofd.FileName, setParameters[INTEGRATION_SERVICES] as string);
                }
                catch(Exception ex)
                {
                    (new Alert("Unable to uplaod Package:" + ex.Message, Choice: false)).ShowDialog();
                }
                finally
                {
                    dg.Cursor = System.Windows.Input.Cursors.Arrow;
                }
            };
            MenuItem upload = new MenuItem
            {
                Header = "Upload Package",
                Name = _UPLOAD
            };
            upload.Click += (sender, args) =>
            {
                MenuItem im = sender as MenuItem;
                if (im == null)
                    return;
                ContextMenu icm = im.Parent as ContextMenu;
                DataGrid dg = icm?.PlacementTarget as DataGrid;
                DataRowView selected = dg?.SelectedItem as DataRowView;
                if (selected == null)
                    return;
                string pkg = selected[PACKAGE].ToString();
                string destination = FileSaveHelper.GetSaveFile("SSIS Packages|*.dstx", ".dtsx");
                if (destination == null)
                    return;
                try
                {
                    dg.Cursor = System.Windows.Input.Cursors.Wait;
                    DTS_SaveHelper.SaveDTSPackage(destination, pkg, setParameters[INTEGRATION_SERVICES] as string);
                }
                catch (Exception ex)
                {
                    (new Alert("Unable to upload Package:" + ex.Message, Choice: false)).ShowDialog();
                }
                finally
                {
                    dg.Cursor = System.Windows.Input.Cursors.Arrow;
                }
            };
            cm.Items.Add(profile);
            cm.Items.Add(upload);
            #endregion
            MenuItem m = MenuItemBuilder.BuildInitial("DTS Package Management");
            Dictionary<string, Action> toPass = new Dictionary<string, Action>();
            toPass.Add("Check Profiles", () =>
            {
                var ds = Connection.RunCommand(new System.Data.SqlClient.SqlCommand(PROFILE_SELECT) { CommandType = CommandType.StoredProcedure });
                if (ds == null || ds.Tables == null || ds.Tables.Count == 0)
                    return;
                _caller.UpdateDisplay(ds.Tables[0], internalID, cm);                
            });
            

            /*
            toPass.Add("Save Package", () =>
            {

                ProfileSelection ps = ProfileSelection.ChooseProfile(Connection, setParameters[PROFILE_SELECT] as string,
                    setParameters[PROFILE_ID] as string, setParameters[PROFILE_DESCRIPTION] as string, setParameters[PACKAGE] as string);
                if (ps == null)
                    return;
                OpenFileDialog ofd = new OpenFileDialog
                {
                    AddExtension = false,
                    CheckFileExists = true,
                    DefaultExt = "dtsx",
                    Multiselect = false
                };
                var b = ofd.ShowDialog() ?? false;
                if (!b)
                    return;
                DTS_SaveHelper.UploadDTSPackage(ps.PackagePath, ofd.FileName, setParameters[INTEGRATION_SERVICES] as string);
            });
            toPass.Add("Download", () =>
            {
                ProfileSelection ps = ProfileSelection.ChooseProfile(Connection, setParameters[PROFILE_SELECT] as string,
                    setParameters[PROFILE_ID] as string, setParameters[PROFILE_DESCRIPTION] as string, setParameters[PACKAGE] as string);
                if (ps == null)
                    return;
                string destination = FileSaveHelper.GetSaveFile("SSIS Packages|*.dstx", ".dtsx");
                if (destination == null)
                    return;
                DTS_SaveHelper.SaveDTSPackage(destination, ps.PackagePath, setParameters[INTEGRATION_SERVICES] as string);
            });
            m = MenuItemBuilder.Build(m, toPass);
            */
            return m;
        }
         
    }
    public class ProfileSelection
    {
        public int? ProfileID { get; set; }
        public string Description { get; set; }
        public string PackagePath { get; set; }
        public ProfileSelection(int? Profile, string Desc, string Package)
        {
            ProfileID = Profile;
            Description = Desc;
            PackagePath = Package;
        }
        public override string ToString()
        {
            string temp = ProfileID.HasValue ? ProfileID.Value + ": " : "";
            return $"{temp}{Description ?? PackagePath}";
        }
        public static List<ProfileSelection> GetProfiles(DataTable dt, string Profile, string Description, string Package)
        {
            if(Profile != null && Description != null)
                return (from DataRow r in dt.Rows
                        let pd = (int)r[Profile]
                        let desc = r[Description].ToString()
                        let pkg = r[Package].ToString()
                        where !string.IsNullOrWhiteSpace(pkg)
                        select new ProfileSelection(pd, desc, pkg)
                        ).ToList();
            if (Profile == null)
                return (from DataRow r in dt.Rows
                        let desc = r[Description].ToString()
                        let pkg = r[Package].ToString()
                        where !string.IsNullOrWhiteSpace(pkg)
                        select new ProfileSelection(null, desc, pkg)).ToList();
            if (Description != null)
                return (from DataRow r in dt.Rows
                        let pd = (int)r[Profile]
                        let pkg = r[Package].ToString()
                        where !string.IsNullOrWhiteSpace(pkg)
                        select new ProfileSelection(pd, null, pkg)).ToList();
            return null;
        }
        public static ProfileSelection ChooseProfile(DatabaseConnection Connection, 
            string PROFILE_SELECT, string PROFILE_COL, string PROFILE_DESC, string PACKAGE)
        {
            var ds = Connection.RunCommand(new System.Data.SqlClient.SqlCommand(PROFILE_SELECT) { CommandType = CommandType.StoredProcedure });
            if (ds == null || ds.Tables == null || ds.Tables.Count == 0)
                return null;
            /*
            List<ProfileSelection> profiles = ProfileSelection.GetProfiles(ds.Tables[0],
                setParameters[PROFILE_ID] as string,
                setParameters[PROFILE_DESCRIPTION] as string,
                setParameters[PACKAGE] as string);
                */
            var profiles = GetProfiles(ds.Tables[0], PROFILE_COL, PROFILE_DESC, PACKAGE);
            SelectorWindow sw = new SelectorWindow("Choose Profile", profiles?.ToArray());
            var b = sw.ShowDialog() ?? false;
            if (!b)
                return null;
            return (ProfileSelection)sw.Selection;
        }
    }
}
