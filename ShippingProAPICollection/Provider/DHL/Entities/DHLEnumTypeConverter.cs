using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ShippingProAPICollection.Provider.DHL.Entities
{
    /// <summary>
    /// Lets the configuration binder read the DHL enums by their wire value ("parcel-de") as well as by name ("ParcelDe");
    /// the default enum conversion only knows the names and silently drops the entry for anything else.
    /// </summary>
    public class DHLEnumTypeConverter<T> : EnumConverter where T : struct, Enum
    {
        public DHLEnumTypeConverter() : base(typeof(T)) { }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string text)
                return base.ConvertFrom(context, culture, value);

            var field = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(f => string.Equals(f.GetCustomAttribute<EnumMemberAttribute>()?.Value, text, StringComparison.OrdinalIgnoreCase));

            return field != null ? field.GetValue(null) : base.ConvertFrom(context, culture, value);
        }
    }
}
