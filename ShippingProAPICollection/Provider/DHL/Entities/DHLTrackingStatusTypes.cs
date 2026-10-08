using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// see  https://developer.dhl.com/sites/default/files/2024-10/status_2.csv
    /// </summary>
    public static class DHLTrackingStatusTypes
    {
        public static readonly IReadOnlyDictionary<string, TrackingStatusType> _trackingStatusTypes = new Dictionary<string, TrackingStatusType>
        {
            ["delivered"] = TrackingStatusType.DELIVERED,
            ["failure"] = TrackingStatusType.FAILED,
            ["pre-transit"] = TrackingStatusType.TRANSIT,
            ["transit"] = TrackingStatusType.TRANSIT,
            ["unknown"] = TrackingStatusType.UNKNOWN,
    
        };

        public static TrackingStatusType GetTrackingStatusType(string dhlTrackingStatusType)
            => _trackingStatusTypes.TryGetValue(dhlTrackingStatusType, out var trackingStatusType)
            ? trackingStatusType
            : TrackingStatusType.UNKNOWN;
    }
}