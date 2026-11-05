using ShippingProAPICollection.Provider.DHL;

namespace ShippingProAPICollection.NUnitTests
{
    [TestFixture]
    public class DHLSubscriptionActivationsTests
    {
        private static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TryGet_WithoutRecord_ReturnsFalse()
        {
            Assert.That(DHLSubscriptionActivations.TryGet("activation-unknown", out _), Is.False);
        }

        [Test]
        public void Record_Success_IsReportedWithoutError()
        {
            DHLSubscriptionActivations.Record("activation-ok", null, Now);

            Assert.That(DHLSubscriptionActivations.TryGet("activation-ok", out var error), Is.True);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void Record_Failure_IsReportedWithMessage()
        {
            DHLSubscriptionActivations.Record("activation-failed", "DHL rejected the subscriber activation: 401", Now);

            Assert.That(DHLSubscriptionActivations.TryGet("activation-failed", out var error), Is.True);
            Assert.That(error, Does.Contain("401"));
        }

        [Test]
        public void Record_DropsOutcomesOlderThanTenMinutes()
        {
            DHLSubscriptionActivations.Record("activation-old", null, Now);

            DHLSubscriptionActivations.Record("activation-new", null, Now.AddMinutes(11));

            Assert.That(DHLSubscriptionActivations.TryGet("activation-old", out _), Is.False);
            Assert.That(DHLSubscriptionActivations.TryGet("activation-new", out _), Is.True);
        }
    }
}
