using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ryan_UtilityCode.Dynamics.Configurations;
using Ryan_UtilityCode.Dynamics.Windows;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using System.Data;
using System.Data.SqlClient;
using static SEIDR_ProfileManager.MySetup;

namespace SEIDR_ProfileManager
{
    /// <summary>
    /// Interaction logic for SEIDR_ProfileManagement.xaml
    /// </summary>
    public partial class SEIDR_ProfileManagement : BasicSessionWindow
    {
        DatabaseConnection connection;
        const string BATCH_TYPE_LIST_KEY = "SEIDR_PROFILE Management Batch Types";
        ComboDisplay BatchTypeList
        {
            get
            {
                ComboDisplay temp = SessionManager[BATCH_TYPE_LIST_KEY] as ComboDisplay;
                if (temp == null)
                    LoadbatchTypeList();
                else
                    return temp;
                return SessionManager[BATCH_TYPE_LIST_KEY] as ComboDisplay;
            }
            set
            {
                SessionManager[BATCH_TYPE_LIST_KEY] = value;
            }
        }
        public SEIDR_ProfileManagement(int? Profile = null, DatabaseConnection db = null)
        {
            InitializeComponent();
            connection = db ?? MySetup.conn;
            if (connection == null)
            {
                throw new Exception("No Database connection set up.");
            }
        }
        private void LoadbatchTypeList()
        {
            DataTable dt = new DataTable();
            using(SqlConnection c = new SqlConnection(connection.ConnectionString))
            {
                c.Open();
                SqlDataAdapter sda = new SqlDataAdapter("SELECT distinct LoadBatchTypeCode FROM APP.LoadProfiles", c);
                sda.Fill(dt);
                c.Close();
            }
            List<ComboDisplayItem> temp = new List<ComboDisplayItem>();
            foreach(DataRow row in dt.Rows)
            {
                string s = row["LoadBatchTypeCode"] as string;
                temp.Add(new ComboDisplayItem(s, s));
            }
            BatchTypeList = new ComboDisplay("LoadBatchTypeCode", temp.ToArray());
        }
        LoadProfiles editProfiles = null;
        private void CreateNewProfile()
        {
            OrganizationSelector s = new OrganizationSelector(connection);
            var r = s.ShowDialog();
            if (r ?? false)
            {
                editProfiles = new LoadProfiles(connection);
                editProfiles.OrganizationID = s.selected["OrganizationID"] as int?;
                editProfiles.FacilityId = s.selected["FacilityID"] as short?;
                EditableObjectDisplay od = new EditableObjectDisplay(editProfiles,
                    "Create New Profile",
                    new string[] { "OrganizationID", "FacilityID" }, 
                    new ComboDisplay[] { BatchTypeList, ProfileChooser.GetProfileCombolist("ParentProfileID") },
                    false);
                r = od.ShowDialog();
                if(r ?? false)
                {
                    editProfiles.InsertUpdate();
                    if((bool)settings[CHECK_FOLDERS])
                        editProfiles.CheckFolders(settings[ANDROMEDA_BASE_FOLDER] as string, s.selected["Description"] as string);
                    ToggleActiveControls(true);
                    return;
                }                
            }
            editProfiles = null;
            ToggleActiveControls(false);
        }
        private void EditMode(int profileID)
        {
            editProfiles = DBTableManager<LoadProfiles>.SelectSingleWithKey("LoadProfileID", profileID, db: connection);
            ToggleActiveControls(true);
            /*
             * ToDo: Enable edit buttons for opening other windows 
             * 
             * ??? What other windows?            
             */
        }
        private void EditProfile(object sender, EventArgs e)
        {
            
            EditableObjectDisplay od = new EditableObjectDisplay(editProfiles,
                    "Create New Profile",
                    new string[] { "OrganizationID", "FacilityID" },
                    new ComboDisplay[] { BatchTypeList, ProfileChooser.GetProfileCombolist("ParentProfileID", editProfiles.ParentProfileID) },
                    false);
            var r = od.ShowDialog();
            if (r ?? false)
            {
                editProfiles.InsertUpdate();                                
            }
        }
        
        private void ToggleActiveControls(bool active)
        {
            //Set IsEnabled on the main controls
        }
        private void OpenOperationsEditor(object sender, RoutedEventArgs e)
        {
            
        }

    }
}



