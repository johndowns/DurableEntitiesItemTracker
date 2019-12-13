using DurableEntitiesItemTracker.Entities;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker
{
    public static class TrackingOrchestrationFunctions
    {
        public static async Task CreateOrderItem(
            string orderItemId, int quantity,
            IDurableOrchestrationContext context)
        {
            var orderItemEntityId = new EntityId(nameof(OrderItem), orderItemId);

            var orderItemProxy = context.CreateEntityProxy<IOrderItem>(orderItemEntityId);
            await orderItemProxy.SetQuantity(quantity);
        }

        public static async Task ApplyTrackingConfiguration(
            string orderItemId, string trackerId,
            IDurableOrchestrationContext context)
        {
            // You may want to call out to other activity functions to do more validation logic here.

            var trackedItemId = await CreateTrackedItemForOrderItem(orderItemId, context);
            await AssignTrackerToTrackedItem(trackerId, trackedItemId, context);
        }

        private static async Task<string> CreateTrackedItemForOrderItem(
            string orderItemId,
            IDurableOrchestrationContext context)
        {
            var orderItemEntityId = new EntityId(nameof(OrderItem), orderItemId);

            // Ensure that we have exclusive access to the OrderItem.
            using (await context.LockAsync(orderItemEntityId))
            {
                var orderItemProxy = context.CreateEntityProxy<IOrderItem>(orderItemEntityId);

                // Confirm that we haven't already used all of the slots for tracked items within this order item.
                var orderItemQuantity = await orderItemProxy.GetQuantity();
                var currentTrackedItemCount = await orderItemProxy.GetTrackedItemCount();
                if (currentTrackedItemCount >= orderItemQuantity)
                {
                    throw new InvalidOperationException("This order item has reached its maximum number of tracked items.");
                }

                // Update the order item so that it knows a slot has been reserved for this order item.
                var trackedItemId = $"{orderItemId}-{currentTrackedItemCount + 1}";
                await orderItemProxy.AddTrackedItem(trackedItemId);

                return trackedItemId;
            }
        }

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
