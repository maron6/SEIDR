using SEIDR.Dynamics;
using SEIDR.Dynamics.Configurations.ContextMenuConfiguration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static SEIDR.WindowMonitor.sExceptionManager;
using static SEIDR.WindowMonitor.MonitorConfigurationHelpers.LibraryManagement;

namespace SEIDR.WindowMonitor.Models
{
    public static class ContextActionQueue
    {
        
        /*
            TODO: Queue Multi selected context actions instead of performing immediately
             
             */        
        static object qlock = new object();
        static List<ActionQueueItem> queue = new List<ActionQueueItem>();
        static int _BatchSize = 5;
        public const int MaxQueueSize = 60;
        public static double QueueAllotmentFilled
        {
            get
            {
                return (queue.Count * 100) / MaxQueueSize;
            }
        }
        public static string Status
        {
            get
            {
                return $"Queue Allotment: {queue.Count} ({MaxQueueSize})";
            }
        }
        static int _QueueSize = MaxQueueSize;
        public static int BatchSize
        {
            get
            {
                return _BatchSize;
            }
            set
            {
                if (value < 1)
                    return;
                _BatchSize = value;
            }
        }
        public static int? QueueLimit
        {
            get { return _QueueSize; }
            set {
                if (value < 5)
                {
                    _QueueSize = 5;
                    Handle("Attempted to set Queue size to " + value + " - Setting to Minimum (5)", ExceptionLevel.Background);
                    return;
                }
                if (value > MaxQueueSize)
                {
                    Handle("Attempted to set Queue size to " + value + " - Setting to Max (" + MaxQueueSize + ")", ExceptionLevel.Background);
                    _QueueSize = MaxQueueSize;
                }
                _QueueSize = value ?? MaxQueueSize;
            }
        }  
        /// <summary>
        /// Queues the action to be performed by a background process calling <see cref="ProcessQueueBatch"/>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="content"></param>
        /// <param name="DBConnection"></param>
        /// <returns>Returns true if an action was queued, false ifthe context item could not queue an action. See background log entries.</returns>
        public static bool QueueAction(ContextMenuConfiguration context, DataRowView content,  int DBConnection)
        {
            if(queue.Count >= QueueLimit)
            {
                Handle("Queue is full");
                return false;
            }
            if(context.IsSwitch)
            {
                Handle($"Cannot queue Switch context - '{context.Key}' ({context.ID})", ExceptionLevel.Background);
                return false;
            }
            else if(context.Dashboard != null)
            {
                Handle($"Cannot queue Dashboard opening context - '{context.Key}' ({context.ID})", ExceptionLevel.Background);
                return false;
            }
            lock (qlock)
            {
                queue.Add(new ActionQueueItem(DBConnection, content, context));
            }
            return true;
        }
        public static bool BatchQueueAction(ContextMenuConfiguration context, 
            IEnumerable<DataRowView> batchContent, int DbConnection)
        {            
            if (context.ProcedureCall == null)
            {
                Handle($"Cannot queue context with no Procedure call - '{context.Key}' ({context.ID})", ExceptionLevel.Background);
                return false;
            }
            if (context.IsSwitch)
            {
                Handle($"Cannot queue Switch context - '{context.Key}' ({context.ID})", ExceptionLevel.Background);
                return false;
            }
            else if (context.Dashboard != null)
            {
                Handle($"Cannot queue Dashboard opening context - '{context.Key}' ({context.ID})", ExceptionLevel.Background);
                return false;
            }
            int canAdd = _QueueSize - queue.Count;
            if(canAdd <= 0)
            {
                Handle("Cannot add any records to queue - Queue is full.");
                return false;
            }
            var add = (from content in batchContent
                       select new ActionQueueItem(DbConnection, content, context))
                       .Take(canAdd);
            lock (qlock)
            {
                queue.AddRange(add);
            }
            if(add.Count() < canAdd)
            {
                Handle("Could not queue " + (add.Count() - canAdd)
                    + " actions, due to Queue Limit (" + QueueLimit.ToString() + ")");
                return true;
            }
            return true;
        }
        public static void ProcessQueueBatch()
        {

            int? DBConnection = (from q in queue                                   
                                   select q.DBConnection).FirstOrDefault();
            if (DBConnection == null) //Note that this should also catch the queue being empty...
                return;
            //var db = SettingManager.myConnections[DBConnection]; //?.InternalDBConn;
            var db = __SESSION__.Broker.Connections[DBConnection];
            if(db == null)
            {                
                lock (qlock)
                {
                    var ex = (from q in queue
                              where q.DBConnection == db.ID
                              select q);
                    foreach(var exclude in ex)
                    {
                        queue.Remove(exclude);
                    }
                    //queue = queue.Exclude(r => r.DBConnection == db.ID);
                    //queue = (from q in queue
                    //         where q.DBConnection != null && q.DBConnection != DBConnection
                    //         select q).ToList(); //Remove invalid actions due to missing DB...
                    return;
                }
            }
            var batch = (from q in queue
                         where q.DBConnection == DBConnection
                         select q).OrderBy(q=>q.Created).Take(BatchSize);
            foreach(var action in batch)
            {
                try
                {
                    action.Execute(db);
                }
                catch(Exception ex)
                {
                    Handle(ex, "Unable to complete Action execution: " + action.DetailInfo,
                        ExceptionLevel.Background);
                    continue;
                }
            }
            lock (qlock)
            {
                queue.RemoveAll(b => batch.Contains(b));                
            }

        }
        class ActionQueueItem
        {
            public int DBConnection;
            public DataRowView Content;
            public ContextMenuConfiguration Context;
            public DateTime Created { get; private set; }
            public string DetailInfo
            {
                get
                {
                    return $"{Context.Key} ({Context.ID}, {DBConnection}) MyScope: {Context.MyScope.GetDescription()} Owner: ID - {Context.ParentID}, Scope - {Context.ParentScope.GetDescription()}";                        
                }
            }
            public ActionQueueItem(int DBConnection, DataRowView content, ContextMenuConfiguration c)
            {
                this.DBConnection = DBConnection;
                Content = content;
                Context = c;
                Created = DateTime.Now;
            }
            public void Execute(SEIDR.Dynamics.Configurations.DatabaseConfiguration.Database db)
            {                
                if (db == null)
                    return;
                if(db.ID != DBConnection)
                {
                    Handle($"ID Mismatch! Expected {DBConnection}, received {db.ID} ({db.Key})");
                    return;
                }
                ConetextMenuItemHelper.RunContext(Context, Content, db);
                //using (ContextMenuItemQuery q = new ContextMenuItemQuery(Context, Content, db))
                //{
                //    q.Execute();
                //}
            }
        }
    }
    
}
