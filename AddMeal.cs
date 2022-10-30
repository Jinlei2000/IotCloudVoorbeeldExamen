using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using IotCloudVoorbeeldExamen.Models;
using Microsoft.Azure.Cosmos;

namespace MCT.Functions
{
    public static class AddMeal
    {
        [FunctionName("AddMeal")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "childern/{childId}/meals")] HttpRequest req,
            string childId,
            ILogger log)
        {
            try
            {
                log.LogInformation("AddMeal function");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Meal meal = JsonConvert.DeserializeObject<Meal>(requestBody);
                meal.Id = Guid.NewGuid();

                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);


                var containerMeals = cosmosClient.GetContainer("registrationmeals", "meals");
                await containerMeals.CreateItemAsync<Meal>(meal, new PartitionKey(meal.Id.ToString()));

                //update child with new meal
                var containerChildern = cosmosClient.GetContainer("registrationmeals", "childern");
                string sql = $"SELECT * FROM c WHERE c.id = '{childId}'";
                var iterator = containerChildern.GetItemQueryIterator<Child>(sql);
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    Child child = item;
                    child.Meals.Add(meal);
                    await containerChildern.ReplaceItemAsync<Child>(child, child.Id.ToString(), new PartitionKey(child.Id.ToString()));
                }
                return new CreatedResult($"/childern/{childId}/meals/", "meal added");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}
