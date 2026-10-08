namespace ShippingProAPICollection.Models
{
    public class ParcelTrackingEvent
    {
        public string? ParcelNumber { get; set; }
        public string? Status { get; set; }
        public string? APIValue { get; set; }

        public DateTime? CreatedAt = DateTime.UtcNow;
    }
}