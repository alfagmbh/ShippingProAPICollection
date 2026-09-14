
using Newtonsoft.Json;



namespace ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo
{
    /// <summary>
    /// A link to the Delivery Information API
    /// </summary>
    public class LinksDeliveryInfo
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
