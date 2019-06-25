using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    public class DocDatabaseLoader :IDisposable
    {
        System.Data.SqlClient.SqlBulkCopy BulkCopy;
        public DocDatabaseLoader(string TableName, DataBase.DatabaseManager connectionManager, System.Data.SqlClient.SqlBulkCopyOptions bulkCopyOptions = System.Data.SqlClient.SqlBulkCopyOptions.Default, string Schema = null)
        {            
            BulkCopy = new System.Data.SqlClient.SqlBulkCopy(connectionManager.GetConnectionString(), bulkCopyOptions);
            BulkCopy.DestinationTableName = (Schema ?? connectionManager.DefaultSchema) + "." + TableName;
        }

        public int BatchSize = 5000;
        /// <summary>
        /// Write the records to the destination table.
        /// </summary>
        /// <param name="records"></param>
        public void BulkLoadRecords(IEnumerable<IDataRecord> records)
        {
            if (records.UnderMaximumCount(0))
                return;
            var fr = records.First();
            var dtDoc = new DataTableDoc<TypedDataRecord>(fr.Columns.GetEmptyTable());
            int c = 0;
            foreach(var record in records)
            {
                dtDoc.AddRecord(record);
                c++;
                if(c > BatchSize)
                {
                    BulkLoadTable(dtDoc);
                    c = 0;
                    dtDoc.Clear();
                }
            }
            if(c > 0)
            {
                BulkLoadTable(dtDoc);
            }
        }
        public void BulkLoadTable(System.Data.DataTable tableData)
        {
            if (tableData.Rows.Count == 0)
                return;
            BulkCopy.WriteToServer(tableData);
        }
        ~DocDatabaseLoader()
        {
            if (BulkCopy == null)
                return;
            ((IDisposable)BulkCopy).Dispose();
        }
        /// <summary>
        /// Dispose underlying SQL Bulk copy object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (BulkCopy == null)
                return;
            ((IDisposable)BulkCopy).Dispose();
        }
    }
}
