using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    public class GLSWebhookSubscriberRequest : SubscriberRequest
    {
        [JsonIgnore]
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.GLS;
        [JsonProperty("id")]
        public string Id => Guid.NewGuid().ToString();

        [JsonProperty("customerId")]
        public required string CustomerId { get; set; }

        [JsonProperty("contactId")]
        public required string ContactId { get; set; }

        [JsonProperty("portalUsername")]
        public required string PortalUsername { get; set; }

        [JsonProperty("portalPassword")]
        public required string PortalPassword { get; set; }

        [JsonProperty("hookUri")]
        public override string HookUri { get; set; }
    }
}