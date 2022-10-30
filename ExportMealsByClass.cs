using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using CsvHelper;
using IotCloudVoorbeeldExamen.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MCT.Functions
{
    public class ExportMealsByClass
    {
        [FunctionName("ExportMealsByClass")]
        public async Task Run([TimerTrigger("0 1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            if (myTimer.IsPastDue)
            {
                log.LogInformation("Timer is running late!");
            }
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);

                List<Meal> meals = new List<Meal>();
                List<string> classTags = new List<string>();

                var container = cosmosClient.GetContainer("registrationmeals", "childern");
                string sql = $"SELECT c.ClassTag FROM c";
                var iterator = container.GetItemQueryIterator<List<string>>(sql);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        foreach (var classTag in item)
                        {
                            classTags.Add(classTag);
                        }
                    }
                }

                foreach (var classTag in classTags)
                {
                    sql = $"SELECT c.Meals FROM c WHERE c.ClassTag = '{classTag}'";
                    var iterator2 = container.GetItemQueryIterator<Meal>(sql);
                    while (iterator2.HasMoreResults)
                    {
                        var response = await iterator2.ReadNextAsync();
                        foreach (var item in response)
                        {
                            //check for yesterday
                            if (item.Date.ToString("dd-MM-yyyy") == DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy"))
                            {
                                meals.Add(new Meal()
                                {
                                    Id = item.Id,
                                    MealCategory = item.MealCategory,
                                    Date = item.Date
                                });
                            }
                        }
                    }

                    string containerName = $"M{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                    string csvFileName = $"{classTag}.csv";

                    // make a new container
                    BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("BlobStorageConnectionString"));
                    BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
                    // make a new blob
                    BlobClient blobClient = containerClient.GetBlobClient($"{DateTime.Now.ToString("yyyyMMddHHmmss")}");
                    //upload the csv file
                    using (var writer = new StreamWriter(csvFileName))
                    {
                        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                            csv.WriteRecords(meals);
                        }
                    }
                    await blobClient.UploadAsync(csvFileName);
                }


            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
