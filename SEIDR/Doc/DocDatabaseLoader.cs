using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Helper class for loading <see cref="IDataRecord"/> implementations into a database.
    /// </summary>
    public class DocDatabaseLoader :IDisposable
    {
        SqlBulkCopy BulkCopy;
        /// <summary>
        /// Construct a helper class for bulk loading file content to a DataTable.
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="connectionManager"></param>
        /// <param name="bulkCopyOptions"></param>
        /// <param name="Schema"></param>
        public DocDatabaseLoader(string TableName, DataBase.DatabaseManager connectionManager, SqlBulkCopyOptions bulkCopyOptions = System.Data.SqlClient.SqlBulkCopyOptions.Default, string Schema = null)
        {            
            BulkCopy = new SqlBulkCopy(connectionManager.GetConnectionString(), bulkCopyOptions);
            DefaultSchema = '[' + connectionManager.DefaultSchema.Replace("[", "").Replace("]", "") + ']';
            SetDestinationTable(TableName, Schema);                        
        }
        /// <summary>
        /// Default schema when setting table name.
        /// </summary>
        public readonly string DefaultSchema;
        DataTable linkTable = null;
        /// <summary>
        /// Indicates whether or not the loader is linked to a datatable.
        /// </summary>
        public bool Linked => linkTable != null;
        /// <summary>
        /// Links the bulk loader to a DataTable object. 
        /// <para>As the number of rows in the DataTable reaches <see cref="BatchSize"/>, the loader will automatically call <see cref="BulkLoadLinkedTable(bool)"/>, indicating to clear the row data.</para>
        /// </summary>
        /// <param name="toLink"></param>
        public void Link(DataTable toLink)
        {
            if (Linked)
                throw new InvalidOperationException("Instance is already linked to a DataTable object.");
            linkTable = toLink;
            linkTable.RowChanged += LinkTable_RowChanged;
        }
        /// <summary>
        /// Removes the link to the DataTable object
        /// </summary>
        public void UnLink()
        {
            if (!Linked)
                throw new InvalidOperationException("Instance is not linked.");
            linkTable.RowChanged -= LinkTable_RowChanged;
            linkTable = null;
        }
        void LinkTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            var dt = sender as DataTable;
            if (e.Action == DataRowAction.Add && dt.Rows.Count >= BatchSize)
            {
                BulkLoadLinkTable(true);
            }
        }


        /// <summary>
        /// Number of records to go through before writing to the database when enumerating a file.
        /// </summary>
        public int BatchSize { get; private set; } = 5000;
        /// <summary>
        /// Sets the <see cref="BatchSize"/>
        /// </summary>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public DocDatabaseLoader SetBatchSize(int batchSize)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            if (batchSize <= 0)
                throw new ArgumentException("BatchSize must be greater than zero.", nameof(batchSize));
            BatchSize = batchSize;
            return this;
        }
        /// <summary>
        /// Add a set of column mappings.
        /// </summary>
        /// <param name="columnMappings"></param>
        public void AddColumnMappings(params SqlBulkCopyColumnMapping[] columnMappings)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            foreach (SqlBulkCopyColumnMapping map in columnMappings)
            {
                BulkCopy.ColumnMappings.Add(map);
            }
        }
        /// <summary>
        /// Add a set of column mappings.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="Schema"></param>
        public DocDatabaseLoader SetDestinationTable(string tableName, string Schema)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            if (Linked)
                throw new InvalidOperationException("Cannot change Destination table while linked.");
            if(!string.IsNullOrEmpty(Schema) && Schema.IndexOfAny(new[] { '[', ']' }) >= 0)
            {
                Schema = '[' + Schema.Replace("[", "").Replace("]", "") + ']';
            }
            if(tableName.IndexOfAny(new[] { '[', ']' }) >= 0)
            {
                tableName = '[' + tableName.Replace("[", "").Replace("]", "") + ']';
            }
            BulkCopy.DestinationTableName = (Schema ?? DefaultSchema) + "." + tableName;
            return this;
        }
        /// <summary>
        /// Add a mapping of a column in the IDataRecords to the destination column of the table.
        /// </summary>
        /// <param name="sourceColumn"></param>
        /// <param name="destinationColumn"></param>
        public DocDatabaseLoader AddColumnMapping(string sourceColumn, string destinationColumn)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            BulkCopy.ColumnMappings.Add(sourceColumn, destinationColumn);
            return this;
        }
        /// <summary>
        /// Add a set of column mappings.
        /// </summary>
        public DocDatabaseLoader AddColumnMapping(DocRecordColumnInfo sourceColumn, int DestinationColumn)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            BulkCopy.ColumnMappings.Add(sourceColumn.Position, DestinationColumn);
            return this;
        }

        /// <summary>
        /// Add a set of column mappings.
        /// </summary>
        public DocDatabaseLoader AddColumnMapping(DocRecordColumnInfo sourceColumn, string destinationColumn)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            BulkCopy.ColumnMappings.Add(sourceColumn.ColumnName, destinationColumn);
            return this;
        }
        /// <summary>
        /// Overrides all column mappings on the underlying bulk copier.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public DocDatabaseLoader SetColumnMappings(params DocRecordColumnInfo[] source)
        {
            return SetColumnMappings(source.AsEnumerable());
        }
        /// <summary>
        /// Overrides all column mappings on the underlying bulk copier.
        /// </summary>
        /// <param name="colSource"></param>
        /// <returns></returns>
        public DocDatabaseLoader SetColumnMappings(IEnumerable<DocRecordColumnInfo> colSource)
        {
            BulkCopy.ColumnMappings.Clear();
            foreach(var col in colSource)
            {
                BulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }
            return this;
        }
        /// <summary>
        /// Overrides all column mappings on the underlying bulk copier.
        /// </summary>
        /// <param name="colSource"></param>
        /// <param name="include">Function to return true if the column should be mapped.</param>
        /// <returns></returns>
        public DocDatabaseLoader SetColumnMappings(IEnumerable<DocRecordColumnInfo> colSource, Func<DocRecordColumnInfo, bool> include)
        {
            BulkCopy.ColumnMappings.Clear();
            foreach (var col in colSource)
            {
                if(include(col))
                    BulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }
            return this;
        }
        /// <summary>
        /// Clear column mappings
        /// </summary>
        public void ClearColumnMappings()
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            BulkCopy.ColumnMappings.Clear();
        }
        /// <summary>
        /// Write the records to the destination table.
        /// <para>Cannot be called while linked to a DataTable.</para>
        /// </summary>
        /// <param name="records"></param>
        public void BulkLoadRecords(IEnumerable<IDataRecord> records)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            if (Linked)
                throw new InvalidOperationException("Object is linked to a DataTable - cannot process IEnumerable in this state.");
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
        /// <summary>
        /// The destination table for this loader.
        /// </summary>
        public string DestinationTableName => BulkCopy.DestinationTableName;
        /// <summary>
        /// Bulk loads the link table data to the Bulk copy destination, and optionally clears the row data.
        /// </summary>
        public void BulkLoadLinkedTable(bool clearRowData)
        {
            if (!Linked)
                throw new InvalidOperationException("Object is not linked to a DataTable.");
            BulkLoadLinkTable(clearRowData);
        }
        private void BulkLoadLinkTable(bool clearRowData)
        {
            BulkLoadTable(linkTable);
            if(clearRowData)
                linkTable.Rows.Clear();
        }
        /// <summary>
        /// Bulk load the DataTable to the table specified by <see cref="DestinationTableName"/>
        /// </summary>
        /// <param name="tableData"></param>
        public void BulkLoadTable(DataTable tableData)
        {
            if (tableData.Rows.Count == 0)
                return;
            if(Linked && tableData != linkTable)
            {
                throw new InvalidOperationException("Loader instance is linked to a DataTable, and the passed DataTable does not match.");
            }
            try
            {
                BulkCopy.WriteToServer(tableData);
            }
            catch (SqlException ex)
            {
                //https://stackoverflow.com/questions/10442686/received-an-invalid-column-length-from-the-bcp-client-for-colid-6
                if (ex.Message.Contains("Received an invalid column length from the bcp client for colid"))
                {
                    string pattern = @"\d+";
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(ex.Message.ToString(), pattern);
                    var index = Convert.ToInt32(match.Value) - 1;

                    FieldInfo fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", BindingFlags.NonPublic | BindingFlags.Instance);
                    var sortedColumns = fi.GetValue(BulkCopy);
                    var items = (object[])sortedColumns.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sortedColumns);

                    FieldInfo itemdata = items[index].GetType().GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
                    var metadata = itemdata.GetValue(items[index]);

                    var column = metadata.GetType().GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                    var length = metadata.GetType().GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                    throw new Exception(string.Format("Column: {0} contains data with a length greater than: {1}", column, length), ex);
                }
                throw;
            }
        }
        ~DocDatabaseLoader()
        {
            if (BulkCopy == null)
                return;
            if (Linked)
            {
                BulkLoadLinkTable(true);
                UnLink();
            }
            ((IDisposable)BulkCopy).Dispose();
        }
        /// <summary>
        /// Dispose underlying SQL Bulk copy object.
        /// <para>If <see cref="Linked"/>, will attempt to bulk insert any data remaining in the datatable, and then clear row data.</para>
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (BulkCopy == null)
                return;
            if (Linked)
            {
                BulkLoadLinkTable(true);
                UnLink();
            }
            ((IDisposable)BulkCopy).Dispose();
            BulkCopy = null;
        }
    }
}
