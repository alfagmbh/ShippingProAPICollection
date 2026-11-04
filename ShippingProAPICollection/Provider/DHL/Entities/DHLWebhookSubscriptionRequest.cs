using Newtonsoft.Json;
using System.Collections.Generic;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    public class DHLWebhookSubscriptionRequest
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("service")]
        public DHLServiceDivisionTypes Service { get; set; }

        [JsonProperty("hook")]
        public DHLWebhookHook Hook { get; set; }

        [JsonProperty("events")]
        public List<DHLNotificationEventTypes> Events { get; set; } = new List<DHLNotificationEventTypes>
        {
            DHLNotificationEventTypes.Transit, DHLNotificationEventTypes.Delivered, DHLNotificationEventTypes.PreTransit, DHLNotificationEventTypes.Failure, DHLNotificationEventTypes.Unknown
        };

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("shipmentIDs")]
        public List<string> ShipmentIDs { get; set; }
    }

    public class DHLWebhookHook
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("authentication")]
        public DHLWebhookAuthentication Authentication { get; set; }
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
}