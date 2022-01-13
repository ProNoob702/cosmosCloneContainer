using Newtonsoft.Json;

namespace CosmosDbCloner
{
    public class CosmoDbEventDataModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = null!;
        public string AggregateId { get; set; } = null!;
        public int AggregateSequenceNumber { get; set; }
        public string Data { get; set; } = null!;
        public string Metadata { get; set; } = null!;
        [JsonProperty(PropertyName = "oldts")]
        public int OldTimestamp
        {
            get
            {
                return Timestamp;
            }
        }
        [JsonProperty(PropertyName = "_ts")]
        public int Timestamp { get; set; }
    }
}
