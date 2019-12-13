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
        const string TestOrderItemId = "Order123";
        const int TestOrderItemQuantity = 1;
        const string TestTrackerId = "yz";

        [FunctionName("Scenario1")]
        public static async Task Scenario1EntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(Scenario1Orchestrator));
        }

        [FunctionName(nameof(Scenario1Orchestrator))]
        public static async Task Scenario1Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            // This scenario creates an order item with a quantity of 2, then sets up two tracked items with trackers.
            // This scenario should run successfully.
            var orderItemId = $"Scenario1TestOrder{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackers = new[] { "Scenario1Tracker1", "Scenario1Tracker2" };

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItemId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItemId} now exists, and has a quantity of {quantity}.");

            foreach (var tracker in trackers)
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItemId, tracker, context);
                if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {tracker} with one of the tracked items in order {orderItemId}.");
            }
        }
    }
}
