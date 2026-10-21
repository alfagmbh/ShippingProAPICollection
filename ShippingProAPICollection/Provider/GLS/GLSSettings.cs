using ShippingProAPICollection.Models.Entities;
using System.Security.Cryptography;
using System.Text;

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
        public string? CustomerID { get; set; }
        public string? PortalUsername { get; set; }
        public string? PortalPassword { get; set; }
        public override float MaxPackageWeight { get; set; } = 31.5f;

        public required GLSPushNotificationSettings PushNotificationSettings { get; set; }
    }

    public class GLSPushNotificationSettings : PushNotificationSettings
    {
        public required string ApiDomain { get; set; }
        public required string ContactID { get; set; }
        public required string WebhookName { get; set; }
        public required string WebhookUserId { get; set; }
        public required string WebhookUsername { get; set; }
        public required string WebhookPassword { get; set; }

        /// <summary>
        /// Checks the Basic Authorization header of an incoming push notification against the webhook credentials.
        /// Fails closed when no credentials are configured.
        /// </summary>
        public bool IsValidAuthorizationHeader(string? authorizationHeader)
        {
            if (string.IsNullOrEmpty(WebhookUsername) || string.IsNullOrEmpty(WebhookPassword))
                return false;

            const string scheme = "Basic ";
            if (authorizationHeader is null || !authorizationHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return false;

            byte[] provided;
            try
            {
                provided = Convert.FromBase64String(authorizationHeader.Substring(scheme.Length).Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            var expected = Encoding.UTF8.GetBytes($"{WebhookUsername}:{WebhookPassword}");
            return CryptographicOperations.FixedTimeEquals(provided, expected);
        }
    }
}
