
namespace ShippingProAPICollection.Models
{
    public enum NotificationResultStatus
    {
        Success,
        Failed,
        Unknown
    }

    public class ParcelTrackingEventResult
    {
        public NotificationResultStatus Status { get; set; }
        public string? Message { get; set; }
        public ParcelTrackingEvent[]? TrackingEvent { get; set; }
    }
    public class ParcelTrackingEvent
    {
        public string? ParcelNumber { get; set; }
        public string? Status { get; set; }
        public string? APIValue { get; set; }
        public DateTime? CreatedAt = DateTime.UtcNow;
    }
}