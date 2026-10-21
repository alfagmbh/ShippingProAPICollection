using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// DHL service divisions (the "service" value of a push subscription).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DHLServiceDivisionTypes
    {
        [EnumMember(Value = "parcel-de")]
        ParcelDe,
        [EnumMember(Value = "express")]
        Express,
        [EnumMember(Value = "ecommerce")]
        ECommerce
    }
}
