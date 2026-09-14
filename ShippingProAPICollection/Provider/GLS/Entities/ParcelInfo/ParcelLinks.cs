
using Newtonsoft.Json;

using ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo;



namespace ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo
{
    /// <summary>
    /// Container for HATEOAS links related to a parcel
    /// </summary>
    public class ParcelLinks
    {
        /// <summary>
        /// A link to the Delivery Information API
        /// </summary>
        [JsonProperty("deliveryInfo")]
        public LinksDeliveryInfo DeliveryInfo { get; set; }
    }
}
