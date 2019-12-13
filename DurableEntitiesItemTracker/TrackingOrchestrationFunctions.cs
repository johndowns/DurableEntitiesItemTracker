using DurableEntitiesItemTracker.Entities;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker
{
    public static class TrackingOrchestrationFunctions
    {
        [Deterministic]
        public static async Task CreateOrder(
            string orderId, int quantity,
            IDurableOrchestrationContext context)
        {
            var orderEntityId = new EntityId(nameof(Order), orderId);

            var orderProxy = context.CreateEntityProxy<IOrder>(orderEntityId);
            await orderProxy.SetQuantity(quantity);
        }

        [Deterministic]
        public static async Task ApplyTrackingConfiguration(
            string orderId, string trackerId,
            IDurableOrchestrationContext context)
        {
            // You may want to call out to other activity functions to do more validation logic here.

            var trackedItemId = await CreateTrackedItemForOrder(orderId, context);
            await AssignTrackerToTrackedItem(trackerId, trackedItemId, context);
        }

        [Deterministic]
        private static async Task<string> CreateTrackedItemForOrder(
            string orderId,
            IDurableOrchestrationContext context)
        {
            var orderEntityId = new EntityId(nameof(Order), orderId);

            // Ensure that we have exclusive access to the order.
            using (await context.LockAsync(orderEntityId))
            {
                var orderProxy = context.CreateEntityProxy<IOrder>(orderEntityId);

                // Confirm that we haven't already used all of the slots for tracked items within this order item.
                var orderQuantity = await orderProxy.GetQuantity();
                var currentTrackedItemCount = await orderProxy.GetTrackedItemCount();
                if (currentTrackedItemCount >= orderQuantity)
                {
                    throw new InvalidOperationException("This order item has reached its maximum number of tracked items.");
                }

                // Update the order item so that it knows a slot has been reserved for this order item.
                var trackedItemId = $"{orderId}-{currentTrackedItemCount + 1}";
                await orderProxy.AddTrackedItem(trackedItemId);

                return trackedItemId;
            }
        }

        [Deterministic]
        private static async Task AssignTrackerToTrackedItem(
            string trackerId, string trackedItemId,
            IDurableOrchestrationContext context)
        {
            var trackerEntityId = new EntityId(nameof(Tracker), trackerId);
            var trackedItemEntityId = new EntityId(nameof(TrackedItem), trackedItemId);

            // Ensure that we have exclusive access to the Tracker and the TrackedItem.
            using (await context.LockAsync(trackerEntityId, trackedItemEntityId))
            {
                var trackerProxy = context.CreateEntityProxy<ITracker>(trackerEntityId);
                var trackedItemProxy = context.CreateEntityProxy<ITrackedItem>(trackedItemEntityId);

                var currentTrackerItemId = await trackerProxy.GetTrackedItemId();
                if (currentTrackerItemId != null)
                {
                    throw new InvalidOperationException("This tracker has already been assigned.");
                }

                var currentTrackedItemTrackerId = await trackedItemProxy.GetTrackerId();
                if (currentTrackedItemTrackerId != null)
                {
                    throw new InvalidOperationException("This item has already had a tracker assigned.");
                }

                // Assign the tracker to the TrackedItem atomically.
                await trackerProxy.SetTrackedItemId(trackedItemId);
                await trackedItemProxy.SetTrackerId(trackerId);
            }
        }
    }
}
