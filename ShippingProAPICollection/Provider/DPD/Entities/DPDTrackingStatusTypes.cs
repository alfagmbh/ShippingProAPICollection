using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.DPD.Entities
{
    public class DPDTrackingStatusTypes
    {
        private static readonly IReadOnlyDictionary<string, TrackingStatusType> _trackingStatusTypes = new Dictionary<string, TrackingStatusType>()
        {
            [DPDTrackingStatusType.start_order.ToString()] = TrackingStatusType.TRANSIT,

            [DPDTrackingStatusType.pickup_driver.ToString()] = TrackingStatusType.TRANSIT,
            [DPDTrackingStatusType.pickup_depot.ToString()] = TrackingStatusType.TRANSIT,

            [DPDTrackingStatusType.delivery_depot.ToString()] = TrackingStatusType.TRANSIT,
            [DPDTrackingStatusType.delivery_carload.ToString()] = TrackingStatusType.INDELIVERY,
            [DPDTrackingStatusType.delivery_nab.ToString()] = TrackingStatusType.FAILED,
            [DPDTrackingStatusType.delivery_notification.ToString()] = TrackingStatusType.INDELIVERY,
            [DPDTrackingStatusType.delivery_customer.ToString()] = TrackingStatusType.DELIVERED,
            [DPDTrackingStatusType.delivery_shop.ToString()] = TrackingStatusType.DELIVERED,

            [DPDTrackingStatusType.error_pickup.ToString()] = TrackingStatusType.FAILED,
            [DPDTrackingStatusType.error_return.ToString()] = TrackingStatusType.FAILED,

            [DPDTrackingStatusType.pickup_by_consignee.ToString()] = TrackingStatusType.DELIVERED,
            [DPDTrackingStatusType.no_pickup_by_consignee.ToString()] = TrackingStatusType.FAILED,
        };

        public static TrackingStatusType GetTrackingStatusType(string dpdTrackingStatusType)
            => _trackingStatusTypes.TryGetValue(dpdTrackingStatusType, out var trackingStatusType)
            ? trackingStatusType
            : throw new ArgumentException($"Invalid DPD tracking status type: {dpdTrackingStatusType}");
    }

    public enum DPDTrackingStatusType
    {
        start_order,
        pickup_driver,
        pickup_depot,
        delivery_depot,
        delivery_carload,
        delivery_nab,
        delivery_notification,
        delivery_customer,
        delivery_shop,
        error_pickup,
        error_return,
        pickup_by_consignee,
        no_pickup_by_consignee
    }
}