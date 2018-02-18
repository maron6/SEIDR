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

namespace SEIDR.Dynamics.Configurations.DynamicEditor
{
    /// <summary>
    /// Interaction logic for ContextAddonEditor.xaml
    /// </summary>
    public partial class QueryEditor : BaseEditorWindow<QueryConfiguration.Query>
    {
        public QueryEditor(QueryConfiguration.Query toEdit) 
            :base(toEdit, UserConfiguration.BasicUserPermissions.QueryEditor)           
        {
            InitializeComponent();
        }
    }
}
