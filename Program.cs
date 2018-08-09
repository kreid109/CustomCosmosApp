using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Configuration;
using System;
using System.IO;
using System.Text;

namespace CosmosAppWithBenchmark
{
    class Program
    {
        public static readonly string endpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"];
        public static readonly string primaryKey = ConfigurationManager.AppSettings["CosmosDbPrimaryKey"];
       
        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                using (DocumentClient client = new DocumentClient(new Uri(endpoint), primaryKey))
                {
                     await createDatabase(client);
                    await createCollection(client);
                    await createCustomCollection(client, "EntertainmentDatabase");
                    await createPurchaseFoodOrBeverage(client, "CustomCollection", "EntertainmentDatabase");
                    //TestTrigger.Run;
                    



                }
                

            }).Wait();
        }

        public static async Task createDatabase(DocumentClient client)
        {
            
                Database targetDatabase = new Database { Id = "EntertainmentDatabase" };
                targetDatabase =  await client.CreateDatabaseIfNotExistsAsync(targetDatabase);                
                await Console.Out.WriteLineAsync($"Database Self-Link:\t{targetDatabase.SelfLink}");
            
        }

        public static async Task createCollection(DocumentClient client)
        {
            Uri databaselink = UriFactory.CreateDatabaseUri("EntertainmentDatabase");

            DocumentCollection defaultCollection = new DocumentCollection
            {
                Id = "DefaultCollection"
            };

            defaultCollection = await client.CreateDocumentCollectionIfNotExistsAsync(databaselink, defaultCollection);
            await Console.Out.WriteLineAsync($"Default Collection Self-Link:\t{defaultCollection.SelfLink}");
        }

        public static IndexingPolicy indexingPolicy()
        {
            IndexingPolicy indexingPolicy = new IndexingPolicy
            {
                IndexingMode = IndexingMode.Consistent,
                Automatic = true,
                IncludedPaths = new Collection<IncludedPath>
                {
                    new IncludedPath
                    {
                        Path = "/*",
                        Indexes = new Collection<Index>
                        {
                            new RangeIndex(DataType.Number, -1),
                            new RangeIndex(DataType.String, -1)
                        }
                    }
                 }
            };

            return indexingPolicy;
        }


        private static PartitionKeyDefinition setPartitionKey(String partitionKey)
        {
            PartitionKeyDefinition partitionKeyDefinition = new PartitionKeyDefinition
            {
                Paths = new Collection<string> { partitionKey }
            };

            return partitionKeyDefinition;
        }

        public static async Task createCustomCollection(DocumentClient client, String databaseName)
        {
            DocumentCollection customCollection = await client.CreateDocumentCollectionIfNotExistsAsync(createDatabaseLink(databaseName), customCollectionParams("CustomCollection", "/type"), setRequestionOptions(1100));
            await Console.Out.WriteLineAsync($"Custom Collection Self-Link:\t{customCollection.SelfLink}");
        }

        private static Uri createDatabaseLink(String databaseName)
        {
            Uri database = UriFactory.CreateDatabaseUri(databaseName);
            return database;
        }

        private static Uri createCollectionLink(String databaseName, String collectionName)
        {
            Uri collection = UriFactory.CreateDocumentCollectionUri(databaseName, collectionName);
            return collection;
        }

        private static DocumentCollection customCollectionParams(String id, String partitionKey)
        {
            
            DocumentCollection customCollection = new DocumentCollection
            {
                Id = id,
                PartitionKey = setPartitionKey(partitionKey),
                IndexingPolicy = indexingPolicy()
            };
           
            return customCollection;
        }

        private static RequestOptions setRequestionOptions(int throughput)
        {
            RequestOptions requestOptions = new RequestOptions
            {
                OfferThroughput = throughput
            };
            return requestOptions;
        }

        private static async Task createPurchaseFoodOrBeverage(DocumentClient client, String collectionLink , String databaseLink)
        {
            var foodInteractions = new Bogus.Faker<PurchaseFoodOrBeverage>()
                .RuleFor(i => i.type, (fake) => nameof(PurchaseFoodOrBeverage))
                .RuleFor(i => i.unitPrice, (fake) => Math.Round(fake.Random.Decimal(1.99m, 15.99m), 2))
                .RuleFor(i => i.quantity, (fake) => fake.Random.Number(1, 5))
                .RuleFor(i => i.totalPrice, (fake, user) => Math.Round(user.unitPrice * user.quantity, 2))
                .Generate(20);

            foreach (var interaction in foodInteractions)
            {

                ResourceResponse<Document> result = await client.CreateDocumentAsync(createCollectionLink(databaseLink,collectionLink), interaction);
                await Console.Out.WriteLineAsync($"Document #{foodInteractions.IndexOf(interaction):000} Created\t{result.Resource.Id}");
            }

        }


    }
}
