using Microsoft.AspNetCore.Mvc;
using JsonSubTypes;
using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.TRANSOFLEX;

namespace ShippingProAPICollection.Provider
{
    public interface IParcelStatusChangeNotification
    {
        ShippingProviderType ShippingProviderType { get; }
    }
}