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
        const string TestOrderItemId = "Order123";
        const int TestOrderItemQuantity = 1;
        const string TestTrackerId = "yz";

        [FunctionName("CreateOrderItemEntryPoint")]
        public static async Task CreateOrderItemEntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(CreateOrderItemOrchestrator));
        }

        [FunctionName("CreateOrderItemOrchestrator")]
        public static async Task CreateOrderItemOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            await CreateOrderItem(TestOrderItemId, TestOrderItemQuantity, context);
        }

        private static Task CreateOrderItem(
            string orderItemId, int quantity,
            IDurableOrchestrationContext context)
        {
            var orderItemEntityId = new EntityId(nameof(OrderItem), orderItemId);

            var orderItemProxy = context.CreateEntityProxy<IOrderItem>(orderItemEntityId);
            orderItemProxy.SetQuantity(quantity);
            return Task.CompletedTask;
        }

        // ------

        [FunctionName("ApplyConfigurationEntryPoint")]
        public static async Task ApplyConfigurationEntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(ApplyTrackingConfigurationOrchestrator));
        }

        [FunctionName("ApplyTrackingConfigurationOrchestrator")]
        public static async Task ApplyTrackingConfigurationOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            await OrchestrationFunctions.ApplyTrackingConfiguration(TestOrderItemId, TestTrackerId, context);
        }
    }
}
