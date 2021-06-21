using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipeHow.AzBobbyTables
{
    internal static class AzDataTableService
    {
        private static TableClient _tableClient;

        static AzDataTableService() {}

        internal static void Connect(string connectionString, string tableName)
        {
            _tableClient = new TableClient(connectionString, tableName);
        }

        internal static void AddRowToTable(Hashtable row)
        {
            var transactions = new List<TableTransactionAction>();

            var entity = new TableEntity();
            foreach (var key in row.Keys)
            {
                entity.Add(key.ToString(), row[key]);
            }

            _tableClient.AddEntity(entity);
        }
        internal static void AddRowsToTable(IEnumerable<Hashtable> rows)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = rows.Select(r =>
            {
                TableEntity entity = new TableEntity();
                foreach (var key in r.Keys)
                {
                    entity.Add(key.ToString(), r[key]);
                }
                return entity;
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Add, e)));
            
            Response<IReadOnlyList<Response>> response = _tableClient.SubmitTransaction(transactions);
        }
    }
}
