using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker.Entities
{
    public interface IOrderItem
    {
        Task<int> GetQuantity();

        Task SetQuantity(int quantity);

        Task<int> GetTrackedItemCount();

        Task AddTrackedItem(string trackedItemId);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class OrderItem : IOrderItem
    {
        [JsonProperty("trackedItems")]
        private HashSet<string> TrackedItems { get; set; } = new HashSet<string>();

        [FunctionName(nameof(OrderItem))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<OrderItem>();

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        public Task<int> GetQuantity() => Task.FromResult(this.Quantity);

        public Task SetQuantity(int quantity) => Task.FromResult(this.Quantity = quantity);

        public Task<int> GetTrackedItemCount() => Task.FromResult(TrackedItems.Count);

        public Task AddTrackedItem(string trackedItemId) => Task.FromResult(TrackedItems.Add(trackedItemId));
    }
}
