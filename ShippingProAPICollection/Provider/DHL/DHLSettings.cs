using Newtonsoft.Json;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Provider.DHL.Entities;

namespace ShippingProAPICollection.Provider.DHL
{
    public class DHLSettings : ProviderSettings
    {
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.DHL;
        /// <summary>
        /// Api domain is the XXXXXXX part of the api url provides by DHL => https://api-XXXXXXX.dhl.com/parcel/de/shipping/v2/	
        /// </summary>
        public required string ApiDomain { get; set; }
        public required string NationalAccountNumber { get; set; }
        public required string InternationalAccountNumber { get; set; }
        public string WarenpostNationalAccountNumber { get; set; } = string.Empty;
        public string WarenpostInternationalAccountNumber { get; set; } = string.Empty;
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string LabelPrintFormat { get; set; }
        public required string DHLShipmentProfile { get; set; }
        public required string APIKey { get; set; }
        /// <summary>
        /// en-US or de-DE
        /// </summary>
        public required string APILanguage { get; set; }
        public override float MaxPackageWeight { get; set; } = 31.5f;

        /// <summary>
        /// Push subscriptions (one per parcel and subscription entry) DHL allows per day; no further parcels are subscribed once it is reached.
        /// </summary>
        public int PushSubscriptionDailyLimit { get; set; } = 500;

        /// <summary>
        /// Filled from the secrets file, never persisted with the provider settings.
        /// </summary>
        [JsonIgnore]
        public DHLPushNotificationSettings? PushNotificationSettings { get; set; }
    }

    public class DHLPushNotificationSettings
    {
        public required string ApiDomain { get; set; }
        public required string APIKey { get; set; }
        public required string APILanguage { get; set; }
        public required string Format { get; set; }
        /// <summary>
        /// Name of the header DHL sends the <see cref="AuthenticationToken"/> in; not a secret, so not read from the secrets file.
        /// </summary>
        public string AuthenticationHeader { get; set; } = "Authorization";
        public required string AuthenticationToken { get; set; }
        /// <summary>
        /// https URL of the caller's webhook that DHL pushes to; the caller forwards the pushes to the push endpoint of the REST API.
        /// </summary>
        public required string HookUri { get; set; }
        /// <summary>
        /// One push subscription is registered per entry.
        /// </summary>
        public required DHLPushSubscription[] Subscriptions { get; set; }
    }

    public class DHLPushSubscription : PushNotificationSettings
    {
        // Active unless a configuration explicitly says otherwise, so entries that never set IsActive keep subscribing.
        public DHLPushSubscription() => IsActive = true;

        public required DHLServiceDivisionTypes Service { get; set; }
        public required DHLNotificationEventTypes[] Events { get; set; }
    }
}
