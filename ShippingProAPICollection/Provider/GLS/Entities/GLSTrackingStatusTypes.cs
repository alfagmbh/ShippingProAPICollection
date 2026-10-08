using ShippingProAPICollection.Models.Entities;
using System.Collections.ObjectModel;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    public static class GLSTrackingStatusTypes
    {
        private static readonly IReadOnlyDictionary<string, TrackingStatusType> _trackingStatusTypes = new Dictionary<string, TrackingStatusType>()
        {
            [GLSTrackingStatusType.PLANNEDPICKUP.ToString()] = TrackingStatusType.TRANSIT,
            [GLSTrackingStatusType.INPICKUP.ToString()] = TrackingStatusType.TRANSIT,
            [GLSTrackingStatusType.NOTPICKEDUP.ToString()] = TrackingStatusType.FAILED,
            [GLSTrackingStatusType.PREADVICE.ToString()] = TrackingStatusType.TRANSIT,
            [GLSTrackingStatusType.INTRANSIT.ToString()] = TrackingStatusType.TRANSIT,
            [GLSTrackingStatusType.INDELIVERY.ToString()] = TrackingStatusType.INDELIVERY,
            [GLSTrackingStatusType.DELIVEREDPS.ToString()] = TrackingStatusType.DELIVERED,
            [GLSTrackingStatusType.DELIVERED.ToString()] = TrackingStatusType.DELIVERED,
            [GLSTrackingStatusType.INWAREHOUSE.ToString()] = TrackingStatusType.TRANSIT,
            [GLSTrackingStatusType.NOTDELIVERED.ToString()] = TrackingStatusType.FAILED,
            [GLSTrackingStatusType.CANCELED.ToString()] = TrackingStatusType.CANCELLED,
            [GLSTrackingStatusType.FINAL.ToString()] = TrackingStatusType.DELIVERED
        };

        public static TrackingStatusType GetTrackingStatusType(string glsTrackingStatusType)
            => _trackingStatusTypes.TryGetValue(glsTrackingStatusType, out var trackingStatusType)
            ? trackingStatusType
            : TrackingStatusType.UNKNOWN;
    }

    public enum GLSTrackingStatusType
    {
        PLANNEDPICKUP, INPICKUP, NOTPICKEDUP, PREADVICE, INTRANSIT, INDELIVERY, DELIVEREDPS, DELIVERED, INWAREHOUSE, NOTDELIVERED, CANCELED, FINAL
    }
}