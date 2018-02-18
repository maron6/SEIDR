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
using Ryan_UtilityCode.Processing.Data.DBObjects;
using System.Data;
using Ryan_UtilityCode.Dynamics.Windows;
using System.Data.SqlClient;
using static SEIDR_ProfileManager.MySetup;

namespace SEIDR_ProfileManager
{
    /// <summary>
    /// Interaction logic for ProfileChooser.xaml
    /// </summary>
    public partial class ProfileChooser : Ryan_UtilityCode.Dynamics.Configurations.BasicSessionWindow
    {
        public DataRowView Profile = null;
        public const string Profile_SL = "APP.usp_LoadProfile_sl_Selection";
        const string PROFILE_LIST_KEY = "SEIDR.Profile ListProfiles";
        public static DataTable ProfileList
        {
            get
            {
                var temp = SessionManager[PROFILE_LIST_KEY] as DataTable;
                if (temp != null)
                    return temp;
                SessionManager[PROFILE_LIST_KEY] = GetList();
                return SessionManager[PROFILE_LIST_KEY] as DataTable;
            }
            set
            {
                SessionManager.SetCache(PROFILE_LIST_KEY, value, 20); //Profiles are more likely to be changed than org and we're not providing a way to pick this..
                //SessionManager[PROFILE_LIST_KEY] = value;
            }
        }
        public static ComboDisplay GetProfileCombolist(string Name = "LoadProfileID", int? profileID = null)
        {
            List<ComboDisplayItem> temp = new List<ComboDisplayItem>();
            int index = -1;
            bool found = false;
            foreach(DataRow profile in ProfileList.Rows)
            {
                if (profileID.HasValue && !found)
                {
                    index++;//start at -1
                    if ((int)profile["LoadProfileID"] == profileID.Value)
                        found = true;                    
                }
                temp.Add(new ComboDisplayItem(profile["Description"].ToString(), (int)profile["LoadProfileID"]));
            }
            if (!found)
                index = -1; //Index sets the starting selected index when converting combo display to combo box
            return new ComboDisplay(Name, temp.ToArray(), index);
        }
        
        public ProfileChooser()
        {
            InitializeComponent();            
            //_conn = conn; //Deprecated, just from setup..
            //ProfileData.ItemsSource = null;            
            ProfileData.ItemsSource = ProfileList.AsDataView();            
        }        
        private static DataTable GetList()
        {
            DataTable temp = new DataTable();
            using (SqlConnection c = new SqlConnection(conn.ConnectionString))
            {
                c.Open();
                using (SqlCommand comm = new SqlCommand(Profile_SL, c) { CommandType = CommandType.StoredProcedure })
                {
                    SqlDataAdapter sda = new SqlDataAdapter(comm);                    
                    sda.Fill(temp);                    
                }
                c.Close();
            }
            return temp;
        }
        private void Set()
        {
            //DataRowView drv = 
            Profile = ProfileData.SelectedItem as DataRowView;            
            //Profile = drv["LoadProfileID"] as int?;
            this.DialogResult = true;
            this.Close();
        }
        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Set();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Set();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Profile = null;
            this.DialogResult = false;
            this.Close();
        }
    }
}
