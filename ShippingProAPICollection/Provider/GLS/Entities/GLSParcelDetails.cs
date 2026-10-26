using Newtonsoft.Json;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    /// <summary>
    /// Response envelope of the GLS Track And Trace API
    /// </summary>
    public record GLSParcelsResponse
    {
        [JsonProperty("parcels")]
        public List<GLSParcelDetails> Parcels { get; init; } = [];
    }

    /// <summary>
    /// Current status and event history of one parcel. When GLS cannot resolve the requested parcel only
    /// <see cref="Requested"/>, <see cref="ErrorCode"/> and <see cref="ErrorMessage"/> are set.
    /// </summary>
    public record GLSParcelDetails
    {
        /// <summary>The originally requested parcel number</summary>
        [JsonProperty("requested")]
        public string? Requested { get; init; }

        /// <summary>The GLS parcel number (unit no)</summary>
        [JsonProperty("unitno")]
        public string? UnitNo { get; init; }

        [JsonProperty("status")]
        public GLSParcelStatus? Status { get; init; }

        [JsonProperty("statusDateTime")]
        public DateTimeOffset? StatusDateTime { get; init; }

        /// <summary>Events of the parcel history, newest first</summary>
        [JsonProperty("events")]
        public List<GLSParcelEvent> Events { get; init; } = [];

        [JsonProperty("errorCode")]
        public GLSParcelErrorCode? ErrorCode { get; init; }

        /// <summary>Error description, only available in English</summary>
        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; init; }
    }

    public record GLSParcelEvent
    {
        /// <summary>Event type, for example DELIVD.PARCELSHOP</summary>
        [JsonProperty("code")]
        public string? Code { get; init; }

        [JsonProperty("city")]
        public string? City { get; init; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; init; }

        /// <summary>ISO 3166-1 alpha-2 country code</summary>
        [JsonProperty("country")]
        public string? Country { get; init; }

        /// <summary>Event description in the requested language</summary>
        [JsonProperty("description")]
        public string? Description { get; init; }

        [JsonProperty("eventDateTime")]
        public DateTimeOffset? EventDateTime { get; init; }
    }
}
