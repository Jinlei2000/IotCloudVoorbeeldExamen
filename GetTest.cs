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

                //get the storage account.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                //create the blob client.
                CloudBlobClient blobClient2 = storageAccount.CreateCloudBlobClient();

                // name of the container
                string containerName = "m" + DateTime.Now.ToString("yyyyMMdd");
                // string containerName = "M" + DateTime.Now.ToString("yyyyMMdd");

                //get a reference to a container to use for the sample code
                CloudBlobContainer container2 = blobClient2.GetContainerReference(containerName);

                //create the container if it doesn't already exist.
                await container2.CreateIfNotExistsAsync();

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

                        //make csv file
                        using (var writer = new StreamWriter(csvFileName))
                        {
                            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                            {
                                csv.WriteRecords(meals);
                            }
                        }

                        //upload the csv file
                        CloudBlockBlob blockBlob = container2.GetBlockBlobReference(containerName);
                        await blockBlob.UploadFromFileAsync(csvFileName);
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
