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
                // List<bool> test = new List<bool>();
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
                                // test.Add(meal.Date.ToString("dd-MM-yyyy") == DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy"));
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
                    // return new OkObjectResult(test);
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
                return new OkObjectResult("stop");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }


        }
    }
}
