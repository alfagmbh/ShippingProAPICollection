using ShippingProAPICollection.Provider.DHL;

namespace ShippingProAPICollection.NUnitTests
{
    [TestFixture]
    public class DHLPushSubscriptionQuotaTests
    {
        private static readonly DateTime Day1 = new(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Reserve_GrantsUpToTheDailyLimit()
        {
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-limit", 300, 500, Day1), Is.EqualTo(300));
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-limit", 300, 500, Day1), Is.EqualTo(200));
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-limit", 1, 500, Day1), Is.EqualTo(0));
        }

        [Test]
        public void Reserve_StartsAgainOnTheNextUtcDay()
        {
            DHLPushSubscriptionQuota.Reserve("quota-day", 500, 500, Day1);

            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-day", 10, 500, Day1.AddHours(10)), Is.EqualTo(0));
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-day", 10, 500, Day1.AddDays(1).Date), Is.EqualTo(10));
        }

        [Test]
        public void Reserve_CountsEveryContractSeparately()
        {
            DHLPushSubscriptionQuota.Reserve("quota-a", 500, 500, Day1);

            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-b", 5, 500, Day1), Is.EqualTo(5));
        }

        [Test]
        public void Exhaust_BlocksTheRestOfTheDay_AndSurvivesALowerLimitChange()
        {
            DHLPushSubscriptionQuota.Exhaust("quota-exhaust", Day1);

            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-exhaust", 1, 500, Day1), Is.EqualTo(0));
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-exhaust", 1, int.MaxValue, Day1), Is.EqualTo(0));
            Assert.That(DHLPushSubscriptionQuota.Reserve("quota-exhaust", 1, 500, Day1.AddDays(1)), Is.EqualTo(1));
        }
    }
}
