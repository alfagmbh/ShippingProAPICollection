using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.GLS
{
    [JsonObject("statusChange")]
    public class GLSParcelStatusChangeNotification : IParcelStatusChangeNotification
    {
        [JsonIgnore]
        public ShippingProviderType ShippingProviderType => ShippingProviderType.GLS;

        [JsonProperty("parcelId")]
        public string? ParcelId { get; set; }

        [JsonProperty("trackId")]
        public string? TrackId { get; set; }

        [JsonProperty("parcelNumberWithCheck")]
        public string? ParcelNumberWithCheck { get; set; }

        [JsonProperty("parcelStatus")]
        public string? ParcelStatus { get; set; }

        [JsonProperty("locationCode")]
        public string? LocationCode { get; set; }

        [JsonProperty("eventCode")]
        public string? EventCode { get; set; }

        [JsonProperty("eventDateTime")]
        public string? EventDateTime { get; set; }

        [JsonProperty("references")]
        public List<string>? References { get; set; }

    }
}