using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
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
        /// Processes the request of an incoming provider push exactly as the caller received it (method, headers, query and body): checks the type,
        /// builds the notification of that provider from the request and processes it. DHL (JSON body, push token / hook secret in the headers),
        /// GLS (JSON body, Authorization header) and DPD (query string, optionally a JSON object body) are supported.
        /// </summary>
        public async Task<ParcelTrackingEventResult> ProcessPushNotification(ShippingProviderType type, HttpRequest request, CancellationToken ct = default)
        {
            switch (type)
            {
                case ShippingProviderType.DHL:
                    var serviceDHL = GetServiceProviderFromType<DHLShipmentService>().First();
                    var dhlNotification = await ReadJsonBody<DHLParcelStatusChangeNotification>(request, ct);
                    dhlNotification.RequestHeaders = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                    return await serviceDHL.ProcessParcelStatusChangeNotification(dhlNotification, ct);

                case ShippingProviderType.DPD:
                    var serviceDPD = GetServiceProviderFromType<DPDShipmentService>().First();
                    var dpdValues = request.Query.ToDictionary(q => q.Key, q => (string?)q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                    var dpdBody = await ReadBody(request, ct);
                    if (!string.IsNullOrWhiteSpace(dpdBody))
                        foreach (var pair in ParseJson<Dictionary<string, string?>>(dpdBody))
                            dpdValues[pair.Key] = pair.Value;
                    return await serviceDPD.ProcessParcelStatusChangeNotification(DPDParcelStatusChangeNotification.FromKeyValues(dpdValues), ct);

                case ShippingProviderType.GLS:
                    var serviceGLS = GetServiceProviderFromType<GLSShipmentService>().First();
                    var glsNotification = await ReadJsonBody<GLSParcelStatusChangeNotification>(request, ct);
                    glsNotification.AuthorizationHeader = request.Headers["Authorization"].ToString();
                    return await serviceGLS.ProcessParcelStatusChangeNotification(glsNotification, ct);

                default:
                    throw new InvalidOperationException("Unknown shipping provider");
            }
        }

        private static async Task<string> ReadBody(HttpRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private static async Task<T> ReadJsonBody<T>(HttpRequest request, CancellationToken ct) where T : class =>
            ParseJson<T>(await ReadBody(request, ct));

        /// <summary>
        /// A body that is not valid JSON (or does not fit the notification) is an invalid request, not an unexpected error.
        /// </summary>
        private static T ParseJson<T>(string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? throw new InvalidOperationException("Invalid notification type");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid notification type", ex);
            }
        }

        /// <summary>
        /// Runs the push notification setup workflow of the provider (auth, subscribe) using the provider settings.
        /// Returns what the provider answered (GLS returns the webhook user id to store in the secrets file; DPD an empty list).
        /// DHL parcels are subscribed when their label is created; <paramref name="trackingNumbers"/> subscribes parcels manually
        /// (DHL then calls <paramref name="hookUri"/> instead of the one in the push settings), without them DHL returns an empty list.
        /// </summary>
        public async Task<IReadOnlyList<PushSubscriptionInfo>> InitializePushNotification(ShippingProviderType type, string hookUri, string[]? trackingNumbers = null, CancellationToken ct = default)
        {
            EnsureHttpsUrl(hookUri, nameof(hookUri));

            switch (type)
            {
                case ShippingProviderType.DHL:
                    if (trackingNumbers is null || trackingNumbers.Length == 0)
                        return [];

                    return await GetServiceProviderFromType<DHLShipmentService>().First().SubscribeToPushNotification(trackingNumbers, hookUri, ct);

                case ShippingProviderType.GLS:
                    return [await GetServiceProviderFromType<GLSShipmentService>().First().SubscribeToPushNotification(hookUri, ct)];

                case ShippingProviderType.DPD:
                    // DPD pushes are configured on the DPD side, nothing to set up here.
                    return [];

                default:
                    throw new InvalidOperationException("Unknown shipping provider");
            }
        }

        private static void EnsureHttpsUrl(string? url, string paramName)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Hook URL must be an absolute https URL.", paramName);
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
            throw new InvalidOperationException($"No {typeof(T).Name} configured. Add a provider of this type to the application settings.");
        }
        
        public Task<GLSParcelDetails[]> GetGLSParcelInfo(string parcelNumber) => GetServiceProviderFromType<GLSShipmentService>().First().GetParcelDetails(new[] { parcelNumber });
    }
}