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

namespace MCT.Functions
{
    public static class GetMealsByChild
    {
        [FunctionName("GetMealsByChild")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "childern/{childId}/meals")] HttpRequest req,
            string childId,
            ILogger log)
        {
            try
            {
                log.LogInformation("GetMealsByChild function");

                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);

                List<Meal> meals = new List<Meal>();

                var container = cosmosClient.GetContainer("registrationmeals", "childern");
                string sql = $"SELECT c.Meals FROM c WHERE c.id = '{childId}'";

                var iterator = container.GetItemQueryIterator<Child>(sql);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        foreach (var meal in item.Meals)
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

                return new OkObjectResult(meals);
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }

        }
    }
}
