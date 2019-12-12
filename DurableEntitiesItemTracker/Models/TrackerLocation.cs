using Newtonsoft.Json;
using System;

namespace DurableEntitiesItemTracker.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackerLocation
    {
        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
