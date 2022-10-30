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
using IotCloudVoorbeeldExamen.Models;
using System.Collections.Generic;

namespace MCT.Functions
{
    public static class AddChild
    {
        [FunctionName("AddChild")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = "childern")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("AddChild function");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Child child = JsonConvert.DeserializeObject<Child>(requestBody);
                child.Id = Guid.NewGuid();
                child.Meals = new List<Meal>();

                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);

                var container = cosmosClient.GetContainer("registrationmeals", "childern");
                var response = await container.CreateItemAsync<Child>(child, new PartitionKey(child.Id.ToString()));
                
                return new CreatedResult($"/childern/", "Child added");
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}
