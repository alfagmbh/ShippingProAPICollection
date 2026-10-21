using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// Tracking events a DHL push subscription can be registered for.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DHLNotificationEventTypes
    {
        [EnumMember(Value = "Pre-Transit")]
        PreTransit,
        [EnumMember(Value = "Transit")]
        Transit,
        [EnumMember(Value = "Delivered")]
        Delivered,
        [EnumMember(Value = "Failure")]
        Failure,
        [EnumMember(Value = "Unknown")]
        Unknown
    }
}
