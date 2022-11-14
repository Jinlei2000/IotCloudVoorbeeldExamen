using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using IotCloudVoorbeeldExamen.Models;
using CsvHelper;
using System.Globalization;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MCT.Functions
{
    public static class GetTest
    {
        [FunctionName("GetTest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequest req,
            ILogger log)
        {
            try
            {
                // Get the connection string from app settings and use it to create a container client.
                string storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

                // // name of the container
                string containerName = "m" + DateTime.Now.ToString("yyyyMMdd");

                // Get a reference to a container named "containerName" and then create it
                BlobContainerClient containerBlob = new BlobContainerClient(storageConnectionString, containerName);
                await containerBlob.CreateIfNotExistsAsync();


                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);

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
                List<Meal> meals = new List<Meal>();
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
                        string csvFileName = $"{classTag}.csv";

                        // Get temporarily path on your pc
                        string localFilePath = $"{Path.GetTempPath()}{csvFileName}";

                        //make csv file
                        using (var writer = new StreamWriter(csvFileName))
                        {
                            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                            {
                                csv.WriteRecords(meals);
                            }
                        }

                        // Get a reference to a blob named "csvFileName" in a container named "containerName"
                        BlobClient blob = containerBlob.GetBlobClient(localFilePath);

                        // Upload csv file
                        await blob.UploadAsync(localFilePath);

                        // Delete File
                        File.Delete(localFilePath);

                    }
                    else
                    {
                        log.LogInformation("No meals found today form this class: " + classTag);
                    }
                }
                return new OkObjectResult("test");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
