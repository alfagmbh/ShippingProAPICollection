using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShippingProAPICollection.Provider.GLS.Entities
{
    public record GLSAuthToken
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; init; }
        [JsonProperty("token_type")]
        public required string TokenType { get; init; }
        [JsonProperty("expires_in")]
        public required int ExpiresIn { get; init; }
    }
}
