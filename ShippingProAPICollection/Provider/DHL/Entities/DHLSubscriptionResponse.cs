using Newtonsoft.Json;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// Answer of DHL to a subscribe request (spec: SubscriptionRequestState).
    /// </summary>
    public class DHLSubscriptionResponse
    {
        [JsonProperty("self")]
        public string? Self { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonProperty("expires")]
        public string? Expires { get; set; }

        [JsonProperty("note")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Error answer of DHL (spec: ProblemDetail).
    /// </summary>
    public class DHLProblemDetail
    {
        [JsonProperty("detail")]
        public string? Detail { get; set; }
    }
}
