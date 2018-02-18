using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEIDR.Dynamics.Configurations.QueryConfiguration;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using SEIDR.Dynamics.Configurations.AddonConfiguration;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SEIDR.Dynamics.Configurations.DatabaseConfiguration;
using System.Data;
using SEIDR.WindowMonitor.Dashboards;
using SEIDR.Dynamics.Windows;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;
using SEIDR.Dynamics.Configurations;

namespace SEIDR.WindowMonitor.ConfigurationViewModels
{
    public class ContextMenuViewModel: INotifyPropertyChanged
    {
        ContextMode mode;
        public ObservableCollection<ContextMenuViewModel> Children { get; set; }
        public QueryChartModel Chart { get; set; } = null;
        public ContextAddonConfiguration AddonConfiguration { get; private set; } = null;
        public ContextMenuConfiguration Configuration { get; private set; } = null;
        public string Header { get; private set; }
        public string Description { get; private set; } = null;
        private ContextMenuViewModel(ContextMenuConfiguration config)
        {
            Children = new ObservableCollection<ContextMenuViewModel>();
            Configuration = config;
            Header = config.Key;
            Description = config.Description.nTrim(true);
            switch (config.MyScope)
            {
                case Dynamics.Configurations.WindowConfigurationScope.CM:
                    mode = ContextMode.ContextAction;
                    break;
                case Dynamics.Configurations.WindowConfigurationScope.SW:
                    mode = ContextMode.Switch;
                    break;
                case Dynamics.Configurations.WindowConfigurationScope.D:
                    mode = ContextMode.DetailWindow;
                    break;
                default:
                    mode = ContextMode.None;
                    break;
            }
        }
        public ContextMenuViewModel(ContextMenuConfiguration config, ContextMenuList menulist, ContextAddonList addons)
            :this(config)
        {            
            foreach (var cm in menulist.GetChildren(config))
            {
                Children.Add(new ContextMenuViewModel(cm, menulist, addons));
            }
            var q = (from acm in addons.ConfigurationEntries
                     where acm.ParentScope == config.MyScope
                     && acm.Parent == config.ID
                     select acm);
            q.ForEach(acm => Children.Add(new ContextMenuViewModel(acm)));
        }
        public ContextMenuViewModel(ContextAddonConfiguration config)
        {
            AddonConfiguration = config;
            Header = config.Key;
            Description = config.Description.nTrim(true);
            Children = new ObservableCollection<ContextMenuViewModel>();
            mode = ContextMode.Addon;
        }
        public ContextMenuViewModel(string dummy)
        {
            Header = dummy;
            Children = new ObservableCollection<ContextMenuViewModel>();
            mode = ContextMode.None;
        }
        public ContextMenuViewModel(QueryChartModel chart)
        {
            Chart = chart;
            Header = chart.Header;
            Description = chart.Description;
            Children = new ObservableCollection<ContextMenuViewModel>();
            mode = ContextMode.Chart;
        }
        public static ContextMenuViewModel GetRefresh(Query q)
        {
            return new ContextMenuViewModel(new ContextMenuConfiguration
            {
                SwitchID = q.ID,
                ParentScope = Dynamics.Configurations.WindowConfigurationScope.Q,
                Key = "Refresh"
            }, null, null);
        }
        /// <summary>
        /// If true, require re-doing paging
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public bool Execute(IEnumerable<DataRowView> rows)
        {
            Query q = __SESSION__._currentQuery;                        
            if (q == null || rows.UnderMaximumCount(1))
                return false;
            var limit = __SESSION__.MySettings.MultiSelectContextSprocLimit;
            if (limit.HasValue && mode.NotIn( ContextMode.Chart))
                rows = rows.Take(limit.Value);
            Database db = SessionBroker.Connections[q.DBConnection];            
            switch (mode)
            {
                case ContextMode.ContextAction:
                    {
                        if (db == null)
                            return false;
                        int rc = 0;
                        if (Configuration.MultiSelect)
                        {                            
                            if (Configuration.UseQueue)
                            {                                
                                Models.ContextActionQueue.BatchQueueAction(Configuration, rows, db.ID.Value);
                                return false;
                            }                            
                            foreach(var row in rows)
                            {
                                using (var cmq = new Models.ContextMenuItemQuery(Configuration, row, db))
                                {
                                    rc += cmq.Execute();
                                }
                            }
                        }
                        else
                        {
                            if (Configuration.UseQueue)
                            {
                                Models.ContextActionQueue.QueueAction(Configuration, rows.First(), db.ID.Value);
                                return false;
                            }
                            using (var cmq = new Models.ContextMenuItemQuery(Configuration, rows.First(), db))
                            {
                                rc = cmq.Execute();
                            }
                        }                        
                        return rc > 0;                                     
                    }
                case ContextMode.DetailWindow:
                    {
                        Models.ConetextMenuItemHelper.RunContext(Configuration, rows.First(), db);
                        break;
                    }
                case ContextMode.Switch:
                    {
                        q = SessionBroker.Queries[Configuration.SwitchID];
                        var row = rows.First();
                        var cols = row.DataView.Table.Columns;
                        __SESSION__._currentQuery = q;                        
                        __SESSION__._lastParameters = Models.ContextSwitch.DoContextSwitch(Configuration, q, row);
                        return true;
                    }
                case ContextMode.Addon:
                    {
                        string Message = "Unable to load Addon " + AddonConfiguration.Key;
                        try
                        {
                            SEIDR_WindowAddon_MetaData md;
                            var cm = GetContextAddon(AddonConfiguration, out md);                            
                            cm.Caller = SessionBroker.ConfiguredUser.Clone();
                            cm.Connection = db.Connection;                            

                            if (md.MultiSelect)
                                Message = cm.Execute(rows, AddonConfiguration.Parameters);
                            else
                                Message = cm.Execute(rows.First(), AddonConfiguration.Parameters);

                            if (!string.IsNullOrWhiteSpace(Message))
                                new Alert(Message, Choice: false, mode: AlertMode.Message).ShowDialog();                            
                        }
                        catch(Exception ex)
                        {
                            Handle(ex, Message);
                            return false;
                        }
                        break;
                    }
                #region chart
                case ContextMode.Chart:
                    {
                        switch (Chart.ChartType)
                        {
                            case QueryBasicChartType.Aggregate:
                                {
                                    var agg = ChartHelper.GetAggregateData(rows,
                                        Chart.Column,
                                        q.ExcludedResultColumns,
                                        Chart.SubColumn);
                                    new StackedBarChartDisplay(
                                        q.ProcedureCall,
                                        $"{q.Key} ({db.Key})",
                                        agg
                                    );
                                    break;
                                }
                            case QueryBasicChartType.FrequencyAgg:
                                {
                                    new FrequencyDonutChartDisplay(
                                        q.ProcedureCall, 
                                        Chart.SubTitle,
                                        ChartHelper.GetAggregateData(
                                            rows,
                                            Chart.Column,
                                            q.ExcludedResultColumns,
                                            Chart.SubColumn
                                        )).ShowDialog();
                                    break;
                                }
                            case QueryBasicChartType.PieChart:
                                {
                                    new PieChartDisplay(q.ProcedureCall, rows.First(), Chart.SubTitle).ShowDialog();
                                    break;
                                }
                            case QueryBasicChartType.PreAggregateBar:
                                {
                                    new AggBarChartDisplay(q.ProcedureCall, rows.First(), Chart.SubTitle).ShowDialog();
                                    break;
                                }
                        }
                        break;
                    }
                #endregion
                case ContextMode.None:
                default:
                    return false;
            }
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Invoke([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public static ObservableCollection<ContextMenuViewModel> Configure()
        {
            Query q = __SESSION__._currentQuery;
            db = SessionBroker.Connections[q.DBConnection];
            if (db == null)
                return null; 
            ObservableCollection<ContextMenuViewModel> r = new ObservableCollection<ContextMenuViewModel>();            
            foreach(var cm in SessionBroker.ContextMenus.GetAllChildren(q))
            {
                r.Add(new ContextMenuViewModel(cm, SessionBroker.ContextMenus, SessionBroker.ContextAddons));
            }
            var query = (from ContextAddonConfiguration acm in SessionBroker.ContextAddons.ConfigurationEntries
                         where acm.Parent == q.ID && acm.ParentScope == Dynamics.Configurations.WindowConfigurationScope.Q
                         select acm);
            foreach(var acm in query)
            {
                r.Add(new ContextMenuViewModel(acm));
            }
            #region aggregate charts
            if (q.EnableFrequencyChart || q.EnableAggregateChart)
            {             
                var agg = new ContextMenuViewModel("Aggregate Bar Charts");
                foreach(var group in q.GroupedResultColumns)
                {
                    var groupMenu = new ContextMenuViewModel(ChartHelper.GetFriendlyName(group));
                    if (q.EnableAggregateChart)
                    {
                        var nonExcluded = new ContextMenuViewModel("Aggregate, Non-Excluded");                        
                        QueryChartModel qcm = new QueryChartModel
                        {
                            Header = group,
                            Column = group,
                            ChartType = QueryBasicChartType.Aggregate,
                            Description = "Open Records as a bar chart(int and smallint columns only)",
                        };
                        nonExcluded.Children.Add(new ContextMenuViewModel(qcm));
                        groupMenu.Children.Add(nonExcluded);
                    }
                    if (q.EnableFrequencyChart)
                    {
                        var freq = new ContextMenuViewModel("Frequency");                        
                        var sgroup = from g in q.GroupedResultColumns
                                        where g != @group
                                        select g;
                        foreach (string sgroupCol in sgroup)
                        {
                            QueryChartModel qcm = new QueryChartModel
                            {
                                SubTitle = $"{q.Key} ({db.Key}) - Chart Frequency of '{sgroup}' values associated with '{EditableObjectHelper.FriendifyLabel(group)}'",
                                ChartType = QueryBasicChartType.FrequencyAgg,
                                Column = group,
                                SubColumn = sgroupCol,
                                Description = $"Count frequency of different '{sgroupCol}' values",
                                Header = EditableObjectHelper.FriendifyLabel(sgroupCol)
                            };
                            groupMenu.Children.Add(new ContextMenuViewModel(qcm));
                        }                        
                    }
                    if (groupMenu.Children.Count > 0)
                        agg.Children.Add(groupMenu);
                }
                if(agg.Children.Count > 0)
                    r.Add(agg);                
            }
            #endregion
            #region Pie Chart/ pre agg
            if (q.EnablePieChart)
            {
                ContextMenuViewModel preAgg = new ContextMenuViewModel("PreAggregated Charts");
                QueryChartModel qcm = new QueryChartModel
                {
                    Header = "Open Record as Pie Chart (int and smallint columns only)",
                    ChartType = QueryBasicChartType.PieChart
                };
                QueryChartModel aggBar = new QueryChartModel
                {
                    Header = "Open Record as a BarChart (int and smallint columns only)",
                    ChartType = QueryBasicChartType.PreAggregateBar
                };
                preAgg.Children.Add(new ContextMenuViewModel(qcm));
                preAgg.Children.Add(new ContextMenuViewModel(aggBar));
                r.Add(preAgg);
            }
            #endregion

            r.Add(GetRefresh(q));
            return r;
        }
        static Database db;
        enum ContextMode
        {
            DetailWindow,
            ContextAction,
            Addon,
            Chart,
            Switch,
            None
        }
    }
}
