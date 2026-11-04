using Newtonsoft.Json;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Provider;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;

namespace ShippingProAPICollection.RestApi.Entities
{
    public class ApplicationSettingService
    {
        private const string settingFilePath = "application-settings.json";
        private const string pushNotificationSection = "PushNotificationSettings";

        private readonly IConfiguration configuration;

        public ApplicationSettingService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Update account settings
        /// </summary>
        /// <param name="collectionSettings"></param>
        /// <param name="newAccountSettings"></param>
        public void UpdateAccountSettings(ShippingProAPICollectionSettings collectionSettings, ShippingProAPIShipFromAddress newAccountSettings)
        {
            // Load current settings
            var currentSettings = LoadApplicationSettings();
            // Update account settings
            currentSettings.AccountSettings = newAccountSettings;

            SaveAndUpdateCurrentSettings(collectionSettings, currentSettings);
        }

        /// <summary>
        /// Add or update provider settings
        /// </summary>
        /// <param name="collectionSettings"></param>
        /// <param name="newSettings"></param>
        public void AddOrUpdateProviderSettings(ShippingProAPICollectionSettings collectionSettings, ProviderSettings newSettings)
        {
            // Load current settings
            var currentSettings = LoadApplicationSettings();

            collectionSettings.AddSettings(newSettings.ContractID, newSettings);
            currentSettings.ProviderSettings = collectionSettings.GetProviders().Select(x => x.Value).ToList();
            SaveAndUpdateCurrentSettings(collectionSettings, currentSettings);
        }

        /// <summary>
        /// Delete provider by contract id
        /// </summary>
        /// <param name="collectionSettings"></param>
        /// <param name="contractID"></param>
        public void DeleteProvider(ShippingProAPICollectionSettings collectionSettings, string contractID)
        {
            // Load current settings
            var currentSettings = LoadApplicationSettings();
            var providerToDelete = currentSettings.ProviderSettings.FirstOrDefault(x => x.ContractID.Equals(contractID));
            if (providerToDelete != null)
            {
                currentSettings.ProviderSettings.Remove(providerToDelete);
                SaveAndUpdateCurrentSettings(collectionSettings, currentSettings);
            }
        }

        /// <summary>
        /// Build new collection settings
        /// </summary>
        /// <returns></returns>
        public ShippingProAPICollectionSettings BuildCollectionSettings()
        {
            var currentSettings = LoadApplicationSettings();
            var s = new ShippingProAPICollectionSettings(currentSettings.AccountSettings);
            currentSettings.ProviderSettings.ForEach(x =>
            {
                FillSettings(x);
                FillPushNotificationSettings(x);
                s.AddSettings(x.ContractID, x);
            });
            return s;
        }

        private void FillSettings(ProviderSettings provider)
        {
            var section = configuration.GetSection(provider.ContractID);

            switch (provider)
            {
                case GLSSettings gls:
                    PopulateGls(section, gls);
                    break;

                case DPDSettings dpd:
                    PopulateDpd(section, dpd);
                    break;

                case DHLSettings dhl:
                    PopulateDhl(section, dhl);
                    break;
            }

            static void PopulateGls(IConfigurationSection section, GLSSettings gls)
            {
                gls.ApiDomain = section["ApiDomain"] ?? gls.ApiDomain;
                gls.ApplicationID = section["ApplicationID"] ?? gls.ApplicationID;
                gls.ApplicationSecret = section["ApplicationSecret"] ?? gls.ApplicationSecret;
                gls.ContactID = section["ContactID"] ?? gls.ContactID;
                gls.CustomerID = section["CustomerID"] ?? gls.CustomerID;
                gls.Username = section["Username"] ?? gls.Username;
                gls.Password = section["Password"] ?? gls.Password;
            }
            static void PopulateDpd(IConfigurationSection section, DPDSettings dpd)
            {
                dpd.ApiDomain = section["ApiDomain"] ?? dpd.ApiDomain;
                dpd.Username = section["Username"] ?? dpd.Username;
                dpd.Password = section["Password"] ?? dpd.Password;
            }
            static void PopulateDhl(IConfigurationSection section, DHLSettings dhl)
            {
                dhl.ApiDomain = section["ApiDomain"] ?? dhl.ApiDomain;
                dhl.Username = section["Username"] ?? dhl.Username;
                dhl.Password = section["Password"] ?? dhl.Password;
                dhl.APIKey = section["APIKey"] ?? dhl.APIKey;
            }
        }

        /// <summary>
        /// Push notification settings are not part of the saved settings, they are read from the secrets file (PushNotificationSettings:{ContractID})
        /// </summary>
        private void FillPushNotificationSettings(ProviderSettings provider)
        {
            var section = configuration.GetSection($"{pushNotificationSection}:{provider.ContractID}");

            switch (provider)
            {
                case GLSSettings gls:
                    gls.PushNotificationSettings = section.Get<GLSPushNotificationSettings>();
                    break;

                case DHLSettings dhl:
                    dhl.PushNotificationSettings = section.Get<DHLPushNotificationSettings>();
                    break;

                case DPDSettings dpd:
                    dpd.PushNotificationSettings = section.Get<DPDPushNotificationSettings>();
                    break;
            }
        }

        private void SaveAndUpdateCurrentSettings(ShippingProAPICollectionSettings collectionSettings, ApplicationSettings newSettings)
        {
            // Save settings to file
            SaveApplicationSettings(newSettings);
            // Update application settings
            collectionSettings.OverrideSettings(BuildCollectionSettings());
        }

        private void SaveApplicationSettings(ApplicationSettings newSettings)
        {
            File.WriteAllText(settingFilePath, JsonConvert.SerializeObject(newSettings));
        }

        /// <summary>
        /// Load current application settings from file or create new file
        /// </summary>
        /// <returns></returns>
        public ApplicationSettings LoadApplicationSettings()
        {
            if (!File.Exists(settingFilePath))
            {
                // Create json file if not exist
                SaveApplicationSettings(new ApplicationSettings());
            }

            string jsonText = File.ReadAllText(settingFilePath);
            return JsonConvert.DeserializeObject<ApplicationSettings>(jsonText) ?? new ApplicationSettings();
        }
    }
}