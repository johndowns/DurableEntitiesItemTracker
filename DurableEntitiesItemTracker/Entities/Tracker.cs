using DurableEntitiesItemTracker.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker.Entities
{
    public interface ITracker
    {
        Task<string> GetTrackedItemId();

        Task SetTrackedItemId(string trackedItemId);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Tracker : ITracker
    {
        private IDurableEntityContext context;

        [JsonProperty("location")]
        public TrackerLocation Location { get; set; }

        [JsonProperty("trackedItemId")]
        public string TrackedItemId { get; set; }

        public Task<string> GetTrackedItemId() => Task.FromResult(this.TrackedItemId);

        [FunctionName(nameof(Tracker))]
        public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context)
        {
            // we initialize the entity explicitly here (instead of relying on the implicit default constructor)
            // so we can pass the context through for inter-entity signalling
            return context.DispatchAsync<Tracker>(context);
        }

        public Tracker(IDurableEntityContext context)
        {
            this.context = context;
        }

        public void SetCurrentLocation(TrackerLocation location)
        {
            // Note that if we don't care about locations when there is no TrackedItemId, we'd probably add a check here.

            if (Location != null && location.Timestamp < this.Location.Timestamp)
            {
                // The location we received was out of date, so we will ignore it.
                return;
            }

            this.Location = location;

            if (TrackedItemId != null)
            {
                // Signal the tracked item to let it know it's got a new location.
                context.SignalEntity(new EntityId(nameof(TrackedItem), TrackedItemId), nameof(TrackedItem.SetLocation), location);
            }
        }

        public Task SetTrackedItemId(string trackedItemId)
        {
            if (this.TrackedItemId != null)
            {
                throw new InvalidOperationException();
            }

            this.TrackedItemId = trackedItemId;
            return Task.CompletedTask;
        }
    }
}
