namespace ShippingProAPICollection.Models
{
    public enum NotificationResultStatus
    {
        Success,
        Failed,
        Unknown
    }

    public class ParcelTrackingEventResult
    {
        public NotificationResultStatus Status { get; set; }
        public string? Message { get; set; }
        public ParcelTrackingEvent[]? TrackingEvent { get; set; }

        /// <summary>
        /// Set for provider subscription handshake messages (e.g. DHL subscription.validate), which carry no tracking events.
        /// </summary>
        public PushSubscriptionInfo? Subscription { get; set; }
    }

    /// <summary>
    /// A push subscription as reported by the provider: either from a handshake push or from the answer to a subscribe request.
    /// </summary>
    public class PushSubscriptionInfo
    {
        public string? Service { get; set; }
        public string? Scope { get; set; }
        public string? SubscriptionId { get; set; }

        /// <summary>
        /// Validation-of-intent secret, needed by the caller to activate the subscription.
        /// </summary>
        public string? Secret { get; set; }

        public string? Expires { get; set; }
        public string? Note { get; set; }

        /// <summary>
        /// Set when the provider rejected this subscription.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// DHL: the parcels this subscription covers.
        /// </summary>
        public string[]? TrackingNumbers { get; set; }

        /// <summary>
        /// Provider-issued values the caller has to store in the secrets file (key = setting name).
        /// </summary>
        public Dictionary<string, string>? ValuesToSave { get; set; }

        public string ApiResponse { get; set; } = string.Empty;
    }

    public class ParcelTrackingEvent
    {
        public string? ParcelNumber { get; set; }
        public string? Status { get; set; }
        public string? StatusMessage { get; set; }
        public string? EstimatedDeliveryDate { get; set; }
        public ParcelReturn? IsReturn { get; set; }
        public string? APIValue { get; set; }
        public DateTime? CreatedAt = DateTime.UtcNow;
    }


    public enum ParcelReturn
    {
        IsReturn,
        IsNotReturn,
        Unknown
    }
}