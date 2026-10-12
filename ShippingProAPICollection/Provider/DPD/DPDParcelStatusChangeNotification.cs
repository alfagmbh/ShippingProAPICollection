using Microsoft.AspNetCore.Mvc;
using ShippingProAPICollection.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShippingProAPICollection.Provider.DPD
{
    public class DPDParcelStatusChangeNotification : ParcelStatusChangeNotification
    {

        [JsonIgnore]
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.DPD;

        [FromQuery(Name = "pushid")]
        [Required]
        public string? PushId { get; set; }

        [FromQuery(Name = "ref")]
        public string? Reference { get; set; }

        [FromQuery(Name = "pnr")]
        [Required]
        public string? ParcelNumber { get; set; }

        [FromQuery(Name = "depot")]
        public string? Depot { get; set; }

        [FromQuery(Name = "status")]
        [Required]
        public string? Status { get; set; }

        [FromQuery(Name = "statusdate")]
        public string? StatusDateRaw { get; set; }

        [JsonIgnore]
        public DateTime? StatusDate =>
            DateTime.TryParseExact(
                StatusDateRaw,
                "ddMMyyyyHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var result)
                ? result
                : (DateTime?)null;

        [FromQuery(Name = "weight")]
        public string? Weight { get; set; }

        [FromQuery(Name = "receiver")]
        public string? Receiver { get; set; }

        [FromQuery(Name = "pod")]
        public string? ProofOfDelivery { get; set; }

        [FromQuery(Name = "services")]
        public string? Services { get; set; }

        [FromQuery(Name = "scaninfo")]
        public string? ScanInfo { get; set; }

    }
}