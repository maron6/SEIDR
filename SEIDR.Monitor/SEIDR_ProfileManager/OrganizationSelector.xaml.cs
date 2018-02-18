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
using System.Data;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using Ryan_UtilityCode.Dynamics.Windows;
using Ryan_UtilityCode.Dynamics;
using System.Data.SqlClient;
using static SEIDR_ProfileManager.MySetup;
using Ryan_UtilityCode.Dynamics.Configurations;

namespace SEIDR_ProfileManager
{
    /// <summary>
    /// Interaction logic for OrganizationSelector.xaml
    /// </summary>
    public partial class OrganizationSelector : BasicSessionWindow
    {
        DatabaseConnection tempDb;
        public static DataTable organizations
        {
            get { return SessionManager["OrganizationsList"] as DataTable; }
            set { SessionManager["OrganizationsList"] = value; }
        }        
        public DataRow selected;
        const string orgSelect = "STAGING.usp_Organization_sl";    
        public OrganizationSelector(DatabaseConnection db = null)
        {
            if (db == null)
                db = conn;
            tempDb = db;
            InitializeComponent();
            if (organizations == null)
            {
                SetupOrgs(tempDb);
            }
            OrganizationData.ItemsSource = organizations.DefaultView;
        }
        private static void SetupOrgs(DatabaseConnection db)
        {
            if (db == null)
                db = conn;
            organizations = new DataTable();
            using (SqlConnection c = new SqlConnection(db.ConnectionString))
            {
                c.Open();
                using (SqlCommand sc = new SqlCommand(orgSelect, c) { CommandType = CommandType.StoredProcedure })
                {
                    SqlDataAdapter sda = new SqlDataAdapter(sc);
                    sda.Fill(organizations);
                }
                c.Close();
            }
            organizations.PrimaryKey = new DataColumn[] { organizations.Columns["OrganizationID"] };
        }
        public static List<string> GetOrgs(DatabaseConnection db)
        {
            if (db == null)
                db = conn;
            if (organizations == null)
            {
                SetupOrgs(db);
            }
            var q = from DataRow record in organizations.Rows
                    select record["Description"].ToString();
            return q.ToList<string>();
            /*List<string> ret = new List<string>();
            foreach (DataRow r in organizations)
            {
                ret.Add(r["Description"].ToString());
            }
            return ret;*/
        }
        public static string GetOrgDescription(DatabaseConnection db, int? OrgID)
        {
            if (db == null)
                db = conn;
            if (!OrgID.HasValue)
                return null;
            if (organizations == null)
            {
                SetupOrgs(db);
            }
            foreach (DataRow r in organizations.Rows)
            {
                if ((int)r["OrganizationID"] == OrgID.Value)
                {
                    return r["Description"] as string;
                }
            }
            return null;
        }
        public static bool AddOrganization(string Description, int Organization, int FacilityID)
        {
            if (organizations == null)
            {
                Alert o = new Alert("You should check the existing organizations before trying to add a new one.", Choice: false);
                o.ShowDialog();
                return false;
            }
            if (organizations.Rows.Contains(Organization))
            {
                Alert o = new Alert("OrganizationID already exists", Choice: false);
                o.ShowDialog();
                return false;
            }
            DataRow r = organizations.NewRow();
            r["Description"] = Description;
            r["OrganizationID"] = Organization;
            r["FacilityID"] = FacilityID;
            organizations.Rows.Add(r);
            //organizations.Rows.Add(Description, Organization, FacilityID); //Not sure if this will work as intended... 
            //Probably fine as long as the proc returns them in this order
            return true;
        }
        private void Set()
        {
            selected = (OrganizationData.ItemsSource as DataView).Table.Rows[OrganizationData.SelectedIndex];
            //selected = OrganizationData.SelectedItem as DataRowView;
            this.DialogResult = true;
            this.Close();

        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
            Set();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Set();
        }

        private void ForceRefresh_Click(object sender, RoutedEventArgs e)
        {
            organizations.Clear();
            SetupOrgs(tempDb);
            
            OrganizationData.ItemsSource = organizations.DefaultView;
        }
    }
}
