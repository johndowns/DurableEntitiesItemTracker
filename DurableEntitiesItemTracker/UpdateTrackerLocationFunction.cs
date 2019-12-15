using DurableEntitiesItemTracker.Entities;
using DurableEntitiesItemTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker
{
    public class UpdateTrackerLocationFunction
    {
        [FunctionName(nameof(UpdateTrackerLocation))]
        public static async Task UpdateTrackerLocation(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, ILogger log,
            [DurableClient] IDurableEntityClient entityClient)
        {
            string trackerId = req.Query["trackerId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var locationData = JsonConvert.DeserializeObject<TrackerLocation>(requestBody);

            var entityId = new EntityId(nameof(Tracker), trackerId);
            await entityClient.SignalEntityAsync(entityId, nameof(Tracker.SetCurrentLocation), locationData);
        }
    }
}
