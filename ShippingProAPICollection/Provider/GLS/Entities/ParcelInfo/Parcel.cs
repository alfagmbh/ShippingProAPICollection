
using Newtonsoft.Json;

using ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo;



namespace ShippingProAPICollection.Provider.GLS.Entities.ParcelInfo
{
    /// <summary>
    /// Parcel Data including the current status and events history
    /// </summary>
    public class Parcel
    {
        /// <summary>
        /// The originally requested parcel id or reference
        /// </summary>
        [JsonProperty("requested")]
        public string Requested { get; set; }

        /// <summary>
        /// The GLS Parcel Number (Unit No) that relate to the requested parcel id or reference
        /// </summary>
        [JsonProperty("unitno")]
        public string UnitNo { get; set; }

        /// <summary>
        /// The current status of the parcel.
        /// </summary>
        [JsonProperty("status")]
        [JsonConverter(typeof(GLSParcelStatus))]
        public GLSParcelStatus? Status { get; set; }

        /// <summary>
        /// Status change occurrence date and time in ISO 8601 format. Example: 2024-10-07T10:46:14+0200
        /// </summary>
        [JsonProperty("statusDateTime")]
        public string StatusDateTime { get; set; }

        /// <summary>
        /// Include HATEOAS links to other resources related to the parcel. Example: Delivery Information
        /// </summary>
        [JsonProperty("_links")]
        public ParcelLinks Links { get; set; }

        /// <summary>
        /// Events occurred in the parcel history
        /// </summary>
        [JsonProperty("events")]
        public List<Event> Events { get; set; }

        /// <summary>
        /// Error code related to the requested parcel id
        /// </summary>
        [JsonProperty("errorCode")]
        [JsonConverter(typeof(GLSParcelErrorCode))]
        public GLSParcelErrorCode? ErrorCode { get; set; }

        /// <summary>
        /// Error description related to the requested parcel id or reference. This only available in English.
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
