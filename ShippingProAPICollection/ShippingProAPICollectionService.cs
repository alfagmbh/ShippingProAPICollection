using Microsoft.Extensions.Caching.Memory;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DHL.Entities;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
using ShippingProAPICollection.Provider.GLS.Entities;
using ShippingProAPICollection.Provider.TRANSOFLEX;

namespace ShippingProAPICollection
{
    public class ShippingProAPICollectionService
    {
        public Dictionary<string, IShippingProviderService> providerServices = new Dictionary<string, IShippingProviderService>();

        public ShippingProAPICollectionService(IMemoryCache _cache, ShippingProAPICollectionSettings providerSettings)
        {
            Dictionary<string, ProviderSettings> providers = providerSettings.GetProviders();

            foreach (KeyValuePair<string, ProviderSettings> provider in providers)
            {
                providerServices.Add(provider.Key, BuildProviderService(providerSettings.DefaultShipFromAddress, provider.Value, _cache));
            }
        }

        private IShippingProviderService BuildProviderService(ShippingProAPIShipFromAddress defaultShipFromAddress, ProviderSettings settings, IMemoryCache _cache)
        {
            switch (settings)
            {
                case GLSSettings providerSettings:
                    return new GLSShipmentService(defaultShipFromAddress, providerSettings);

                case DHLSettings providerSettings:
                    return new DHLShipmentService(defaultShipFromAddress, providerSettings);

                case DPDSettings providerSettings:
                    return new DPDShipmentService(defaultShipFromAddress, providerSettings, _cache);

                case TOFSettings providerSettings:
                    return new TOFShipmentService(defaultShipFromAddress, providerSettings, _cache);

                case CustomProviderSettings providerSettings:
                    return providerSettings.CreateProviderService(defaultShipFromAddress, _cache);

                default: throw new Exception("provider not available");
            }
        }

        public async Task<List<RequestShippingLabelResponse>> RequestLabel(RequestShipmentBase request, CancellationToken ct = default)
        {
            request.Validate();

            if (providerServices.TryGetValue(request.ContractID, out var service))
            {
                // Validate package weight
                foreach (var item in request.Items!)
                {
                    if (item.Weight > service.GetMaxPackageWeight()) throw new ShipmentRequestWeightException(0.1f, service.GetMaxPackageWeight(), item.Weight);
                }

                return await service.RequestLabel(request, ct);
            }

            throw new InvalidOperationException("Unknown shipping provider");
        }

        public async Task<ValidationReponse> ValidateLabel(RequestShipmentBase request, CancellationToken ct = default)
        {
            request.Validate();

            if (providerServices.TryGetValue(request.ContractID, out var service))
            {
                return await service.ValidateLabel(request, ct);
            }

            throw new InvalidOperationException("Unknown shipping provider");
        }

        public async Task<uint> GetEstimatedDeliveryDays(RequestShipmentBase request, CancellationToken ct = default)
        {
            request.Validate();

            if (providerServices.TryGetValue(request.ContractID, out var service))
            {
                return await service.GetEstimatedDeliveryDays(request, ct);
            }

            throw new InvalidOperationException("Unknown shipping provider");
        }

        public async Task<ShippingCancelResult> CancelLabel(string contractID, string cancelId, CancellationToken ct = default)
        {
            if (providerServices.TryGetValue(contractID, out var service))
            {
                return await service.CancelLabel(cancelId, ct);
            }

            throw new InvalidOperationException("Unknown shipping provider");
        }

        /// <summary>
        /// Processes an incoming provider push. DPD pushes are passed as a key/value map of the query string,
        /// GLS pushes are authorized with the Authorization header of the request by the GLS provider.
        /// </summary>
        public Task<ParcelTrackingEventResult> ProcessPushNotification(ShippingProviderType type, object notificationData, string? authorizationHeader = null, CancellationToken ct = default)
        {
            switch (type)
            {
                case ShippingProviderType.DHL:
                    var serviceDHL = GetServiceProviderFromType<DHLShipmentService>().First();
                    return serviceDHL.ProcessParcelStatusChangeNotification(
                        notificationData is DHLParcelStatusChangeNotification dhlNotification
                        ? dhlNotification
                        : throw new InvalidOperationException("Invalid notification type"), ct);

                case ShippingProviderType.DPD:
                    var serviceDPD = GetServiceProviderFromType<DPDShipmentService>().First();
                    return serviceDPD.ProcessParcelStatusChangeNotification(
                        notificationData is IReadOnlyDictionary<string, string?> dpdValues
                        ? DPDParcelStatusChangeNotification.FromKeyValues(dpdValues)
                        : throw new InvalidOperationException("Invalid notification type"), ct);

                case ShippingProviderType.GLS:
                    var serviceGLS = GetServiceProviderFromType<GLSShipmentService>().First();
                    var glsNotification = notificationData as GLSParcelStatusChangeNotification
                        ?? throw new InvalidOperationException("Invalid notification type");
                    glsNotification.AuthorizationHeader = authorizationHeader;
                    return serviceGLS.ProcessParcelStatusChangeNotification(glsNotification, ct);

                default:
                    throw new InvalidOperationException("Unknown shipping provider");
            }
        }

        /// <summary>
        /// Runs the push notification setup workflow of the provider (auth, subscribe) using the provider settings.
        /// DHL subscriptions still have to be activated via <see cref="ActivateDHLSubscriber"/>.
        /// </summary>
        public async Task InitializePushNotification(ShippingProviderType type, string hookUri, CancellationToken ct = default)
        {
            EnsureHttpsUrl(hookUri, nameof(hookUri));

            switch (type)
            {
                case ShippingProviderType.DHL:
                    await GetServiceProviderFromType<DHLShipmentService>().First().InitializePushNotification(hookUri, ct);
                    break;

                case ShippingProviderType.GLS:
                    await GetServiceProviderFromType<GLSShipmentService>().First().InitializePushNotification(hookUri, ct);
                    break;

                case ShippingProviderType.DPD:
                    // DPD pushes are configured on the DPD side, nothing to set up here.
                    break;

                default:
                    throw new InvalidOperationException("Unknown shipping provider");
            }
        }

        private static void EnsureHttpsUrl(string? url, string paramName)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Hook URL must be an absolute https URL.", paramName);
        }

        public async Task ActivateDHLSubscriber(DHLSubscriberActivatorRequest request, CancellationToken ct = default)
        {
            // Activation only uses values from the request, so a single call is enough regardless of how many DHL contracts exist.
            var serviceDHL = GetServiceProviderFromType<DHLShipmentService>().First();
            await serviceDHL.ActivateSubscriber(request, ct);
        }

        public async Task Confirm(string contractID, string confirmId, CancellationToken ct = default)
        {
            if (providerServices.TryGetValue(contractID, out var service))
            {
                await service.ConfirmShipment(confirmId, ct);
            }
            else
            {
                throw new InvalidOperationException("Unknown shipping provider");
            }
        }

        public void ResetDPDAutToken()
        {
            var dpdServices = GetServiceProviderFromType<DPDShipmentService>();
            foreach (var dpdService in dpdServices)
            {
                dpdService.ResetDPDAutToken();
            }
        }

        private T[] GetServiceProviderFromType<T>() where T : class
        {
            var service = providerServices.Where(x => x.Value.GetType() == typeof(T)).Select(x => x.Value as T).ToArray();
            if (service.Length > 0) return service;
            throw new InvalidOperationException("Service not found for the specified type.");
        }
        
        public Task<GLSParcelDetails[]> GetGLSParcelInfo(string parcelNumber) => GetServiceProviderFromType<GLSShipmentService>().First().GetParcelDetails(new[] { parcelNumber });
    }
}