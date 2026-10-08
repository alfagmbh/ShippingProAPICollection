using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShippingProAPICollection.Provider.DHL
{
    public class DHLParcelStatusChangeNotification : IParcelStatusChangeNotification
    {
        [JsonIgnore]
        public ShippingProviderType ShippingProviderType => ShippingProviderType.DHL;

        [JsonProperty("self")]
        public string? Self { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonProperty("expires")]
        public string? Expires { get; set; }

        [JsonProperty("note")]
        public string? Note { get; set; }

        [JsonProperty("shipments")]
        public required List<DHLPushShipment> Shipments { get; set; }
    }

    public class DHLPushShipment
    {
        [JsonProperty("id")]
        public required string Id { get; set; } //shipment identifier tracking number

        [JsonProperty("service")]
        public string? Service { get; set; }

        [JsonProperty("status")]
        public required DHLPushStatus Status { get; set; }
    }

    public class DHLPushStatus
    {
        [JsonProperty("timestamp")]
        public string? Timestamp { get; set; }

        [JsonProperty("location")]
        public DHLPushLocation? Location { get; set; }

        [JsonProperty("statusCode")]
        public string? StatusCode { get; set; }

        [JsonProperty("status")]
        public string? StatusText { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("pieceIds")]
        public List<string>? PieceIds { get; set; }

        [JsonProperty("remark")]
        public string? Remark { get; set; }

        [JsonProperty("nextSteps")]
        public string? NextSteps { get; set; }
    }

    public class DHLPushLocation
    {
        [JsonProperty("address")]
        public DHLPushAddress? Address { get; set; }
    }

    public class DHLPushAddress
    {
        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; set; }

        [JsonProperty("addressLocality")]
        public string? AddressLocality { get; set; }
    }
}
