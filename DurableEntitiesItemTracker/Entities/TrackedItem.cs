using DurableEntitiesItemTracker.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DurableEntitiesItemTracker.Entities
{
    public interface ITrackedItem
    {
        Task<string> GetTrackerId();

        Task SetTrackerId(string trackerId);

        Task SetLocation(TrackerLocation location);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TrackedItem : ITrackedItem
    {
        [JsonProperty("trackerId")]
        public string TrackerId { get; set; }

        [JsonProperty("location")]
        public TrackerLocation Location { get; set; }

        [FunctionName(nameof(TrackedItem))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<TrackedItem>();

        public Task<string> GetTrackerId() => Task.FromResult(this.TrackerId);

        public Task SetTrackerId(string trackerId)
        {
            // Check if there is already a tracker associated with this TrackedItem.
            if (this.TrackerId != null)
            {
                Debug.Assert(false); // This scenario shouldn't happen if we are going through our UpdateTrackerConfiguration orchestrator.
                throw new InvalidOperationException();
            }

            this.TrackerId = trackerId;
            return Task.CompletedTask;
        }

        public Task SetLocation(TrackerLocation location) => Task.FromResult(this.Location = location); // You might perform any geofencing checks in here.
    }
}
