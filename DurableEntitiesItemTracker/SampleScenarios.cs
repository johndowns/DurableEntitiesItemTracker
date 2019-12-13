using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker
{
    public class SampleScenarios
    {
        #region Scenario 1: Happy path
        // This scenario creates an order item with a quantity of 2, then sets up two tracked items with trackers.
        // This scenario should run successfully.
        // To invoke this, issue a GET to http://localhost:7071/api/Scenario1

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
            var orderId = $"Scenario1TestOrder-{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerIds = new[] { $"Scenario1Tracker-{context.NewGuid().ToString()}-1", $"Scenario1Tracker-{context.NewGuid().ToString()}-2" };

            await TrackingOrchestrationFunctions.CreateOrder(orderId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderId} now exists, and has a quantity of {quantity}.");

            foreach (var trackerId in trackerIds)
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderId, trackerId, context);
                if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {orderId}.");
            }
        }
        #endregion

        #region Scenario 2: Tracker already in use
        // This scenario creates an order, assigns a tracker to an item within it, and then tries to assign the same tracker to a different order item.
        // This scenario results in an exception.
        // To invoke this, issue a GET to http://localhost:7071/api/Scenario2

        [FunctionName("Scenario2")]
        public static async Task Scenario2EntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(Scenario2Orchestrator));
        }

        [FunctionName(nameof(Scenario2Orchestrator))]
        public static async Task Scenario2Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var order1Id = $"Scenario2TestOrder-{context.NewGuid().ToString()}";
            var order2Id = $"Scenario2TestOrder-{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerId = $"Scenario2Tracker-{context.NewGuid().ToString()}";

            await TrackingOrchestrationFunctions.CreateOrder(order1Id, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {order1Id} now exists, and has a quantity of {quantity}.");

            await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(order1Id, trackerId, context);
            if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {order1Id}.");

            await TrackingOrchestrationFunctions.CreateOrder(order2Id, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {order2Id} now exists, and has a quantity of {quantity}.");

            try
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(order2Id, trackerId, context);

                // The above call should fail, since the tracker is already associated with order1Id.
                Debug.Assert(false);
            }
            catch (InvalidOperationException ex)
            {
                if (!context.IsReplaying) log.LogInformation($"Received exception (as expected!): {ex.Message}");
            }
        }
        #endregion

        #region Scenario 3: Order already has enough trackers
        // This scenario creates an order with a quantity of 2, assigns two trackers to item within it, and then tries to assign another tracker to the order.
        // This scenario results in an exception.
        // To invoke this, issue a GET to http://localhost:7071/api/Scenario3

        [FunctionName("Scenario3")]
        public static async Task Scenario3EntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(Scenario3Orchestrator));
        }

        [FunctionName(nameof(Scenario3Orchestrator))]
        public static async Task Scenario3Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var orderId = $"Scenario3TestOrder-{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerIds = new[] { $"Scenario3Tracker-{context.NewGuid().ToString()}-1", $"Scenario3Tracker-{context.NewGuid().ToString()}-2" };

            await TrackingOrchestrationFunctions.CreateOrder(orderId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderId} now exists, and has a quantity of {quantity}.");

            foreach (var trackerId in trackerIds)
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderId, trackerId, context);
                if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {orderId}.");
            }

            try
            {
                var anotherTrackerId = $"Scenario3Tracker-{context.NewGuid().ToString()}-3";
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderId, anotherTrackerId, context);

                // The above call should fail, since the order already has the maximum number of trackers.
                Debug.Assert(false);
            }
            catch (InvalidOperationException ex)
            {
                if (!context.IsReplaying) log.LogInformation($"Received exception (as expected!): {ex.Message}");
            }
        }
        #endregion

        #region Scenario 4: Add multiple trackers simultaneously
        // This scenario creates an order with a quantity of 1, and then attempts to assign multiple trackers.
        // Only one of these attempts succeeds due to the locking behaviour of the durable entities.
        // To invoke this, issue a GET to http://localhost:7071/api/Scenario4

        [FunctionName("Scenario4")]
        public static async Task Scenario4EntryPoint(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {
            await orchestrationClient.StartNewAsync(nameof(Scenario4Orchestrator));
        }

        [FunctionName(nameof(Scenario4Orchestrator))]
        public static async Task Scenario4Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var orderId = $"Scenario4TestOrder-{context.NewGuid().ToString()}";
            var quantity = 1;

            await TrackingOrchestrationFunctions.CreateOrder(orderId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderId} now exists, and has a quantity of {quantity}.");

            // Send multiple simultaneous attempts to add a tracker to the order item.
            var assignmentAttempts = 10;
            var trackerAssignmentTasks = new List<Task>();
            for (int i = 0; i < assignmentAttempts; i++)
            {
                trackerAssignmentTasks.Add(TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderId, $"Scenario4Tracker-{context.NewGuid().ToString()}-{i}", context));
            }
            await Task.WhenAll(trackerAssignmentTasks.Select(task => IgnoreTaskException(task)));

            // Check how many succeeded and failed.
            var successfulAttempts = trackerAssignmentTasks.Count(t => t.IsCompletedSuccessfully);
            var failedAttempts = trackerAssignmentTasks.Count(t => t.IsFaulted);

            if (!context.IsReplaying) log.LogInformation($"Out of {assignmentAttempts} attempts to place a tracker on the order item, {successfulAttempts} succeeded and {failedAttempts} failed.");
        }
        #endregion

        #region Helpers
        private static async Task IgnoreTaskException(Task task)
        {
            try
            {
                await task;
                return;
            }
            catch
            {
                return;
            }
        }
        #endregion
    }
}
