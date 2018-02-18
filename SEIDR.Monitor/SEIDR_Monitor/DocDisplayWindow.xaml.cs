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
using System.Windows.Xps.Packaging;

namespace SEIDR.WindowMonitor
{
    /// <summary>
    /// Interaction logic for DocDisplayWindow.xaml
    /// </summary>
    public partial class DocDisplayWindow : SessionWindow
    {
        public const string CHARTS = "Charts.xps";
        public const string CONTEXT_MENUS = "ContextMenus.xps";
        public const string GENERAL = "General.xps";
        public const string PLUGINS = "Plugins.xps";
        public const string QUERIES = "Queries.xps";
        public const string HELPER_DOCS_FOLDER = "HelpDocs";
        public DocDisplayWindow(string Document)
            :base(true)
        {
            InitializeComponent();
            if (Document.IndexOf('.') < 0)
                Document += ".xps";
            string docBasePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            DocViewer.Document = new XpsDocument($@"{docBasePath}\{HELPER_DOCS_FOLDER}\{Document}", System.IO.FileAccess.Read).GetFixedDocumentSequence();
            //DocViewer.Do
        }
    }
}
