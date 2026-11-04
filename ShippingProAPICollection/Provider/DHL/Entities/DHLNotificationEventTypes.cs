using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// Tracking events a DHL push subscription can be registered for.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [TypeConverter(typeof(DHLEnumTypeConverter<DHLNotificationEventTypes>))]
    public enum DHLNotificationEventTypes
    {
        [EnumMember(Value = "pre-transit")]
        PreTransit,
        [EnumMember(Value = "transit")]
        Transit,
        [EnumMember(Value = "delivered")]
        Delivered,
        [EnumMember(Value = "failure")]
        Failure,
        [EnumMember(Value = "unknown")]
        Unknown
    }
}
