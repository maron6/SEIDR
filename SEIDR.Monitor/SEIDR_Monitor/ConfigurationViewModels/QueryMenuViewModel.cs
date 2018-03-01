using SEIDR.Dynamics.Configurations.QueryConfiguration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SEIDR.WindowMonitor.ConfigurationViewModels
{
    public class QueryMenuViewModel:INotifyPropertyChanged
    {
        const string REFRESH_MENU = "Refresh";
        string header = REFRESH_MENU; //Special handling when it's refresh..
        Visibility iconVis = Visibility.Hidden;
        QueryMenuViewModel _Root = null;
        public Visibility IconVis
        {
            get { return iconVis; }
            set
            {
                if(iconVis != value)
                {
                    iconVis = value;
                    Invoke();
                }
            }
        }
        public string Header
        {
            get { return header; }
            set
            {
                if (header != value)
                {
                    header = value;
                    Invoke();
                }
            }
        }
        int? qid = null;

        //public List<iQueryBasicChartModel> Charts { get; set; }
        /// <summary>
        /// Don't remember what this is foooor.
        /// </summary>
        //public List<QueryChartModel> Charts { get; set; }
        public int? QueryID
        {
            get { return qid; }
            set
            {
                if(qid != value)
                {
                    qid = value;
                    Invoke();
                    Invoke(nameof(MenuName));
                }
            }
        }

        [Obsolete]
        public string MenuName
        {
            get
            {
                if (Header == REFRESH_MENU)
                    return REFRESH_MENU;
                if(QueryID.HasValue)
                    return QueryID.Value.ToString();
                return SEIDR.Dynamics.Windows.EditableObjectHelper.GET_WPF_NAME(Header);
            }
        }
        private bool Execute()
        {
            Query Current = mgr._currentQuery;
            if(Header == REFRESH_MENU || qid == Current?.ID)
            {
                if (Current == null)
                    return false;
                var db = mgr.Broker.Connections[Current.DBConnection];
                if (db == null)
                    return false;
                var parameters = mgr._lastParameters;
                if (parameters == null)
                    return false;
                mgr.SetCache("_MonitorData", Current.Execute(db, parameters, mgr.CurrentUser?.ID));
                return true;
                //DoRefresh? 
                //Not sure how well this will work.. 
                //could have it populate the cached DataTable I guess?               
            }
            else if (qid.HasValue)
            {
                //Get query, call execute?
                Query q = mgr.Broker.Queries[qid];                
                bool x = false;
                mgr._currentQuery = q;
                if (_Root != null)
                    _Root.QueryID = q?.ID;//Invoke, calls the refresh on the new query, Refresh(false)                
                return x;
            }
            return false;
        }
        public ObservableCollection<ContextMenuViewModel> ConfigureContextMenu()
        {
            Query q = mgr.Broker.Queries[qid];
            if (q == null)
                return null;
            return ContextMenuViewModel.Configure();
            //return ContextMenuViewModel.Configure(q, mgr);
        }
        //ToDo: Use command for click... Not completely sure how well it would work, given the other stuff I want the menu to be able to do..
        ICommand _Command;

        public ObservableCollection<QueryMenuViewModel> SubMenus { get; set; } = new ObservableCollection<QueryMenuViewModel>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Invoke([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private void ClearNested()
        {
            if(SubMenus.Count > 0)
            {
                foreach(var s in SubMenus)
                {
                    s.ClearNested();
                }
            }
            SubMenus.Clear();
        }
        /// <summary>
        /// Should only be called on the root.
        /// </summary>
        /// <param name="queries"></param>
        public void Reconfigure(QueryList queries)
        {
            ClearNested();
            var categories = queries.GetCategories();
            foreach(var c in categories)
            {
                var category = new QueryMenuViewModel { Header = c, IconVis = Visibility.Visible, SubMenus = new ObservableCollection<QueryMenuViewModel>() };                
                foreach(Query q in queries.GetQueries(c, null))
                {
                    category.SubMenus.Add(new QueryMenuViewModel { Header = q.Description, IconVis = Visibility.Visible, qid = q.ID, _Root = this }); //Set field instead of property to avoid invoking when creating the QueryMenuViewModel
                }
                var subCats = queries.GetSubCategories(c);
                foreach(var sc in subCats)
                {
                    var subcategory = new QueryMenuViewModel { Header = sc, IconVis = Visibility.Visible, SubMenus = new ObservableCollection<QueryMenuViewModel>() };
                    foreach(Query q in queries.GetQueries(c, sc))
                    {
                        subcategory.SubMenus.Add(new QueryMenuViewModel { Header = q.Description, IconVis = Visibility.Visible, qid = q.ID, _Root = this });
                    }
                    category.SubMenus.Add(subcategory);
                }
                foreach(Query q in queries.GetQueries(null, null))
                {
                    
                    category.SubMenus.Add(new QueryMenuViewModel { Header = q.Description, IconVis = Visibility.Visible, qid = q.ID, _Root = this });
                }
                SubMenus.Add(category);
            }
            SubMenus.Add(new QueryMenuViewModel { Header = REFRESH_MENU, IconVis = Visibility.Visible });
            Invoke(nameof(SubMenus));
        }
        public static QueryMenuViewModel CreateRoot(QueryList queries, UserSessionManager manager)
        {
            mgr = manager;
            var r = new QueryMenuViewModel
            {
                Header = "Queries",
                iconVis = Visibility.Visible,
                qid = null
            };
            r.Reconfigure(queries);
            return r;
        }
        static UserSessionManager mgr;        
    }
}
