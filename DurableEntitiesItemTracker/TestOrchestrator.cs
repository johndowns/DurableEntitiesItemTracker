using DurableEntitiesItemTracker.Entities;
using Dynamitey.DynamicObjects;
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
    public class TestOrchestrator
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
            var orderItemId = $"Scenario1TestOrder{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerIds = new[] { $"Scenario1Tracker-{context.NewGuid().ToString()}-1", $"Scenario1Tracker-{context.NewGuid().ToString()}-2" };

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItemId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItemId} now exists, and has a quantity of {quantity}.");

            foreach (var trackerId in trackerIds)
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItemId, trackerId, context);
                if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {orderItemId}.");
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
            var orderItem1Id = $"Scenario2TestOrder-{context.NewGuid().ToString()}";
            var orderItem2Id = $"Scenario2TestOrder-{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerId = $"Scenario2Tracker-{context.NewGuid().ToString()}";

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItem1Id, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItem1Id} now exists, and has a quantity of {quantity}.");

            await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItem1Id, trackerId, context);
            if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {orderItem1Id}.");

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItem2Id, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItem2Id} now exists, and has a quantity of {quantity}.");

            try
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItem2Id, trackerId, context);

                // The above call should fail, since the tracker is already associated with orderItem1Id.
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
            var orderItemId = $"Scenario3TestOrder{context.NewGuid().ToString()}";
            var quantity = 2;
            var trackerIds = new[] { $"Scenario3Tracker-{context.NewGuid().ToString()}-1", $"Scenario3Tracker-{context.NewGuid().ToString()}-2" };

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItemId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItemId} now exists, and has a quantity of {quantity}.");

            foreach (var trackerId in trackerIds)
            {
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItemId, trackerId, context);
                if (!context.IsReplaying) log.LogInformation($"Successfully associated tracker {trackerId} with one of the tracked items in order {orderItemId}.");
            }

            try
            {
                var anotherTrackerId = $"Scenario3Tracker-{context.NewGuid().ToString()}-3";
                await TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItemId, anotherTrackerId, context);

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
        // TODO
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
            var orderItemId = $"Scenario4TestOrder-{context.NewGuid().ToString()}";
            var quantity = 1;

            await TrackingOrchestrationFunctions.CreateOrderItem(orderItemId, quantity, context);
            if (!context.IsReplaying) log.LogInformation($"Order item entity {orderItemId} now exists, and has a quantity of {quantity}.");

            // Send multiple simultaneous attempts to add a tracker to the order item.
            var assignmentAttempts = 3;
            var trackerAssignmentTasks = new List<Task>();
            for (int i = 0; i < assignmentAttempts; i++)
            {
                trackerAssignmentTasks.Add(TrackingOrchestrationFunctions.ApplyTrackingConfiguration(orderItemId, $"Scenario4Tracker-{context.NewGuid().ToString()}-{i}", context));
            }
            var x = await Task.WhenAll(trackerAssignmentTasks.Select(task => WrapResultOrException(task)));

            // Check how many succeeded and failed.
            var successfulAttempts = trackerAssignmentTasks.Count(t => ! t.IsFaulted);
            var failedAttempts = trackerAssignmentTasks.Count(t => t.IsFaulted);

            if (!context.IsReplaying) log.LogInformation($"Out of {assignmentAttempts} attempts to place a tracker on the order item, {successfulAttempts} succeeded and {failedAttempts} failed.");
        }
        #endregion

        private static async Task<TaskSucceededOrFailed> WrapResultOrException(Task task)
        {
            try
            {
                await task;
                return new TaskSucceededOrFailed(true);
            }
            catch (Exception ex)
            {
                return new TaskSucceededOrFailed(false);
            }
        }

        public class TaskSucceededOrFailed
        {
            public TaskSucceededOrFailed(bool isSuccess)
            {
                IsSuccess = isSuccess;
            }

            public bool IsSuccess { get; }
        }

        public class ResultOrException<T>
        {
            public ResultOrException(T result)
            {
                IsSuccess = true;
                Result = result;
            }

            public ResultOrException(Exception ex)
            {
                IsSuccess = false;
                Exception = ex;
            }

            public bool IsSuccess { get; }
            public T Result { get; }
            public Exception Exception { get; }
        }
    }
}
