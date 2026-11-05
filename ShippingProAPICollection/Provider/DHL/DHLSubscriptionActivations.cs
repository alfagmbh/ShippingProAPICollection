using System;
using System.Collections.Generic;
using System.Linq;

namespace ShippingProAPICollection.Provider.DHL
{
    /// <summary>
    /// Remembers how the activation of DHL subscriptions went. The activation happens when DHL's validation push reaches the push handler,
    /// which is a different request than the one that created the subscription, so a manual subscribe call reads the result from here.
    /// In memory only: it is lost on restart and not shared between application instances.
    /// </summary>
    public static class DHLSubscriptionActivations
    {
        private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
        private static readonly object gate = new object();
        private static readonly Dictionary<string, (DateTime At, string? Error)> results = new Dictionary<string, (DateTime, string?)>();

        /// <summary>
        /// Stores the outcome of an activation (<paramref name="error"/> is null when it succeeded) and drops outcomes older than ten minutes.
        /// </summary>
        public static void Record(string subscriptionId, string? error, DateTime utcNow)
        {
            lock (gate)
            {
                foreach (var expired in results.Where(r => utcNow - r.Value.At > Retention).Select(r => r.Key).ToList())
                    results.Remove(expired);

                results[subscriptionId] = (utcNow, error);
            }
        }

        /// <summary>
        /// Returns true when an activation outcome is known; <paramref name="error"/> is null when the activation succeeded.
        /// </summary>
        public static bool TryGet(string subscriptionId, out string? error)
        {
            lock (gate)
            {
                if (results.TryGetValue(subscriptionId, out var result))
                {
                    error = result.Error;
                    return true;
                }

                error = null;
                return false;
            }
        }
    }
}
