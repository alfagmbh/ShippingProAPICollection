using ShippingProAPICollection.Models.Entities;
using System.Text.Json.Serialization;

namespace ShippingProAPICollection.Provider.DPD
{
    public class DPDParcelStatusChangeNotification : ParcelStatusChangeNotification
    {

        public override ShippingProviderType ShippingProviderType => ShippingProviderType.DPD;

        public string? PushId { get; set; }

        public string? Reference { get; set; }

        public string? ParcelNumber { get; set; }

        public string? Depot { get; set; }

        public string? Status { get; set; }

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

        public string? Weight { get; set; }

        public string? Receiver { get; set; }

        public string? ProofOfDelivery { get; set; }

        public string? Services { get; set; }

        public string? ScanInfo { get; set; }

        /// <summary>
        /// Builds the notification from the key/value pairs of the DPD push query string (keys are matched case-insensitively):
        /// pushid, ref, pnr, depot, status, statusdate, weight, receiver, pod, services, scaninfo.
        /// </summary>
        public static DPDParcelStatusChangeNotification FromKeyValues(IReadOnlyDictionary<string, string?> values)
        {
            var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
                map[pair.Key] = pair.Value;

            string? Get(string key) => map.TryGetValue(key, out var value) ? value : null;

            return new DPDParcelStatusChangeNotification
            {
                PushId = Get("pushid"),
                Reference = Get("ref"),
                ParcelNumber = Get("pnr"),
                Depot = Get("depot"),
                Status = Get("status"),
                StatusDateRaw = Get("statusdate"),
                Weight = Get("weight"),
                Receiver = Get("receiver"),
                ProofOfDelivery = Get("pod"),
                Services = Get("services"),
                ScanInfo = Get("scaninfo")
            };
        }

    }
}
