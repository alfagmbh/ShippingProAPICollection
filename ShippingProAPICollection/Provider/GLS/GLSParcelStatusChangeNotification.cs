using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.GLS
{
    [JsonObject("statusChange")]
    public class GLSParcelStatusChangeNotification : ParcelStatusChangeNotification
    {
        [JsonIgnore]
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.GLS;

        /// <summary>
        /// Authorization header of the incoming request, never part of the payload.
        /// </summary>
        [JsonIgnore]
        [BindNever]
        public string? AuthorizationHeader { get; set; }

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