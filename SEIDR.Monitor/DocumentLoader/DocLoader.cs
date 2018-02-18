using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Windows;
using System.ComponentModel.Composition;
using SEIDR.Dynamics.Configurations;
using SEIDR.Dynamics.Configurations.UserConfiguration;
using SEIDR.Doc;
using SEIDR.DataBase;
using SEIDR;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Data;

namespace DocumentLoader
{
    [Export(typeof(SEIDR_WindowMenuAddOn)),
        ExportMetadata("Name", "Document Loader"),
        ExportMetadata("UsesCaller", true)]    
    public class DocLoader : SEIDR_WindowMenuAddOn
    {
        /// <summary>
        /// Note that the interface requires 'set' only, get is for within this class
        /// </summary>
        //[Import(typeof(SEIDR_Window))]
        public SEIDR_Window callerWindow { private get; set; }

        ~DocLoader()
        {
            if (ddr != null)
            {                
                ddr.Dispose();
            }
        }
        
        public MenuItem Setup(BasicUser User, DatabaseConnection Connection, int internalID, Dictionary<string, object> setParameters)
        {
            /*
            ddr.PageSize = Convert.ToInt32(setParameters[PAGE_SIZE]);
            DelimitedRecord.NullIfTruncated = true;                        
            if (setParameters.ContainsKey(PAGE_SIZE))
                DelimitedDocumentReader.DefaultPageSize = Convert.ToInt32(setParameters[PAGE_SIZE]);
            else
                DelimitedDocumentReader.DefaultPageSize = (int)(5 * 1000);

            if (DelimitedDocumentReader.DefaultPageSize > 10000)
                DelimitedDocumentReader.DefaultPageSize = 10 * 1000;
            */

            Action a = () =>
            {
                page = 0;                
                // show window asking for file path using SetParameters
                //MainWindow d = new MainWindow(setParameters["DefaultDirectory"] as string);   
                var d = new OpenFileDialog() { InitialDirectory = setParameters["DefaultDirectory"] as string };          
                var b = d.ShowDialog();
                if(b ?? false)
                {
                    string s = System.IO.Path.GetExtension(d.FileName);
                    if (s.ToUpper().In("XLS", "XLSX"))
                        throw new Exception($"Unsupported extension: '{s}' - Cannot be loaded");

                    //ddr = new DelimitedDocumentReader(d.FileName); // e, d.Delimiter, d.LinesToSkip);                    
                    dr = new DocReader(null, d.FileName);                    

                    DataTable dt = SetupPage(page);
         
                    ContextMenu menu = new ContextMenu();
                    menu.Items.Add(MenuItemBuilder.BuildInitial("Next Page", UpdateWithNextPage));
                    menu.Items.Add(MenuItemBuilder.BuildInitial("Previous Page", UpdateWithPreviousPage));
                    menu.Items.Add(MenuItemBuilder.BuildInitial("Restart", Restart));
                    new Alert("Finished set up!", Choice: false, mode:AlertMode.Message).ShowDialog();
                    callerWindow.UpdateDisplay(dt,
                        internalID,
                        menu
                        );
                    callerWindow.UpdateLabel("DOC Loader", "File Page 0");
                }
            };
            return MenuItemBuilder.BuildInitial("DocumentLoader", a);
        }
        int page = 0;
        void SetLabel()
        {
            callerWindow.UpdateLabel("DOC Loader", "File Page " + (page + 1) + $": Records {ddr.minRecord} - {ddr.maxRecord}");
        }
        void UpdateWithNextPage()
        {
            DataTable dt = SetupPage(page + 1);
            if (dt.Rows.Count > 0)
                page++;            
            callerWindow.UpdateDisplayNoContextChange(dt);
            SetLabel();
        }
        void UpdateWithPreviousPage()
        {
            if(page == 0)
            {
                new Alert("You are already on the first page.", Choice: false).ShowDialog();
                return;
            }
            callerWindow.UpdateDisplayNoContextChange(SetupPage(page--));
            SetLabel();
        }
        void Restart()
        {
            if(page == 0)
            {
                new Alert("You are already on the first page.", Choice:false).ShowDialog();
                return;
            }
            page = 0;
            callerWindow.UpdateDisplayNoContextChange(SetupPage(page));
            SetLabel();
        }
        DocReader dr;        
        DelimitedDocumentReader ddr;
        DataTable SetupPage(int page)
        {
            DataTable dt = new DataTable();
            //var r = ddr.GetPage(page);
            //var headers = ddr.GetHeader();
            var headers = dr.Columns;
            
            foreach (var header in headers)
            {
                dt.Columns.Add(header.ColumnName.ToUpper().Trim());
            }
            foreach (var record in dr.GetPage(page)) //First page ONLY. Should be plenty big enough with the default size
            {
                var row = dt.NewRow();
                
                foreach (var header in headers)
                {
                    row[header.ColumnName.ToUpper().Trim()] = record[header];
                }
                dt.Rows.Add(row);
            }
            if(dt.Rows.Count == 0)
            {
                new Alert("End of file reached - No records", Choice:false).ShowDialog();
            }
            return dt;

        }
        const string PAGE_SIZE = "PageSize";
        public Dictionary<string, Type> GetParameterInfo()
        {
            return new Dictionary<string, Type>
            {
                {"DefaultDirectory", typeof(string) },
                //{PAGE_SIZE, typeof(uint) } //maybe need to add a default value dictionary method too....
            };
        }
    }


    /*
    [MetadataAttribute()]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
    public class DocLoaderMetaDataAttribute : ExportAttribute, SEIDR_WindowMenuAddOn_MetaData
    {
        public bool HasParameterMapping
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        //public DocLoaderMetaDataAttribute() : base(typeof(SEIDR_WindowMenuAddOn)) { }
        public string Name
        {
            get
            {
                return "DocumentLoader";
            }
        }

        public Dictionary<string, Type> parameterInfo
        {
            get
            {
                return new Dictionary<string, Type>
                {
                    {"DefaultDirectory", typeof(string) }
                };
            }
        }

        public bool RequirePermission
        {
            get
            {
                return true;
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
                return null;
            }
        }

        public bool UsesCaller
        {
            get
            {
                return true;
            }
        }
    }//*/
}
