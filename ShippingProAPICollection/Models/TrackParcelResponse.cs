namespace ShippingProAPICollection.Models
{
    public class TrackParcelResponse
    {
        public required string ParcelNumber { get; set; } 
        public required string Status { get; set; }
        public required string ResponseCode { get; set; } 
        public string? City { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; } 
        public string? ErrorMessage { get; set; } 

        
    }
}
