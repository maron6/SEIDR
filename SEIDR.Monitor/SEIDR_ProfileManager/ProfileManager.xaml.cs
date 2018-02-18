using DataObjects.DataServices.APP;
using Ryan_UtilityCode.Processing;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Ryan_UtilityCode.Dynamics.Windows;
using static SEIDR_ProfileManager.MySetup;

namespace SEIDR_ProfileManager
{
    /// <summary>
    /// Interaction logic for ProfileManager.xaml
    /// </summary>
    public partial class ProfileManager : Ryan_UtilityCode.Dynamics.Configurations.BasicSessionWindow
    {
        DatabaseConnection PreprocessSql;        
        public const string Profile_SS = "STAGING.usp_LoadProfile_ss";
        public const string BatchTypes_SL = "SELECT distinct LoadBatchTypeCode FROM APP.LoadProfiles WHERE Active = 1";
        static List<string> _BatchTypeList = new List<string>();        
        LoadProfiles EditProfile = null;
        bool _CanSave;
        bool CanSave { get { return _CanSave; } set { this.SaveProfile.IsEnabled = value; _CanSave = value; } }
        /// <summary>
        /// Creates a window for managing DataServices LoadProfiles
        /// </summary>
        /// <param name="Profile">Nullable ProfileID. pass a value to setup initially as though a profile has already  been chosen</param>
        /// <param name="connection">Override the default connection</param>
        public ProfileManager(int? Profile = null, DatabaseConnection db = null)
        {
            
            InitializeComponent();
                        
            PreprocessSql = db ?? conn;//connection;
            
            if (PreprocessSql == null)
            {
                throw new Exception("No Database connection set up.");                                
            }
            
#if DEBUG
            EditProfile = new LoadProfiles(db :PreprocessSql);
            if (!EditProfile.Exists(true))
            {
                DBTableManager<LoadProfiles>.Create(dbConn:PreprocessSql);
            }
            EditProfile = null;
#endif
            //LoaderProfileOptions.IsEnabled = false;
            //CanSave = false; //Handled by togglePanels..
            //Organization.ItemsSource = OrganizationSelector.GetOrgs(PreprocessSql);
            if (_BatchTypeList.Count == 0)
            {
                using (SqlConnection c = new SqlConnection(PreprocessSql.ConnectionString))
                {
                    c.Open();
                    using (SqlCommand cmd = new SqlCommand(BatchTypes_SL, c))
                    {
                        DataTable dt = new DataTable();
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        sda.Fill(dt);
                        foreach (DataRow r in dt.Rows)
                        {
                            _BatchTypeList.Add(r[0] as string);
                        }
                    }
                    c.Close();
                }
            }
            BatchTypeList.ItemsSource = _BatchTypeList;
            TogglePanels(false);
            if (Profile.HasValue)
            {
                Setup(Profile.Value);
            }
        }

        private void ProfileChooser_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            LoaderProfileOptions.IsEnabled = false;
            ProfileChooser pc = new ProfileChooser();// PreprocessSql);            
            var r = pc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                EditProfile = DBTableManager<LoadProfiles>.PopulateFromRow(pc.Profile.Row, dbConn: PreprocessSql);
                FolderMenu.IsEnabled = true;
                Setup(EditProfile.LoadProfileID.Value);
                CanSave = true;
                //TogglePanels(true, Update:true); //Called by setup                 
            }
            else if(EditProfile == null)
            {
                LoaderProfileOptions.IsEnabled = false;
                FolderMenu.IsEnabled = false;
                CurrentProfileLabel.Content = "(No Profile Chosen)";
                CanSave = false;
                TogglePanels(false);
            }
        }
        private void TogglePanels(bool Enable, bool Update = false)
        {
            TopPanel.IsEnabled = Enable;
            LeftPanel.IsEnabled = Enable;
            MiddlePanel.IsEnabled = Enable;
            RightPanel.IsEnabled = Enable;
            GeneralUploadPackage.IsEnabled = Enable;
            General.IsEnabled = Enable;
            CanSave = Enable;
            LoaderProfileOptions.IsEnabled = Enable;
            FolderMenu.IsEnabled = Enable && Update;
            LM_STEP.IsEnabled = Enable && Update;            
        }
        private void Setup(int profile)
        {
            
            Dictionary<string, object> d = new Dictionary<string, object>();
            d.Add("LoadProfileID", profile);
            //LoadProfile vwProfile = DBViewManager<LoadProfile>.SelectSingle(d, connectionName);
            EditProfile = DBTableManager<LoadProfiles>.PopulateFromRow(DBTableManager<LoadProfiles>.SelectSingle(d, db: PreprocessSql));
            CurrentProfileID.Content = "Current ProfileID:" + profile;
            //LoaderProfileOptions.IsEnabled = true; //ToggleProfiles
            ProfileDescription.Text = EditProfile.Name;
            CurrentProfileLabel.Content = profile.ToString();
            Organization.Content = OrganizationSelector.GetOrgDescription(PreprocessSql, EditProfile.OrganizationID)?? "Select an Organization";
            if(!_BatchTypeList.Contains(EditProfile.LoadBatchTypeCode))
            {
                _BatchTypeList.Add(EditProfile.LoadBatchTypeCode);
                BatchTypeList.ItemsSource = _BatchTypeList;                
            }
            BatchTypeList.SelectedIndex = _BatchTypeList.IndexOf(EditProfile.LoadBatchTypeCode);
            FileMask.Text = EditProfile.InputFilter;
            DayOffset.Value = EditProfile.daysOffset;
            InputFileDateFormat.Text = EditProfile.DateMask;
            ChooseParent.Content = EditProfile.ParentProfileID.HasValue ? EditProfile.ParentProfileID.Value.ToString() : "Choose Parent";
            RegisterOnly.IsChecked = EditProfile.RegisterOnly;            
            LoadSequence.IsChecked = EditProfile.SeqControl;
            //Active.IsChecked = !EditProfile.Active.HasValue || EditProfile.Active.Value == 1;
            TogglePanels(true, true);
        }

        private void ProfileDownloadPackage_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            //TODO: Remove general upload/download, only do for the chosen profile?
            ProfileChooser pc = new ProfileChooser(); // PreprocessSql);
            var r = pc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                string path = pc.Profile["PackagePath"] as string;
                if (path != null)
                {
                    string to = GetSavePath(System.IO.Path.GetFileName(path)); //Remove folder from path name 
                    DTS_SaveHelper.SaveDTSPackage(to, path, 
                        settings[INTEGRATION_SERVICES] as string, 
                        settings[PACKAGE_FOLDER] as string);
                }
            }
        }
        //Named as general, but really it's for selected profile
        private void GeneralUploadPackage_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (EditProfile == null || EditProfile.LoadProfileID == null){
                new Alert("No Active profile chosen to upload to.", Choice:false).ShowDialog();
                return;
            }
            if (string.IsNullOrWhiteSpace(EditProfile.PackagePath))
            {
                new Alert("PackagePath is not set up", Choice: false).ShowDialog();
                return;
            }
            string FileSource = ChoosePackage();
            if (FileSource == null)
                return;
            FileSaveHelper.UploadDTSPackage(EditProfile.PackagePath, FileSource, 
                settings[INTEGRATION_SERVICES] as string, 
                settings[PACKAGE_FOLDER] as string);
            
            /*
            SaveNameSet sns = new SaveNameSet(System.IO.Path.GetFileNameWithoutExtension(FileSource));
            var r = sns.ShowDialog();
            if (r.HasValue && r.Value)
            {
                //string xml = System.IO.File.ReadAllText(open.FileName);
                //Save xml to the package server as 'MetrixPreprocessing\' + sns.FileName
                string SaveName = @"\MetrixPreprocessing\" + sns.FileName;
                //SavePackage(FileSource, SaveName);
                FileSaveHelper.UploadDTSPackage(SaveName, FileSource, Settings);
            }   */
            
        }
    
        private string ChoosePackage()
        {
            Microsoft.Win32.OpenFileDialog open = new Microsoft.Win32.OpenFileDialog();
            open.CheckFileExists = true;
            open.DefaultExt = "*.dtsx";
            open.Filter = "SSIS Packages|*.dtsx";
            open.Multiselect = false;
            var r = open.ShowDialog();
            if (r.HasValue && r.Value)
                return open.FileName;
            return null;
        }
        private void ProfileUploadPackage_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ProfileChooser pc = new ProfileChooser();// PreprocessSql);
            var r = pc.ShowDialog();
            if (!r.HasValue || !r.Value)
                return;
            
            LoadProfile lp = DBViewManager<LoadProfile>.GetViewFromDataRow(pc.Profile.Row);
            if (lp.SSISPackage == null)
                return;

            string fPath = ChoosePackage();
            //SavePackage(fPath, pc.Profile["SSISPackage"] as string);
            DTS_SaveHelper.UploadDTSPackage(fPath, 
                                            lp.SSISPackage, //pc.Profile["SSISPackage"] as string, 
                                            settings[INTEGRATION_SERVICES] as string, 
                                            settings[PACKAGE_FOLDER] as string);
        }

        private void General_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            //Get package for this profile...
            string xml = EditProfile.PackagePath; //use to load xml to string
            if(xml == null)
            {
                new Alert("No package path has been set up for this profile.", Choice: false).ShowDialog();
                return;
            }
            string to = GetSavePath(System.IO.Path.GetFileName(EditProfile.PackagePath));            
            DTS_SaveHelper.SaveDTSPackage(to, xml, 
                settings[INTEGRATION_SERVICES] as string, 
                settings[PACKAGE_FOLDER] as string);
        } 
        private string GetSavePath(string TempName)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.FileName = TempName;
            sfd.ValidateNames = true;
            sfd.AddExtension = true;
            sfd.DefaultExt = ".dtsx";
            sfd.Filter = "DTS Packages |*.dtsx";
            var r = sfd.ShowDialog();
            if (r.HasValue && r.Value)
                return sfd.FileName;
            return null;
        }

        private void Thread_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (EditProfile.LoadBatchTypeCode == "DMAP")
            {
                Alert noDMAP_ThreadEdit = new Alert("You cannot change the thread on a DMAP LoadProfile.", Choice: false);
                noDMAP_ThreadEdit.ShowDialog();
                return;
            }
            GetNumeric gn = new GetNumeric(false);
            var r = gn.ShowDialog();
            if (!r.HasValue || !r.Value)
                return;
            string setThread = "UPDATE APP.LoadProfile SET ThreadID = @Thread WHERE LoadProfileID = @LoadProfileID";
            using (SqlConnection c = new SqlConnection(PreprocessSql.ConnectionString))
            {
                c.Open();
                using (SqlCommand sc = new SqlCommand(setThread, c))
                {
                    sc.Parameters.AddWithValue("@Thread", gn.value);
                    sc.Parameters.AddWithValue("@LoadProfileID", EditProfile.LoadProfileID.Value);                    
                    sc.ExecuteNonQuery();                                        
                }
                c.Close();
            }
        }

        private void Position_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            GetNumeric gn = new GetNumeric(false);
            var r = gn.ShowDialog();
            if(!r.HasValue || !r.Value)
                return;            
            string posSet = "UPDATE APP.LoadProfile SET Position = @Pos WHERE LoadProfileID = @LoadProfileID";
            using (SqlConnection c = new SqlConnection(PreprocessSql.ConnectionString))
            {
                c.Open();
                using (SqlCommand sc = new SqlCommand(posSet, c))
                {
                    sc.Parameters.AddWithValue("@Pos", gn.value);
                    sc.Parameters.AddWithValue("@LoadProfileID", EditProfile.LoadProfileID.Value);
                    sc.ExecuteNonQuery();
                }
                c.Close();
            }
        }

        private void HoldDate_Click(object sender, RoutedEventArgs e)
        {
            if(EditProfile != null)
                EditProfile.HoldProfileDate = null;
            e.Handled = true;
        }       
        private void CheckFolders()
        {
            if (EditProfile.InputFolder != null && !EditProfile.InputFolder.Contains(EditProfile.LoadProfileID.ToString()))
                return;
            if (settings[CHECK_FOLDERS] == null || !(bool)settings[CHECK_FOLDERS])
                return;
            //Make sure shortcuts exist, and the main folders.
            string BaseFolder = System.IO.Path.Combine(ANDROMEDA_BASE_FOLDER, Organization.Content.ToString()) + "\\";
            if (!System.IO.Directory.Exists(BaseFolder))
            {
                System.IO.Directory.CreateDirectory(BaseFolder);
            }            
            var d = new System.IO.DirectoryInfo(BaseFolder);
            var subD = d.GetDirectories("*master*", System.IO.SearchOption.TopDirectoryOnly);
            if (subD.Length == 0)
            {
                System.IO.Directory.CreateDirectory(BaseFolder + "MasterLoads\\");
            }
            subD = d.GetDirectories("*daily*", System.IO.SearchOption.TopDirectoryOnly);
            if (subD.Length == 0)
            {
                System.IO.Directory.CreateDirectory(BaseFolder + "DailyLoads\\");
                BaseFolder = BaseFolder + "DailyLoads\\";
            }
            else
            {
                BaseFolder = subD[0].FullName;                
            }
            d = new System.IO.DirectoryInfo(BaseFolder);
            subD = d.GetDirectories("*Preprocess*", System.IO.SearchOption.TopDirectoryOnly);
            if (subD.Length == 0)
            {
                d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, "_Preprocessing", EditProfile.LoadBatchTypeCode));
            }
            else
            {
                d = subD[0];
                subD = d.GetDirectories("*" + EditProfile.LoadBatchTypeCode + "*", System.IO.SearchOption.TopDirectoryOnly);
                if (subD.Length == 0)
                {
                    d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, EditProfile.LoadBatchTypeCode));
                }
                else
                {
                    d = subD[0];
                }
            }
            subD = d.GetDirectories(EditProfile.LoadProfileID.Value.ToString());
            if (subD.Length == 0)
            {
                d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, EditProfile.LoadProfileID.Value.ToString()));
            }
            else
            {
                d = subD[0];
            }
            BaseFolder = d.FullName;
            if (!string.IsNullOrWhiteSpace(LongDescription.Text))
            {
                using (var sw = new System.IO.StreamWriter(System.IO.Path.Combine(BaseFolder, "Description.txt"), false))
                {
                    //sw.Write(new TextRange(this.LongDescription.Document.ContentStart, LongDescription.Document.ContentEnd).Text);
                    sw.Write(LongDescription.Text);
                }
            }
            subD = d.GetDirectories("*input*", System.IO.SearchOption.TopDirectoryOnly);
            System.IO.DirectoryInfo iDir;
            string input = "";
            string output = "";
            string devInput = "";
            if (subD.Length == 0)
            {
                iDir = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, "Input"));
                input = iDir.FullName;
            }
            else
            {
                iDir = subD[0];
                input = iDir.FullName;
            }
            subD = d.GetDirectories("*output*", System.IO.SearchOption.TopDirectoryOnly);
            if (subD.Length == 0)
            {
                output = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, "Output")).FullName;
            }
            else
            {
                output = subD[0].FullName;
            }
            subD = iDir.GetDirectories("*dev*", System.IO.SearchOption.TopDirectoryOnly);
            if (subD.Length == 0)
            {
                devInput = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(input, "DEV")).FullName;
            }
            else
            {
                devInput = subD[0].FullName;              
            }
            FileSaveHelper.CreateShortCut(System.IO.Path.Combine(BaseFolder, "Dev"), devInput);
            FileSaveHelper.CreateShortCut(System.IO.Path.Combine(devInput, "Output"), output);

            if (EditProfile.InputFolder == null) 
            {
                EditProfile.InputFolder = input;
                EditProfile.OutputFolder = output;
            }
        }
        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (EditProfile != null)
            {
                bool newProfile = !EditProfile.LoadProfileID.HasValue; //New if it does not have a loadprofileID yet
                EditProfile.Name = ProfileDescription.Text;
                EditProfile.LoadBatchTypeCode = BatchTypeList.Text; //.SelectedValue.ToString();
                EditProfile.InputFilter = FileMask.Text;
                EditProfile.InputMask = FileMask.Text;
                EditProfile.DateMask = InputFileDateFormat.Text;
                
                //EditProfile.daysOffset = (GetDayOffset.Content as short?)??0;
                //EditProfile.ProfileType = Convert.ToByte(ProfileType.SelectedIndex + 1);
                EditProfile.LU = DateTime.Now;
                if (!newProfile)
                {
                    CheckFolders();
                    if (string.IsNullOrWhiteSpace(EditProfile.PackagePath))
                    {
                        EditProfile.PackagePath = Organization.Content + "_" + EditProfile.LoadBatchTypeCode + "_" + EditProfile.LoadProfileID.Value.ToString();
                    }
                }
                
                EditProfile.InsertUpdate();
                if (newProfile)
                {
                    CheckFolders();
                    if (string.IsNullOrWhiteSpace(EditProfile.PackagePath))
                    {
                        EditProfile.PackagePath = Organization.Content + "_" + EditProfile.LoadBatchTypeCode + "_" + EditProfile.LoadProfileID.Value.ToString();
                    }
                    EditProfile.InsertUpdate();
                    TogglePanels(false);
                }
                else
                {
                    LoaderProfileOptions.IsEnabled = true;
                    TogglePanels(true, true);
                }
                string prof = EditProfile.LoadProfileID.HasValue ? EditProfile.LoadProfileID.Value.ToString() : "(No ProfileID)";
                CurrentProfileID.Content = prof;
                CurrentProfileLabel.Content = prof;
                foreach (object item in ComplexParentList.Items)
                {
                    SqlCommand c = new SqlCommand("STAGING.usp_ComplexParent_i"){ CommandType=CommandType.StoredProcedure};
                    c.Parameters.AddWithValue("@LoadProfileID", EditProfile.LoadProfileID);
                    c.Parameters.AddWithValue("@ParentProfile", Convert.ToInt32(item));
                    PreprocessSql.RunQuery(c);
                    c.Dispose();
                }
            }
            else
            {
                Alert a = new Alert("No profile chosen.", Choice: false);
                a.ShowDialog();
            }
            e.Handled = true;
        }

        private void HoldDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditProfile != null)
                EditProfile.HoldProfileDate = HoldDatePicker.SelectedDate;
            else if(HoldDatePicker.SelectedDate != null)
                HoldDatePicker.SelectedDate = null;
            e.Handled = true;
        }

        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            CurrentProfileLabel.Content = "LoadProfileID not set yet.";
            LoaderProfileOptions.IsEnabled = false;
            FolderMenu.IsEnabled = false;
            TogglePanels(true);
            if (EditProfile == null)
            {
                EditProfile = new LoadProfiles();
            }
            else
            {
                EditProfile.LoadProfileID = null;
            }
            e.Handled = true;
        }

        private void Selector_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Organization_Click(object sender, RoutedEventArgs e)
        {
            OrganizationSelector os = new OrganizationSelector(PreprocessSql);
            var r = os.ShowDialog();
            if (!r.HasValue || !r.Value)
                return;
            Organization.Content = os.selected["Description"] as string;//Description will be grabbed from here for file path
            EditProfile.OrganizationID = os.selected["OrganizationID"] as int?;
            EditProfile.FacilityId = os.selected["FacilityID"] as Int16?;
            e.Handled = true;
        }
        

        private void ChooseParent_Click(object sender, RoutedEventArgs e)
        {
            ProfileChooser pc = new ProfileChooser(); // this.PreprocessSql);
            var r = pc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                EditProfile.ParentProfileID = pc.Profile["LoadProfileID"]as int?;
            }
            e.Handled = true;
        }

        private void ClearParent_Click(object sender, RoutedEventArgs e)
        {
            if (EditProfile != null)
                EditProfile.ParentProfileID = null;
            ChooseParent.Content = "Choose Parent Profile";
            e.Handled = true;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(EditProfile !=  null)
                EditProfile.SeqControl = LoadSequence.IsChecked;
            e.Handled = true;
        }

        private void AddComplexParent_Click(object sender, RoutedEventArgs e)
        {
            ProfileChooser pc = new ProfileChooser(); // PreprocessSql);
            var r = pc.ShowDialog();
            if (r.HasValue && r.Value)
            {
                ComplexParentList.Items.Add((int)pc.Profile["LoadProfileID"]);
            }
            e.Handled = true;
        }

        private void DropComplex_Click(object sender, RoutedEventArgs e)
        {
            var Parent = ComplexParentList.SelectedItem;
            Alert a = new Alert("Delete Parent Profile Relation with Profile " + Parent.ToString(), true);
            var r = a.ShowDialog();
            if (!r.Value)
                return;
            SqlCommand c = new SqlCommand("STAGING.usp_ComplexParent_d") { CommandType = CommandType.StoredProcedure };
            c.Parameters.AddWithValue("@LoadProfileID", EditProfile.LoadProfileID);
            c.Parameters.AddWithValue("@ParentProfile", Convert.ToInt32(Parent));
            PreprocessSql.RunQuery(c);
            c.Dispose();

            ComplexParentList.Items.Remove(Parent);
            e.Handled = true;
        }

        private void Active_Checked(object sender, RoutedEventArgs e)
        {
            if (EditProfile != null) 
                //EditProfile.Active = Active.IsChecked.HasValue ? Convert.ToInt16(Active.IsChecked.Value ? 1 : 0) : (short)1;
            e.Handled = true;
        }

        private void Folder_Input_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(EditProfile.InputFolder);
            }
            catch { }
            e.Handled = true;
        }

        private void Folder_Output_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(EditProfile.OutputFolder);
            }
            catch { }
            e.Handled = true;
        }

        private void DayOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if(EditProfile != null)
                EditProfile.daysOffset = DayOffset.Value.HasValue ? Convert.ToInt16(DayOffset.Value.Value) : Convert.ToInt16(0);
            e.Handled = true;
        }

        private void RegisterOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (EditProfile != null)
                EditProfile.RegisterOnly = RegisterOnly.IsChecked;
            e.Handled = true;
        }

        private void LM_STEP_Click(object sender, RoutedEventArgs e)
        {
            //Type ls = typeof(LoaderMagic.DataBaseObjects.LoaderMagicStep);
            SqlCommand c = new SqlCommand("SEIDR.usp_LoaderMagicStep_SL");
            c.CommandType = CommandType.StoredProcedure;
            c.Parameters.AddWithValue("@LoadProfileID", EditProfile.LoadProfileID);
            CRUDWindow crw = new CRUDWindow(conn, "Loader magic Step", c, ProcedureInfo.STEP_INFO, ProcedureInfo.STEP_CREATE, ProcedureInfo.STEP_UPDATE,
                ProcedureInfo.STEP_DELETE);
            crw.Nest("Edit Loader magic Step Parameters", "SEIDR.usp_LoaderMagicStepParameter_SL", ProcedureInfo.STEP_PARAMETER_INFO,
                ProcedureInfo.STEP_PARAMETER_CREATE, ProcedureInfo.STEP_PARAMETER_EDIT, ProcedureInfo.STEP_PARAMETER_DELETE);
            crw.ShowDialog();
            //Action setupOpBox = () =>
            //{
            //    var opsList = DBTableManager<LoaderMagic.DataBaseObjects.LoaderMagicOperation>.SelectRecordList(null, db: PreprocessSql);
            //    var records = (from op in opsList
            //                   select new ComboDisplayItem(op.AssemblyName, op.LoaderMagicOperationID)
            //                   ).ToArray();
            //    ComboDisplay t = new ComboDisplay("LoaderMagicOperationID", records);
            //    Models.LoaderMagicComboResources.AddResource(ls, t);
            //};
            //if (Models.LoaderMagicComboResources.GetDisplay(ls) == null)
            //{
            //    setupOpBox();
            //}
            //LoaderMagicEditor lmse = new LoaderMagicEditor(ls, EditProfile.LoadProfileID.Value);
            //lmse.AddParameterSteps(setupOpBox);
            //lmse.ShowDialog();
            //Need a step editor
            e.Handled = true;
        }

        private void LM_OPS_Click(object sender, RoutedEventArgs e)
        {
            //LoaderMagicOperationEditor lmoe = new LoaderMagicOperationEditor(this.PreprocessSql);
            //LoaderMagicEditor lmoe = new LoaderMagicEditor(typeof(LoaderMagic.DataBaseObjects.LoaderMagicOperation), null);
            //lmoe.ShowDialog();
            
            SqlCommand c = new SqlCommand("SEIDR.usp_LoaderMagicOperation_SL");
            c.CommandType = CommandType.StoredProcedure;
            
            CRUDWindow crw = new CRUDWindow(conn, "Loader Magic Operations", c, ProcedureInfo.OPERATION_INFO, ProcedureInfo.OPERATION_CREATE, ProcedureInfo.OPERATION_UPDATE,
                ProcedureInfo.OPERATION_DELETE);
            crw.ShowDialog();
            e.Handled = true;
        }
    }
}
