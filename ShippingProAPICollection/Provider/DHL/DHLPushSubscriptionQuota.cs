using System;
using System.Collections.Generic;

namespace ShippingProAPICollection.Provider.DHL
{
    /// <summary>
    /// Counts the push subscriptions created per contract and UTC day. Static because the provider services are created per request;
    /// the count starts again at zero when the application restarts.
    /// </summary>
    public static class DHLPushSubscriptionQuota
    {
        private static readonly object gate = new object();
        private static readonly Dictionary<string, (DateTime Day, int Used)> usage = new Dictionary<string, (DateTime, int)>();

        /// <summary>
        /// Takes up to <paramref name="wanted"/> subscriptions from today's quota and returns how many were granted.
        /// </summary>
        public static int Reserve(string contractId, int wanted, int dailyLimit, DateTime utcNow)
        {
            lock (gate)
            {
                var today = utcNow.Date;
                var used = usage.TryGetValue(contractId, out var state) && state.Day == today ? state.Used : 0;

                var granted = Math.Max(0, Math.Min(wanted, dailyLimit - used));
                usage[contractId] = (today, used + granted);
                return granted;
            }
        }

        /// <summary>
        /// Uses up the rest of today's quota, e.g. when DHL answers that the limit is reached.
        /// </summary>
        public static void Exhaust(string contractId, DateTime utcNow)
        {
            lock (gate)
                usage[contractId] = (utcNow.Date, int.MaxValue);
        }
    }
}
