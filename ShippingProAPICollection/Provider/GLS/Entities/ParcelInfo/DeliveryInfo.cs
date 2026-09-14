
using Newtonsoft.Json;
using ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo;



namespace ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo
{
    public class DeliveryInfo
    {
        /// <summary>
        /// List of parcels
        /// </summary>
        [JsonProperty("parcels")]
        public List<Parcel> Parcels { get; set; }
    }
}
