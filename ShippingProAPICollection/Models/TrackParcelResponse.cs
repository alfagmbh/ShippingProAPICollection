namespace ShippingProAPICollection.Models
{
    public class TrackParcelResponse
    {
        public string? ParcelNumber { get; set; } 
        public string? Status { get; set; }
        public DateTime? EventDateTime = DateTime.UtcNow;
        public string? LastEvent { get; set; } 
        public string? ErrorMessage { get; set; } 
    }
}
