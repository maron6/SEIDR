using System;
using System.Data;
using System.Data.SqlClient; //Just to make sure inherited functions do not have any issues.
using Ryan_UtilityCode.Processing.Data.DBObjects.Attributes;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using Ryan_UtilityCode.Dynamics.Windows; 



/// <summary>
/// Business Object representation for Table DataServices.APP.LoadProfiles 
/// </summary>
namespace SEIDR_ProfileManager
{
	//Non-Unique Index On Column(s): LoadProfileID, LoadBatchTypeCode, Active
	//Column(s) included with the index: OrganizationID, facilityid, InputFolder, InputFilter, InputMask, PackagePath, OutputFolder, TrimTable, TrimSchema, DateMask, daysOffset, ServerName
	[DBIndex("NonClusteredIndex-20160114-131806", false, 3, "LoadProfileID", "LoadBatchTypeCode", "Active", "OrganizationID", "facilityid", "InputFolder", "InputFilter", "InputMask", "PackagePath", "OutputFolder", "TrimTable", "TrimSchema", "DateMask", "daysOffset", "ServerName")]
	//Non-Unique Index On Column(s): LoadProfileID, LoadBatchTypeCode
	//Column(s) included with the index: OrganizationID, Name, InputFolder, PackagePath, HoldProfileDate
	[DBIndex("NonClusteredIndex-20160114-131946", false, 2, "LoadProfileID", "LoadBatchTypeCode", "OrganizationID", "Name", "InputFolder", "PackagePath", "HoldProfileDate")]
	[DBObjectDescription("DataServices", "APP")]
	public sealed class LoadProfiles : Ryan_UtilityCode.Processing.Data.DBObjects.DBTable
	{
		#region Inherited Constructor
        public LoadProfiles(DatabaseConnection db) : base(db) { }
		public LoadProfiles(string dbName):base(dbName){}
		public LoadProfiles():base(){}
		#endregion

		[DBKey(true)] //Primary Key
		public int? LoadProfileID {get;  set; } 
		public int? OrganizationID {get;  set; } 
		public short? FacilityId {get;  set; } 
		[DBColumnSize(25)]
		public string LoadBatchTypeCode{ get { 	return _LoadBatchTypeCode; } set { if(value != null && value.Length > 25) { if(ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:"+ value); else _LoadBatchTypeCode = value.Substring(0, 25); } else { _LoadBatchTypeCode = value; }}} private string _LoadBatchTypeCode;

		[DBColumnSize(50)]
        public string Name { get { return _Name; } set { if (value != null && value.Length > 50) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _Name = value.Substring(0, 50); } else { _Name = value; } } } private string _Name;

		[DBColumnSize(250)]
        public string InputFolder { get { return _InputFolder; } set { if (value != null && value.Length > 250) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _InputFolder = value.Substring(0, 250); } else { _InputFolder = value; } } } private string _InputFolder;

		[DBColumnSize(250)]
        public string InputFilter { get { return _InputFilter; } set { if (value != null && value.Length > 250) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _InputFilter = value.Substring(0, 250); } else { _InputFilter = value; } } } private string _InputFilter;

		[DBColumnSize(50)]
        public string InputMask { get { return _InputMask; } set { if (value != null && value.Length > 50) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _InputMask = value.Substring(0, 50); } else { _InputMask = value; } } } private string _InputMask;

		[DBColumnSize(500)]
        public string PackagePath { get { return _PackagePath; } set { if (value != null && value.Length > 500) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _PackagePath = value.Substring(0, 500); } else { _PackagePath = value; } } } private string _PackagePath;

        [EditableObjectInfo(false)]
		public DateTime? LU {get;  set; }
        [EditableObjectInfo(false)]
        public DateTime? DC {get;  set; }
        
		[DBColumnSize(250)]
        public string OutputFolder { get { return _OutputFolder; } set { if (value != null && value.Length > 250) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _OutputFolder = value.Substring(0, 250); } else { _OutputFolder = value; } } } private string _OutputFolder;

		public bool? Track {get;  set; } 
		public bool? SeqControl {get;  set; } 
		public int? ParentProfileID {get;  set; } 
		[DBColumnSize(150)]
        public string TrimTable { get { return _TrimTable; } set { if (value != null && value.Length > 150) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _TrimTable = value.Substring(0, 150); } else { _TrimTable = value; } } } private string _TrimTable;

		[DBColumnSize(50)]
        public string TrimSchema { get { return _TrimSchema; } set { if (value != null && value.Length > 50) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _TrimSchema = value.Substring(0, 50); } else { _TrimSchema = value; } } } private string _TrimSchema;

		[DBColumnSize(50)]
        public string DateMask { get { return _DateMask; } set { if (value != null && value.Length > 50) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _DateMask = value.Substring(0, 50); } else { _DateMask = value; } } } private string _DateMask;

		public short? daysOffset {get;  set; } 
		[DBColumnSize(35)]
        public string ServerName { get { return _ServerName; } set { if (value != null && value.Length > 35) { if (ThrowErrorOnVarcharTruncation)  throw new Exception("Value too large:" + value); else _ServerName = value.Substring(0, 35); } else { _ServerName = value; } } } private string _ServerName;

		public DateTime? HoldProfileDate {get;  set; }

        public bool? RegisterOnly { get; set; }

        public string LongDescription { get; set; }
        public void CheckFolders(string BASE_FOLDER, string Organization)
        {
            if (this.InputFolder != null && !InputFolder.Contains(this.LoadProfileID.ToString()))
                return;
            if (BASE_FOLDER == null || Organization == null)
                return;
            //Make sure shortcuts exist, and the main folders.
            string BaseFolder = System.IO.Path.Combine(BASE_FOLDER, Organization) + "\\";
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
                d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, "_Preprocessing", this.LoadBatchTypeCode));
            }
            else
            {
                d = subD[0];
                subD = d.GetDirectories("*" + this.LoadBatchTypeCode + "*", System.IO.SearchOption.TopDirectoryOnly);
                if (subD.Length == 0)
                {
                    d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, this.LoadBatchTypeCode));
                }
                else
                {
                    d = subD[0];
                }
            }
            subD = d.GetDirectories(this.LoadProfileID.Value.ToString());
            if (subD.Length == 0)
            {
                d = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(d.FullName, this.LoadProfileID.Value.ToString()));
            }
            else
            {
                d = subD[0];
            }
            BaseFolder = d.FullName;
            if (!string.IsNullOrWhiteSpace(LongDescription))
            {
                using (var sw = new System.IO.StreamWriter(System.IO.Path.Combine(BaseFolder, "Description.txt"), false))
                {
                    //sw.Write(new TextRange(this.LongDescription.Document.ContentStart, LongDescription.Document.ContentEnd).Text);
                    sw.Write(LongDescription);
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

            if (this.InputFolder == null)
            {
                this.InputFolder = input;
                this.OutputFolder = output;
            }
        }
    }
}