using SEIDR.Dynamics.Configurations.Encryption;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace SEIDR.Dynamics.Configurations.QueryConfiguration
{
    public class QueryList : WindowConfigurationList<Query>
    {
        public QueryList()
            : base(WindowConfigurationScope.Q)
        {
        }
        public override DataTable MyData
        {
            get
            {
                return ConfigurationEntries
                    .ToDataTableLimited(
                        nameof(Query.Key),
                        nameof(Query.ProcedureCall),
                        nameof(Query.Description),
                        nameof(Query.Category),
                        nameof(Query.SubCategory),
                        nameof(Query.RefreshTime)
                        );
                    //.ToArray()
                    //.ToDataTable("ID", "GroupedResultColumns", "Parameters", "RecordVersion",
                    //    //"EnablePieChart", "EnableFrequencyChart", "EnableAggregateChart",
                    //    "MyScope", "ExcludedResultColumns", "RefreshTime");
            }
        }

        /// <summary>
        /// Basic save - saves to a file specified by the load model.
        /// </summary>
        public override void Save()
        {
            var other = LoadModel.Tag.ToString().DeserializeXML<QueryList>();
            if (other != null && other.Version != Version)
                throw new Exception("The record has been changed by another user.");
            string content = this.SerializeToXML();
            if (!LoadModel.UserSpecific)
                content = content.Encrypt(LoadModel.Key);
            System.IO.File.WriteAllText(LoadModel.Tag.ToString(), content);
            ConfigurationEntries.Where(c => c.Altered).ForEach(c => c.Altered = false);
        }
        public override WindowConfigurationList<Query> cloneSetup()
        {
            return this.XClone();
        }

        /// <summary>
        /// Gets the list of distinct Categories across all queries
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetCategories()
            => (from query in ConfigurationEntries
                     select query.Category).Distinct();                    
        /// <summary>
        /// Get the list of non-nullable subCategories for the provided Category
        /// </summary>
        /// <param name="Category"></param>
        /// <returns></returns>
        public IEnumerable<string> GetSubCategories(string Category)
            =>(from query in ConfigurationEntries
                     let subcat = query.SubCategory
                     where Category != null 
                     && query.Category == Category
                     && subcat != null
                     select subcat).Distinct();
        /// <summary>
        /// Get the queries for the specified Category/subcategory
        /// </summary>
        /// <param name="Category"></param>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        public IEnumerable<Query> GetQueries(string Category, string subcategory)
        {
            var ql = from q in ConfigurationEntries
                     where (q.Category == Category || string.IsNullOrEmpty(Category).And(q.Category == null))
                     && (q.SubCategory == subcategory || string.IsNullOrEmpty(subcategory).And(q.SubCategory == null))
                     select q;
            return ql;
        }
        
    }
}
