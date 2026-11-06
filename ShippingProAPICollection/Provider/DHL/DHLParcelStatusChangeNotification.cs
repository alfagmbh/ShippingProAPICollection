using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShippingProAPICollection.Provider.DHL
{
    public class DHLParcelStatusChangeNotification : ParcelStatusChangeNotification
    {
        [JsonIgnore]
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.DHL;

        /// <summary>
        /// Headers of the incoming push request; set by the receiver, never read from the payload.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, string>? RequestHeaders { get; set; }

        [JsonProperty("format")]
        public string? Format { get; set; }

        [JsonProperty("self")]
        public string? Self { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonProperty("secret")]
        public string? Secret { get; set; }

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

        [JsonProperty("origin")]
        public DHLPushLocation? Origin { get; set; }

        [JsonProperty("destination")]
        public DHLPushLocation? Destination { get; set; }

        [JsonProperty("status")]
        public required DHLPushStatus Status { get; set; }

        [JsonProperty("estimatedTimeOfDelivery")]
        public string? EstimatedTimeOfDelivery { get; set; }

        [JsonProperty("returnFlag")]
        public bool? ReturnFlag { get; set; }

        [JsonProperty("details")]
        public DHLPushDetails? Details { get; set; }

        [JsonProperty("events")]
        public List<DHLPushStatus>? Events { get; set; }
    }

    public class DHLPushDetails
    {
        [JsonProperty("product")]
        public DHLPushProduct? Product { get; set; }

        [JsonProperty("proofOfDeliverySignedAvailable")]
        public bool? ProofOfDeliverySignedAvailable { get; set; }
    }

    public class DHLPushProduct
    {
        [JsonProperty("productCode")]
        public string? ProductCode { get; set; }

        [JsonProperty("productName")]
        public string? ProductName { get; set; }
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

        [JsonProperty("statusDetailed")]
        public string? StatusDetailed { get; set; }

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
