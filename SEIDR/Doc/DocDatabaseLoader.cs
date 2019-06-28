﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.Doc
{
    /// <summary>
    /// Helper class for loading <see cref="IDataRecord"/> implementations into a database.
    /// </summary>
    public class DocDatabaseLoader :IDisposable
    {
        System.Data.SqlClient.SqlBulkCopy BulkCopy;
        /// <summary>
        /// Construct a helper class for bulk loading file content to a DataTable.
        /// </summary>
        /// <param name="TableName"></param>
        /// <param name="connectionManager"></param>
        /// <param name="bulkCopyOptions"></param>
        /// <param name="Schema"></param>
        public DocDatabaseLoader(string TableName, DataBase.DatabaseManager connectionManager, System.Data.SqlClient.SqlBulkCopyOptions bulkCopyOptions = System.Data.SqlClient.SqlBulkCopyOptions.Default, string Schema = null)
        {            
            BulkCopy = new System.Data.SqlClient.SqlBulkCopy(connectionManager.GetConnectionString(), bulkCopyOptions);
            DefaultSchema = '[' + connectionManager.DefaultSchema.Replace("[", "").Replace("]", "") + ']';
            SetDestinationTable(TableName, Schema);                        
        }
        /// <summary>
        /// Default schema when setting table name.
        /// </summary>
        public readonly string DefaultSchema;
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
        public void AddColumnMappings(params System.Data.SqlClient.SqlBulkCopyColumnMapping[] columnMappings)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
            foreach (System.Data.SqlClient.SqlBulkCopyColumnMapping map in columnMappings)
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
        /// </summary>
        /// <param name="records"></param>
        public void BulkLoadRecords(IEnumerable<IDataRecord> records)
        {
            if (BulkCopy == null)
                throw new InvalidOperationException("Object has been disposed.");
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
        /// Bulk load the DataTable to the table specified by <see cref="DestinationTableName"/>
        /// </summary>
        /// <param name="tableData"></param>
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
            BulkCopy = null;
        }
    }
}