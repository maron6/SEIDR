using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ryan_UtilityCode.Processing.Data.DBObjects;
using Ryan_UtilityCode.Dynamics.Windows;
using Ryan_UtilityCode.Dynamics;

namespace SEIDR_ProfileManager.Objects
{
    public class BatchProfile: DBTable
    {
        #region Inherited Constructor
        public BatchProfile(DatabaseConnection db) : base(db) { }
        public BatchProfile(string dbName):base(dbName){ }
        public BatchProfile():base(){ }
        #endregion
        [EditableObjectInfo(false)]
        public int LoadProfileID { get; set; }
        [EditableObjectInfo(false)]
        public int StepCount { get; set; }
    }
    public class BatchProfileStep: DBTable
    {
        #region Inherited Constructor
        public BatchProfileStep(DatabaseConnection db) : base(db) { }
        public BatchProfileStep(string dbName):base(dbName){ }
        public BatchProfileStep():base(){ }
        #endregion
        [EditableObjectInfo(false)]
        public int StepID { get; set; }
        [EditableObjectInfo(false)]
        public int OperationID { get; set; }

        public Dictionary<int, BatchProfileStepParameter> parametersList; //Key should match ID
        [EditableObjectMethod("Edit Parameter", refreshAfter:false)]
        public void SelectStepParameters()
        {
            SelectorWindow sw = new SelectorWindow("Select Step to edit", parametersList.Values.ToArray());
            if(sw.ShowDialog() ?? false)
            {
                BatchProfileStepParameter edit = (sw.Selection as BatchProfileStepParameter).DClone();
                if(edit != null)
                {
                    EditableObjectDisplay paramEdit = new EditableObjectDisplay(edit, "Edit LM Step, " + sw.ToString(), ManagedSaving: true);
                    if(paramEdit.ShowDialog()?? false)
                    {
                        BatchProfileStepParameter update = (BatchProfileStepParameter)paramEdit.myData;
                        update.InsertUpdate();
                        parametersList[update.ID] = update;                        
                    }
                }
            }
        }
    }
    public class BatchProfileStepParameter : DBTable
    {
        #region Inherited Constructor
        public BatchProfileStepParameter(DatabaseConnection db) : base(db) { }
        public BatchProfileStepParameter(string dbName):base(dbName){ }
        public BatchProfileStepParameter():base(){ }
        #endregion
        [EditableObjectInfo(false)]
        public int ID { get; set; }
        public string Description { get; set; }
        [EditableObjectInfo(false)]
        public short ParameterNumber { get; set; }
        public override string ToString()
        {
            return $"Parameter {ParameterNumber}: {Description}";
        }
    }

}
