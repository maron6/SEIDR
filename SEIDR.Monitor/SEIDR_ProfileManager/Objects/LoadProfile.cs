using System;
using System.Data;
using System.Data.SqlClient; //Just to make sure inherited functions do not have any issues.
using Ryan_UtilityCode.Processing.Data;
using Ryan_UtilityCode.Processing.Data.DBObjects.Attributes;
using Ryan_UtilityCode.Processing.Data.DBObjects;


/// <summary>
/// Business Object representation for View DataServices.APP.LoadProfile 
/// <para>Note that all setter methods are private so that only Table objects can actually updated. 
/// </para>
/// </summary>
namespace DataObjects.DataServices.APP
{
	[DBObjectDescription("DataServices", "APP")]
    public sealed class LoadProfile : Ryan_UtilityCode.Processing.Data.DBObjects.DBView
	{
		#region Inherited Constructor
        public LoadProfile(DatabaseConnection db) : base(db) { }
		public LoadProfile(string dbName):base(dbName){}
		public LoadProfile():base(){}
		#endregion

		public int? LoadProfileID {get; private set; } 
		public int? OrganizationID {get; private set; } 
		public short? FacilityID {get; private set; } 
		public string LoadBatchTypeCode {get; private set; } 
		public string Description {get; private set; } 
		public string InputFolder {get; private set; } 
		public string OutputFolder {get; private set; } 
		public string FileMask {get; private set; } 
		public string SSISPackage {get; private set; } 
		public int? ThreadID {get; private set; } 
		public bool? Hold {get; private set; } 
		public bool? CheckCancel {get; private set; } 
		public DateTime? LastQueued {get; private set; } 
		public int? Position {get; private set; } 
		public bool? ForceFinish {get; private set; } 
		public bool? Track {get; private set; } 
		public short? Active {get; private set; } 
		public DateTime? Created {get; private set; } 
		public DateTime? Updated {get; private set; } 
		public int? ParentProfileID {get; private set; } 
		public bool? SequenceControl {get; private set; } 
		public string DateMask {get; private set; } 
		public short? DayOffset {get; private set; } 
		public string SchemaTrim {get; private set; } 
		public string TableTrim {get; private set; } 
		public string ServerInstanceName {get; private set; } 
		public DateTime? HoldProfileDate {get; private set; }
        public bool? RegisterOnly { get; private set; }
	}
}