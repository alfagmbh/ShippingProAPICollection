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
        /// <summary>
        /// Webhook user id returned by GLS POST /management/user.
        /// </summary>
        [JsonProperty("id")]
        public required string WebhookUserId { get; set; }

        [JsonProperty("customerId")]
        public required string CustomerId { get; set; }

        [JsonProperty("contactId")]
        public required string ContactId { get; set; }

        [JsonProperty("portalUsername")]
        public required string PortalUsername { get; set; }

        [JsonProperty("portalPassword")]
        public required string PortalPassword { get; set; }

        [JsonProperty("webhookUrl")]
        public override string HookUri { get; set; }
    }
}