﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker.Entities
{
    public interface IOrder
    {
        [Deterministic]
        Task<int?> GetQuantity();

        [Deterministic]
        Task SetQuantity(int quantity);

        [Deterministic]
        Task<int> GetTrackedItemCount();

        [Deterministic]
        Task AddTrackedItem(string trackedItemId);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Order : IOrder
    {
        [JsonProperty("trackedItems")]
        private HashSet<string> TrackedItems { get; set; } = new HashSet<string>();

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        [FunctionName(nameof(Order))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Order>();

        public Task<int?> GetQuantity() => Task.FromResult(this.Quantity);

        public Task SetQuantity(int quantity) => Task.FromResult(this.Quantity = quantity);

        public Task<int> GetTrackedItemCount() => Task.FromResult(TrackedItems.Count);

        public Task AddTrackedItem(string trackedItemId) => Task.FromResult(TrackedItems.Add(trackedItemId));
    }
}
