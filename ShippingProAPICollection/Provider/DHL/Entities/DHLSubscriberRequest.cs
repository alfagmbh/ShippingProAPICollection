using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    public class DHLWebhookSubscriberRequest : SubscriberRequest
    {
        [JsonIgnore]
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.DHL;
        public required string DhlApiKey { get; set; }
        public required string Format { get; set; }
        public required string Service { get; set; }
        public required string AuthenticationHeader { get; set; }
        public required string AuthenticationToken { get; set; }
        public required string WebhookHeaderName { get; set; }
        public required string WebhookHeaderValue { get; set; }
        public required string[] Events { get; set; }
        public required string AccountID { get; set; }
        public override required string HookUri { get; set; }

    }

    public class DHLSubscriberActivatorRequest 
    {
        public required string SubscriberId { get; set; }
        public required string DhlApiHookSecret { get; set; }
        public required string DhlApiKey { get; set; }

    }
}
