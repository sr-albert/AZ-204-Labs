using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AdventureWorks.Context;
using AdventureWorks.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Migrate
{
    class Program
    {

        private const string sqlDBConnectionString = "Server=tcp:polysqlsrvr-albert.database.windows.net,1433;Initial Catalog=AdventureWorks;Persist Security Info=False;User ID=testuser;Password=P@ssw0rd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        private const string cosmosDBConnectionString = "AccountEndpoint=https://polycosmosdb-albert.documents.azure.com:443/;AccountKey=JK86tbkup3803nyzo5YcRowvR8tG0URtg6vHr5h6YuQSOanCbzYCWOVvcKv92vuMPF10yw0skNcVBqr4IWh9Ow==;";

        public static async Task Main(string[] args)
        {
            int count = 0;

            await Console.Out.WriteLineAsync("Start Migration");

            using AdventureWorksSqlContext context = new AdventureWorksSqlContext(sqlDBConnectionString);

            List<Model> items = await context.Models.Include(m => m.Products).ToListAsync<Model>();

            await Console.Out.WriteLineAsync($"Total Azure SQL DB Records: {items.Count}");

            // Cosmos DB 
            using CosmosClient client = new CosmosClient(cosmosDBConnectionString);
            // Create new database named [Retail]
            Database database = await client.CreateDatabaseIfNotExistsAsync("Retail");

            // Create new container named [Outline]
            // with Partition key path of [/Category]
            Container container = await database.CreateContainerIfNotExistsAsync("Online", partitionKeyPath: $"/{nameof(Model.Category)}");

            foreach (var item in items)
            {
                // Upsert object into the cosmos db collection 
                ItemResponse<Model> document = await container.UpsertItemAsync<Model>(item);

                await Console.Out.WriteLineAsync($"Upserted document #{++count:000} [Activity Id: {document.ActivityId}]");
                await Console.Out.WriteLineAsync($"Total Azure Cosmos DB Documents: {count}");
            }

        }



    }
}
