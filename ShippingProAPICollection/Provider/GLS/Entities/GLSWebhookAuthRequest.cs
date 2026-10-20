using Newtonsoft.Json;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    /// <summary>
    /// Step 2 of the GLS push notification setup (POST /management/auth).
    /// The BASIC credentials are taken from <see cref="GLSSettings"/>, not from the request.
    /// </summary>
    public class GLSWebhookAuthRequest
    {
        /// <summary>
        /// Webhook user id returned by step 1.
        /// </summary>
        [JsonProperty("id")]
        public required string WebhookUserId { get; set; }

        [JsonProperty("webhookUrl")]
        public required string WebhookUrl { get; set; }
    }
}
