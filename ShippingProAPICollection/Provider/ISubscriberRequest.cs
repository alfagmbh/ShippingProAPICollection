using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShippingProAPICollection.Provider
{
    public abstract class SubscriberRequest
    {
        public abstract ShippingProviderType ShippingProviderType { get; }
        public abstract string HookUri { get; set; }
    }
}
