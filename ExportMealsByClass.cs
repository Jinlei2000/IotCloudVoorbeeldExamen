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
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer, ILogger log)
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
                string sql = $"SELECT DISTINCT c.ClassTag FROM c";
                var iterator = container.GetItemQueryIterator<Child>(sql);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (Child child in response)
                    {
                        classTags.Add(child.ClassTag);
                    }
                }

                foreach (var classTag in classTags)
                {
                    sql = $"SELECT c.Meals FROM c WHERE c.ClassTag = '{classTag}'";
                    var iterator2 = container.GetItemQueryIterator<Child>(sql);
                    while (iterator2.HasMoreResults)
                    {
                        var response = await iterator2.ReadNextAsync();
                        foreach (var item in response)
                        {
                            foreach (var meal in item.Meals)
                            {
                                //check for yesterday
                                if (meal.Date.ToString("dd-MM-yyyy") == DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy"))
                                {
                                    meals.Add(new Meal()
                                    {
                                        Id = meal.Id,
                                        MealCategory = meal.MealCategory,
                                        Date = meal.Date
                                    });
                                }
                            }
                        }
                    }

                    if (meals.Count > 0)
                    {
                        string containerName = $"M{DateTime.Now.ToString("yyyyMMdd")}";
                        string csvFileName = $"{classTag}.csv";

                        string connectionStringBlob = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
                        // make a new container
                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionStringBlob);
                        BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
                        // make a new blob
                        BlobClient blobClient = containerClient.GetBlobClient(containerName);
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
                    else
                    {
                        log.LogInformation("No meals found today form this class: " + classTag);
                    }
                }
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
