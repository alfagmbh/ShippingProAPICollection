using ShippingProAPICollection.Models.Entities;

namespace ShippingProAPICollection.Provider.GLS
{
    public class GLSSettings : ProviderSettings
    {
        public override ShippingProviderType ShippingProviderType => ShippingProviderType.GLS;
        /// <summary>
        /// Api domain => https://shipit-wbm-test01.gls-group.eu:443
        /// </summary>
        public required string ApiDomain { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string ApplicationID { get; set; }
        public required string ApplicationSecret { get; set; }
        public required string ContactID { get; set; }
        /// <summary>
        /// Basic auth credentials GLS must send when calling our push notification endpoint.
        /// They are registered at GLS (POST /management/auth) and validated on incoming notifications.
        /// </summary>
        public string? PushNotificationUsername { get; set; }
        public string? PushNotificationPassword { get; set; }
        public override float MaxPackageWeight { get; set; } = 31.5f;
    }
}
