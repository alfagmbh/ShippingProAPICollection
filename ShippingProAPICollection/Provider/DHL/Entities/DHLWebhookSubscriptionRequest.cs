using Newtonsoft.Json;
using System.Collections.Generic;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    public class DHLWebhookSubscriptionRequest
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }

        [JsonProperty("hook")]
        public DHLWebhookHook Hook { get; set; }

        [JsonProperty("events")]
        public List<string> Events { get; set; } = new List<string> { "Transit", "Delivered", "Pre-Transit", "Failure", "Unknown" };

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("accountID")]
        public string AccountID { get; set; }
    }

    public class DHLWebhookHook
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("authentication")]
        public DHLWebhookAuthentication Authentication { get; set; }

        [JsonProperty("headers")]
        public List<DHLWebhookHeader> Headers { get; set; }
    }

    public class DHLWebhookAuthentication
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("header")]
        public string Header { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }

    public class DHLWebhookHeader
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}