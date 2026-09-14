
using Newtonsoft.Json;



namespace ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo
{
    /// <summary>
    /// An event occurred in the parcel history
    /// </summary>
    public class Event
    {
        [JsonProperty("code")]
        public required string Code { get; set;}
        [JsonProperty("City")]
        public string? City { get; set; }
        [JsonProperty("country")]
        public string? PostalCode { get; set; }
        [JsonProperty("country")]
        public string? Country { get; set; }
        [JsonProperty("description")]
        public string? Description { get; set; }
        [JsonProperty("eventDateTime")]
        public string? EventDateTime { get; set; }

    }
}
