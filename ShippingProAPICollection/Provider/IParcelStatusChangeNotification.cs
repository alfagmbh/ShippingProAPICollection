using JsonSubTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.TRANSOFLEX;

namespace ShippingProAPICollection.Provider
{
    public abstract class ParcelStatusChangeNotification
    {
        [BindNever]
        [JsonIgnore]
        public abstract ShippingProviderType ShippingProviderType { get; }
    }
}