using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    public class DHLTrackingResponse
    {
        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("firstUrl")]
        public string? FirstUrl { get; set; }

        [JsonProperty("prevUrl")]
        public string? PrevUrl { get; set; }

        [JsonProperty("nextUrl")]
        public string? NextUrl { get; set; }

        [JsonProperty("lastUrl")]
        public string? LastUrl { get; set; }

        [JsonProperty("shipments")]
        public List<Shipment> Shipments { get; set; } = new();

        [JsonProperty("possibleAdditionalShipmentsUrl")]
        public List<string> PossibleAdditionalShipmentsUrl { get; set; } = new();
    }

    public class Shipment
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("service")]
        public string? Service { get; set; }

        [JsonProperty("origin")]
        public Place? Origin { get; set; }

        [JsonProperty("destination")]
        public Place? Destination { get; set; }

        [JsonProperty("status")]
        public TrackingEvent? Status { get; set; }

        [JsonProperty("details")]
        public ShipmentDetails? Details { get; set; }

        [JsonProperty("events")]
        public List<TrackingEvent> Events { get; set; } = new();
    }

    /// <summary>Wrapper used by origin, destination and event locations.</summary>
    public class Place
    {
        [JsonProperty("address")]
        public Address? Address { get; set; }
    }

    public class Address
    {
        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; set; }

        [JsonProperty("addressLocality")]
        public string? AddressLocality { get; set; }
    }

    /// <summary>Used for both the shipment's current "status" and each item in "events".</summary>
    public class TrackingEvent
    {
        // Source timestamps have no timezone offset (e.g. "2023-01-29T16:02:00").
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        // Absent on some events (e.g. "LABEL CREATED").
        [JsonProperty("location")]
        public Place? Location { get; set; }

        // e.g. "transit", "pre-transit", "unknown"
        [JsonProperty("statusCode")]
        public string? StatusCode { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }
    }

    public class ShipmentDetails
    {
        [JsonProperty("product")]
        public Product? Product { get; set; }

        [JsonProperty("weight")]
        public Weight? Weight { get; set; }

        [JsonProperty("references")]
        public List<Reference> References { get; set; } = new();
    }

    public class Product
    {
        [JsonProperty("productName")]
        public string? ProductName { get; set; }
    }

    public class Weight
    {
        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("unitText")]
        public string? UnitText { get; set; }
    }

    public class Reference
    {
        [JsonProperty("number")]
        public string? Number { get; set; }

        // e.g. "customer-confirmation-number", "ecommerce-number", "local-tracking-number"
        [JsonProperty("type")]
        public string? Type { get; set; }
    }
}