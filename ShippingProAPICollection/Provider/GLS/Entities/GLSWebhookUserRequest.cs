using Newtonsoft.Json;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    /// <summary>
    /// Step 1 of the GLS push notification setup (POST /management/user).
    /// </summary>
    public class GLSWebhookUserRequest
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("webhookUrl")]
        public required string WebhookUrl { get; set; }
    }
}
