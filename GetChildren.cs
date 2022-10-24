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
using System.Diagnostics;
using IotCloudVoorbeeldExamen.Models;
using System.Collections.Generic;

namespace MCT.Functions
{
    public static class GetChildren
    {
        [FunctionName("GetChildren")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "childern")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("GetChildren function");

                var connectionString = Environment.GetEnvironmentVariable("CosmosConectionString");

                CosmosClientOptions options = new CosmosClientOptions()
                {
                    ConnectionMode = ConnectionMode.Gateway
                };

                var cosmosClient = new CosmosClient(connectionString, options);

                List<Child> childern = new List<Child>();

                var container = cosmosClient.GetContainer("registrationmeals", "childern");
                string sql = "SELECT * FROM c";

                var iterator = container.GetItemQueryIterator<Child>(sql);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        childern.Add(new Child()
                        {
                            StudBookNumber = item.StudBookNumber,
                            ClassTag = item.ClassTag,
                            FirstName = item.FirstName,
                            LastName = item.LastName,
                            EmailAdult = item.EmailAdult,
                            Meal = item.Meal,
                            Date = item.Date
                        });
                    }
                }
                return new CreatedResult($"/childern/", childern);
            }
            catch (System.Exception ex)
            {
                log.LogError(ex.Message);
                return new StatusCodeResult(500);
            }
        }
    }
}
