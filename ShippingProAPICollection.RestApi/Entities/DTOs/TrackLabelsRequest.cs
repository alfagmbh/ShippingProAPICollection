namespace ShippingProAPICollection.RestApi.Entities.DTOs
{
    public class TrackLabelsRequest
    {
       public string ContractID { get; set; }
       public string[] TrackingNumbers { get; set; }
    }
}
