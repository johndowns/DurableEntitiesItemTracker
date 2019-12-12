using DurableEntitiesItemTracker.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker
{
    public class TestOrchestrator
    {
        [FunctionName("MyFunction")]
        public static async Task EntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync("Test");
        }

        [FunctionName("Test")]
        public static async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var trackerId = "xxyzzz";
            var trackedItemId = "order1234-4";

            var success = await AssignTrackerToTrackedItem(trackerId, trackedItemId, context);
            if (success)
            {
                if (!context.IsReplaying) log.LogInformation("Succeeded!");
            }
            else
            {
                if (!context.IsReplaying) log.LogInformation("Failed");
            }
        }

        private static async Task<bool> AssignTrackerToTrackedItem(
            string trackerId, string trackedItemId,
            IDurableOrchestrationContext context)
        {
            var trackerEntityId = new EntityId(nameof(Tracker), trackerId);
            var trackedItemEntityId = new EntityId(nameof(TrackedItem), trackedItemId);

            using (await context.LockAsync(trackerEntityId, trackedItemEntityId))
            {
                var trackerProxy = context.CreateEntityProxy<ITracker>(trackerEntityId);
                var trackedItemProxy = context.CreateEntityProxy<ITrackedItem>(trackedItemEntityId);

                var currentTrackerItemId = await trackerProxy.GetTrackedItemId();
                if (currentTrackerItemId != null)
                {
                    // we can't complete the assignment - the tracker is already in use
                    return false;
                }

                var currentTrackedItemTrackerId = await trackedItemProxy.GetTrackerId();
                if (currentTrackedItemTrackerId != null)
                {
                    // we can't complete the assignment - the item already has a tracker
                    return false;
                }

                await trackerProxy.SetTrackedItemId(trackedItemId);
                await trackedItemProxy.SetTrackerId(trackerId);
                return true;
            }
        }
    }
}
