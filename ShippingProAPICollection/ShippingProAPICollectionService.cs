using Microsoft.Extensions.Caching.Memory;
using ShippingProAPICollection.Models;
using ShippingProAPICollection.Models.Entities;
using ShippingProAPICollection.Models.Error;
using ShippingProAPICollection.Provider;
using ShippingProAPICollection.Provider.DHL;
using ShippingProAPICollection.Provider.DPD;
using ShippingProAPICollection.Provider.GLS;
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

        public async Task ProcessPushNotification(IParcelStatusChangeNotification notificationData, CancellationToken ct = default)
        {
            switch (notificationData.ShippingProviderType)
            {
                case ShippingProviderType.DHL:
                    var serviceDHL = GetServiceProviderFromType<DHLShipmentService>().FirstOrDefault();
                    await serviceDHL.ProcessParcelStatusChangeNotification(notificationData, ct);
                    break;

                case ShippingProviderType.DPD:
                    var serviceDPD = GetServiceProviderFromType<DPDShipmentService>().FirstOrDefault();
                    await serviceDPD.ProcessParcelStatusChangeNotification(notificationData, ct);
                    break;

                case ShippingProviderType.GLS:
                    var serviceGLS = GetServiceProviderFromType<GLSShipmentService>().FirstOrDefault();
                    await serviceGLS.ProcessParcelStatusChangeNotification(notificationData, ct);
                    break;

                default:
                    throw new InvalidOperationException("Unknown shipping provider");
            }
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
    }
}