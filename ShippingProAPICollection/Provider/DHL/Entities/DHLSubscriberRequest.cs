using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    public class DHLSubscriberActivatorRequest
    {
        public required string SubscriberId { get; set; }
        public required string DhlApiHookSecret { get; set; }
        public required string DhlApiKey { get; set; }

    }
}
