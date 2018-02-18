using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace Ryan_UtilityCode.Dynamics.Windows
{
    /// <summary>
    /// Builds up a DTS application.
    /// </summary>
    public class DtsPackageBuilder
    {
        /// <summary>
        /// For determining type of DataFlow task to add to the package
        /// </summary>
        public enum DataFlowType
        {
            /// <summary>
            /// Script task
            /// </summary>
            Script,
            /// <summary>
            /// Source - File, Destination - Database
            /// </summary>
            File_DB,
            /// <summary>
            /// Source - Database, Destination - Database
            /// </summary>
            DB_DB,
            /// <summary>
            /// Source - File, Destination - File
            /// </summary>
            File_File,
            /// <summary>
            /// Source - Database, Destination - File
            /// </summary>
            DB_File
        }
        /// <summary>
        /// Stores the package to the given file path
        /// </summary>
        /// <param name="Filepath">Destination of saving</param>
        /// <param name="p">Package to save. If passed null, will use the current working package</param>
        public void SavePackage(string Filepath, Package p)
        {
            if (p == null)
                p = workingPackage;
            _App.SaveToXml(Filepath, p, null);
        }
        /// <summary>
        /// Stores the working package to the specified location
        /// </summary>
        /// <param name="Filepath"></param>
        public void SavePackage(string Filepath)
        {
            _App.SaveToXml(Filepath, workingPackage, null);
        }
        /// <summary>
        /// Connection managers
        /// </summary>
        public enum ConnectionType
        {
            /// <summary>
            /// For flat files
            /// </summary>
            FlatFile,
            /// <summary>
            /// For connections to SQL Server (Ole DB)
            /// </summary>
            OleDB
        }
        Application _App;
        /// <summary>
        /// The working package that gets added on to by the various methods.
        /// </summary>
        public Package workingPackage { get; private set; }
        short ConnectionCount = 0;
        short componentCount = 0;
        /// <summary>
        /// Initializes the working package and environment for building a package.
        /// <para>The package can be grabbed with the property 'workingPackage' after applying updates.</para>
        /// </summary>
        public DtsPackageBuilder()
        {
            _App = new Application();
            workingPackage = new Package();
        }
        /// <summary>
        /// Creates a new package. Existing package is lost and should be grabbed first if anything is to be done with it.
        /// </summary>
        /// <returns></returns>
        public Package CreateNewPackage()
        {
            workingPackage = null;
            workingPackage = new Package();        
            return workingPackage;
        }
        /// <summary>
        /// Adds a list of variable names and the starting value to the package as variables at the top level.
        /// </summary>
        /// <param name="mapping"></param>
        public void AddVariableCollection(Dictionary<string, object> mapping)
        {
            Variables pkgVars = workingPackage.Variables;
            foreach (var kv in mapping)
            {
                Variable temp = pkgVars.Add(kv.Key, false, "User", kv.Value);
            }
        }
        /// <summary>
        /// Adds a basic connection to the package, with optional name
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="Connstring"></param>
        /// <param name="Name"></param>
        public void AddConnection(ConnectionType connection, string Connstring, string Name = null)
        {
            if (Name == null)
                Name = $"Connection #{ConnectionCount++}: {connection.ToString()}";
            Connections c = workingPackage.Connections;
            switch (connection)
            {
                case ConnectionType.FlatFile:
                    {
                        ConnectionManager m = c.Add("File");
                        m.ConnectionString = Connstring;                        
                        m.Name = Name;
                        m.Description = FLAT_FILE_DESC;
                        return;
                    }
                case ConnectionType.OleDB:
                    {
                        ConnectionManager db = c.Add("OLEDB");
                        db.ConnectionString = Connstring;
                        db.Name = Name;
                        db.Description = OLE_DB_DESC;
                        return;
                    }
            }
        }
        /// <summary>
        /// Description to be added to Ole DB connection managers.
        /// </summary>
        public const string OLE_DB_DESC = "OLE DB Connection";
        /// <summary>
        /// Description to be added to Flat file connection managers and also to find them.
        /// </summary>
        public const string FLAT_FILE_DESC = "Flat File Connection";
        /// <summary>
        /// Adds a chain of Tasks to the package. These tasks will be connected and require success to complete each consecutive.
        /// <para>Calling the method again will create a separate chain of tasks that can run independently</para>
        /// </summary>
        /// <param name="taskTypes">See the Enum - describes the Task to add</param>
        public void AddChainedDataFlowTasks( DataFlowType[] taskTypes)
        {
            Executable last = null;
            for (int i = 0; i < taskTypes.Length; i++)
            {
                DataFlowType taskType = taskTypes[i];
                Executable temp = null;
                switch (taskType)
                {
                    case DataFlowType.Script:
                        {
                            temp = workingPackage.Executables.Add("STOCK:ScriptTask"); //Note: Task, not script
                            break;
                        }
                    case DataFlowType.DB_DB:
                        {
                            temp = workingPackage.Executables.Add("STOCK:PipelineTask");
                            TaskHost thMainPipe = temp as TaskHost;
                            MainPipe df = thMainPipe.InnerObject as MainPipe;
                            SetupDataFlow(df, ConnectionType.OleDB, ConnectionType.OleDB);
                            break;
                        }
                    case DataFlowType.DB_File:
                        {
                            temp = workingPackage.Executables.Add("STOCK:PipelineTask");
                            TaskHost thMainPipe = temp as TaskHost;
                            MainPipe df = thMainPipe.InnerObject as MainPipe;
                            SetupDataFlow(df, ConnectionType.OleDB, ConnectionType.FlatFile);
                            break;
                        }
                    case DataFlowType.File_File:
                        {
                            temp = workingPackage.Executables.Add("STOCK:PipelineTask");
                            TaskHost thMainPipe = temp as TaskHost;
                            MainPipe df = thMainPipe.InnerObject as MainPipe;
                            SetupDataFlow(df, ConnectionType.FlatFile, ConnectionType.FlatFile);
                            break;
                        }
                    case DataFlowType.File_DB:
                        {
                            temp = workingPackage.Executables.Add("STOCK:PipelineTask");
                            TaskHost thMainPipe = temp as TaskHost;
                            MainPipe df = thMainPipe.InnerObject as MainPipe;
                            SetupDataFlow(df, ConnectionType.FlatFile, ConnectionType.OleDB);
                            break;
                        }
                }
                if (last == null)
                {
                    last = temp;
                    continue;
                }
                PrecedenceConstraint pc = workingPackage.PrecedenceConstraints.Add(last, temp);
                pc.Value = DTSExecResult.Success;                
                last = temp;
            }
        }

        private void SetupDataFlow(MainPipe pipe, ConnectionType Type1, ConnectionType Type2)
        {
            
            var component = pipe.ComponentMetaDataCollection.New();
            component.Name = $"Component ${componentCount++} - Ole DB Source";
            //component.ComponentClassID = _App.PipelineComponentInfos["OLE DB Source"].CreationName;
            switch (Type1)
            {
                case ConnectionType.FlatFile:
                    {
                        //component.ComponentClassID = "DTSAdapter.OleDbSource";
                        component.ComponentClassID = _App.PipelineComponentInfos["Flat File Source"].CreationName;
                        SetupConnection(component, FLAT_FILE_DESC);
                        break;
                    }
                case ConnectionType.OleDB:
                    {
                        //component.ComponentClassID = "DTSAdapter.OleDbSource";
                        component.ComponentClassID = _App.PipelineComponentInfos["OLE DB Source"].CreationName;
                        SetupConnection(component);
                        break;
                    }
            }
            
            var script = pipe.ComponentMetaDataCollection.New();
            script.ComponentClassID = _App.PipelineComponentInfos["Script Component"].CreationName;
            script.Name = $"Component {componentCount++} - Script Transformation";
            ConnectPath(pipe, component, script);

            var component2 = pipe.ComponentMetaDataCollection.New();
            component2.Name = $"Component ${componentCount++} - Ole DB Source";
            switch (Type2)
            {
                case ConnectionType.FlatFile:
                    {
                        
                        //component2.ComponentClassID = "DTSAdapter.FlatFileDestination";  //"DTSAdapter.OleDbDestination";
                        component2.ComponentClassID = _App.PipelineComponentInfos["Flat File Destination"].CreationName;
                        SetupConnection(component2, FLAT_FILE_DESC);
                        break;
                    }
                case ConnectionType.OleDB:
                    {
                        //component2.ComponentClassID = "DTSAdapter.OleDbDestination";
                        component2.ComponentClassID = _App.PipelineComponentInfos["OLE DB Destination"].CreationName;
                        SetupConnection(component2);
                        break;
                    }
            }
            
            

            ConnectPath(pipe, script, component2);
        }
        private void SetupConnection(IDTSComponentMetaData100 component, string TYPE_DESC = OLE_DB_DESC)
        {
            var instance = component.Instantiate();
            instance.ProvideComponentProperties();
            if (component.RuntimeConnectionCollection.Count > 0)
            {
                var dbConns = (from ConnectionManager c in workingPackage.Connections
                               where c.Description == TYPE_DESC
                               select c
                              ).ToArray();
                ConnectionManager dbConn = null;
                if (dbConns.Length > 0)
                    dbConn = dbConns[0];
                if (dbConn != null)
                {
                    component.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(dbConn);
                    component.RuntimeConnectionCollection[0].ConnectionManagerID = dbConn.ID;
                }
            }
        }
        private void ConnectPath(MainPipe pipe, IDTSComponentMetaData100 componentFrom, IDTSComponentMetaData100 componentTo)
        {
            var sourceInst = componentFrom.Instantiate();
            sourceInst.ProvideComponentProperties();

            var destInst = componentTo.Instantiate();
            destInst.ProvideComponentProperties();

            IDTSPath100 path = pipe.PathCollection.New();
            path.AttachPathAndPropagateNotifications(componentFrom.OutputCollection[0], componentTo.InputCollection[0]);
        }
    }
    
}
