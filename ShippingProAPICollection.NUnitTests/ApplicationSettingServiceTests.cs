using Microsoft.Extensions.Configuration;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.RestApi.Entities;

namespace ShippingProAPICollection.NUnitTests
{
    [TestFixture]
    public class ApplicationSettingServiceTests
    {
        private string _originalDirectory = null!;
        private string _workDirectory = null!;

        // ApplicationSettingService reads and writes application-settings.json in the current directory.
        [SetUp]
        public void SetUp()
        {
            _originalDirectory = Directory.GetCurrentDirectory();
            _workDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workDirectory);
            Directory.SetCurrentDirectory(_workDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.SetCurrentDirectory(_originalDirectory);
            Directory.Delete(_workDirectory, true);
        }

        private static ShippingProAPICollectionSettings CreateCollectionSettings() => new(new ShippingProAPIShipFromAddress
        {
            Name = "Homer Simpson",
            Street = "Simpsonstreet 1",
            ContactName = "Maggie Simpson",
            CountryIsoA2Code = "DE",
            City = "Springfield",
            Email = "homer@duffbeer.de",
            PostCode = "73479",
            Phone = "123456789"
        });

        private static GLSSettings CreateGlsSettings(string contractId) => new()
        {
            ContractID = contractId,
            ApiDomain = "https://shipit-wbm-test01.gls-group.eu:443",
            Username = "user",
            Password = "password",
            ApplicationID = "id",
            ApplicationSecret = "secret",
            ContactID = "contact"
        };

        [Test]
        public void AddOrUpdateProviderSettings_FillsGlsPushNotificationSettingsFromConfiguration_AndDoesNotPersistThem()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PushNotificationSettings:GLS1:ApiDomain"] = "https://shipit-wbm-test01.gls-group.eu:443",
                ["PushNotificationSettings:GLS1:ContactID"] = "contact",
                ["PushNotificationSettings:GLS1:IsActive"] = "true",
                ["PushNotificationSettings:GLS1:WebhookName"] = "GLSWebhook",
                ["PushNotificationSettings:GLS1:WebhookUserId"] = "webhook-user",
                ["PushNotificationSettings:GLS1:WebhookUsername"] = "hook",
                ["PushNotificationSettings:GLS1:WebhookPassword"] = "hook-password-123",
            }).Build();
            var service = new ApplicationSettingService(configuration);
            var collectionSettings = CreateCollectionSettings();

            service.AddOrUpdateProviderSettings(collectionSettings, CreateGlsSettings("GLS1"));

            var gls = (GLSSettings)collectionSettings.GetProviders()["GLS1"];
            Assert.That(gls.PushNotificationSettings, Is.Not.Null);
            Assert.That(gls.PushNotificationSettings!.IsActive, Is.True);
            Assert.That(gls.PushNotificationSettings.WebhookUsername, Is.EqualTo("hook"));
            Assert.That(gls.PushNotificationSettings.WebhookPassword, Is.EqualTo("hook-password-123"));
            Assert.That(File.ReadAllText("application-settings.json"), Does.Not.Contain("hook-password-123"));
        }

        [Test]
        public void BuildCollectionSettings_FillsDhlPushNotificationSettingsIncludingSubscriptions()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PushNotificationSettings:DHL1:ApiDomain"] = "sandbox",
                ["PushNotificationSettings:DHL1:APIKey"] = "key",
                ["PushNotificationSettings:DHL1:APILanguage"] = "de-DE",
                ["PushNotificationSettings:DHL1:Format"] = "Default",
                ["PushNotificationSettings:DHL1:AuthenticationToken"] = "token",
                ["PushNotificationSettings:DHL1:HookUri"] = "https://example.com/hook",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Service"] = "ParcelDe",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Events:0"] = "Transit",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Events:1"] = "Delivered",
            }).Build();
            var service = new ApplicationSettingService(configuration);
            var collectionSettings = CreateCollectionSettings();

            service.AddOrUpdateProviderSettings(collectionSettings, new DHLSettings
            {
                ContractID = "DHL1",
                ApiDomain = "sandbox",
                NationalAccountNumber = "1",
                InternationalAccountNumber = "2",
                Username = "user",
                Password = "password",
                LabelPrintFormat = "910-300-410",
                DHLShipmentProfile = "STANDARD_GRUPPENPROFIL",
                APIKey = "key",
                APILanguage = "de-DE"
            });

            var dhl = (DHLSettings)service.BuildCollectionSettings().GetProviders()["DHL1"];
            Assert.That(dhl.PushNotificationSettings, Is.Not.Null);
            Assert.That(dhl.PushNotificationSettings!.AuthenticationToken, Is.EqualTo("token"));
            Assert.That(dhl.PushNotificationSettings.AuthenticationHeader, Is.EqualTo("Authorization"));
            Assert.That(dhl.PushNotificationSettings.Subscriptions, Has.Length.EqualTo(1));
            Assert.That(dhl.PushNotificationSettings.Subscriptions[0].IsActive, Is.True);
            Assert.That(dhl.PushNotificationSettings.Subscriptions[0].Service, Is.EqualTo(DHLServiceDivisionTypes.ParcelDe));
            Assert.That(dhl.PushNotificationSettings.Subscriptions[0].Events,
                Is.EqualTo(new[] { DHLNotificationEventTypes.Transit, DHLNotificationEventTypes.Delivered }));
        }

        [Test]
        public void BuildCollectionSettings_ReadsDhlSubscriptionsByWireValue()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PushNotificationSettings:DHL1:ApiDomain"] = "sandbox",
                ["PushNotificationSettings:DHL1:APIKey"] = "key",
                ["PushNotificationSettings:DHL1:APILanguage"] = "de-DE",
                ["PushNotificationSettings:DHL1:Format"] = "Default",
                ["PushNotificationSettings:DHL1:AuthenticationToken"] = "token",
                ["PushNotificationSettings:DHL1:HookUri"] = "https://example.com/hook",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Service"] = "parcel-de",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Events:0"] = "pre-transit",
                ["PushNotificationSettings:DHL1:Subscriptions:0:Events:1"] = "delivered",
            }).Build();
            var service = new ApplicationSettingService(configuration);
            var collectionSettings = CreateCollectionSettings();

            service.AddOrUpdateProviderSettings(collectionSettings, new DHLSettings
            {
                ContractID = "DHL1",
                ApiDomain = "sandbox",
                NationalAccountNumber = "1",
                InternationalAccountNumber = "2",
                Username = "user",
                Password = "password",
                LabelPrintFormat = "910-300-410",
                DHLShipmentProfile = "STANDARD_GRUPPENPROFIL",
                APIKey = "key",
                APILanguage = "de-DE"
            });

            var dhl = (DHLSettings)service.BuildCollectionSettings().GetProviders()["DHL1"];
            Assert.That(dhl.PushNotificationSettings!.Subscriptions, Has.Length.EqualTo(1));
            Assert.That(dhl.PushNotificationSettings.Subscriptions[0].Service, Is.EqualTo(DHLServiceDivisionTypes.ParcelDe));
            Assert.That(dhl.PushNotificationSettings.Subscriptions[0].Events,
                Is.EqualTo(new[] { DHLNotificationEventTypes.PreTransit, DHLNotificationEventTypes.Delivered }));
        }

        [Test]
        public void AddOrUpdateProviderSettings_WithoutConfigurationSection_LeavesPushNotificationSettingsNull()
        {
            var service = new ApplicationSettingService(new ConfigurationBuilder().Build());
            var collectionSettings = CreateCollectionSettings();

            service.AddOrUpdateProviderSettings(collectionSettings, CreateGlsSettings("GLS1"));

            var gls = (GLSSettings)collectionSettings.GetProviders()["GLS1"];
            Assert.That(gls.PushNotificationSettings, Is.Null);
        }

        [Test]
        public void BuildCollectionSettings_FillsGlsCredentialsFromConfigurationSection_AndKeepsJsonValuesForMissingKeys()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GLS1:ApplicationSecret"] = "secret-from-secrets",
                ["GLS1:CustomerID"] = "customer-from-secrets",
            }).Build();
            var service = new ApplicationSettingService(configuration);
            var settings = service.LoadApplicationSettings();
            settings.ProviderSettings.Add(CreateGlsSettings("GLS1"));
            File.WriteAllText("application-settings.json", Newtonsoft.Json.JsonConvert.SerializeObject(settings));

            var gls = (GLSSettings)service.BuildCollectionSettings().GetProviders()["GLS1"];

            Assert.That(gls.ContractID, Is.EqualTo("GLS1"));
            Assert.That(gls.ApplicationSecret, Is.EqualTo("secret-from-secrets"));
            Assert.That(gls.CustomerID, Is.EqualTo("customer-from-secrets"));
            Assert.That(gls.ApplicationID, Is.EqualTo("id"));
            Assert.That(gls.ContactID, Is.EqualTo("contact"));
        }
    }
}
