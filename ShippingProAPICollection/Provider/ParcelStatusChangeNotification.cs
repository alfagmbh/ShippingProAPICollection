using JsonSubTypes;
using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.TRANSOFLEX;


namespace ShippingProAPICollection.Provider
{
    [JsonConverter(typeof(JsonSubtypes), nameof(ShippingProviderType))]
    [JsonSubtypes.KnownSubType(typeof(GLSParcelStatusChangeNotification), ShippingProviderType.GLS)]
    [JsonSubtypes.KnownSubType(typeof(DPDWebhookData), ShippingProviderType.DPD)]
    [JsonSubtypes.KnownSubType(typeof(DHLWebhookData), ShippingProviderType.DHL)]
    public abstract class ParcelStatusChangeNotification
    {

    }
}
